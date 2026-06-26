#!/bin/bash
set -euo pipefail

GATEWAY_URL="${GATEWAY_URL:-http://localhost:5000}"
CUSTOMER_URL="${CUSTOMER_URL:-http://localhost:5002}"
MAX_RETRIES=30
RETRY_INTERVAL=5

echo "Waiting for gateway to be healthy..."
for i in $(seq 1 $MAX_RETRIES); do
  if curl -sf "$GATEWAY_URL/healthz" > /dev/null 2>&1; then
    echo "Gateway is healthy!"
    break
  fi
  if [ "$i" -eq "$MAX_RETRIES" ]; then
    echo "Gateway failed to become healthy after $((MAX_RETRIES * RETRY_INTERVAL))s"
    exit 1
  fi
  echo "Attempt $i/$MAX_RETRIES - waiting ${RETRY_INTERVAL}s..."
  sleep $RETRY_INTERVAL
done

echo ""
echo "Waiting for customer-service to be healthy..."
for i in $(seq 1 $MAX_RETRIES); do
  if curl -sf "$CUSTOMER_URL/healthz" > /dev/null 2>&1; then
    echo "Customer service is healthy!"
    break
  fi
  if [ "$i" -eq "$MAX_RETRIES" ]; then
    echo "Customer service failed to become healthy after $((MAX_RETRIES * RETRY_INTERVAL))s"
    exit 1
  fi
  echo "Attempt $i/$MAX_RETRIES - waiting ${RETRY_INTERVAL}s..."
  sleep $RETRY_INTERVAL
done

echo ""
echo "=== Smoke Test: GET /healthz on customer-service (via direct port) ==="
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$CUSTOMER_URL/healthz")
echo "Response code: $HTTP_CODE"
if [ "$HTTP_CODE" = "200" ]; then
  echo "PASS: Customer service health check returns 200"
else
  echo "FAIL: Expected 200, got $HTTP_CODE"
  exit 1
fi

echo ""
echo "=== Smoke Test: GET /api/customers (unauthenticated - expect 401) ==="
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$GATEWAY_URL/api/customers")
echo "Response code: $HTTP_CODE"
if [ "$HTTP_CODE" = "401" ]; then
  echo "PASS: Unauthenticated request correctly returns 401"
else
  echo "FAIL: Expected 401, got $HTTP_CODE"
  exit 1
fi

echo ""
echo "All smoke tests passed!"
