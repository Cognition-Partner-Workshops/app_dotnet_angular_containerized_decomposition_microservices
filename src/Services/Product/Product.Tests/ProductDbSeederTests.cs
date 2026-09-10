using Microsoft.EntityFrameworkCore;
using Product.Infrastructure.Data;

namespace Product.Tests;

[Collection(ProductDatabaseCollection.Name)]
public class ProductDbSeederTests : IAsyncLifetime
{
    private readonly ProductDatabaseFixture _fixture;

    public ProductDbSeederTests(ProductDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Seeding_populates_the_catalog_and_is_idempotent()
    {
        await using (var context = _fixture.CreateContext())
        {
            await ProductDbSeeder.SeedAsync(context);
            await ProductDbSeeder.SeedAsync(context);
        }

        await using var verifyContext = _fixture.CreateContext();
        Assert.Equal(3, await verifyContext.Products.CountAsync());
        Assert.Equal(2, await verifyContext.ProductCategories.CountAsync());
        Assert.All(
            await verifyContext.Products.Include(p => p.ProductCategory).ToListAsync(),
            p => Assert.NotNull(p.ProductCategory));
    }
}
