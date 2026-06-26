#!/usr/bin/env bash
set -euo pipefail

# Identity Service E2E Smoke Test
# Runs against docker-compose services (gateway + identity-service + postgres)

GATEWAY_URL="${GATEWAY_URL:-http://localhost:5000}"
IDENTITY_URL="${IDENTITY_URL:-http://localhost:5001}"
PASS=0
FAIL=0

log_pass() { echo "✅ PASS: $1"; PASS=$((PASS + 1)); }
log_fail() { echo "❌ FAIL: $1 — $2"; FAIL=$((FAIL + 1)); }

wait_for_service() {
    local url=$1
    local name=$2
    local max_attempts=30
    local attempt=0
    echo "Waiting for $name at $url..."
    while [ $attempt -lt $max_attempts ]; do
        if curl -sf "$url" > /dev/null 2>&1; then
            echo "$name is ready."
            return 0
        fi
        ((attempt++))
        sleep 2
    done
    echo "$name failed to start after $((max_attempts * 2))s"
    return 1
}

echo "=== Identity Service E2E Smoke Test ==="
echo ""

# 1. Health check (direct)
echo "--- Test 1: Health check (direct) ---"
wait_for_service "$IDENTITY_URL/healthz" "Identity Service"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$IDENTITY_URL/healthz")
if [ "$HTTP_CODE" = "200" ]; then
    log_pass "GET /healthz → 200"
else
    log_fail "GET /healthz" "expected 200, got $HTTP_CODE"
fi

# 2. Register a test user
echo ""
echo "--- Test 2: Register user ---"
REGISTER_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$IDENTITY_URL/api/identity/register" \
    -H "Content-Type: application/json" \
    -d '{"userName":"smoketest","email":"smoke@test.com","password":"SmokeTest123!"}')
REGISTER_BODY=$(echo "$REGISTER_RESPONSE" | head -n -1)
REGISTER_CODE=$(echo "$REGISTER_RESPONSE" | tail -n 1)
if [ "$REGISTER_CODE" = "201" ]; then
    log_pass "POST /api/identity/register → 201"
else
    log_fail "POST /api/identity/register" "expected 201, got $REGISTER_CODE — body: $REGISTER_BODY"
fi

# 3. Login
echo ""
echo "--- Test 3: Login ---"
LOGIN_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$IDENTITY_URL/api/identity/login" \
    -H "Content-Type: application/json" \
    -d '{"userName":"smoketest","password":"SmokeTest123!"}')
LOGIN_BODY=$(echo "$LOGIN_RESPONSE" | head -n -1)
LOGIN_CODE=$(echo "$LOGIN_RESPONSE" | tail -n 1)
if [ "$LOGIN_CODE" = "200" ]; then
    log_pass "POST /api/identity/login → 200"
else
    log_fail "POST /api/identity/login" "expected 200, got $LOGIN_CODE — body: $LOGIN_BODY"
fi

# Extract token
TOKEN=$(echo "$LOGIN_BODY" | grep -o '"token":"[^"]*"' | sed 's/"token":"//;s/"//' || true)
if [ -n "$TOKEN" ]; then
    log_pass "JWT token received"
else
    log_fail "JWT token" "no token in login response"
fi

# 4. GET /api/identity/users without token → 401
echo ""
echo "--- Test 4: Protected endpoint without token → 401 ---"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$IDENTITY_URL/api/identity/users")
if [ "$HTTP_CODE" = "401" ]; then
    log_pass "GET /api/identity/users (no token) → 401"
else
    log_fail "GET /api/identity/users (no token)" "expected 401, got $HTTP_CODE"
fi

# 5. GET /api/identity/users with token → 200
echo ""
echo "--- Test 5: Protected endpoint with token → 200 ---"
if [ -n "$TOKEN" ]; then
    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$IDENTITY_URL/api/identity/users" \
        -H "Authorization: Bearer $TOKEN")
    if [ "$HTTP_CODE" = "200" ]; then
        log_pass "GET /api/identity/users (with token) → 200"
    else
        log_fail "GET /api/identity/users (with token)" "expected 200, got $HTTP_CODE"
    fi
else
    log_fail "GET /api/identity/users (with token)" "skipped — no token available"
fi

# 6. Gateway tests (if gateway is running)
echo ""
echo "--- Test 6: Gateway routing ---"
GW_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/healthz" 2>/dev/null || echo "000")
if [ "$GW_HEALTH" = "200" ]; then
    echo "Gateway is reachable."
    
    # Register through gateway
    GW_REGISTER=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$GATEWAY_URL/api/identity/register" \
        -H "Content-Type: application/json" \
        -d '{"userName":"gwsmoke","email":"gwsmoke@test.com","password":"GwSmoke123!"}')
    if [ "$GW_REGISTER" = "201" ]; then
        log_pass "Gateway: POST /api/identity/register → 201"
    else
        log_fail "Gateway: POST /api/identity/register" "expected 201, got $GW_REGISTER"
    fi

    # Login through gateway
    GW_LOGIN_RESP=$(curl -s -w "\n%{http_code}" -X POST "$GATEWAY_URL/api/identity/login" \
        -H "Content-Type: application/json" \
        -d '{"userName":"gwsmoke","password":"GwSmoke123!"}')
    GW_LOGIN_BODY=$(echo "$GW_LOGIN_RESP" | head -n -1)
    GW_LOGIN_CODE=$(echo "$GW_LOGIN_RESP" | tail -n 1)
    if [ "$GW_LOGIN_CODE" = "200" ]; then
        log_pass "Gateway: POST /api/identity/login → 200"
    else
        log_fail "Gateway: POST /api/identity/login" "expected 200, got $GW_LOGIN_CODE"
    fi

    # Protected without token through gateway
    GW_NOAUTH=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/api/identity/users")
    if [ "$GW_NOAUTH" = "401" ]; then
        log_pass "Gateway: GET /api/identity/users (no token) → 401"
    else
        log_fail "Gateway: GET /api/identity/users (no token)" "expected 401, got $GW_NOAUTH"
    fi

    # Protected with token through gateway
    GW_TOKEN=$(echo "$GW_LOGIN_BODY" | grep -o '"token":"[^"]*"' | sed 's/"token":"//;s/"//' || true)
    if [ -n "$GW_TOKEN" ]; then
        GW_AUTH=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/api/identity/users" \
            -H "Authorization: Bearer $GW_TOKEN")
        if [ "$GW_AUTH" = "200" ]; then
            log_pass "Gateway: GET /api/identity/users (with token) → 200"
        else
            log_fail "Gateway: GET /api/identity/users (with token)" "expected 200, got $GW_AUTH"
        fi
    else
        log_fail "Gateway: GET /api/identity/users (with token)" "skipped — no gateway token"
    fi
else
    echo "Gateway not reachable at $GATEWAY_URL — skipping gateway tests."
fi

echo ""
echo "=== Results: $PASS passed, $FAIL failed ==="

if [ "$FAIL" -gt 0 ]; then
    exit 1
fi
