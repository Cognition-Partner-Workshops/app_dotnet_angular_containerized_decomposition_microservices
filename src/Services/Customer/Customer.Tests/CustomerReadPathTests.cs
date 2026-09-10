using Customer.API.Controllers;
using Customer.API.ViewModels;
using Customer.Domain.Entities;
using Customer.Infrastructure.Data;
using Customer.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using CustomerEntity = Customer.Domain.Entities.Customer;

namespace Customer.Tests;

/// <summary>
/// Characterizes the read path ported from the monolith's
/// CustomerService.GetAllCustomersData and CustomerController.Get.
/// </summary>
[Collection(nameof(CustomerDatabaseCollection))]
public class CustomerReadPathTests : IAsyncLifetime
{
    private readonly CustomerDatabaseFixture _fixture;

    public CustomerReadPathTests(CustomerDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await using var context = _fixture.CreateContext();
        await CustomerDbSeeder.SeedAsync(context);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private CustomerController CreateController(CustomerDbContext context) =>
        new(new CustomerRepository(context), NullLogger<CustomerController>.Instance);

    [Fact]
    public async Task GetAll_returns_all_seeded_customers_ordered_by_name()
    {
        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).GetAll();

        var customers = Assert.IsAssignableFrom<IEnumerable<CustomerVM>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).ToList();

        Assert.Equal(
            ["Ebenezer Monney", "Itachi Uchiha", "Jane Doe", "John Doe"],
            customers.Select(c => c.Name));
    }

    [Fact]
    public async Task GetAll_projects_the_monolith_CustomerVM_shape()
    {
        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).GetAll();

        var customers = Assert.IsAssignableFrom<IEnumerable<CustomerVM>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).ToList();

        var itachi = customers.Single(c => c.Name == "Itachi Uchiha");
        Assert.Equal("uchiha@narutoverse.com", itachi.Email);
        Assert.Equal("+81123456789", itachi.PhoneNumber);
        Assert.Equal("Some fictional Address, Street 123, Konoha", itachi.Address);
        Assert.Equal("Konoha", itachi.City);
        Assert.Equal("Male", itachi.Gender);
        Assert.NotEqual(0, itachi.Id);
    }

    [Fact]
    public async Task GetAll_exposes_orders_by_id_only()
    {
        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).GetAll();

        var customers = Assert.IsAssignableFrom<IEnumerable<CustomerVM>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).ToList();

        var order = Assert.Single(customers.Single(c => c.Name == "Ebenezer Monney").Orders!);
        Assert.Equal(1, order.Id);
        Assert.Equal(0m, order.Discount);
        Assert.Null(order.Comments);

        Assert.Empty(customers.Single(c => c.Name == "John Doe").Orders!);
    }

    [Fact]
    public async Task GetTopActiveCustomers_ranks_customers_by_order_count()
    {
        await using var context = _fixture.CreateContext();
        var jane = context.Customers.Single(c => c.Name == "Jane Doe");
        jane.OrderRefs.Add(new CustomerOrderRef { OrderId = 10 });
        jane.OrderRefs.Add(new CustomerOrderRef { OrderId = 11 });
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetTopActiveCustomers(2);

        var customers = Assert.IsAssignableFrom<IEnumerable<CustomerVM>>(
            Assert.IsType<OkObjectResult>(result.Result).Value).ToList();

        Assert.Equal(["Jane Doe", "Ebenezer Monney"], customers.Select(c => c.Name));
    }

    [Fact]
    public async Task GetTopActiveCustomers_rejects_non_positive_counts()
    {
        await using var context = _fixture.CreateContext();
        var result = await CreateController(context).GetTopActiveCustomers(0);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Seeding_is_idempotent()
    {
        await using var context = _fixture.CreateContext();
        await CustomerDbSeeder.SeedAsync(context);

        Assert.Equal(4, context.Customers.Count());
    }

    [Fact]
    public async Task Customer_entity_holds_only_customer_owned_fields()
    {
        await using var context = _fixture.CreateContext();
        var entityType = context.Model.FindEntityType(typeof(CustomerEntity))!;

        Assert.All(
            entityType.GetNavigations(),
            n => Assert.Equal(typeof(CustomerOrderRef), n.TargetEntityType.ClrType));
    }
}
