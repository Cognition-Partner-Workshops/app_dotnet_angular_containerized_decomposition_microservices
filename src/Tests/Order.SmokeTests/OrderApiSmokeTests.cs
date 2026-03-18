using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.Infrastructure.Data;
using Shared.Contracts.DTOs;

namespace Order.SmokeTests;

public class OrderApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrderApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<OrderDbContext>(options =>
                    options.UseInMemoryDatabase("OrderSmokeTestDb"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/order");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderById_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/order/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_ReturnsCreated()
    {
        var request = new CreateOrderRequest(
            CustomerId: 1,
            CashierId: "admin",
            Discount: 10.00m,
            Comments: "Smoke test order",
            OrderDetails:
            [
                new CreateOrderDetailRequest(
                    ProductId: 1,
                    ProductName: "Test Product",
                    UnitPrice: 99.99m,
                    Quantity: 2,
                    Discount: 0m
                )
            ]
        );

        var response = await _client.PostAsJsonAsync("/api/order", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);
        Assert.Equal(1, created.CustomerId);
        Assert.Equal("Smoke test order", created.Comments);
        Assert.Single(created.OrderDetails);
    }

    [Fact]
    public async Task CreateAndGetOrder_RoundTrip()
    {
        var request = new CreateOrderRequest(
            CustomerId: 2,
            CashierId: "user",
            Discount: 5.00m,
            Comments: "Round trip test",
            OrderDetails:
            [
                new CreateOrderDetailRequest(
                    ProductId: 10,
                    ProductName: "Widget",
                    UnitPrice: 25.00m,
                    Quantity: 3,
                    Discount: 1.00m
                )
            ]
        );

        var createResponse = await _client.PostAsJsonAsync("/api/order", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/order/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(2, fetched.CustomerId);
    }

    [Fact]
    public async Task UpdateOrder_ReturnsOk()
    {
        // Create first
        var createRequest = new CreateOrderRequest(
            CustomerId: 3,
            CashierId: null,
            Discount: 0m,
            Comments: "Before update",
            OrderDetails:
            [
                new CreateOrderDetailRequest(
                    ProductId: 5,
                    ProductName: "Gadget",
                    UnitPrice: 50.00m,
                    Quantity: 1,
                    Discount: 0m
                )
            ]
        );

        var createResponse = await _client.PostAsJsonAsync("/api/order", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);

        // Update
        var updateRequest = new UpdateOrderRequest(Discount: 15.00m, Comments: "After update");
        var updateResponse = await _client.PutAsJsonAsync($"/api/order/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(updated);
        Assert.Equal("After update", updated.Comments);
        Assert.Equal(15.00m, updated.Discount);
    }

    [Fact]
    public async Task DeleteOrder_ReturnsNoContent()
    {
        // Create first
        var createRequest = new CreateOrderRequest(
            CustomerId: 4,
            CashierId: null,
            Discount: 0m,
            Comments: "To be deleted",
            OrderDetails:
            [
                new CreateOrderDetailRequest(
                    ProductId: 7,
                    ProductName: "Temp",
                    UnitPrice: 10.00m,
                    Quantity: 1,
                    Discount: 0m
                )
            ]
        );

        var createResponse = await _client.PostAsJsonAsync("/api/order", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/api/order/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify deleted
        var getResponse = await _client.GetAsync($"/api/order/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
