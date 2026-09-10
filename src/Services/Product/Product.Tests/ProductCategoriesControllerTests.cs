using Microsoft.AspNetCore.Mvc;
using Product.API.Controllers;
using Product.API.ViewModels;
using Product.Domain.Entities;
using Product.Infrastructure.Data;
using Product.Infrastructure.Repositories;

namespace Product.Tests;

[Collection(ProductDatabaseCollection.Name)]
public class ProductCategoriesControllerTests : IAsyncLifetime
{
    private readonly ProductDatabaseFixture _fixture;

    public ProductCategoriesControllerTests(ProductDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private ProductCategoriesController CreateController(ProductDbContext context) =>
        new(new ProductCategoryRepository(context));

    private async Task<int> SeedOneAsync()
    {
        await using var context = _fixture.CreateContext();
        var category = new ProductCategory { Name = "Cars", Description = "Motor vehicles" };
        context.ProductCategories.Add(category);
        await context.SaveChangesAsync();
        return category.Id;
    }

    [Fact]
    public async Task GetAll_returns_categories()
    {
        var categoryId = await SeedOneAsync();

        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).GetAll(CancellationToken.None);

        var categories = Assert.IsAssignableFrom<IEnumerable<ProductCategoryVM>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).ToList();

        var category = Assert.Single(categories);
        Assert.Equal(categoryId, category.Id);
        Assert.Equal("Cars", category.Name);
    }

    [Fact]
    public async Task GetById_returns_the_category_and_404_when_missing()
    {
        var categoryId = await SeedOneAsync();

        await using var context = _fixture.CreateContext();
        var controller = CreateController(context);

        var found = await controller.GetById(categoryId, CancellationToken.None);
        Assert.Equal("Cars", Assert.IsType<ProductCategoryVM>(
            Assert.IsType<OkObjectResult>(found.Result).Value).Name);

        var missing = await controller.GetById(4242, CancellationToken.None);
        Assert.IsType<NotFoundResult>(missing.Result);
    }

    [Fact]
    public async Task Create_persists_the_category_and_returns_201()
    {
        await using (var context = _fixture.CreateContext())
        {
            var result = await CreateController(context).Create(
                new ProductCategoryVM { Name = "Bikes", Description = "Two wheels" },
                CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.True(Assert.IsType<ProductCategoryVM>(created.Value).Id > 0);
        }

        await using var verifyContext = _fixture.CreateContext();
        Assert.Single(verifyContext.ProductCategories);
    }

    [Fact]
    public async Task Update_changes_the_stored_category()
    {
        var categoryId = await SeedOneAsync();

        await using (var context = _fixture.CreateContext())
        {
            var result = await CreateController(context).Update(
                categoryId,
                new ProductCategoryVM { Name = "Vehicles", Description = "Renamed" },
                CancellationToken.None);

            Assert.Equal("Vehicles", Assert.IsType<ProductCategoryVM>(
                Assert.IsType<OkObjectResult>(result.Result).Value).Name);
        }

        await using var verifyContext = _fixture.CreateContext();
        Assert.Equal("Renamed", (await verifyContext.ProductCategories.FindAsync(categoryId))!.Description);
    }

    [Fact]
    public async Task Update_returns_404_for_unknown_id()
    {
        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).Update(
            4242,
            new ProductCategoryVM { Name = "Ghost" },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_removes_the_category_and_404s_when_missing()
    {
        var categoryId = await SeedOneAsync();

        await using var context = _fixture.CreateContext();
        var controller = CreateController(context);

        Assert.IsType<NoContentResult>(await controller.Delete(categoryId, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.Delete(categoryId, CancellationToken.None));
    }
}
