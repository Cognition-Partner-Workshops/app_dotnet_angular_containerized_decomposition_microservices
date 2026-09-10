# Extraction boundaries: quickapp-monolith → quickapp-microservices

Source of truth for the Product, Identity and Order extractions. The Customer extraction
(`src/Services/Customer`) is the reference implementation: mirror its layout, naming and test style.

The monolith (`Cognition-Partner-Workshops/quickapp-monolith`) is **read-only**. Never open a PR,
branch or commit there.

## Service layout (mirror Customer)

```
src/Services/<Name>/
  <Name>.Domain/          entities + interfaces, no EF, no ASP.NET
  <Name>.Infrastructure/  <Name>DbContext, EF Core migrations, repositories
  <Name>.API/             controllers, Program.cs, Dockerfile, appsettings.json
  <Name>.Tests/           xUnit
```

Each service owns its own Postgres database (`identitydb`, `customerdb`, `orderdb`, `productdb`,
`notificationdb` — already wired in `src/docker-compose.yml`). No service reads another service's
database or DbContext.

## What moves into each service

| Service  | Monolith source | Owns |
| --- | --- | --- |
| Customer (reference) | `Models/Shop/Customer`, `Services/Shop/CustomerService`, `Controllers/CustomerController`, `ViewModels/Shop/CustomerVM` | customer records |
| Product | `Models/Shop/Product`, `Models/Shop/ProductCategory`, `Services/Shop/ProductService` (+`IProductService`), `ViewModels/Shop/ProductVM` | catalog, categories, stock fields |
| Identity | `Models/Account/*` (`ApplicationUser`, `ApplicationRole`, `ApplicationPermission`), `Services/Account/*` (`UserAccountService`, `UserRoleService`, `ApplicationPermissions`, `CustomClaims`), controllers `AuthorizationController`, `UserAccountController`, `UserRoleController`, `Configuration/OidcServerConfig`, `Authorization/*`, `ViewModels/Account/*` | users, roles, permissions, token issuance |
| Order | `Models/Shop/Order`, `Models/Shop/OrderDetail`, `Services/Shop/OrdersService` (+`IOrdersService`), `ViewModels/Shop/OrderVM` | orders, order lines |

Anything not listed for your service stays where it is. If you think you need something owned by
another context, you need an ID and a gateway call, not the type.

## Cutting the cross-context links

The monolith's EF graph is `Customer 1—* Order 1—* OrderDetail *—1 Product`, plus
`Order *—1 ApplicationUser` (cashier) and `Product *—1 ProductCategory` / `Product *—1 Product` (parent).

Rules:

1. **Foreign keys become plain IDs.** Drop the navigation property, keep the scalar. `Order.Customer`
   → `Order.CustomerId`; `Order.Cashier` → `Order.CashierId` (string, the Identity user id);
   `OrderDetail.Product` → `OrderDetail.ProductId`; `Customer.Orders` and `Product.OrderDetails`
   are deleted outright.
2. **No EF navigation, `Include`, or join across a service boundary.** No cross-database FK
   constraints. An ID that points at another service is not validated by the database.
3. **Intra-context relations stay.** `Product ↔ ProductCategory`, `Product.Parent/Children` and
   `Order ↔ OrderDetail` are inside one context — keep them as EF navigations and cascade normally.
4. **Enrichment goes through the gateway**, server-to-server over HTTP on the compose network, never
   by querying another service's Postgres. The routes are already defined in
   `src/ApiGateway/appsettings.json`:

   | Context | Gateway route | Upstream |
   | --- | --- | --- |
   | Identity | `/api/identity/*` | `identity-service:5001` |
   | Customer | `/api/customers/*` | `customer-service:5002` |
   | Order | `/api/orders/*` | `order-service:5003` |
   | Product | `/api/products/*` | `product-service:5004` |
   | Notification | `/api/notifications/*` | `notification-service:5005` |

   Enrichment is optional and best-effort: a failed lookup degrades the response (null/partial
   related data), it does not fail the request.
5. **Events, not calls, for side effects.** Order publishes `Shared.Contracts.Events.OrderPlacedEvent`
   to RabbitMQ; Notification already consumes it. Order does not call Notification.
6. **Auth is Identity's.** Other services validate the bearer token Identity issues; they never read
   the user/role tables.

## Files each child may touch

Allowed:

- `src/Services/<YourName>/**` — your own service folder only.
- `src/Shared/Shared.Contracts/**` — **additive only**: new DTO/event files. Do not edit or rename
  existing contracts (`OrderPlacedEvent`, `ServiceHealthDto`); another service compiles against them.
- `src/docker-compose.yml` — only environment variables / `depends_on` for *your* service. Do not
  touch other services' blocks, ports, or the `postgres`/`rabbitmq` services.
- `src/Microservices.sln` — add your own new projects (e.g. `<Name>.Tests`) only.
- `docs/` — only if your PR needs a note; do not rewrite this file.

Forbidden for everyone:

- The monolith repo, in any way.
- Another service's folder, another service's compose block, `src/ApiGateway/**`, `src/Shared/Shared.Infrastructure/**`.
  The five gateway routes above already exist and are correct — if you believe the gateway needs a
  change, say so in your PR and stop, do not edit it.
- Anything the Customer extraction PR touches. If you need something from Customer (a shared helper,
  a fixed csproj reference, a base class), **wait for that PR to merge and rebase onto `main`** —
  do not copy it into your branch.

## Definition of done (all three)

- Your projects build: `(cd src && dotnet build Services/<Name>/<Name>.API/<Name>.API.csproj -c Release)`.
- Your xUnit tests pass.
- The full stack boots: `docker compose -f src/docker-compose.yml up --build`.
- Your route answers through the gateway on `:5000` (e.g. `curl :5000/api/products`), evidenced in the PR.
- One PR per service, against `main`. Address Devin Review; two rounds of review, maximum.

Known repo bug (pre-existing): `Notification.API.csproj` references `..\..\Shared` instead of
`..\..\..\Shared`. It is not yours to fix unless it blocks your compose boot — if it does, say so in
the PR.
