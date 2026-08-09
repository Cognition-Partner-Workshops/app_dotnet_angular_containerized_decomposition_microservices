using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests;

/// <summary>
/// Executable contract for the intended Product -> Order -> Notification flow.
/// Assumptions: Product accepts POST /api/product with name, description, and
/// price and returns an id; Order accepts POST /api/order with customerId and
/// items containing productId and quantity and returns id, customerId, and
/// totalAmount; a successful order publishes Shared.Contracts.Events.OrderPlacedEvent.
/// </summary>
public class OrderProductNotificationFlowContractTests
{
    private static readonly Uri OrderUrl = new(
        Environment.GetEnvironmentVariable("ORDER_URL") ?? "http://localhost:5003");
    private static readonly Uri ProductUrl = new(
        Environment.GetEnvironmentVariable("PRODUCT_URL") ?? "http://localhost:5004");
    private static readonly Uri NotificationUrl = new(
        Environment.GetEnvironmentVariable("NOTIFICATION_URL") ?? "http://localhost:5005");

    private static readonly Guid KnownCustomerId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact(Skip = "Blocked: Product.API has no POST /api/product endpoint or product persistence.")]
    public async Task ProductCanBeCreatedForAnOrder()
    {
        using var client = new HttpClient();

        var response = await CreateProduct(client);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductRecord>();
        Assert.NotNull(product);
        Assert.NotEqual(Guid.Empty, product!.Id);
    }

    [Fact(Skip = "Blocked: Product.API and Order.API have no POST endpoints, so an order cannot yet reference a created product.")]
    public async Task OrderCanBePlacedReferencingAProduct()
    {
        using var client = new HttpClient();
        var product = await CreateProductRecord(client);

        var response = await PlaceOrder(client, KnownCustomerId, product.Id);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderRecord>();
        Assert.NotNull(order);
        Assert.NotEqual(Guid.Empty, order!.Id);
        Assert.Equal(KnownCustomerId, order.CustomerId);
        Assert.True(order.TotalAmount > 0);
    }

    [Fact(Skip = "Blocked: Order.API does not publish Shared.Contracts.Events.OrderPlacedEvent after an order is placed.")]
    public async Task PlacedOrderProducesNotificationWithMatchingEventFields()
    {
        using var client = new HttpClient();
        var product = await CreateProductRecord(client);
        var orderResponse = await PlaceOrder(client, KnownCustomerId, product.Id);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderRecord>();

        Assert.NotNull(order);
        var notification = await WaitForNotification(client, order!.Id);

        Assert.Equal(order.Id, notification.OrderId);
        Assert.Equal(order.CustomerId, notification.CustomerId);
        Assert.Equal(order.TotalAmount, notification.OrderTotal);
    }

    [Fact(Skip = "Blocked: Order.API has no product-existence validation and no POST /api/order endpoint.")]
    public async Task OrderRejectsAnUnknownProduct()
    {
        using var client = new HttpClient();
        var response = await PlaceOrder(client, KnownCustomerId, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(Skip = "Blocked: Order.API has no customer validation and no POST /api/order endpoint.")]
    public async Task OrderRejectsAnUnknownCustomer()
    {
        using var client = new HttpClient();
        var product = await CreateProductRecord(client);
        var response = await PlaceOrder(client, Guid.NewGuid(), product.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Task<HttpResponseMessage> CreateProduct(HttpClient client)
    {
        return client.PostAsJsonAsync(
            new Uri(ProductUrl, "/api/product"),
            new
            {
                name = "Integration Widget",
                description = "Product created by the integration contract",
                price = 12.50m
            });
    }

    private static async Task<ProductRecord> CreateProductRecord(HttpClient client)
    {
        var response = await CreateProduct(client);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductRecord>();
        Assert.NotNull(product);
        return product!;
    }

    private static Task<HttpResponseMessage> PlaceOrder(
        HttpClient client,
        Guid customerId,
        Guid productId)
    {
        return client.PostAsJsonAsync(
            new Uri(OrderUrl, "/api/order"),
            new
            {
                customerId,
                items = new[]
                {
                    new { productId, quantity = 2 }
                }
            });
    }

    private static async Task<NotificationRecord> WaitForNotification(
        HttpClient client,
        Guid orderId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var notifications = await client.GetFromJsonAsync<List<NotificationRecord>>(
                new Uri(NotificationUrl, "/api/notification"));
            var notification = notifications?.FirstOrDefault(n => n.OrderId == orderId);
            if (notification is not null)
                return notification;

            await Task.Delay(250);
        }

        throw new Xunit.Sdk.XunitException(
            $"No notification was received for order {orderId} within 10 seconds.");
    }

    private sealed record ProductRecord(Guid Id);

    private sealed record OrderRecord(Guid Id, Guid CustomerId, decimal TotalAmount);

    private sealed record NotificationRecord(
        Guid OrderId,
        Guid CustomerId,
        decimal OrderTotal);
}
