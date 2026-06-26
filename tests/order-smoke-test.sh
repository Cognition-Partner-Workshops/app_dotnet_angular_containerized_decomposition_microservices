#!/bin/bash
# Order Service E2E Smoke Test
# Usage: ./tests/order-smoke-test.sh [gateway_url] [service_url]
# Defaults: gateway=http://localhost:5000, service=http://localhost:5003

set -e

GATEWAY_URL=${1:-http://localhost:5000}
SERVICE_URL=${2:-http://localhost:5003}
PASSED=0
FAILED=0

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

# Generate a valid JWT token for testing
generate_jwt() {
    local header=$(echo -n '{"alg":"HS256","typ":"JWT"}' | base64 -w0 | tr '+/' '-_' | tr -d '=')
    local now=$(date +%s)
    local exp=$((now + 3600))
    local payload=$(echo -n "{\"sub\":\"test-user\",\"iss\":\"quickapp-identity\",\"aud\":\"quickapp-api\",\"exp\":${exp},\"iat\":${now}}" | base64 -w0 | tr '+/' '-_' | tr -d '=')
    local secret="QuickApp_Microservices_SuperSecret_Key_For_Dev_Only_Min_32_Chars!"
    local signature=$(echo -n "${header}.${payload}" | openssl dgst -sha256 -hmac "${secret}" -binary | base64 -w0 | tr '+/' '-_' | tr -d '=')
    echo "${header}.${payload}.${signature}"
}

assert_status() {
    local test_name=$1
    local expected=$2
    local actual=$3
    if [ "$actual" -eq "$expected" ]; then
        echo -e "${GREEN}PASS${NC}: $test_name (HTTP $actual)"
        PASSED=$((PASSED + 1))
    else
        echo -e "${RED}FAIL${NC}: $test_name (expected $expected, got $actual)"
        FAILED=$((FAILED + 1))
    fi
}

echo "=== Order Service Smoke Tests ==="
echo "Gateway: $GATEWAY_URL"
echo "Service: $SERVICE_URL"
echo ""

TOKEN=$(generate_jwt)

# Test 1: Health check (direct)
echo "--- Direct Service Tests ---"
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$SERVICE_URL/healthz")
assert_status "GET /healthz → 200" 200 "$STATUS"

# Test 2: Unauthenticated GET → 401 (direct)
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$SERVICE_URL/api/order")
assert_status "GET /api/order (no token) → 401" 401 "$STATUS"

# Test 3: Authenticated POST → 201 (direct)
STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$SERVICE_URL/api/order" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"discount": 5.00, "comments": "smoke test", "cashierId": "user1", "customerId": 1, "orderDetails": [{"unitPrice": 19.99, "quantity": 2, "discount": 0, "productId": 101}]}')
assert_status "POST /api/order (with token) → 201" 201 "$STATUS"

# Test 4: Authenticated GET → 200 (direct)
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$SERVICE_URL/api/order" \
    -H "Authorization: Bearer $TOKEN")
assert_status "GET /api/order (with token) → 200" 200 "$STATUS"

# Test 5: Gateway health check
echo ""
echo "--- Gateway Route Tests ---"
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/api/orders/healthz")
assert_status "GET /api/orders/healthz via gateway → 200" 200 "$STATUS"

# Test 6: Gateway unauthenticated → 401
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/api/orders/api/order")
assert_status "GET /api/orders/api/order (no token) via gateway → 401" 401 "$STATUS"

# Test 7: Gateway authenticated → 200
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/api/orders/api/order" \
    -H "Authorization: Bearer $TOKEN")
assert_status "GET /api/orders/api/order (with token) via gateway → 200" 200 "$STATUS"

echo ""
echo "=== Results: $PASSED passed, $FAILED failed ==="

if [ $FAILED -gt 0 ]; then
    exit 1
fi
