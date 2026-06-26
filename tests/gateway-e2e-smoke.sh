#!/usr/bin/env bash
set -euo pipefail

###############################################################################
# Gateway E2E Integration Smoke Test
#
# Validates that ALL services are reachable through the YARP reverse proxy,
# health checks respond, and (when implemented) JWT auth is enforced.
#
# Usage:
#   ./tests/gateway-e2e-smoke.sh              # from repo root
#   SKIP_COMPOSE=1 ./tests/gateway-e2e-smoke.sh  # skip docker compose up/down
###############################################################################

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
COMPOSE_FILE="$REPO_ROOT/src/docker-compose.yml"
GATEWAY_URL="http://localhost:5000"

PASS=0
FAIL=0
SKIP=0

# ---------- helpers ----------------------------------------------------------

red()   { printf '\033[1;31m%s\033[0m\n' "$*"; }
green() { printf '\033[1;32m%s\033[0m\n' "$*"; }
yellow(){ printf '\033[1;33m%s\033[0m\n' "$*"; }
bold()  { printf '\033[1m%s\033[0m\n' "$*"; }

assert_status() {
  local description="$1" url="$2" expected="$3"
  shift 3
  local extra_args=("$@")

  local status
  status=$(curl -s -o /dev/null -w '%{http_code}' "${extra_args[@]}" "$url") || status="000"

  if [[ "$status" == "$expected" ]]; then
    green "  PASS  $description  (got $status)"
    PASS=$((PASS + 1))
  else
    red   "  FAIL  $description  (expected $expected, got $status)"
    FAIL=$((FAIL + 1))
  fi
}

assert_status_any_of() {
  local description="$1" url="$2"
  shift 2
  local expected_list=()
  while [[ "$1" != "--" ]]; do
    expected_list+=("$1")
    shift
  done
  shift  # consume the "--"
  local extra_args=("$@")

  local status
  status=$(curl -s -o /dev/null -w '%{http_code}' "${extra_args[@]}" "$url") || status="000"

  local matched=false
  for exp in "${expected_list[@]}"; do
    if [[ "$status" == "$exp" ]]; then
      matched=true
      break
    fi
  done

  if $matched; then
    green "  PASS  $description  (got $status)"
    PASS=$((PASS + 1))
  else
    red   "  FAIL  $description  (expected one of [${expected_list[*]}], got $status)"
    FAIL=$((FAIL + 1))
  fi
}

skip_test() {
  yellow "  SKIP  $1"
  SKIP=$((SKIP + 1))
}

# ---------- docker compose ---------------------------------------------------

cleanup() {
  if [[ "${SKIP_COMPOSE:-}" != "1" ]]; then
    bold "Tearing down containers..."
    docker compose -f "$COMPOSE_FILE" down -v --remove-orphans 2>/dev/null || true
  fi
}
trap cleanup EXIT

if [[ "${SKIP_COMPOSE:-}" != "1" ]]; then
  bold "Validating docker compose config..."
  docker compose -f "$COMPOSE_FILE" config --quiet
  green "  docker compose config: OK"

  bold "Bringing up all services..."
  docker compose -f "$COMPOSE_FILE" up --build -d

  bold "Waiting for services to be healthy (polling /healthz)..."

  services=(
    "api-gateway:5000"
    "identity-service:5001"
    "customer-service:5002"
    "order-service:5003"
    "product-service:5004"
    "notification-service:5005"
  )

  MAX_WAIT=120
  POLL_INTERVAL=3

  for svc_port in "${services[@]}"; do
    svc="${svc_port%%:*}"
    port="${svc_port##*:}"
    elapsed=0
    printf "  Waiting for %-25s " "$svc..."
    while true; do
      if curl -sf "http://localhost:$port/healthz" > /dev/null 2>&1; then
        green "ready (${elapsed}s)"
        break
      fi
      if (( elapsed >= MAX_WAIT )); then
        red "TIMEOUT after ${MAX_WAIT}s"
        bold "Container logs for $svc:"
        docker compose -f "$COMPOSE_FILE" logs --tail=40 "$svc" 2>/dev/null || true
        break
      fi
      sleep "$POLL_INTERVAL"
      elapsed=$((elapsed + POLL_INTERVAL))
    done
  done
fi

echo ""
bold "============================================================"
bold " GATEWAY E2E SMOKE TESTS"
bold "============================================================"

# ---------- 1. Gateway health ------------------------------------------------

bold "1. Gateway health check"
assert_status "GET /healthz -> 200" "$GATEWAY_URL/healthz" "200"

# ---------- 2. Route reachability (scaffold or full) -------------------------

bold "2. Route reachability through gateway"

# The gateway strips the prefix (e.g. /api/customers) and forwards the rest.
# Service controllers are at api/{controller} (singular).
# So: gateway /api/customers/api/customer -> service /api/customer

declare -A ROUTE_MAP=(
  ["customer"]="/api/customers/api/customer"
  ["order"]="/api/orders/api/order"
  ["product"]="/api/products/api/product"
  ["notification"]="/api/notifications/api/notification"
  ["identity"]="/api/identity/api/identity"
)

for svc in customer order product notification identity; do
  path="${ROUTE_MAP[$svc]}"
  # Expect 200 (scaffold returns OK) or 401 (if auth is added)
  assert_status_any_of \
    "GET $path -> 200 or 401 (route reachable)" \
    "$GATEWAY_URL$path" \
    "200" "401" \
    --
done

# ---------- 3. Auth enforcement (401 without token) --------------------------

bold "3. Auth enforcement (no Bearer token -> 401)"

AUTH_TESTED=false
for svc in customer order product notification; do
  path="${ROUTE_MAP[$svc]}"
  status=$(curl -s -o /dev/null -w '%{http_code}' "$GATEWAY_URL$path") || status="000"
  if [[ "$status" == "401" ]]; then
    green "  PASS  GET $path -> 401 (auth enforced)"
    PASS=$((PASS + 1))
    AUTH_TESTED=true
  elif [[ "$status" == "200" ]]; then
    skip_test "GET $path -> 401 (service in scaffold mode, no auth yet — got 200)"
  else
    red   "  FAIL  GET $path -> 401 (got $status)"
    FAIL=$((FAIL + 1))
  fi
done

# ---------- 4. Identity flow (register + login) ------------------------------

bold "4. Identity flow (register + login)"

TOKEN=""

# Try to register
REG_BODY='{"userName":"smoketest","email":"smoke@test.local","password":"Smoke123!"}'
REG_STATUS=$(curl -s -o /tmp/reg_response.json -w '%{http_code}' \
  -X POST "$GATEWAY_URL/api/identity/api/identity/register" \
  -H "Content-Type: application/json" \
  -d "$REG_BODY") || REG_STATUS="000"

if [[ "$REG_STATUS" == "200" || "$REG_STATUS" == "201" ]]; then
  green "  PASS  POST /api/identity/api/identity/register -> $REG_STATUS"
  PASS=$((PASS + 1))

  # Try to login
  LOGIN_BODY='{"userName":"smoketest","password":"Smoke123!"}'
  LOGIN_STATUS=$(curl -s -o /tmp/login_response.json -w '%{http_code}' \
    -X POST "$GATEWAY_URL/api/identity/api/identity/login" \
    -H "Content-Type: application/json" \
    -d "$LOGIN_BODY") || LOGIN_STATUS="000"

  if [[ "$LOGIN_STATUS" == "200" ]]; then
    green "  PASS  POST /api/identity/api/identity/login -> 200"
    PASS=$((PASS + 1))
    # Extract JWT token from response
    TOKEN=$(cat /tmp/login_response.json | python3 -c "
import sys, json
try:
    data = json.load(sys.stdin)
    # Try common token field names
    for key in ('token', 'accessToken', 'access_token', 'jwt', 'Token'):
        if key in data:
            print(data[key])
            sys.exit(0)
    # If the response is a string, use it directly
    if isinstance(data, str):
        print(data)
        sys.exit(0)
except:
    pass
" 2>/dev/null) || TOKEN=""
    if [[ -n "$TOKEN" ]]; then
      green "  PASS  JWT token extracted from login response"
      PASS=$((PASS + 1))
    else
      skip_test "Could not extract JWT token from login response"
    fi
  else
    red   "  FAIL  POST /api/identity/api/identity/login -> 200 (got $LOGIN_STATUS)"
    FAIL=$((FAIL + 1))
  fi
elif [[ "$REG_STATUS" == "404" || "$REG_STATUS" == "405" ]]; then
  skip_test "POST /api/identity/api/identity/register (endpoint not implemented — scaffold)"
  skip_test "POST /api/identity/api/identity/login (skipped — no register endpoint)"
else
  red   "  FAIL  POST /api/identity/api/identity/register -> 200|201 (got $REG_STATUS)"
  FAIL=$((FAIL + 1))
  skip_test "POST /api/identity/api/identity/login (skipped — register failed)"
fi

# ---------- 5. Authenticated CRUD (with JWT) ---------------------------------

bold "5. Authenticated CRUD (with Bearer token)"

if [[ -n "$TOKEN" ]]; then
  for svc in customer product order notification; do
    path="${ROUTE_MAP[$svc]}"
    assert_status \
      "GET $path (with token) -> 200" \
      "$GATEWAY_URL$path" \
      "200" \
      -H "Authorization: Bearer $TOKEN"
  done
else
  for svc in customer product order notification; do
    path="${ROUTE_MAP[$svc]}"
    if $AUTH_TESTED; then
      skip_test "GET $path (with token) -> 200 (no JWT token available)"
    else
      # No auth on scaffolds — requests already return 200 without token
      skip_test "GET $path (with token) -> 200 (scaffold — no auth, already returning 200)"
    fi
  done
fi

# ---------- 6. Header forwarding verification --------------------------------

bold "6. Authorization header forwarding"

# Verify the gateway doesn't strip Authorization headers.
# We send a request with a bogus Authorization header and confirm the
# downstream service receives it (YARP forwards all headers by default).
# If the service has auth, a bad token returns 401 (header was forwarded).
# If scaffold (no auth), it returns 200 (header forwarding is a no-op).

BOGUS_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.e30.bogus"
for svc in customer; do
  path="${ROUTE_MAP[$svc]}"
  status=$(curl -s -o /dev/null -w '%{http_code}' \
    -H "Authorization: Bearer $BOGUS_TOKEN" \
    "$GATEWAY_URL$path") || status="000"
  if [[ "$status" == "401" ]]; then
    green "  PASS  Auth header forwarded (bad token -> 401 from downstream)"
    PASS=$((PASS + 1))
  elif [[ "$status" == "200" ]]; then
    skip_test "Auth header forwarding (scaffold — no auth on downstream, can't verify)"
  else
    red   "  FAIL  Unexpected status $status for header forwarding check"
    FAIL=$((FAIL + 1))
  fi
done

# ---------- Summary ----------------------------------------------------------

echo ""
bold "============================================================"
bold " RESULTS"
bold "============================================================"
green "  Passed:  $PASS"
[[ $FAIL -gt 0 ]] && red "  Failed:  $FAIL" || echo "  Failed:  $FAIL"
[[ $SKIP -gt 0 ]] && yellow "  Skipped: $SKIP" || echo "  Skipped: $SKIP"
bold "============================================================"

if [[ $FAIL -gt 0 ]]; then
  red "SMOKE TEST FAILED"
  exit 1
else
  green "SMOKE TEST PASSED"
  exit 0
fi
