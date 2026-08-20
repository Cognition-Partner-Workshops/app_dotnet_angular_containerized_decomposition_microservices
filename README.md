# Decomposed .NET Microservices — Target State

This repository is the **target scaffolding** for decomposing the monolithic QuickApp application into cloud-native .NET microservices deployed on Kubernetes.

## Source Monolith

The before-state monolith lives in [`app_dotnet_angular_containerized_decomposition_monolith`](https://github.com/Cognition-Partner-Workshops/app_dotnet_angular_containerized_decomposition_monolith).

## Architecture

The monolith's bounded contexts are decomposed into the following independently deployable microservices:

```
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  Identity    │  │  Customer    │  │   Order      │
│  Service     │  │  Service     │  │   Service    │
│  (.NET 10)   │  │  (.NET 10)   │  │  (.NET 10)   │
└──────┬───────┘  └──────┬───────┘  └──────┬───────┘
       │                 │                 │
       └────────────┬────┘─────────────────┘
                    │
              ┌─────┴──────┐
              │  API       │
              │  Gateway   │
              └─────┬──────┘
                    │
┌──────────────┐  ┌┴─────────────┐
│  Product     │  │ Notification │
│  Service     │  │  Service     │
│  (.NET 10)   │  │  (.NET 10)   │
└──────────────┘  └──────────────┘
```

## Services

| Service | Port | Description | Monolith Origin |
|---------|------|-------------|-----------------|
| `identity-service` | 5001 | Authentication, authorization, user/role management | `AuthorizationController`, `UserAccountController`, `UserRoleController` |
| `customer-service` | 5002 | Customer CRUD and lookup | `CustomerController`, customer models |
| `order-service` | 5003 | Order management and processing | `OrdersController`, order models |
| `product-service` | 5004 | Product catalog management | `ProductsController`, product models |
| `notification-service` | 5005 | Email and in-app notifications | `NotificationService`, notification models |
| `api-gateway` | 5000 | YARP reverse proxy, request routing, rate limiting | New — replaces monolith's single entry point |

## Project Structure

```
src/
├── ApiGateway/                    # YARP-based API gateway
│   ├── Program.cs
│   ├── appsettings.json
│   ├── ApiGateway.csproj
│   └── Dockerfile
├── Services/
│   ├── Identity/
│   │   ├── Identity.API/          # ASP.NET Core Web API
│   │   ├── Identity.Domain/       # Domain entities and interfaces
│   │   └── Identity.Infrastructure/ # EF Core, external integrations
│   ├── Customer/
│   │   ├── Customer.API/
│   │   ├── Customer.Domain/
│   │   └── Customer.Infrastructure/
│   ├── Order/
│   │   ├── Order.API/
│   │   ├── Order.Domain/
│   │   └── Order.Infrastructure/
│   ├── Product/
│   │   ├── Product.API/
│   │   ├── Product.Domain/
│   │   └── Product.Infrastructure/
│   └── Notification/
│       ├── Notification.API/
│       ├── Notification.Domain/
│       └── Notification.Infrastructure/
├── Shared/
│   ├── Shared.Contracts/          # Shared DTOs, events, interfaces
│   └── Shared.Infrastructure/     # Common middleware, logging, health checks
├── docker-compose.yml
├── docker-compose.override.yml
└── Microservices.sln
```

## Technology Stack

- **.NET 10** — ASP.NET Core Web API per service
- **Entity Framework Core** — per-service database (database-per-service pattern)
- **YARP** — API gateway / reverse proxy
- **RabbitMQ** — async messaging between services
- **Docker** — containerized services
- **Kubernetes** — orchestration (see `app_dotnet_angular_containerized_decomposition_iac` for Helm charts)

## Getting Started

Each service can be run independently:

```bash
# Run all services with Docker Compose
docker compose up --build

# Run a single service
cd src/Services/Identity/Identity.API
dotnet run
```

## Running tests

Run the automated Notification service tests from the repository root:

```bash
dotnet test src/Microservices.sln
```

The tests use SQLite in-memory storage and mocked messaging, so they do not
require external PostgreSQL or RabbitMQ infrastructure.

## Related Repositories

| Repo | Purpose |
|------|---------|
| [`app_dotnet_angular_containerized_decomposition_monolith`](https://github.com/Cognition-Partner-Workshops/app_dotnet_angular_containerized_decomposition_monolith) | Before-state monolith |
| [`app_dotnet_angular_containerized_decomposition_microfrontends`](https://github.com/Cognition-Partner-Workshops/app_dotnet_angular_containerized_decomposition_microfrontends) | Angular micro-frontends target |
| [`app_dotnet_angular_containerized_decomposition_iac`](https://github.com/Cognition-Partner-Workshops/app_dotnet_angular_containerized_decomposition_iac) | App-specific Helm charts |
| [`platform-engineering-shared-services`](https://github.com/Cognition-Partner-Workshops/platform-engineering-shared-services) | Shared EKS cluster and platform infra |
