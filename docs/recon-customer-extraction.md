# Recon: Customer Service Extraction

## 1. Gateway

- **Technology**: YARP (Yet Another Reverse Proxy) via `Yarp.ReverseProxy` NuGet package v2.*
- **Config location**: `src/ApiGateway/appsettings.json`
- **Route registration**: JSON config under `ReverseProxy.Routes` and `ReverseProxy.Clusters`
- **Customer route**: `/api/customers/{**catch-all}` → `http://customer-service:5002/`
  - Transform: `PathRemovePrefix: /api/customers`
- **Other service routes**: identity (5001), order (5003), product (5004), notification (5005)
- **Health check**: `/healthz`

## 2. Service Template

Each service follows a 3-project layout:

| Layer | Target | Purpose |
|-------|--------|---------|
| `{Name}.API` | net10.0 | ASP.NET Core Web API — Controllers, Program.cs, Dockerfile |
| `{Name}.Domain` | net10.0 | Pure domain model (Entities/, Interfaces/) — no external deps |
| `{Name}.Infrastructure` | net10.0 | Data access — EF Core + Npgsql, references Domain project |

**Naming conventions**:
- Solution folder: `src/Services/{Name}/`
- Project files: `{Name}.{Layer}/{Name}.{Layer}.csproj`

**Shared project references** (from API layer):
- `Shared.Contracts` — DTOs (`ServiceHealthDto`), events (`OrderPlacedEvent`)
- `Shared.Infrastructure` — middleware (`CorrelationIdMiddleware`), health checks

**Key NuGet packages (API)**:
- `Microsoft.EntityFrameworkCore.Design 10.*`
- `Swashbuckle.AspNetCore 7.*`

**Key NuGet packages (Infrastructure)**:
- `Microsoft.EntityFrameworkCore 10.*`
- `Npgsql.EntityFrameworkCore.PostgreSQL 10.*`

## 3. JWT / Auth

- **Current state**: No JWT or auth validation in any service — all services are pure scaffolds without `[Authorize]` attributes.
- **Identity service**: Stub only — no token-issuing logic implemented.
- **Future work**: `Microsoft.AspNetCore.Authentication.JwtBearer` will need to be added to `Customer.API.csproj` and configured in `Program.cs`.

## 4. Database

- **Engine**: PostgreSQL 16 (alpine image via docker-compose)
- **Isolation**: Each service owns its own database (e.g., `customerdb`, `orderdb`, `identitydb`)
- **ORM**: Entity Framework Core with Npgsql provider
- **Connection string pattern**:
  - Docker: `Host=postgres;Database={name}db;Username=postgres;Password=postgres`
  - Local dev: `Host=localhost;Database={name}db;Username=postgres;Password=postgres`

## 5. E2E & CI

- **CI**: No `.github/workflows/` directory — no CI pipelines configured.
- **E2E**: No end-to-end test harness exists.
- **Integration testing**: `docker-compose.yml` can be used to bring up the full stack (postgres + rabbitmq + all services) for manual or scripted integration tests.

## 6. Customer Scaffold Status

The `src/Services/Customer/` directory already exists with:

- `Customer.API/` — stub controller, `Program.cs`, `appsettings.json`, `Dockerfile`, csproj
- `Customer.Domain/` — `Entities/` and `Interfaces/` directories with `.gitkeep` placeholders
- `Customer.Infrastructure/` — `Data/CustomerDbContext.cs` (empty context), `Services/` directory

**Build verification**: The scaffold compiles successfully in Release configuration:
```
dotnet build Services/Customer/Customer.API/Customer.API.csproj -c Release
# Build succeeded (1 warning: MSB9008 — Shared.Contracts project reference path note)
```
