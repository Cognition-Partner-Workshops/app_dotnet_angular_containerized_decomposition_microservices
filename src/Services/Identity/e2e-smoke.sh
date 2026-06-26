#!/usr/bin/env bash
#
# E2E smoke test for the Identity microservice, asserted THROUGH the API gateway.
#
# Prerequisites: bring up postgres, identity-service and api-gateway, e.g.:
#   docker compose -f src/docker-compose.yml up --build -d postgres identity-service api-gateway
#
# Then run from the repo root (or anywhere):
#   src/Services/Identity/e2e-smoke.sh
#
# Asserts:
#   1. GET  /api/identity/healthz            -> 200
#   2. GET  /api/identity (no token)         -> 401
#   3. POST /api/identity/connect/token      -> 200 + access_token
#   4. GET  /api/identity (Bearer token)     -> 200
#
# Exits non-zero on the first failed assertion.

set -euo pipefail

GATEWAY="${GATEWAY:-http://localhost:5000}"
ADMIN_USER="${ADMIN_USER:-admin@quickapp.local}"
ADMIN_PASS="${ADMIN_PASS:-Pa\$\$w0rd!}"

fail() { echo "FAIL: $1" >&2; exit 1; }
pass() { echo "PASS: $1"; }

echo "==> Waiting for gateway health at ${GATEWAY}/api/identity/healthz"
for i in $(seq 1 60); do
  code="$(curl -s -o /dev/null -w '%{http_code}' "${GATEWAY}/api/identity/healthz" || true)"
  [ "$code" = "200" ] && break
  sleep 2
done

# 1. Health
code="$(curl -s -o /dev/null -w '%{http_code}' "${GATEWAY}/api/identity/healthz")"
[ "$code" = "200" ] || fail "health expected 200, got $code"
pass "health 200"

# 2. Protected endpoint without a token -> 401
code="$(curl -s -o /dev/null -w '%{http_code}' "${GATEWAY}/api/identity")"
[ "$code" = "401" ] || fail "unauthenticated protected endpoint expected 401, got $code"
pass "unauthenticated 401"

# 3. Obtain a token via password grant
token_resp="$(curl -s -X POST "${GATEWAY}/api/identity/connect/token" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode "grant_type=password" \
  --data-urlencode "username=${ADMIN_USER}" \
  --data-urlencode "password=${ADMIN_PASS}")"

access_token="$(printf '%s' "$token_resp" | sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p')"
[ -n "$access_token" ] || fail "no access_token in token response: $token_resp"
pass "token issued"

# 4. Protected endpoint with the token -> 200
code="$(curl -s -o /dev/null -w '%{http_code}' "${GATEWAY}/api/identity" \
  -H "Authorization: Bearer ${access_token}")"
[ "$code" = "200" ] || fail "authenticated protected endpoint expected 200, got $code"
pass "authenticated 200"

echo "==> All Identity gateway E2E assertions passed."
