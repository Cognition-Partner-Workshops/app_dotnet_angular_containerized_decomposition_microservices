using Identity.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=identitydb;Username=postgres;Password=postgres";

        var builder = new DbContextOptionsBuilder<IdentityDbContext>();
        builder.UseNpgsql(connectionString,
            b => b.MigrationsAssembly(typeof(IdentityDbContext).Assembly.GetName().Name));
        builder.UseOpenIddict();

        return new IdentityDbContext(builder.Options, new DesignTimeUserIdAccessor());
    }

    private sealed class DesignTimeUserIdAccessor : IUserIdAccessor
    {
        public string? GetCurrentUserId() => "SYSTEM";
    }
}
