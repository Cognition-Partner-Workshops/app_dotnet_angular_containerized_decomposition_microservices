#!/usr/bin/env bash
#
# E2E smoke test for the Notification service through the API gateway.
#
# Verifies (through http://localhost:5000, the gateway, which strips the
# /api/notifications prefix and forwards to notification-service:5005):
#   1. GET /api/notifications/healthz          -> 200 (service reachable & healthy)
#   2. GET /api/notifications (no JWT)         -> 401 (read endpoints are protected)
#
# Exits non-zero on any failure.
#
# Usage:
#   src/Services/Notification/e2e-smoke.sh            # uses default GATEWAY_URL
#   GATEWAY_URL=http://localhost:5000 ./e2e-smoke.sh
#
set -euo pipefail

GATEWAY_URL="${GATEWAY_URL:-http://localhost:5000}"
fail=0

check() {
  local desc="$1" expected="$2" url="$3"
  local actual
  actual="$(curl -s -o /dev/null -w '%{http_code}' "$url" || echo "000")"
  if [[ "$actual" == "$expected" ]]; then
    echo "PASS: $desc ($url -> $actual)"
  else
    echo "FAIL: $desc ($url -> expected $expected, got $actual)"
    fail=1
  fi
}

echo "== Notification E2E smoke (gateway: $GATEWAY_URL) =="
check "health endpoint returns 200" 200 "$GATEWAY_URL/api/notifications/healthz"
check "list endpoint without token returns 401" 401 "$GATEWAY_URL/api/notifications"

if [[ "$fail" -ne 0 ]]; then
  echo "SMOKE TEST FAILED"
  exit 1
fi

echo "SMOKE TEST PASSED"
