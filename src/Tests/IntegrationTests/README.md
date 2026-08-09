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

## Current flow coverage

The tests use direct service ports. The gateway is intentionally excluded
because its current plural YARP routes and transforms return 404 instead of
reaching the service controller routes.

| Intended step | Test | Current state |
| --- | --- | --- |
| Create a product through Product | `OrderProductNotificationFlowContractTests.ProductCanBeCreatedForAnOrder` | Blocked by Product scaffold; skipped |
| Place an order referencing that product | `OrderProductNotificationFlowContractTests.OrderCanBePlacedReferencingAProduct` | Blocked by Order/Product scaffolds; skipped |
| Publish and receive `OrderPlacedEvent` in Notification | `OrderProductNotificationFlowContractTests.PlacedOrderProducesNotificationWithMatchingEventFields` | Blocked by Order event publishing; skipped |
| Reject an order with an unknown product | `OrderProductNotificationFlowContractTests.OrderRejectsAnUnknownProduct` | Blocked by Order POST endpoint and product validation; skipped |
| Reject an order with an unknown customer | `OrderProductNotificationFlowContractTests.OrderRejectsAnUnknownCustomer` | Blocked by Order POST endpoint and customer validation; skipped |

The contract tests assume Product `POST /api/product` accepts
`{ name, description, price }` and returns `{ id }`. They assume Order
`POST /api/order` accepts
`{ customerId, items: [{ productId, quantity }] }` and returns
`{ id, customerId, totalAmount }`. A successful order is expected to publish
the existing `Shared.Contracts.Events.OrderPlacedEvent` fields
`OrderId`, `CustomerId`, `TotalAmount`, and `PlacedAt`.

`ScaffoldGapTests` currently checks that the Order/Product POST endpoints are
405 and that their GET endpoints still expose the scaffold marker. These
checks are deliberately named as gaps so implementation will fail loudly;
delete them once the services are implemented.

The expected-currency preview assertion is retained as a skipped test while
the known `NotificationRenderer.FormatCurrency` defect is unresolved. The
renderer divides the posted decimal amount by 100 and formats it using the
process culture, so `42.50` currently renders as `¤0.43`.
