using Microsoft.EntityFrameworkCore;
using Product.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace Product.Tests;

/// <summary>
/// Runs the tests against a real Postgres via Testcontainers, and falls back to the
/// EF in-memory provider when Docker is not available (CI sandboxes, local dev without Docker).
/// </summary>
public sealed class ProductDatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private readonly string _inMemoryName = $"productdb-{Guid.NewGuid()}";

    public bool UsesPostgres => _container is not null;

    public async Task InitializeAsync()
    {
        var container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("productdb")
            .Build();

        try
        {
            await container.StartAsync();
            _container = container;

            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Testcontainers Postgres unavailable, falling back to the in-memory provider: {ex.Message}");

            await container.DisposeAsync();
            _container = null;

            await using var context = CreateContext();
            await context.Database.EnsureCreatedAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    public ProductDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>();

        if (_container is not null)
            options.UseNpgsql(_container.GetConnectionString());
        else
            options.UseInMemoryDatabase(_inMemoryName);

        return new ProductDbContext(options.Options);
    }

    /// <summary>Empties both tables so each test starts from a known state.</summary>
    public async Task ResetAsync()
    {
        await using var context = CreateContext();

        if (UsesPostgres)
        {
            await context.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE \"AppProducts\", \"AppProductCategories\" RESTART IDENTITY CASCADE");
        }
        else
        {
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ProductDatabaseCollection : ICollectionFixture<ProductDatabaseFixture>
{
    public const string Name = "product-database";
}
