using Customer.API.Controllers;
using Customer.API.ViewModels;
using Customer.Infrastructure.Data;
using Customer.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Customer.Tests;

/// <summary>
/// Covers the CRUD verbs the monolith's CustomerController left unimplemented.
/// </summary>
[Collection(nameof(CustomerDatabaseCollection))]
public class CustomerCrudTests : IAsyncLifetime
{
    private readonly CustomerDatabaseFixture _fixture;

    public CustomerCrudTests(CustomerDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private CustomerController CreateController(CustomerDbContext context) =>
        new(new CustomerRepository(context), NullLogger<CustomerController>.Instance);

    private static CustomerVM NewCustomer() => new()
    {
        Name = "Ada Lovelace",
        Email = "ada@analytical.engine",
        PhoneNumber = "+441234567",
        Address = "12 Analytical Way",
        City = "London",
        Gender = "Female"
    };

    private async Task<CustomerVM> CreateAsync()
    {
        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).Post(NewCustomer());
        return Assert.IsType<CustomerVM>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
    }

    [Fact]
    public async Task Post_creates_a_customer_and_returns_it()
    {
        var created = await CreateAsync();

        Assert.NotEqual(0, created.Id);
        Assert.Equal("Ada Lovelace", created.Name);
        Assert.Equal("Female", created.Gender);
        Assert.Empty(created.Orders!);
    }

    [Fact]
    public async Task Post_rejects_payloads_the_monolith_validator_rejected()
    {
        await using var context = _fixture.CreateContext();
        var controller = CreateController(context);

        var noName = await controller.Post(new CustomerVM { Gender = "Male" });
        var noGender = await controller.Post(new CustomerVM { Name = "No Gender" });
        var badGender = await controller.Post(new CustomerVM { Name = "Bad Gender", Gender = "Martian" });

        Assert.IsType<BadRequestObjectResult>(noName.Result);
        Assert.IsType<BadRequestObjectResult>(noGender.Result);
        Assert.IsType<BadRequestObjectResult>(badGender.Result);
    }

    [Fact]
    public async Task GetById_returns_the_created_customer()
    {
        var created = await CreateAsync();

        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).GetById(created.Id);

        var customer = Assert.IsType<CustomerVM>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(created.Id, customer.Id);
        Assert.Equal("ada@analytical.engine", customer.Email);
    }

    [Fact]
    public async Task GetById_returns_404_for_an_unknown_customer()
    {
        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).GetById(4242);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Put_updates_customer_owned_fields()
    {
        var created = await CreateAsync();

        await using var context = _fixture.CreateContext();
        var update = NewCustomer();
        update.Name = "Ada King";
        update.City = "Ockham";

        var result = await CreateController(context).Put(created.Id, update);

        var updated = Assert.IsType<CustomerVM>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Ada King", updated.Name);
        Assert.Equal("Ockham", updated.City);
    }

    [Fact]
    public async Task Put_returns_404_for_an_unknown_customer()
    {
        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).Put(4242, NewCustomer());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_removes_the_customer()
    {
        var created = await CreateAsync();

        await using var context = _fixture.CreateContext();
        var controller = CreateController(context);

        Assert.IsType<NoContentResult>(await controller.Delete(created.Id));
        Assert.IsType<NotFoundResult>((await controller.GetById(created.Id)).Result);
    }

    [Fact]
    public async Task Delete_returns_404_for_an_unknown_customer()
    {
        await using var context = _fixture.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateController(context).Delete(4242));
    }
}
