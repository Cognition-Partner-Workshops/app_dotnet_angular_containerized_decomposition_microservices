using Microsoft.AspNetCore.Mvc;
using Product.API.Controllers;
using Product.API.ViewModels;
using Product.Domain.Entities;
using Product.Infrastructure.Data;
using Product.Infrastructure.Repositories;
using ProductEntity = Product.Domain.Entities.Product;

namespace Product.Tests;

[Collection(ProductDatabaseCollection.Name)]
public class ProductsControllerTests : IAsyncLifetime
{
    private readonly ProductDatabaseFixture _fixture;

    public ProductsControllerTests(ProductDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private ProductsController CreateController(ProductDbContext context) =>
        new(new ProductRepository(context));

    private async Task<(int CategoryId, int ProductId)> SeedOneAsync()
    {
        await using var context = _fixture.CreateContext();
        var category = new ProductCategory { Name = "Cars" };
        var product = new ProductEntity
        {
            Name = "BMW M6",
            Description = "Fast",
            BuyingPrice = 100m,
            SellingPrice = 150m,
            UnitsInStock = 3,
            IsActive = true,
            ProductCategory = category
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        return (category.Id, product.Id);
    }

    [Fact]
    public async Task GetAll_returns_products_with_category_name()
    {
        var (_, productId) = await SeedOneAsync();

        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).GetAll(CancellationToken.None);

        var products = Assert.IsAssignableFrom<IEnumerable<ProductVM>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).ToList();

        var product = Assert.Single(products);
        Assert.Equal(productId, product.Id);
        Assert.Equal("BMW M6", product.Name);
        Assert.Equal("Cars", product.ProductCategoryName);
        Assert.Equal(150m, product.SellingPrice);
    }

    [Fact]
    public async Task GetById_returns_the_product()
    {
        var (categoryId, productId) = await SeedOneAsync();

        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).GetById(productId, CancellationToken.None);

        var product = Assert.IsType<ProductVM>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(productId, product.Id);
        Assert.Equal(categoryId, product.ProductCategoryId);
    }

    [Fact]
    public async Task GetById_returns_404_for_unknown_id()
    {
        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).GetById(4242, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_persists_the_product_and_returns_201()
    {
        var (categoryId, _) = await SeedOneAsync();

        await using (var context = _fixture.CreateContext())
        {
            var result = await CreateController(context).Create(
                new ProductVM
                {
                    Name = "Nissan Patrol",
                    Description = "A true man's choice",
                    BuyingPrice = 78990m,
                    SellingPrice = 86990m,
                    UnitsInStock = 4,
                    IsActive = true,
                    ProductCategoryId = categoryId
                },
                CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var product = Assert.IsType<ProductVM>(created.Value);
            Assert.True(product.Id > 0);
            Assert.Equal("Cars", product.ProductCategoryName);
        }

        await using var verifyContext = _fixture.CreateContext();
        Assert.Equal(2, verifyContext.Products.Count());
    }

    [Fact]
    public async Task Create_rejects_an_unknown_category()
    {
        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).Create(
            new ProductVM { Name = "Orphan", ProductCategoryId = 999 },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_changes_the_stored_product()
    {
        var (categoryId, productId) = await SeedOneAsync();

        await using (var context = _fixture.CreateContext())
        {
            var result = await CreateController(context).Update(
                productId,
                new ProductVM
                {
                    Name = "BMW M6 Competition",
                    SellingPrice = 200m,
                    UnitsInStock = 1,
                    IsDiscontinued = true,
                    ProductCategoryId = categoryId
                },
                CancellationToken.None);

            var product = Assert.IsType<ProductVM>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Equal("BMW M6 Competition", product.Name);
            Assert.True(product.IsDiscontinued);
        }

        await using var verifyContext = _fixture.CreateContext();
        var stored = await verifyContext.Products.FindAsync(productId);
        Assert.Equal(200m, stored!.SellingPrice);
        Assert.Equal(1, stored.UnitsInStock);
    }

    [Fact]
    public async Task Update_returns_404_for_unknown_id()
    {
        var (categoryId, _) = await SeedOneAsync();

        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).Update(
            4242,
            new ProductVM { Name = "Ghost", ProductCategoryId = categoryId },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_removes_the_product()
    {
        var (_, productId) = await SeedOneAsync();

        await using (var context = _fixture.CreateContext())
        {
            var result = await CreateController(context).Delete(productId, CancellationToken.None);
            Assert.IsType<NoContentResult>(result);
        }

        await using var verifyContext = _fixture.CreateContext();
        Assert.Empty(verifyContext.Products);
    }

    [Fact]
    public async Task Delete_returns_404_for_unknown_id()
    {
        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).Delete(4242, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
