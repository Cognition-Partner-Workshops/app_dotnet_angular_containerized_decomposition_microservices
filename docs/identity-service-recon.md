# Identity Service Recon

## Monolith Source Pointers (READ-ONLY)

| Artifact | Monolith Path | Key Details |
|---|---|---|
| User entity | `QuickApp.Core/Models/Account/ApplicationUser.cs` | Extends `IdentityUser`, fields: UserName, Email, PasswordHash (inherited), FullName, JobTitle, Configuration, IsEnabled, CreatedBy/UpdatedBy/CreatedDate/UpdatedDate (via `IAuditableEntity`). Has `Orders` nav property (FORBIDDEN). |
| Role entity | `QuickApp.Core/Models/Account/ApplicationRole.cs` | Extends `IdentityRole`, fields: Name (inherited), Description, audit fields. |
| Permission | `QuickApp.Core/Models/Account/ApplicationPermission.cs` | Value object (Name, Value, GroupName, Description). Not needed in microservice. |
| Base entity | `QuickApp.Core/Models/BaseEntity.cs` | `Id` (int), audit fields with `[MaxLength(40)]` on CreatedBy/UpdatedBy. |
| Auditable interface | `QuickApp.Core/Models/IAuditableEntity.cs` | CreatedBy, UpdatedBy, CreatedDate, UpdatedDate. |
| Auth controller | `QuickApp.Server/Controllers/AuthorizationController.cs` | OpenIddict password+refresh grant at `~/connect/token`. FORBIDDEN: OpenIddict usage. |
| Identity config | `QuickApp.Server/Program.cs:43-161` | ASP.NET Identity + OpenIddict + policy-based auth. |
| User services | `QuickApp.Core/Services/Account/` | UserAccountService, UserRoleService, CustomClaims. |
| ViewModels | `QuickApp.Server/ViewModels/Account/UserVMs.cs`, `RoleVM.cs` | UserBaseVM (Id, UserName, Email, FullName, JobTitle, PhoneNumber, Configuration, IsEnabled), RoleVM (Id, Name, Description). |

## Target Scaffold (quickapp-microservices)

| Project | Path | Status |
|---|---|---|
| Identity.API | `src/Services/Identity/Identity.API/` | Scaffold: Program.cs (EF+Swagger+healthz), IdentityController (TODO stubs) |
| Identity.Domain | `src/Services/Identity/Identity.Domain/` | Empty (csproj only) |
| Identity.Infrastructure | `src/Services/Identity/Identity.Infrastructure/` | Scaffold: IdentityDbContext (empty, TODO) |
| Dockerfile | `src/Services/Identity/Identity.API/Dockerfile` | Exposes port 5001, multi-stage build |
| docker-compose | `src/docker-compose.yml` | identity-service on port 5001, depends_on postgres+rabbitmq |

**Build status**: `dotnet build Identity.API.csproj -c Release` succeeds (1 warning: Shared.Contracts ref path mismatch — non-blocking).

**Target framework**: `net10.0`

**No CI** exists in this repo.

## Simplified Domain (Microservice)

Per decoupling decisions, the microservice uses plain entities (no ASP.NET Identity, no OpenIddict):

### AppUser
| Field | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| UserName | string | Required, MaxLength(100), Unique Index |
| Email | string | Required, MaxLength(100), Unique Index |
| PasswordHash | string | Required |
| FullName | string? | — |
| JobTitle | string? | — |
| IsEnabled | bool | — |
| CreatedAt | DateTime | — |
| UpdatedAt | DateTime | — |

### AppRole
| Field | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| Name | string | Required |
| Description | string? | — |

**Table names**: `Users`, `Roles`

**Password hashing**: BCrypt.Net-Next

## JWT Convention (Shared Across All Services)
- **Secret**: `QuickApp_Microservices_SuperSecret_Key_For_Dev_Only_Min_32_Chars!`
- **Issuer**: `quickapp-identity`
- **Audience**: `quickapp-api`
- **Expiration**: 60 minutes
- **Algorithm**: HMAC-SHA256
- **Claims**: `sub` (user ID), `name`, `email`, `role`
- **Package**: `Microsoft.AspNetCore.Authentication.JwtBearer`

## API Contract
| Endpoint | Method | Auth | Response |
|---|---|---|---|
| `/api/identity/register` | POST | Public | 201 + user info |
| `/api/identity/login` | POST | Public | 200 + JWT token |
| `/api/identity/users` | GET | Protected (JWT) | 200 + user list |
| `/api/identity/users/{id}` | GET | Protected (JWT) | 200 + user |
| `/healthz` | GET | Public | 200 |

## Gateway
- Route: `/api/identity/{**catch-all}` → `http://identity-service:5001/`
- Current config has `PathRemovePrefix: /api/identity` — this strips the prefix before forwarding.
- Since the controller route is `api/[controller]` = `api/identity`, the transform must be REMOVED so the full path `/api/identity/...` is forwarded as-is.

## FORBIDDEN Symbols
- `QuickApp.Core.Models.Shop` (Order, Customer, Product, etc.)
- `ApplicationUser.Orders` navigation property
- OpenIddict (any reference)
- Monolith's `ApplicationDbContext`
- `IdentityUser`, `IdentityRole` (use plain entities instead)

## Task DAG & Stacked PR Chain

| Task | Branch | Base | Gate |
|---|---|---|---|
| T0 — Recon + scaffold | `devin/identity-t0-recon` | `main` | Build green |
| T1 — Domain + contracts | `devin/identity-t1-domain` | `devin/identity-t0-recon` | Build green, zero forbidden refs |
| T2 — Persistence + migration | `devin/identity-t2-persistence` | `devin/identity-t1-domain` | Migration applies against PostgreSQL |
| T3 — API + auth | `devin/identity-t3-api` | `devin/identity-t2-persistence` | Build green, endpoints respond, auth enforced |
| T4 — Gateway route | `devin/identity-t4-gateway` | `devin/identity-t3-api` | Gateway forwards to service |
| T5 — E2E smoke test | `devin/identity-t5-e2e` | `devin/identity-t4-gateway` | All assertions pass |

**Merge order**: T0 → T1 → T2 → T3 → T4 → T5 (each PR merged into its base, then the next PR's base is updated).

## NuGet Packages to Add
- `BCrypt.Net-Next` (Identity.Infrastructure or Identity.API)
- `Microsoft.AspNetCore.Authentication.JwtBearer` (Identity.API)
- `System.IdentityModel.Tokens.Jwt` (Identity.API — for token generation)
