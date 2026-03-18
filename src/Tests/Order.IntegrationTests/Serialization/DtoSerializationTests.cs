using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Order.IntegrationTests.Infrastructure;
using Shared.Contracts.DTOs;
using Shared.Contracts.Events;

namespace Order.IntegrationTests.Serialization;

/// <summary>
/// Verifies that all shared DTOs and events defined in Shared.Contracts
/// survive a full JSON serialization → deserialization roundtrip without
/// data loss or type coercion errors.
///
/// These tests guard the HTTP contract between the QuickApp monolith and the
/// Order microservice: if a DTO changes shape, these tests will catch it
/// before the services diverge.
/// </summary>
public class DtoSerializationTests : IClassFixture<OrderServiceFactory>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DtoSerializationTests(OrderServiceFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -------------------------------------------------------------------------
    // CreateOrderDto roundtrip
    // -------------------------------------------------------------------------

    [Fact]
    public void CreateOrderDto_SerializeDeserialize_PreservesAllFields()
    {
        var original = new CreateOrderDto(
            Discount: 12.50m,
            Comments: "Serialization test",
            CashierId: "cashier-serial",
            CustomerId: 999,
            OrderDetails: new List<CreateOrderDetailDto>
            {
                new(UnitPrice: 19.99m, Quantity: 4, Discount: 0.5m, ProductId: 7),
                new(UnitPrice: 5.00m,  Quantity: 1, Discount: 0m,   ProductId: 8)
            });

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreateOrderDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Discount.Should().Be(original.Discount);
        deserialized.Comments.Should().Be(original.Comments);
        deserialized.CashierId.Should().Be(original.CashierId);
        deserialized.CustomerId.Should().Be(original.CustomerId);
        deserialized.OrderDetails.Should().HaveCount(2);

        var firstDetail = deserialized.OrderDetails.First();
        firstDetail.UnitPrice.Should().Be(19.99m);
        firstDetail.Quantity.Should().Be(4);
        firstDetail.Discount.Should().Be(0.5m);
        firstDetail.ProductId.Should().Be(7);
    }

    [Fact]
    public void CreateOrderDto_WithNullOptionalFields_SerializesAndDeserializesCorrectly()
    {
        var original = new CreateOrderDto(
            Discount: 0m,
            Comments: null,
            CashierId: null,
            CustomerId: 1,
            OrderDetails: new List<CreateOrderDetailDto>
            {
                new(UnitPrice: 1.00m, Quantity: 1, Discount: 0m, ProductId: 1)
            });

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreateOrderDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Comments.Should().BeNull();
        deserialized.CashierId.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // CreateOrderDetailDto roundtrip
    // -------------------------------------------------------------------------

    [Fact]
    public void CreateOrderDetailDto_SerializeDeserialize_PreservesAllFields()
    {
        var original = new CreateOrderDetailDto(
            UnitPrice: 99.95m,
            Quantity: 10,
            Discount: 2.5m,
            ProductId: 42);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreateOrderDetailDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.UnitPrice.Should().Be(original.UnitPrice);
        deserialized.Quantity.Should().Be(original.Quantity);
        deserialized.Discount.Should().Be(original.Discount);
        deserialized.ProductId.Should().Be(original.ProductId);
    }

    [Fact]
    public void CreateOrderDetailDto_ZeroDiscount_RoundTripsCorrectly()
    {
        var original = new CreateOrderDetailDto(
            UnitPrice: 10.00m,
            Quantity: 1,
            Discount: 0m,
            ProductId: 1);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreateOrderDetailDto>(json, JsonOptions);

        deserialized!.Discount.Should().Be(0m);
    }

    // -------------------------------------------------------------------------
    // UpdateOrderDto roundtrip
    // -------------------------------------------------------------------------

    [Fact]
    public void UpdateOrderDto_SerializeDeserialize_PreservesAllFields()
    {
        var original = new UpdateOrderDto(
            Discount: 3.75m,
            Comments: "Updated via serialization test",
            CashierId: "cashier-upd",
            CustomerId: 77);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<UpdateOrderDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Discount.Should().Be(original.Discount);
        deserialized.Comments.Should().Be(original.Comments);
        deserialized.CashierId.Should().Be(original.CashierId);
        deserialized.CustomerId.Should().Be(original.CustomerId);
    }

    [Fact]
    public void UpdateOrderDto_WithNullOptionalFields_RoundTripsCorrectly()
    {
        var original = new UpdateOrderDto(
            Discount: 0m,
            Comments: null,
            CashierId: null,
            CustomerId: 1);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<UpdateOrderDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Comments.Should().BeNull();
        deserialized.CashierId.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // OrderDto roundtrip (response shape from Order service)
    // -------------------------------------------------------------------------

    [Fact]
    public void OrderDto_SerializeDeserialize_PreservesAllFields()
    {
        var now = DateTime.UtcNow;
        var original = new OrderDto(
            Id: 1,
            Discount: 5.0m,
            Comments: "Response DTO test",
            CashierId: "cashier-resp",
            CustomerId: 55,
            CreatedDate: now,
            UpdatedDate: now,
            OrderDetails: new List<OrderDetailDto>
            {
                new(Id: 1, UnitPrice: 25.00m, Quantity: 2, Discount: 0m, ProductId: 3, OrderId: 1)
            });

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<OrderDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(original.Id);
        deserialized.Discount.Should().Be(original.Discount);
        deserialized.Comments.Should().Be(original.Comments);
        deserialized.CashierId.Should().Be(original.CashierId);
        deserialized.CustomerId.Should().Be(original.CustomerId);
        deserialized.OrderDetails.Should().HaveCount(1);
    }

    [Fact]
    public void OrderDto_EmptyOrderDetails_RoundTripsCorrectly()
    {
        var now = DateTime.UtcNow;
        var original = new OrderDto(
            Id: 2,
            Discount: 0m,
            Comments: null,
            CashierId: null,
            CustomerId: 1,
            CreatedDate: now,
            UpdatedDate: now,
            OrderDetails: new List<OrderDetailDto>());

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<OrderDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.OrderDetails.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // OrderDetailDto roundtrip
    // -------------------------------------------------------------------------

    [Fact]
    public void OrderDetailDto_SerializeDeserialize_PreservesAllFields()
    {
        var original = new OrderDetailDto(
            Id: 10,
            UnitPrice: 49.99m,
            Quantity: 3,
            Discount: 1.5m,
            ProductId: 20,
            OrderId: 5);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<OrderDetailDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(original.Id);
        deserialized.UnitPrice.Should().Be(original.UnitPrice);
        deserialized.Quantity.Should().Be(original.Quantity);
        deserialized.Discount.Should().Be(original.Discount);
        deserialized.ProductId.Should().Be(original.ProductId);
        deserialized.OrderId.Should().Be(original.OrderId);
    }

    // -------------------------------------------------------------------------
    // OrderPlacedEvent roundtrip
    // -------------------------------------------------------------------------

    [Fact]
    public void OrderPlacedEvent_SerializeDeserialize_PreservesAllFields()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var placedAt = DateTime.UtcNow;

        var original = new OrderPlacedEvent(
            OrderId: orderId,
            CustomerId: customerId,
            TotalAmount: 149.97m,
            PlacedAt: placedAt);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<OrderPlacedEvent>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.OrderId.Should().Be(original.OrderId);
        deserialized.CustomerId.Should().Be(original.CustomerId);
        deserialized.TotalAmount.Should().Be(original.TotalAmount);
        // DateTime precision may vary slightly; compare to the second
        deserialized.PlacedAt.Should().BeCloseTo(original.PlacedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void OrderPlacedEvent_ZeroAmount_RoundTripsCorrectly()
    {
        var original = new OrderPlacedEvent(
            OrderId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            TotalAmount: 0m,
            PlacedAt: DateTime.UtcNow);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<OrderPlacedEvent>(json, JsonOptions);

        deserialized!.TotalAmount.Should().Be(0m);
    }

    // -------------------------------------------------------------------------
    // ServiceHealthDto roundtrip
    // -------------------------------------------------------------------------

    [Fact]
    public void ServiceHealthDto_SerializeDeserialize_PreservesAllFields()
    {
        var checkedAt = DateTime.UtcNow;
        var original = new ServiceHealthDto(
            ServiceName: "Order",
            Status: "Healthy",
            CheckedAt: checkedAt);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ServiceHealthDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.ServiceName.Should().Be(original.ServiceName);
        deserialized.Status.Should().Be(original.Status);
        deserialized.CheckedAt.Should().BeCloseTo(original.CheckedAt, TimeSpan.FromSeconds(1));
    }

    // -------------------------------------------------------------------------
    // HTTP wire-format tests: verify the Order service produces valid JSON
    // that the monolith can deserialize using its own JsonSerializerOptions
    // -------------------------------------------------------------------------

    [Fact]
    public async Task OrderService_CreateResponse_IsValidJsonDeserializableAsOrderDto()
    {
        var dto = OrderTestFixture.BuildCreateOrderDto(customerId: 500);

        var response = await _client.PostAsJsonAsync("/api/order", dto);
        response.EnsureSuccessStatusCode();

        var rawJson = await response.Content.ReadAsStringAsync();
        rawJson.Should().NotBeNullOrEmpty();

        // Deserialize using case-insensitive options (as the monolith would)
        var order = JsonSerializer.Deserialize<OrderDto>(rawJson, JsonOptions);
        order.Should().NotBeNull();
        order!.Id.Should().BeGreaterThan(0);
        order.CustomerId.Should().Be(500);
    }

    [Fact]
    public async Task OrderService_GetAllResponse_IsValidJsonDeserializableAsOrderDtoList()
    {
        // Seed at least one order
        await OrderTestFixture.CreateOrderAsync(_client, OrderTestFixture.BuildCreateOrderDto(customerId: 501));

        var response = await _client.GetAsync("/api/order");
        response.EnsureSuccessStatusCode();

        var rawJson = await response.Content.ReadAsStringAsync();
        rawJson.Should().NotBeNullOrEmpty();

        var orders = JsonSerializer.Deserialize<List<OrderDto>>(rawJson, JsonOptions);
        orders.Should().NotBeNull();
        orders!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OrderService_CreateOrderDto_CanBePostedAsJsonString()
    {
        // Simulate the monolith serializing a DTO and sending it as a raw JSON string
        var dto = new CreateOrderDto(
            Discount: 0m,
            Comments: "Raw JSON post",
            CashierId: null,
            CustomerId: 502,
            OrderDetails: new List<CreateOrderDetailDto>
            {
                new(UnitPrice: 9.99m, Quantity: 1, Discount: 0m, ProductId: 1)
            });

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/order", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Comments.Should().Be("Raw JSON post");
    }

    // -------------------------------------------------------------------------
    // Decimal precision tests (critical for financial data)
    // -------------------------------------------------------------------------

    [Fact]
    public void CreateOrderDto_DecimalPrecision_IsPreservedInRoundtrip()
    {
        var original = new CreateOrderDto(
            Discount: 0.01m,
            Comments: "Precision test",
            CashierId: null,
            CustomerId: 1,
            OrderDetails: new List<CreateOrderDetailDto>
            {
                new(UnitPrice: 9999.99m, Quantity: 1, Discount: 0.01m, ProductId: 1)
            });

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreateOrderDto>(json, JsonOptions);

        deserialized!.Discount.Should().Be(0.01m);
        deserialized.OrderDetails.First().UnitPrice.Should().Be(9999.99m);
        deserialized.OrderDetails.First().Discount.Should().Be(0.01m);
    }

    [Fact]
    public async Task OrderService_DecimalValues_SurviveHttpRoundtrip()
    {
        var dto = new CreateOrderDto(
            Discount: 0.01m,
            Comments: "Decimal precision",
            CashierId: null,
            CustomerId: 503,
            OrderDetails: new List<CreateOrderDetailDto>
            {
                new(UnitPrice: 1234.56m, Quantity: 2, Discount: 0.99m, ProductId: 1)
            });

        var created = await OrderTestFixture.CreateOrderAsync(_client, dto);

        var getResponse = await _client.GetAsync($"/api/order/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<OrderDto>();

        fetched.Should().NotBeNull();
        fetched!.Discount.Should().Be(0.01m);
        fetched.OrderDetails.First().UnitPrice.Should().Be(1234.56m);
        fetched.OrderDetails.First().Discount.Should().Be(0.99m);
    }
}
