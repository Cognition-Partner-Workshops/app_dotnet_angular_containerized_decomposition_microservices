#!/usr/bin/env bash
set -euo pipefail

# ─── Configuration ───────────────────────────────────────────────────────────
DIRECT_URL="${DIRECT_URL:-http://localhost:5005}"
GATEWAY_URL="${GATEWAY_URL:-http://localhost:5000}"
FAILURES=0

# ─── Colors ──────────────────────────────────────────────────────────────────
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

pass() { echo -e "${GREEN}[PASS]${NC} $1"; }
fail() { echo -e "${RED}[FAIL]${NC} $1"; FAILURES=$((FAILURES + 1)); }

# ─── Generate JWT ────────────────────────────────────────────────────────────
JWT_SECRET='QuickApp_Microservices_SuperSecret_Key_For_Dev_Only_Min_32_Chars!'

jwt_header=$(echo -n '{"alg":"HS256","typ":"JWT"}' | base64 -w0 | tr '+/' '-_' | tr -d '=')
jwt_payload=$(echo -n '{"iss":"quickapp-identity","aud":"quickapp-api","sub":"test-user","exp":'$(date -d "+1 hour" +%s)'}' | base64 -w0 | tr '+/' '-_' | tr -d '=')
jwt_signature=$(echo -n "${jwt_header}.${jwt_payload}" | openssl dgst -sha256 -hmac "${JWT_SECRET}" -binary | base64 -w0 | tr '+/' '-_' | tr -d '=')
TOKEN="${jwt_header}.${jwt_payload}.${jwt_signature}"

echo "======================================"
echo " Notification Service E2E Smoke Tests"
echo "======================================"
echo ""
echo "Direct URL:  ${DIRECT_URL}"
echo "Gateway URL: ${GATEWAY_URL}"
echo ""

# ─── Test 1: Health check ────────────────────────────────────────────────────
echo "--- Test 1: GET /healthz (expect 200) ---"
HTTP_CODE=$(curl -s -o /tmp/response_body -w '%{http_code}' "${DIRECT_URL}/healthz")
if [ "$HTTP_CODE" = "200" ]; then
  pass "GET /healthz returned ${HTTP_CODE}"
else
  fail "GET /healthz returned ${HTTP_CODE} (expected 200)"
fi

# ─── Test 2: Unauthenticated GET /api/notification ───────────────────────────
echo "--- Test 2: GET /api/notification without auth (expect 401) ---"
HTTP_CODE=$(curl -s -o /tmp/response_body -w '%{http_code}' "${DIRECT_URL}/api/notification")
if [ "$HTTP_CODE" = "401" ]; then
  pass "GET /api/notification without auth returned ${HTTP_CODE}"
else
  fail "GET /api/notification without auth returned ${HTTP_CODE} (expected 401)"
fi

# ─── Test 3: POST /api/notification/events/order-placed ──────────────────────
echo "--- Test 3: POST /api/notification/events/order-placed with JWT (expect 201) ---"
HTTP_CODE=$(curl -s -o /tmp/response_body -w '%{http_code}' \
  -X POST \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "11111111-1111-1111-1111-111111111111",
    "customerId": "22222222-2222-2222-2222-222222222222",
    "totalAmount": 9999,
    "placedAt": "2025-01-15T10:30:00Z"
  }' \
  "${DIRECT_URL}/api/notification/events/order-placed")

if [ "$HTTP_CODE" = "201" ]; then
  pass "POST /api/notification/events/order-placed returned ${HTTP_CODE}"
else
  fail "POST /api/notification/events/order-placed returned ${HTTP_CODE} (expected 201)"
fi

# Extract the notification ID from the response
NOTIFICATION_ID=$(jq -r '.id' /tmp/response_body 2>/dev/null || echo "")
if [ -z "$NOTIFICATION_ID" ] || [ "$NOTIFICATION_ID" = "null" ]; then
  fail "Could not extract notification ID from POST response"
  echo "  Response body: $(cat /tmp/response_body)"
fi

# ─── Test 4: GET /api/notification with JWT ──────────────────────────────────
echo "--- Test 4: GET /api/notification with JWT (expect 200, contains notification) ---"
HTTP_CODE=$(curl -s -o /tmp/response_body -w '%{http_code}' \
  -H "Authorization: Bearer ${TOKEN}" \
  "${DIRECT_URL}/api/notification")

if [ "$HTTP_CODE" = "200" ]; then
  # Check that the response contains our notification ID
  if echo "$(cat /tmp/response_body)" | jq -e ".[] | select(.id == \"${NOTIFICATION_ID}\")" > /dev/null 2>&1; then
    pass "GET /api/notification returned ${HTTP_CODE} and contains created notification"
  elif [ -z "$NOTIFICATION_ID" ] || [ "$NOTIFICATION_ID" = "null" ]; then
    pass "GET /api/notification returned ${HTTP_CODE} (skipped body check — no ID from POST)"
  else
    fail "GET /api/notification returned ${HTTP_CODE} but response does not contain notification ${NOTIFICATION_ID}"
    echo "  Response body: $(cat /tmp/response_body | head -c 500)"
  fi
else
  fail "GET /api/notification returned ${HTTP_CODE} (expected 200)"
fi

# ─── Test 5: GET /api/notification/{id}/preview ──────────────────────────────
echo "--- Test 5: GET /api/notification/{id}/preview with JWT (expect 200, HTML) ---"
if [ -n "$NOTIFICATION_ID" ] && [ "$NOTIFICATION_ID" != "null" ]; then
  HTTP_CODE=$(curl -s -o /tmp/response_body -w '%{http_code}' \
    -H "Authorization: Bearer ${TOKEN}" \
    "${DIRECT_URL}/api/notification/${NOTIFICATION_ID}/preview")

  if [ "$HTTP_CODE" = "200" ]; then
    if grep -qi "<html\|<div\|<body\|<head\|<p\|<table" /tmp/response_body; then
      pass "GET /api/notification/${NOTIFICATION_ID}/preview returned ${HTTP_CODE} with HTML content"
    else
      fail "GET /api/notification/${NOTIFICATION_ID}/preview returned ${HTTP_CODE} but no HTML detected"
      echo "  Response body: $(cat /tmp/response_body | head -c 500)"
    fi
  else
    fail "GET /api/notification/${NOTIFICATION_ID}/preview returned ${HTTP_CODE} (expected 200)"
  fi
else
  fail "Skipping preview test — no notification ID available"
fi

# ─── Test 6: Gateway route verification ──────────────────────────────────────
echo "--- Test 6: GET gateway /api/notifications/api/notification without token (expect 401) ---"
HTTP_CODE=$(curl -s -o /tmp/response_body -w '%{http_code}' \
  "${GATEWAY_URL}/api/notifications/api/notification")

if [ "$HTTP_CODE" = "401" ]; then
  pass "Gateway route returned ${HTTP_CODE} (routes to service, auth enforced)"
else
  fail "Gateway route returned ${HTTP_CODE} (expected 401)"
fi

# ─── Summary ─────────────────────────────────────────────────────────────────
echo ""
echo "======================================"
if [ "$FAILURES" -eq 0 ]; then
  echo -e "${GREEN}All tests passed!${NC}"
  exit 0
else
  echo -e "${RED}${FAILURES} test(s) failed.${NC}"
  exit 1
fi
