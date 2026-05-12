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

## Frontend Migration: Angular → React

The original monolith's Angular frontend is being migrated to React. The new React frontend will consume these microservice APIs through the YARP API Gateway.

### Target Frontend Tech Stack

| Concern | Angular (Current) | React (Target) |
|---------|-------------------|-----------------|
| Framework | Angular 14+ | React 18+ with TypeScript |
| Build Tool | Angular CLI / Webpack | Vite |
| Routing | `@angular/router` | React Router v6 |
| State Management | RxJS / NgRx | TanStack Query (server state) + Zustand (client state) |
| HTTP Client | `HttpClient` | Axios + TanStack Query |
| Forms | Reactive Forms | React Hook Form + Zod |
| UI Components | Angular Material / PrimeNG | Material UI (MUI) v5 |
| Testing | Jasmine + Karma | Vitest + React Testing Library |
| E2E Testing | Protractor / Cypress | Playwright |
| Styling | SCSS / Component styles | CSS Modules or Tailwind CSS |
| Containerization | Nginx in Docker | Nginx in Docker |

### Frontend Project Structure

```
src/frontend/
├── src/
│   ├── main.tsx                       # Entry point
│   ├── App.tsx                        # Root component + router
│   ├── api/                           # One API module per service
│   │   ├── client.ts                  # Axios instance (base URL → API Gateway)
│   │   ├── identity.api.ts
│   │   ├── customers.api.ts
│   │   ├── orders.api.ts
│   │   ├── products.api.ts
│   │   └── notifications.api.ts
│   ├── hooks/                         # TanStack Query hooks per service
│   ├── pages/                         # Route-level components
│   │   ├── DashboardPage.tsx
│   │   ├── identity/
│   │   ├── customers/
│   │   ├── orders/
│   │   ├── products/
│   │   └── notifications/
│   ├── components/                    # Shared UI components
│   │   ├── layout/                    # AppShell, Sidebar, Breadcrumbs
│   │   └── shared/                    # DataTable, StatusBadge, CurrencyDisplay
│   ├── store/                         # Zustand stores
│   ├── types/                         # TypeScript interfaces matching backend DTOs
│   └── utils/                         # Currency (cents→dollars), date formatting
├── Dockerfile                         # Multi-stage: Node build → Nginx serve
├── nginx.conf                         # SPA routing + API proxy to gateway
├── package.json
├── vite.config.ts
└── vitest.config.ts
```

### Migration Phases

| Phase | Scope | Duration |
|-------|-------|----------|
| **Phase 0** — Preparation | Scaffold React + Vite + TypeScript, define TS types, create Axios client with `X-Correlation-ID` interceptor, add to Docker Compose | 1–2 days |
| **Phase 1** — Layout & Routing | AppShell with sidebar, React Router v6 nested routes for all 5 domains, breadcrumbs | 2–3 days |
| **Phase 2** — API Integration | TanStack Query hooks for all service endpoints, loading/error/empty states, pagination | 2–3 days |
| **Phase 3** — Feature Pages | Dashboard (health checks), Notification pages (list, detail, HTML preview, test order), scaffold pages for Identity/Customer/Order/Product | 3–5 days |
| **Phase 4** — Authentication | Integrate with Identity service, Zustand auth store, JWT handling, protected routes | 2–3 days |
| **Phase 5** — Testing | Unit tests (Vitest), component tests (RTL), API mock tests (MSW), E2E (Playwright), 80%+ coverage target | 2–3 days |
| **Phase 6** — Containerization & CI | Multi-stage Dockerfile, Nginx config, Docker Compose integration, CI pipeline (lint → typecheck → test → build) | 1–2 days |

**Estimated total: ~3–4 weeks**

### Angular → React Mapping Reference

| Angular Concept | React Equivalent |
|----------------|-----------------|
| `@Component` | Function component |
| `@Input()` / `@Output()` | Props / callback props |
| `ngOnInit` | `useEffect(() => {}, [])` |
| `ngOnDestroy` | `useEffect` cleanup function |
| `@Injectable` service | Custom hook or context |
| `HttpClient` + `Observable` | Axios + TanStack Query |
| `ngIf` / `ngFor` | Conditional rendering / `.map()` |
| `ReactiveForms` / `FormGroup` | React Hook Form `useForm()` |
| `Router` / `ActivatedRoute` | `useNavigate()` / `useParams()` |
| `NgModule` | No equivalent (just imports) |
| `Pipes` | Utility functions |
| Route `Guards` | `ProtectedRoute` wrapper |
| `Interceptors` | Axios interceptors |
| `NgRx Store` | Zustand store |

### Key Technical Notes

- **Currency handling**: Backend transmits `OrderTotal` in cents (integer). The React frontend must convert: `(amountInCents / 100).toLocaleString('en-US', { style: 'currency', currency: 'USD' })`.
- **Correlation ID**: The backend `CorrelationIdMiddleware` expects an `X-Correlation-ID` header. Add via Axios request interceptor.
- **Notification preview**: The `GET /api/notification/{id}/preview` endpoint returns rendered HTML. Display in a sandboxed `<iframe>` with `srcDoc`.
- **API Gateway**: The React frontend proxies API calls through Nginx to the YARP gateway on port 5000. No backend changes required.

## Related Repositories

| Repo | Purpose |
|------|---------|
| [`app_dotnet_angular_containerized_decomposition_monolith`](https://github.com/Cognition-Partner-Workshops/app_dotnet_angular_containerized_decomposition_monolith) | Before-state monolith (includes original Angular frontend) |
| [`app_dotnet_angular_containerized_decomposition_microfrontends`](https://github.com/Cognition-Partner-Workshops/app_dotnet_angular_containerized_decomposition_microfrontends) | Angular micro-frontends target |
| [`app_dotnet_angular_containerized_decomposition_iac`](https://github.com/Cognition-Partner-Workshops/app_dotnet_angular_containerized_decomposition_iac) | App-specific Helm charts |
| [`platform-engineering-shared-services`](https://github.com/Cognition-Partner-Workshops/platform-engineering-shared-services) | Shared EKS cluster and platform infra |
