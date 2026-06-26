#!/usr/bin/env bash
#
# Full-stack cross-service E2E smoke test for QuickApp microservices.
#
# Brings up the entire docker compose stack (postgres, rabbitmq, the 5 services
# and the YARP API gateway), waits for readiness, then asserts end-to-end that:
#   * every service answers health 200 through the gateway
#   * every protected resource returns 401 without a token through the gateway
#   * Identity issues a JWT via the password grant through the gateway
#   * that JWT is accepted (200) by Customer, Product and Order through the gateway
#   * a tampered/invalid token is rejected (401)
#
# The stack is always torn down on exit. The script exits non-zero on any failure
# and is safe to re-run (idempotent: it tears down any prior stack first).
set -euo pipefail

# --- configuration ----------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
COMPOSE_FILE="${REPO_ROOT}/src/docker-compose.yml"
GATEWAY="${GATEWAY_URL:-http://localhost:5000}"

# Demo credentials (dev-only; see canonical JWT contract).
LOGIN_USER="${LOGIN_USER:-admin@quickapp.local}"
LOGIN_PASS="${LOGIN_PASS:-Pa\$\$w0rd!}"

# Services exposed through the gateway: <route-prefix>
SERVICES=(identity customers orders products notifications)
# Services that must accept the cross-service JWT.
AUTHED_SERVICES=(customers products orders)

READY_TIMEOUT="${READY_TIMEOUT:-180}"

# --- helpers ----------------------------------------------------------------
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
FAILURES=0

log()  { printf '%b\n' "$*"; }
pass() { log "${GREEN}PASS${NC} $*"; }
fail() { log "${RED}FAIL${NC} $*"; FAILURES=$((FAILURES + 1)); }
info() { log "${YELLOW}==>${NC} $*"; }

compose() { docker compose -f "${COMPOSE_FILE}" "$@"; }

http_code() {
  # http_code <url> [auth-header-value]
  local url="$1"; shift || true
  if [[ $# -gt 0 && -n "$1" ]]; then
    curl -s -o /dev/null -w '%{http_code}' -H "Authorization: $1" "${url}"
  else
    curl -s -o /dev/null -w '%{http_code}' "${url}"
  fi
}

assert_code() {
  # assert_code <description> <expected> <actual>
  local desc="$1" expected="$2" actual="$3"
  if [[ "${actual}" == "${expected}" ]]; then
    pass "${desc} (${actual})"
  else
    fail "${desc} — expected ${expected}, got ${actual}"
  fi
}

cleanup() {
  info "Tearing down stack"
  compose down -v --remove-orphans >/dev/null 2>&1 || true
}
trap cleanup EXIT

# --- bring up ---------------------------------------------------------------
info "Tearing down any existing stack (idempotency)"
compose down -v --remove-orphans >/dev/null 2>&1 || true

info "Building and starting full stack"
compose up --build -d

info "Waiting for gateway + all services to be ready (timeout ${READY_TIMEOUT}s)"
deadline=$(( $(date +%s) + READY_TIMEOUT ))
until [[ "$(http_code "${GATEWAY}/api/identity/healthz")" == "200" ]]; do
  if [[ $(date +%s) -ge ${deadline} ]]; then
    fail "Gateway did not become ready within ${READY_TIMEOUT}s"
    compose ps
    compose logs --tail=50 || true
    exit 1
  fi
  sleep 3
done
# Give the remaining services a brief moment to finish migrations/startup.
for s in "${SERVICES[@]}"; do
  until [[ "$(http_code "${GATEWAY}/api/${s}/healthz")" == "200" ]]; do
    if [[ $(date +%s) -ge ${deadline} ]]; then break; fi
    sleep 2
  done
done

# --- assertions: health 200 -------------------------------------------------
info "Asserting per-service health (200) through the gateway"
for s in "${SERVICES[@]}"; do
  assert_code "health ${s}" 200 "$(http_code "${GATEWAY}/api/${s}/healthz")"
done

# --- assertions: unauth 401 -------------------------------------------------
info "Asserting per-service unauthenticated access (401) through the gateway"
for s in "${SERVICES[@]}"; do
  assert_code "unauth ${s}" 401 "$(http_code "${GATEWAY}/api/${s}")"
done

# --- login: obtain JWT ------------------------------------------------------
info "Logging in via Identity (password grant) through the gateway"
LOGIN_RESPONSE="$(curl -s -w '\n%{http_code}' -X POST "${GATEWAY}/api/identity/connect/token" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode 'grant_type=password' \
  --data-urlencode "username=${LOGIN_USER}" \
  --data-urlencode "password=${LOGIN_PASS}")"
LOGIN_STATUS="$(printf '%s' "${LOGIN_RESPONSE}" | tail -n1)"
LOGIN_BODY="$(printf '%s' "${LOGIN_RESPONSE}" | sed '$d')"
assert_code "identity login" 200 "${LOGIN_STATUS}"

TOKEN="$(printf '%s' "${LOGIN_BODY}" | sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p')"
if [[ -n "${TOKEN}" ]]; then
  pass "received access_token"
else
  fail "no access_token in login response: ${LOGIN_BODY}"
fi

# --- assertions: cross-service authed 200 -----------------------------------
info "Asserting cross-service authenticated access (200) through the gateway"
for s in "${AUTHED_SERVICES[@]}"; do
  assert_code "authed ${s}" 200 "$(http_code "${GATEWAY}/api/${s}" "Bearer ${TOKEN}")"
done

# --- assertions: invalid token 401 ------------------------------------------
info "Asserting tampered/invalid token rejected (401) through the gateway"
INVALID_TOKEN="${TOKEN%?}X"   # corrupt the signature
[[ "${INVALID_TOKEN}" == "${TOKEN}" || -z "${TOKEN}" ]] && INVALID_TOKEN="not.a.real.token"
for s in "${AUTHED_SERVICES[@]}"; do
  assert_code "invalid-token ${s}" 401 "$(http_code "${GATEWAY}/api/${s}" "Bearer ${INVALID_TOKEN}")"
done

# --- result -----------------------------------------------------------------
echo
if [[ ${FAILURES} -eq 0 ]]; then
  log "${GREEN}E2E SMOKE PASSED${NC} — all assertions succeeded."
  exit 0
else
  log "${RED}E2E SMOKE FAILED${NC} — ${FAILURES} assertion(s) failed."
  compose ps
  compose logs --tail=80 || true
  exit 1
fi
