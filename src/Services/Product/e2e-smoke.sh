#!/usr/bin/env bash
#
# E2E smoke test for the Product microservice, exercised THROUGH the API Gateway.
#
# Asserts:
#   1. GET /api/products/healthz  -> 200 (health is open, no token required)
#   2. GET /api/products          -> 401 (every resource endpoint requires a JWT)
#
# Exits non-zero on the first failed assertion.
#
# Prereq: bring up only the services this gate needs, e.g.
#   docker compose -f src/docker-compose.yml up -d --build postgres product-service api-gateway
#
set -euo pipefail

GATEWAY_URL="${GATEWAY_URL:-http://localhost:5000}"
HEALTH_URL="${GATEWAY_URL}/api/products/healthz"
PRODUCTS_URL="${GATEWAY_URL}/api/products"
MAX_WAIT="${MAX_WAIT:-90}"

fail() { echo "FAIL: $*" >&2; exit 1; }

http_status() {
  curl -s -o /dev/null -w '%{http_code}' --max-time 10 "$1"
}

echo "==> Waiting for gateway health at ${HEALTH_URL} (up to ${MAX_WAIT}s)..."
deadline=$(( $(date +%s) + MAX_WAIT ))
status=000
while [ "$(date +%s)" -lt "$deadline" ]; do
  status="$(http_status "${HEALTH_URL}" || echo 000)"
  if [ "$status" = "200" ]; then
    break
  fi
  sleep 3
done

echo "==> [1/2] Health check (expect 200): ${HEALTH_URL}"
[ "$status" = "200" ] || fail "health check returned ${status}, expected 200"
echo "    OK (200)"

echo "==> [2/2] Unauthenticated products (expect 401): ${PRODUCTS_URL}"
unauth_status="$(http_status "${PRODUCTS_URL}" || echo 000)"
[ "$unauth_status" = "401" ] || fail "unauthenticated GET ${PRODUCTS_URL} returned ${unauth_status}, expected 401"
echo "    OK (401)"

echo "==> Product service E2E smoke PASSED"
