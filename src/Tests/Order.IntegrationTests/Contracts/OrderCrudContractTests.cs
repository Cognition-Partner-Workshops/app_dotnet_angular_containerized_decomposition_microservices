using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Order.IntegrationTests.Infrastructure;
using Shared.Contracts.DTOs;

namespace Order.IntegrationTests.Contracts;

/// <summary>
/// HTTP contract tests verifying that the Order microservice honours the same
/// request/response shapes that the QuickApp monolith previously exposed.
///
/// Each test uses an isolated in-memory database so tests are fully independent
/// and do not require a running PostgreSQL instance.
/// </summary>
public class OrderCrudContractTests : IClassFixture<OrderServiceFactory>
{
    private readonly HttpClient _client;

    public OrderCrudContractTests(OrderServiceFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -------------------------------------------------------------------------
    // GET /api/order
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_EmptyStore_Returns200WithEmptyArray()
    {
        var response = await _client.GetAsync("/api/order");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_AfterCreatingOrders_ReturnsAllOrders()
    {
        await OrderTestFixture.CreateOrderAsync(_client, OrderTestFixture.BuildCreateOrderDto(customerId: 10, comments: "Batch A"));
        await OrderTestFixture.CreateOrderAsync(_client, OrderTestFixture.BuildCreateOrderDto(customerId: 11, comments: "Batch B"));

        var response = await _client.GetAsync("/api/order");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        orders.Should().NotBeNull();
        orders!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAll_ResponseContentType_IsApplicationJson()
    {
        var response = await _client.GetAsync("/api/order");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    // -------------------------------------------------------------------------
    // GET /api/order/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingOrder_Returns200WithCorrectShape()
    {
        var created = await OrderTestFixture.CreateOrderAsync(_client,
            OrderTestFixture.BuildCreateOrderDto(customerId: 20, comments: "Shape check", cashierId: "cashier-1", discount: 5m, detailCount: 2));

        var response = await _client.GetAsync($"/api/order/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Id.Should().Be(created.Id);
        order.CustomerId.Should().Be(20);
        order.Comments.Should().Be("Shape check");
        order.CashierId.Should().Be("cashier-1");
        order.Discount.Should().Be(5m);
        order.OrderDetails.Should().HaveCount(2);
        order.CreatedDate.Should().NotBe(default);
        order.UpdatedDate.Should().NotBe(default);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/order/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_OrderDetailShape_MatchesMonolithContract()
    {
        var createDto = OrderTestFixture.BuildCreateOrderDto(customerId: 21, detailCount: 1);
        var created = await OrderTestFixture.CreateOrderAsync(_client, createDto);

        var response = await _client.GetAsync($"/api/order/{created.Id}");
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();

        order.Should().NotBeNull();
        var detail = order!.OrderDetails.First();
        detail.Id.Should().BeGreaterThan(0);
        detail.UnitPrice.Should().BeGreaterThan(0);
        detail.Quantity.Should().BeGreaterThan(0);
        detail.ProductId.Should().BeGreaterThan(0);
        detail.OrderId.Should().Be(created.Id);
    }

    // -------------------------------------------------------------------------
    // POST /api/order
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_ValidPayload_Returns201Created()
    {
        var dto = OrderTestFixture.BuildCreateOrderDto(customerId: 30, comments: "New order");

        var response = await _client.PostAsJsonAsync("/api/order", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_Returns201_WithLocationHeader()
    {
        var dto = OrderTestFixture.BuildCreateOrderDto(customerId: 31);

        var response = await _client.PostAsJsonAsync("/api/order", dto);

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().ToLowerInvariant().Should().Contain("/api/order/");
    }

    [Fact]
    public async Task Create_ResponseBody_ContainsAssignedId()
    {
        var dto = OrderTestFixture.BuildCreateOrderDto(customerId: 32);

        var response = await _client.PostAsJsonAsync("/api/order", dto);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();

        order.Should().NotBeNull();
        order!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_ResponseBody_ReflectsInputFields()
    {
        var dto = new CreateOrderDto(
            Discount: 7.5m,
            Comments: "Contract check",
            CashierId: "cashier-99",
            CustomerId: 33,
            OrderDetails: new List<CreateOrderDetailDto>
            {
                new(UnitPrice: 49.99m, Quantity: 3, Discount: 1.0m, ProductId: 5)
            });

        var response = await _client.PostAsJsonAsync("/api/order", dto);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();

        order.Should().NotBeNull();
        order!.Discount.Should().Be(7.5m);
        order.Comments.Should().Be("Contract check");
        order.CashierId.Should().Be("cashier-99");
        order.CustomerId.Should().Be(33);
        order.OrderDetails.Should().HaveCount(1);

        var detail = order.OrderDetails.First();
        detail.UnitPrice.Should().Be(49.99m);
        detail.Quantity.Should().Be(3);
        detail.Discount.Should().Be(1.0m);
        detail.ProductId.Should().Be(5);
    }

    [Fact]
    public async Task Create_WithNullOptionalFields_Returns201()
    {
        var dto = new CreateOrderDto(
            Discount: 0m,
            Comments: null,
            CashierId: null,
            CustomerId: 34,
            OrderDetails: new List<CreateOrderDetailDto>
            {
                new(UnitPrice: 9.99m, Quantity: 1, Discount: 0m, ProductId: 1)
            });

        var response = await _client.PostAsJsonAsync("/api/order", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Comments.Should().BeNull();
        order.CashierId.Should().BeNull();
    }

    [Fact]
    public async Task Create_WithMultipleOrderDetails_PersistsAll()
    {
        var dto = OrderTestFixture.BuildCreateOrderDto(customerId: 35, detailCount: 3);

        var response = await _client.PostAsJsonAsync("/api/order", dto);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();

        order.Should().NotBeNull();
        order!.OrderDetails.Should().HaveCount(3);
    }

    [Fact]
    public async Task Create_AuditTimestamps_ArePopulated()
    {
        var dto = OrderTestFixture.BuildCreateOrderDto(customerId: 36);

        var response = await _client.PostAsJsonAsync("/api/order", dto);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();

        order.Should().NotBeNull();
        order!.CreatedDate.Should().NotBe(default(DateTime));
        order.UpdatedDate.Should().NotBe(default(DateTime));
    }

    // -------------------------------------------------------------------------
    // PUT /api/order/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingOrder_Returns204NoContent()
    {
        var created = await OrderTestFixture.CreateOrderAsync(_client,
            OrderTestFixture.BuildCreateOrderDto(customerId: 40, comments: "Before update"));

        var updateDto = new UpdateOrderDto(
            Discount: 15m,
            Comments: "After update",
            CashierId: "updated-cashier",
            CustomerId: 40);

        var response = await _client.PutAsJsonAsync($"/api/order/{created.Id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_ExistingOrder_ChangesArePersisted()
    {
        var created = await OrderTestFixture.CreateOrderAsync(_client,
            OrderTestFixture.BuildCreateOrderDto(customerId: 41, comments: "Original"));

        var updateDto = new UpdateOrderDto(
            Discount: 20m,
            Comments: "Updated comment",
            CashierId: "new-cashier",
            CustomerId: 42);

        await _client.PutAsJsonAsync($"/api/order/{created.Id}", updateDto);

        var getResponse = await _client.GetAsync($"/api/order/{created.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<OrderDto>();

        updated.Should().NotBeNull();
        updated!.Discount.Should().Be(20m);
        updated.Comments.Should().Be("Updated comment");
        updated.CashierId.Should().Be("new-cashier");
        updated.CustomerId.Should().Be(42);
    }

    [Fact]
    public async Task Update_NonExistentOrder_Returns404()
    {
        var updateDto = new UpdateOrderDto(
            Discount: 0m,
            Comments: "Ghost",
            CashierId: null,
            CustomerId: 1);

        var response = await _client.PutAsJsonAsync("/api/order/999999", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithNullOptionalFields_Returns204()
    {
        var created = await OrderTestFixture.CreateOrderAsync(_client,
            OrderTestFixture.BuildCreateOrderDto(customerId: 43, cashierId: "original-cashier"));

        var updateDto = new UpdateOrderDto(
            Discount: 0m,
            Comments: null,
            CashierId: null,
            CustomerId: 43);

        var response = await _client.PutAsJsonAsync($"/api/order/{created.Id}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/order/{created.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        updated!.CashierId.Should().BeNull();
        updated.Comments.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // DELETE /api/order/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_ExistingOrder_Returns204NoContent()
    {
        var created = await OrderTestFixture.CreateOrderAsync(_client,
            OrderTestFixture.BuildCreateOrderDto(customerId: 50));

        var response = await _client.DeleteAsync($"/api/order/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ExistingOrder_OrderNoLongerRetrievable()
    {
        var created = await OrderTestFixture.CreateOrderAsync(_client,
            OrderTestFixture.BuildCreateOrderDto(customerId: 51));

        await _client.DeleteAsync($"/api/order/{created.Id}");

        var getResponse = await _client.GetAsync($"/api/order/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistentOrder_Returns404()
    {
        var response = await _client.DeleteAsync("/api/order/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_RemovesOrderDetails_NotJustHeader()
    {
        var dto = OrderTestFixture.BuildCreateOrderDto(customerId: 52, detailCount: 3);
        var created = await OrderTestFixture.CreateOrderAsync(_client, dto);

        await _client.DeleteAsync($"/api/order/{created.Id}");

        // Confirm the order (and its details) is gone
        var getResponse = await _client.GetAsync($"/api/order/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Health check
    // -------------------------------------------------------------------------

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/healthz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // Correlation-ID middleware contract
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Request_WithCorrelationIdHeader_EchoesItInResponse()
    {
        var correlationId = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/order");
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await _client.SendAsync(request);

        response.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue();
        values!.First().Should().Be(correlationId);
    }

    [Fact]
    public async Task Request_WithoutCorrelationIdHeader_ResponseContainsGeneratedCorrelationId()
    {
        var response = await _client.GetAsync("/api/order");

        response.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue();
        values!.First().Should().NotBeNullOrEmpty();
    }
}
