using Customer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Customer.Tests;

/// <summary>
/// Runs the tests against a real Postgres via Testcontainers, falling back to
/// the EF in-memory provider when Docker is unavailable (e.g. in CI).
/// </summary>
public sealed class CustomerDatabaseFixture : IAsyncLifetime
{
    private const string InMemoryDatabaseName = "customer-tests";

    private PostgreSqlContainer? _container;

    public bool UsesPostgres => _container is not null;

    public async Task InitializeAsync()
    {
        var container = new PostgreSqlBuilder("postgres:16-alpine").Build();

        try
        {
            await container.StartAsync();
            _container = container;
        }
        catch (Exception)
        {
            await container.DisposeAsync();
            return;
        }

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    public CustomerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>();

        if (_container is not null)
            options.UseNpgsql(_container.GetConnectionString());
        else
            options.UseInMemoryDatabase(InMemoryDatabaseName);

        return new CustomerDbContext(options.Options);
    }

    /// <summary>
    /// Empties the schema so each test starts from a known state.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var context = CreateContext();

        if (UsesPostgres)
        {
            await context.Database.ExecuteSqlRawAsync(
                """TRUNCATE TABLE "CustomerOrderRefs", "Customers" RESTART IDENTITY CASCADE;""");
        }
        else
        {
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }
    }
}

[CollectionDefinition(nameof(CustomerDatabaseCollection))]
public sealed class CustomerDatabaseCollection : ICollectionFixture<CustomerDatabaseFixture>;
