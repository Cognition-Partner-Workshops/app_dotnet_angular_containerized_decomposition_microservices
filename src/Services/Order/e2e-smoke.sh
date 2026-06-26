#!/usr/bin/env bash
#
# E2E smoke test for the Order service, exercised THROUGH the API gateway.
# Asserts:
#   1. GET /api/orders/healthz  -> 200  (service is up, health route reachable)
#   2. GET /api/orders (no JWT)  -> 401  (every endpoint is [Authorize]-protected)
#
# Exits non-zero on any failure so it can gate CI / compose-based verification.
#
# Usage:
#   ./e2e-smoke.sh                       # uses http://localhost:5000
#   GATEWAY_URL=http://host:5000 ./e2e-smoke.sh
set -euo pipefail

GATEWAY_URL="${GATEWAY_URL:-http://localhost:5000}"
fail=0

check() {
  local desc="$1" url="$2" expected="$3"
  local actual
  actual="$(curl -s -o /dev/null -w '%{http_code}' "$url")"
  if [[ "$actual" == "$expected" ]]; then
    echo "PASS: $desc -> $actual (expected $expected)"
  else
    echo "FAIL: $desc -> $actual (expected $expected)  [$url]"
    fail=1
  fi
}

echo "Order service E2E smoke (gateway: $GATEWAY_URL)"
check "health through gateway"      "$GATEWAY_URL/api/orders/healthz" 200
check "unauthenticated list -> 401" "$GATEWAY_URL/api/orders"         401

if [[ "$fail" -ne 0 ]]; then
  echo "E2E smoke FAILED"
  exit 1
fi

echo "E2E smoke PASSED"
