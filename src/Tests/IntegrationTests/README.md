# Integration tests

This project drives the running services over HTTP and defaults to the direct
compose ports:

- `ORDER_URL=http://localhost:5003`
- `PRODUCT_URL=http://localhost:5004`
- `NOTIFICATION_URL=http://localhost:5005`

Run locally against an already-running stack:

```bash
dotnet test src/Tests/IntegrationTests/IntegrationTests.csproj
```

Run in Docker Compose, with the test container waiting for the service health
endpoints before starting the suite:

```bash
docker compose -f src/docker-compose.yml -f src/docker-compose.test.yml run --rm integration-tests
```

The covered flow is Order/Product scaffold smoke checks followed by HTTP
`OrderPlacedEvent` ingestion into Notification, persisted retrieval, HTML
preview rendering, unknown-id handling, and invalid payload behavior.

Order and Product do not yet create orders or products, so the tests cannot
exercise a real Order-to-Product call or event publication. The gateway is
also intentionally excluded: its current plural YARP routes and transforms
return 404 instead of reaching the direct service endpoints.

The expected-currency preview assertion is retained as a skipped test while
the known `NotificationRenderer.FormatCurrency` defect is unresolved.
