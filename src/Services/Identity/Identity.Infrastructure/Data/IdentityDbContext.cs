using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Data;

/// <summary>
/// Service-owned EF Core context for the Identity microservice (database-per-service).
/// Persists the ASP.NET Core Identity tables (users, roles, claims, logins, tokens)
/// to the service's own Postgres database.
/// </summary>
public class IdentityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }
}
