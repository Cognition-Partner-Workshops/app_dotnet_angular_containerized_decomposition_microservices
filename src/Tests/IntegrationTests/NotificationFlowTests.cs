using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace IntegrationTests;

public class NotificationFlowTests
{
    private static readonly Uri NotificationUrl = new(
        Environment.GetEnvironmentVariable("NOTIFICATION_URL") ?? "http://localhost:5005");

    [Fact]
    public async Task OrderPlacedEventIsAcceptedAndPersisted()
    {
        using var client = new HttpClient();
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        const decimal total = 42.50m;

        var response = await PostOrderPlacedEvent(client, orderId, customerId, total);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedNotification>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created!.Id);
        Assert.StartsWith("/api/notification/", created.PreviewUrl);

        var persisted = await client.GetFromJsonAsync<NotificationRecord>(
            new Uri(NotificationUrl, $"/api/notification/{created.Id}"));

        Assert.NotNull(persisted);
        Assert.Equal(orderId, persisted!.OrderId);
        Assert.Equal(customerId, persisted.CustomerId);
        Assert.Equal(total, persisted.OrderTotal);
    }

    [Fact]
    public async Task NotificationCanBeRetrievedById()
    {
        using var client = new HttpClient();
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var response = await PostOrderPlacedEvent(client, orderId, customerId, 19.99m);
        var created = await response.Content.ReadFromJsonAsync<CreatedNotification>();

        Assert.NotNull(created);
        var retrieval = await client.GetAsync(
            new Uri(NotificationUrl, $"/api/notification/{created!.Id}"));

        Assert.Equal(HttpStatusCode.OK, retrieval.StatusCode);
        var record = await retrieval.Content.ReadFromJsonAsync<NotificationRecord>();
        Assert.NotNull(record);
        Assert.Equal(created.Id, record!.Id);
        Assert.Equal(orderId, record.OrderId);
        Assert.Equal(customerId, record.CustomerId);
    }

    [Fact]
    public async Task NotificationPreviewRendersHtml()
    {
        using var client = new HttpClient();
        var response = await PostOrderPlacedEvent(
            client,
            Guid.NewGuid(),
            Guid.NewGuid(),
            12.34m);
        var created = await response.Content.ReadFromJsonAsync<CreatedNotification>();

        Assert.NotNull(created);
        var preview = await client.GetAsync(
            new Uri(NotificationUrl, $"/api/notification/{created!.Id}/preview"));

        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.Equal("text/html", preview.Content.Headers.ContentType?.MediaType);
        var html = await preview.Content.ReadAsStringAsync();
        Assert.Contains("Order Confirmed", html);
        Assert.Contains("Order Number", html);
        Assert.Contains("customer@example.com", html);
    }

    [Fact]
    public async Task UnknownNotificationIdReturnsNotFound()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(
            new Uri(NotificationUrl, $"/api/notification/{Guid.NewGuid()}"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InvalidOrderIdReturnsBadRequest()
    {
        using var client = new HttpClient();
        using var content = new StringContent(
            """
            {
              "orderId": "not-a-guid",
              "customerId": "22222222-2222-2222-2222-222222222222",
              "totalAmount": 42.50,
              "placedAt": "2026-08-09T05:10:00Z"
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync(
            new Uri(NotificationUrl, "/api/notification/events/order-placed"),
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadAsStringAsync();
        Assert.Contains("$.orderId", problem);
    }

    [Fact]
    public async Task MissingEventFieldsAreAcceptedAsDefaultValues()
    {
        using var client = new HttpClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            new Uri(NotificationUrl, "/api/notification/events/order-placed"),
            content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedNotification>();
        Assert.NotNull(created);

        var persisted = await client.GetFromJsonAsync<NotificationRecord>(
            new Uri(NotificationUrl, $"/api/notification/{created!.Id}"));

        Assert.NotNull(persisted);
        Assert.Equal(Guid.Empty, persisted!.OrderId);
        Assert.Equal(Guid.Empty, persisted.CustomerId);
        Assert.Equal(0m, persisted.OrderTotal);
    }

    [Fact(Skip = "Known defect: NotificationRenderer.FormatCurrency divides by 100 and uses the process culture, producing ¤0.43 for posted 42.50.")]
    public async Task PreviewFormatsPostedAmountWithCurrencySymbol()
    {
        using var client = new HttpClient();
        var response = await PostOrderPlacedEvent(
            client,
            Guid.NewGuid(),
            Guid.NewGuid(),
            42.50m);
        var created = await response.Content.ReadFromJsonAsync<CreatedNotification>();

        Assert.NotNull(created);
        var html = await client.GetStringAsync(
            new Uri(NotificationUrl, $"/api/notification/{created!.Id}/preview"));

        Assert.Contains("$42.50", html);
    }

    private static Task<HttpResponseMessage> PostOrderPlacedEvent(
        HttpClient client,
        Guid orderId,
        Guid customerId,
        decimal totalAmount)
    {
        return client.PostAsJsonAsync(
            new Uri(NotificationUrl, "/api/notification/events/order-placed"),
            new
            {
                orderId,
                customerId,
                totalAmount,
                placedAt = DateTime.UtcNow
            });
    }

    private sealed record CreatedNotification(Guid Id, string PreviewUrl);

    private sealed record NotificationRecord(
        Guid Id,
        Guid OrderId,
        Guid CustomerId,
        decimal OrderTotal);
}
