#!/usr/bin/env bash
# E2E smoke test for the Customer service, exercised THROUGH the API gateway.
# Asserts:
#   1. GET /api/customers/healthz            -> 200 (service is up via gateway)
#   2. GET /api/customers (no Authorization) -> 401 (every endpoint is [Authorize])
# Exits non-zero on any failure.
set -euo pipefail

GATEWAY_URL="${GATEWAY_URL:-http://localhost:5000}"
HEALTH_PATH="/api/customers/healthz"
RESOURCE_PATH="/api/customers"

fail() { echo "SMOKE FAIL: $*" >&2; exit 1; }

echo "== Customer service E2E smoke (via gateway ${GATEWAY_URL}) =="

echo "-- [1/2] health check: GET ${HEALTH_PATH} (expect 200)"
health_code="$(curl -s -o /dev/null -w '%{http_code}' "${GATEWAY_URL}${HEALTH_PATH}")"
[ "${health_code}" = "200" ] || fail "health expected 200, got ${health_code}"
echo "   OK: ${health_code}"

echo "-- [2/2] unauthenticated access: GET ${RESOURCE_PATH} (expect 401)"
unauth_code="$(curl -s -o /dev/null -w '%{http_code}' "${GATEWAY_URL}${RESOURCE_PATH}")"
[ "${unauth_code}" = "401" ] || fail "unauth expected 401, got ${unauth_code}"
echo "   OK: ${unauth_code}"

echo "== SMOKE PASS: health 200 + unauth 401 =="
