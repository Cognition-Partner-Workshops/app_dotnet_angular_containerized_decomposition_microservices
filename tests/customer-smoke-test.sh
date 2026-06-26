#!/bin/bash
set -e

GATEWAY_URL=${GATEWAY_URL:-http://localhost:5000}
CUSTOMER_URL=${CUSTOMER_URL:-http://localhost:5002}
FAILED=0

echo "=== Customer Microservice Smoke Test ==="

# Wait for services
echo "Waiting for customer service..."
for i in $(seq 1 30); do
    if curl -sf "$CUSTOMER_URL/healthz" > /dev/null 2>&1; then
        echo "Customer service is ready"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "FAIL: Customer service not ready after 30s"
        exit 1
    fi
    sleep 1
done

echo "Waiting for gateway..."
for i in $(seq 1 30); do
    if curl -sf "$GATEWAY_URL/healthz" > /dev/null 2>&1; then
        echo "Gateway is ready"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "FAIL: Gateway not ready after 30s"
        exit 1
    fi
    sleep 1
done

# Test 1: Health check
echo ""
echo "--- Test 1: GET /healthz → 200 ---"
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$CUSTOMER_URL/healthz")
if [ "$STATUS" = "200" ]; then
    echo "PASS: healthz returned $STATUS"
else
    echo "FAIL: healthz returned $STATUS (expected 200)"
    FAILED=1
fi

# Test 2: Unauthenticated → 401
echo ""
echo "--- Test 2: GET /api/customers (no token) → 401 ---"
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/api/customers")
if [ "$STATUS" = "401" ]; then
    echo "PASS: unauthenticated returned $STATUS"
else
    echo "FAIL: unauthenticated returned $STATUS (expected 401)"
    FAILED=1
fi

# Generate JWT token
echo ""
echo "--- Generating JWT token ---"
pip install PyJWT > /dev/null 2>&1 || true
TOKEN=$(python3 -c "
import jwt, datetime
token = jwt.encode({
    'sub': 'test-user',
    'iss': 'quickapp-identity',
    'aud': 'quickapp-api',
    'exp': datetime.datetime.utcnow() + datetime.timedelta(hours=1)
}, 'QuickApp_Microservices_SuperSecret_Key_For_Dev_Only_Min_32_Chars!', algorithm='HS256')
print(token)
")
echo "Token generated successfully"

# Test 3: POST /api/customers with JWT → 201
echo ""
echo "--- Test 3: POST /api/customers (with token) → 201 ---"
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$GATEWAY_URL/api/customers" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"name": "Smoke Test", "email": "smoke@test.com", "gender": "Female", "city": "Test City"}')
STATUS=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | sed '$d')
if [ "$STATUS" = "201" ]; then
    echo "PASS: create returned $STATUS"
    echo "Response: $BODY"
else
    echo "FAIL: create returned $STATUS (expected 201)"
    echo "Response: $BODY"
    FAILED=1
fi

# Test 4: GET /api/customers with JWT → 200 + verify
echo ""
echo "--- Test 4: GET /api/customers (with token) → 200 ---"
RESPONSE=$(curl -s -w "\n%{http_code}" "$GATEWAY_URL/api/customers" \
    -H "Authorization: Bearer $TOKEN")
STATUS=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | sed '$d')
if [ "$STATUS" = "200" ]; then
    echo "PASS: list returned $STATUS"
    if echo "$BODY" | grep -q "Smoke Test"; then
        echo "PASS: created customer found in list"
    else
        echo "FAIL: created customer NOT found in list"
        echo "Response: $BODY"
        FAILED=1
    fi
else
    echo "FAIL: list returned $STATUS (expected 200)"
    echo "Response: $BODY"
    FAILED=1
fi

# Summary
echo ""
if [ $FAILED -eq 0 ]; then
    echo "=== ALL SMOKE TESTS PASSED ==="
    exit 0
else
    echo "=== SOME SMOKE TESTS FAILED ==="
    exit 1
fi
