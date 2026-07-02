using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Cross-service integration tests for the Order → Product → Notification flow.
/// Requires all three services running via Docker Compose.
/// </summary>
[Collection("DockerCompose")]
[TestCaseOrderer("Integration.Tests.PriorityOrderer", "Integration.Tests")]
public class OrderProductNotificationFlowTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _productClient;
    private readonly HttpClient _orderClient;
    private readonly HttpClient _notificationClient;

    public OrderProductNotificationFlowTests(DockerComposeFixture fixture)
    {
        _productClient = fixture.ProductClient;
        _orderClient = fixture.OrderClient;
        _notificationClient = fixture.NotificationClient;
    }

    // ──────────────────────────────────────────────────────────────
    //  Test 1: Create a product via the Product service
    // ──────────────────────────────────────────────────────────────

    [Fact, TestPriority(1)]
    public async Task CreateProduct_ReturnsCreatedWithProductData()
    {
        var request = new
        {
            Name = "Integration Test Widget",
            Description = "A widget created during integration testing",
            Price = 2999m,  // $29.99 in cents
            StockQuantity = 100
        };

        var response = await _productClient.PostAsJsonAsync("/api/product", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Integration Test Widget", product.Name);
        Assert.Equal(2999m, product.Price);
        Assert.Equal(100, product.StockQuantity);
    }

    [Fact, TestPriority(2)]
    public async Task GetProduct_ReturnsCreatedProduct()
    {
        // Create a product first
        var createResponse = await _productClient.PostAsJsonAsync("/api/product", new
        {
            Name = "Retrievable Widget",
            Description = "For GET test",
            Price = 1500m,
            StockQuantity = 50
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(created);

        // Retrieve it
        var getResponse = await _productClient.GetAsync($"/api/product/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var product = await getResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);
        Assert.Equal(created.Id, product.Id);
        Assert.Equal("Retrievable Widget", product.Name);
    }

    // ──────────────────────────────────────────────────────────────
    //  Test 2: Place an order referencing the product
    // ──────────────────────────────────────────────────────────────

    [Fact, TestPriority(3)]
    public async Task PlaceOrder_WithValidProduct_ReturnsCreatedOrder()
    {
        // Step 1: Create a product
        var productResponse = await _productClient.PostAsJsonAsync("/api/product", new
        {
            Name = "Orderable Widget",
            Description = "Product for order flow test",
            Price = 5000m,
            StockQuantity = 200
        });
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);

        var customerId = Guid.NewGuid();

        // Step 2: Place an order
        var orderResponse = await _orderClient.PostAsJsonAsync("/api/order", new
        {
            CustomerId = customerId,
            ProductId = product.Id,
            Quantity = 3
        });

        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

        var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(order);
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(product.Id, order.ProductId);
        Assert.Equal("Orderable Widget", order.ProductName);
        Assert.Equal(3, order.Quantity);
        Assert.Equal(5000m, order.UnitPrice);
        Assert.Equal(15000m, order.TotalAmount); // 5000 * 3
    }

    [Fact, TestPriority(4)]
    public async Task GetOrder_ReturnsPlacedOrder()
    {
        // Create product + order
        var productResponse = await _productClient.PostAsJsonAsync("/api/product", new
        {
            Name = "GetOrder Widget",
            Price = 1000m,
            StockQuantity = 10
        });
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);

        var orderResponse = await _orderClient.PostAsJsonAsync("/api/order", new
        {
            CustomerId = Guid.NewGuid(),
            ProductId = product.Id,
            Quantity = 1
        });
        var created = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(created);

        // Retrieve it
        var getResponse = await _orderClient.GetAsync($"/api/order/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var order = await getResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(order);
        Assert.Equal(created.Id, order.Id);
    }

    // ──────────────────────────────────────────────────────────────
    //  Test 3: Verify the Notification service received the event
    // ──────────────────────────────────────────────────────────────

    [Fact, TestPriority(5)]
    public async Task PlaceOrder_TriggersNotification_WithCorrectData()
    {
        // Step 1: Create a product
        var productResponse = await _productClient.PostAsJsonAsync("/api/product", new
        {
            Name = "Notification Widget",
            Description = "Product for notification flow test",
            Price = 7500m,
            StockQuantity = 50
        });
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);

        var customerId = Guid.NewGuid();

        // Step 2: Place an order (this should trigger notification)
        var orderResponse = await _orderClient.PostAsJsonAsync("/api/order", new
        {
            CustomerId = customerId,
            ProductId = product.Id,
            Quantity = 2
        });
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(order);

        // Step 3: Poll notifications until the order-placed notification appears
        var notification = await PollForNotification(order.Id, timeout: TimeSpan.FromSeconds(15));

        Assert.NotNull(notification);
        Assert.Equal(order.Id, notification.OrderId);
        Assert.Equal(customerId, notification.CustomerId);
        Assert.Equal(15000m, notification.OrderTotal); // 7500 * 2
        Assert.Equal(1, notification.Status); // NotificationStatus.Rendered
        Assert.NotNull(notification.RenderedSubject);
        Assert.Contains("Order Confirmed", notification.RenderedSubject);
    }

    [Fact, TestPriority(6)]
    public async Task Notification_HasRenderedEmailPreview()
    {
        // Create product + order
        var productResponse = await _productClient.PostAsJsonAsync("/api/product", new
        {
            Name = "Preview Widget",
            Price = 4200m,
            StockQuantity = 10
        });
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);

        var orderResponse = await _orderClient.PostAsJsonAsync("/api/order", new
        {
            CustomerId = Guid.NewGuid(),
            ProductId = product.Id,
            Quantity = 1
        });
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(order);

        // Find the notification
        var notification = await PollForNotification(order.Id, timeout: TimeSpan.FromSeconds(15));
        Assert.NotNull(notification);

        // Fetch the rendered preview
        var previewResponse = await _notificationClient.GetAsync($"/api/notification/{notification.Id}/preview");
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);

        var html = await previewResponse.Content.ReadAsStringAsync();
        Assert.Contains("Order Confirmed", html);
        Assert.Contains("<!DOCTYPE html>", html);
    }

    // ──────────────────────────────────────────────────────────────
    //  Test 4: Order service validates product existence
    // ──────────────────────────────────────────────────────────────

    [Fact, TestPriority(7)]
    public async Task PlaceOrder_WithNonExistentProduct_ReturnsBadRequest()
    {
        var fakeProductId = Guid.NewGuid();

        var response = await _orderClient.PostAsJsonAsync("/api/order", new
        {
            CustomerId = Guid.NewGuid(),
            ProductId = fakeProductId,
            Quantity = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("does not exist", body);
    }

    [Fact, TestPriority(8)]
    public async Task PlaceOrder_ProductValidation_UsesCorrectProductData()
    {
        // Create a product with a specific price
        var productResponse = await _productClient.PostAsJsonAsync("/api/product", new
        {
            Name = "Validation Widget",
            Price = 3333m,
            StockQuantity = 5
        });
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);

        // Place an order — total should be computed from the actual product price
        var orderResponse = await _orderClient.PostAsJsonAsync("/api/order", new
        {
            CustomerId = Guid.NewGuid(),
            ProductId = product.Id,
            Quantity = 4
        });
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        Assert.NotNull(order);

        Assert.Equal(3333m, order.UnitPrice);
        Assert.Equal(13332m, order.TotalAmount); // 3333 * 4
        Assert.Equal("Validation Widget", order.ProductName);
    }

    // ──────────────────────────────────────────────────────────────
    //  Test 5: Error scenarios
    // ──────────────────────────────────────────────────────────────

    [Fact, TestPriority(9)]
    public async Task PlaceOrder_WithEmptyCustomerId_ReturnsBadRequest()
    {
        // Create a valid product first
        var productResponse = await _productClient.PostAsJsonAsync("/api/product", new
        {
            Name = "Error Test Widget",
            Price = 100m,
            StockQuantity = 10
        });
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);

        var response = await _orderClient.PostAsJsonAsync("/api/order", new
        {
            CustomerId = Guid.Empty,
            ProductId = product.Id,
            Quantity = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("customer", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact, TestPriority(10)]
    public async Task PlaceOrder_WithZeroQuantity_ReturnsBadRequest()
    {
        var productResponse = await _productClient.PostAsJsonAsync("/api/product", new
        {
            Name = "Quantity Test Widget",
            Price = 100m,
            StockQuantity = 10
        });
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);

        var response = await _orderClient.PostAsJsonAsync("/api/order", new
        {
            CustomerId = Guid.NewGuid(),
            ProductId = product.Id,
            Quantity = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Quantity", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact, TestPriority(11)]
    public async Task PlaceOrder_WithNegativeQuantity_ReturnsBadRequest()
    {
        var productResponse = await _productClient.PostAsJsonAsync("/api/product", new
        {
            Name = "Negative Qty Widget",
            Price = 100m,
            StockQuantity = 10
        });
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);

        var response = await _orderClient.PostAsJsonAsync("/api/order", new
        {
            CustomerId = Guid.NewGuid(),
            ProductId = product.Id,
            Quantity = -5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact, TestPriority(12)]
    public async Task PlaceOrder_WithEmptyProductId_ReturnsBadRequest()
    {
        var response = await _orderClient.PostAsJsonAsync("/api/order", new
        {
            CustomerId = Guid.NewGuid(),
            ProductId = Guid.Empty,
            Quantity = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("product", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact, TestPriority(13)]
    public async Task GetNonExistentOrder_ReturnsNotFound()
    {
        var response = await _orderClient.GetAsync($"/api/order/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact, TestPriority(14)]
    public async Task GetNonExistentProduct_ReturnsNotFound()
    {
        var response = await _productClient.GetAsync($"/api/product/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact, TestPriority(15)]
    public async Task GetNonExistentNotification_ReturnsNotFound()
    {
        var response = await _notificationClient.GetAsync($"/api/notification/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact, TestPriority(16)]
    public async Task FullEndToEndFlow_MultipleOrders_CreateDistinctNotifications()
    {
        // Create two products
        var product1Response = await _productClient.PostAsJsonAsync("/api/product", new
        {
            Name = "E2E Widget Alpha",
            Price = 1000m,
            StockQuantity = 50
        });
        var product1 = await product1Response.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);

        var product2Response = await _productClient.PostAsJsonAsync("/api/product", new
        {
            Name = "E2E Widget Beta",
            Price = 2000m,
            StockQuantity = 30
        });
        var product2 = await product2Response.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);

        Assert.NotNull(product1);
        Assert.NotNull(product2);

        var customer1 = Guid.NewGuid();
        var customer2 = Guid.NewGuid();

        // Place two orders for different products/customers
        var order1Response = await _orderClient.PostAsJsonAsync("/api/order", new
        {
            CustomerId = customer1,
            ProductId = product1.Id,
            Quantity = 2
        });
        var order1 = await order1Response.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);

        var order2Response = await _orderClient.PostAsJsonAsync("/api/order", new
        {
            CustomerId = customer2,
            ProductId = product2.Id,
            Quantity = 5
        });
        var order2 = await order2Response.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);

        Assert.NotNull(order1);
        Assert.NotNull(order2);

        // Verify each order generated its own notification
        var notif1 = await PollForNotification(order1.Id, timeout: TimeSpan.FromSeconds(15));
        var notif2 = await PollForNotification(order2.Id, timeout: TimeSpan.FromSeconds(15));

        Assert.NotNull(notif1);
        Assert.NotNull(notif2);

        Assert.NotEqual(notif1.Id, notif2.Id);
        Assert.Equal(order1.Id, notif1.OrderId);
        Assert.Equal(order2.Id, notif2.OrderId);
        Assert.Equal(2000m, notif1.OrderTotal);   // 1000 * 2
        Assert.Equal(10000m, notif2.OrderTotal);  // 2000 * 5
    }

    // ──────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task<NotificationResponse?> PollForNotification(Guid orderId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            var response = await _notificationClient.GetAsync("/api/notification");
            if (response.IsSuccessStatusCode)
            {
                var notifications = await response.Content
                    .ReadFromJsonAsync<NotificationResponse[]>(JsonOptions);

                var match = notifications?.FirstOrDefault(n => n.OrderId == orderId);
                if (match is not null)
                    return match;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return null;
    }
}

// ──────────────────────────────────────────────────────────────
//  Response DTOs for deserialization
// ──────────────────────────────────────────────────────────────

public record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    DateTime CreatedAt);

public record OrderResponse(
    Guid Id,
    Guid CustomerId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    int Status,
    DateTime CreatedAt);

public record NotificationResponse(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    decimal OrderTotal,
    int Type,
    int Status,
    string? RenderedSubject,
    DateTime CreatedAt,
    DateTime? SentAt);
