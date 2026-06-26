# Customer Microservice — Recon Document

## Source: QuickApp Monolith (READ-ONLY)

### Entity: `Customer` (extends `BaseEntity`)
| Field | Type | Required | Constraints |
|---|---|---|---|
| Id | int | PK | auto-increment |
| Name | string | required | maxlen 100, indexed |
| Email | string | required | maxlen 100 |
| PhoneNumber | string? | - | unicode=false, maxlen 30 |
| Address | string? | - | - |
| City | string? | - | maxlen 50 |
| Gender | Gender (enum) | - | None=0, Female=1, Male=2 |
| CreatedBy | string? | - | maxlen 40 |
| UpdatedBy | string? | - | maxlen 40 |
| CreatedDate | DateTime | - | - |
| UpdatedDate | DateTime | - | - |

### ORM Config (from `ApplicationDbContext.OnModelCreating`)
- Table name: `AppCustomers` (prefix `App` + `Customers`)
- `Name`: `.IsRequired().HasMaxLength(100)` + `.HasIndex(c => c.Name)`
- `Email`: `.HasMaxLength(100)`
- `PhoneNumber`: `.IsUnicode(false).HasMaxLength(30)`
- `City`: `.HasMaxLength(50)`
- `CreatedBy`/`UpdatedBy`: `[MaxLength(40)]` on BaseEntity

### DROPPED Cross-Context Members
- `ICollection<Order> Orders` navigation property — FORBIDDEN
- No references to: Order, OrderDetail, Product, ApplicationUser, ApplicationDbContext

### ViewModel: `CustomerVM` → `CustomerDto`
| Field | Type | Notes |
|---|---|---|
| Id | int | |
| Name | string? | |
| Email | string? | |
| PhoneNumber | string? | |
| Address | string? | |
| City | string? | |
| Gender | string? | |
| ~~Orders~~ | ~~ICollection~~ | **DROPPED** |

### Controller Pattern (Monolith)
- Route: `api/[controller]` → `api/customer`
- `GET /api/customer` — list all
- `GET /api/customer/{id}` — get by ID
- `POST /api/customer` — create
- `PUT /api/customer/{id}` — update
- `DELETE /api/customer/{id}` — delete

### Service Interface (Monolith — to be expanded)
- `GetTopActiveCustomers(int count)` → not needed for microservice
- `GetAllCustomersData()` → becomes `GetAllAsync()`

### Microservice Interface (new)
- `GetAllAsync() → Task<IEnumerable<Customer>>`
- `GetByIdAsync(int id) → Task<Customer?>`
- `CreateAsync(Customer customer) → Task<Customer>`
- `UpdateAsync(Customer customer) → Task`
- `DeleteAsync(int id) → Task<bool>`

## Target: quickapp-microservices Scaffold

### Project Structure (existing)
- `Customer.API/` — ASP.NET Core Web API (net10.0), port 5002
- `Customer.Domain/` — domain entities, enums, interfaces
- `Customer.Infrastructure/` — EF Core DbContext, services

### Scaffold State
- `Customer.API/Program.cs`: basic setup with DbContext, Swagger, health checks — needs JWT auth
- `Customer.API/Controllers/CustomerController.cs`: scaffold with GET only — needs full CRUD + auth
- `Customer.Infrastructure/Data/CustomerDbContext.cs`: empty DbContext — needs entity config
- `Customer.Domain/Entities/.gitkeep`: empty — needs Customer entity
- `Customer.Domain/Interfaces/.gitkeep`: empty — needs ICustomerService

### Docker
- Dockerfile at `Customer.API/Dockerfile` — port 5002
- `docker-compose.yml`: customer-service with PostgreSQL (`customerdb`)

### Gateway (YARP)
- Route: `/api/customers/{**catch-all}` → `PathRemovePrefix: /api/customers` → `http://customer-service:5002/`
- **ISSUE**: After prefix removal, requests arrive at `/` but controller expects `/api/customer`
- **FIX needed in T4**: Add `PathPrefix: /api/customer` transform

### JWT Auth Convention
- Secret: `QuickApp_Microservices_SuperSecret_Key_For_Dev_Only_Min_32_Chars!`
- Issuer: `quickapp-identity`
- Audience: `quickapp-api`
- Expiration: 60 minutes
- Package: `Microsoft.AspNetCore.Authentication.JwtBearer`

## Task DAG

```
T0 (recon + scaffold) ← main
  └─ T1 (domain + contracts) ← T0
       └─ T2 (persistence + migration) ← T1
            └─ T3 (API + auth) ← T2
                 └─ T4 (gateway route fix) ← T3
                      └─ T5 (E2E smoke test) ← T4
```

### Stacked PR Chain (merge order)
1. T0 `devin/customer-service` → `main`
2. T1 `devin/customer-service-t1` → `devin/customer-service`
3. T2 `devin/customer-service-t2` → `devin/customer-service-t1`
4. T3 `devin/customer-service-t3` → `devin/customer-service-t2`
5. T4 `devin/customer-service-t4` → `devin/customer-service-t3`
6. T5 `devin/customer-service-t5` → `devin/customer-service-t4`

### Validation Gates
- **T0**: Scaffold builds (`dotnet build` succeeds)
- **T1**: Build succeeds, zero forbidden references
- **T2**: Build succeeds, EF migration applies against PostgreSQL
- **T3**: Build succeeds, all endpoints protected (unauth → 401), healthz public
- **T4**: Gateway routes correctly to service
- **T5**: E2E smoke test passes (healthz 200, unauth 401, CRUD with JWT)
