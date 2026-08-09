#!/bin/sh
set -eu

wait_for_health() {
  name="$1"
  url="$2"
  echo "Waiting for $name at $url/healthz..."
  until curl --fail --silent --show-error "$url/healthz" >/dev/null; do
    sleep 1
  done
}

wait_for_health order "${ORDER_URL}"
wait_for_health product "${PRODUCT_URL}"
wait_for_health notification "${NOTIFICATION_URL}"

dotnet test Tests/IntegrationTests/IntegrationTests.csproj --no-restore
