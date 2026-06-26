#!/bin/bash
set -e

GATEWAY_URL="${GATEWAY_URL:-http://localhost:5000}"
SERVICE_URL="${SERVICE_URL:-http://localhost:5004}"
JWT_SECRET="QuickApp_Microservices_SuperSecret_Key_For_Dev_Only_Min_32_Chars!"
PASS=0
FAIL=0

# Generate a JWT token
generate_jwt() {
    local header='{"alg":"HS256","typ":"JWT"}'
    local payload="{\"sub\":\"test-user\",\"iss\":\"quickapp-identity\",\"aud\":\"quickapp-api\",\"exp\":$(($(date +%s) + 3600))}"

    local header_b64=$(echo -n "$header" | base64 -w0 | tr '+/' '-_' | tr -d '=')
    local payload_b64=$(echo -n "$payload" | base64 -w0 | tr '+/' '-_' | tr -d '=')
    local signature=$(echo -n "${header_b64}.${payload_b64}" | openssl dgst -sha256 -hmac "$JWT_SECRET" -binary | base64 -w0 | tr '+/' '-_' | tr -d '=')

    echo "${header_b64}.${payload_b64}.${signature}"
}

assert_status() {
    local description="$1"
    local expected="$2"
    local actual="$3"

    if [ "$actual" = "$expected" ]; then
        echo "PASS: $description (HTTP $actual)"
        PASS=$((PASS + 1))
    else
        echo "FAIL: $description — expected $expected, got $actual"
        FAIL=$((FAIL + 1))
    fi
}

TOKEN=$(generate_jwt)
echo "Generated JWT token"
echo "Testing against gateway: $GATEWAY_URL"
echo "Testing against service: $SERVICE_URL"
echo "---"

# Test 1: Health check (direct to service — public)
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$SERVICE_URL/healthz")
assert_status "GET /healthz (direct) → 200" "200" "$STATUS"

# Test 2: GET /api/product without token → 401 (direct)
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$SERVICE_URL/api/product")
assert_status "GET /api/product without token (direct) → 401" "401" "$STATUS"

# Test 3: GET /api/products without token → 401 (through gateway)
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/api/products")
assert_status "GET /api/products without token (gateway) → 401" "401" "$STATUS"

# Test 4: Create a category with valid JWT → 201
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$GATEWAY_URL/api/products/categories" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"name":"Electronics","description":"Electronic devices","icon":"electronics-icon"}')
STATUS=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | sed '$d')
assert_status "POST /api/products/categories with JWT → 201" "201" "$STATUS"

# Extract category ID
CATEGORY_ID=$(echo "$BODY" | grep -o '"id":[0-9]*' | head -1 | cut -d: -f2)
echo "Created category ID: $CATEGORY_ID"

# Test 5: Create a product with valid JWT → 201
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$GATEWAY_URL/api/products" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{\"name\":\"Test Product\",\"description\":\"A test product\",\"icon\":\"test-icon\",\"buyingPrice\":10.50,\"sellingPrice\":15.99,\"unitsInStock\":100,\"isActive\":true,\"isDiscontinued\":false,\"parentId\":null,\"productCategoryId\":$CATEGORY_ID}")
STATUS=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | sed '$d')
assert_status "POST /api/products with JWT → 201" "201" "$STATUS"

# Extract product ID
PRODUCT_ID=$(echo "$BODY" | grep -o '"id":[0-9]*' | head -1 | cut -d: -f2)
echo "Created product ID: $PRODUCT_ID"

# Test 6: GET /api/products with valid JWT → 200 (list includes product)
RESPONSE=$(curl -s -w "\n%{http_code}" "$GATEWAY_URL/api/products" \
    -H "Authorization: Bearer $TOKEN")
STATUS=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | sed '$d')
assert_status "GET /api/products with JWT → 200" "200" "$STATUS"

# Verify the product is in the list
if echo "$BODY" | grep -q "Test Product"; then
    echo "PASS: Response contains 'Test Product'"
    PASS=$((PASS + 1))
else
    echo "FAIL: Response does not contain 'Test Product'"
    FAIL=$((FAIL + 1))
fi

# Test 7: GET /api/products/{id} with valid JWT → 200
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/api/products/$PRODUCT_ID" \
    -H "Authorization: Bearer $TOKEN")
assert_status "GET /api/products/$PRODUCT_ID with JWT → 200" "200" "$STATUS"

echo "---"
echo "Results: $PASS passed, $FAIL failed"

if [ $FAIL -gt 0 ]; then
    exit 1
fi
echo "All smoke tests passed!"
