using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Order.Infrastructure.Data;
using Shared.Contracts.DTOs;

namespace Order.IntegrationTests;

public class OrderServiceSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly string DatabaseName = $"TestOrderDb_{Guid.NewGuid()}";

    public OrderServiceSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove all DbContext-related registrations to swap Npgsql for InMemory
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType.FullName != null &&
                                d.ServiceType.FullName.Contains("DbContextOptions"))
                    .ToList();
                foreach (var descriptor in descriptorsToRemove)
                    services.Remove(descriptor);

                // Also remove the DbContext itself so we can re-register it
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(OrderDbContext));
                if (dbContextDescriptor != null)
                    services.Remove(dbContextDescriptor);

                services.AddDbContext<OrderDbContext>(options =>
                    options.UseInMemoryDatabase(DatabaseName));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
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
        var createDto = new CreateOrderDto(
            Discount: 10.0m,
            Comments: "Integration test order",
            CashierId: "test-cashier",
            CustomerId: 1,
            OrderDetails: new List<CreateOrderDetailDto>
            {
                new(UnitPrice: 25.99m, Quantity: 2, Discount: 0, ProductId: 1)
            }
        );

        var response = await _client.PostAsJsonAsync("/api/order", createDto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal("Integration test order", order.Comments);
        Assert.Single(order.OrderDetails);
    }

    [Fact]
    public async Task CreateAndGetOrder_RoundTrip()
    {
        var createDto = new CreateOrderDto(
            Discount: 5.0m,
            Comments: "Round trip test",
            CashierId: null,
            CustomerId: 2,
            OrderDetails: new List<CreateOrderDetailDto>
            {
                new(UnitPrice: 10.00m, Quantity: 1, Discount: 0, ProductId: 1),
                new(UnitPrice: 20.00m, Quantity: 3, Discount: 1.5m, ProductId: 2)
            }
        );

        var createResponse = await _client.PostAsJsonAsync("/api/order", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/order/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(2, fetched.OrderDetails.Count);
    }

    [Fact]
    public async Task UpdateOrder_ReturnsNoContent()
    {
        var createDto = new CreateOrderDto(
            Discount: 0m,
            Comments: "To be updated",
            CashierId: null,
            CustomerId: 3,
            OrderDetails: new List<CreateOrderDetailDto>
            {
                new(UnitPrice: 15.00m, Quantity: 1, Discount: 0, ProductId: 1)
            }
        );

        var createResponse = await _client.PostAsJsonAsync("/api/order", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);

        var updateDto = new UpdateOrderDto(
            Discount: 5.0m,
            Comments: "Updated comment",
            CashierId: "new-cashier",
            CustomerId: 3
        );

        var updateResponse = await _client.PutAsJsonAsync($"/api/order/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteOrder_ReturnsNoContent()
    {
        var createDto = new CreateOrderDto(
            Discount: 0m,
            Comments: "To be deleted",
            CashierId: null,
            CustomerId: 4,
            OrderDetails: new List<CreateOrderDetailDto>
            {
                new(UnitPrice: 5.00m, Quantity: 1, Discount: 0, ProductId: 1)
            }
        );

        var createResponse = await _client.PostAsJsonAsync("/api/order", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync($"/api/order/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/order/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
