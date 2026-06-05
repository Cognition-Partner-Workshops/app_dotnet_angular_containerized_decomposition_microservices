using System.Globalization;
using Notification.API.Services;
using Notification.Domain.Entities;

namespace Microservices.Tests;

public class NotificationRendererTests : IDisposable
{
    private readonly NotificationRenderer _renderer = new();
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public NotificationRendererTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
    }

    [Fact]
    public void RenderNotification_OrderConfirmation_ReturnsSubjectWithFormattedAmount()
    {
        var notification = CreateTestNotification(orderTotal: 4999m);

        var (subject, _) = _renderer.RenderNotification(notification);

        Assert.Contains("$49.99", subject);
        Assert.StartsWith("Order Confirmed", subject);
    }

    [Fact]
    public void RenderNotification_OrderConfirmation_ReturnsHtmlBody()
    {
        var notification = CreateTestNotification(orderTotal: 10000m);

        var (_, body) = _renderer.RenderNotification(notification);

        Assert.Contains("<!DOCTYPE html>", body);
        Assert.Contains("Order Confirmed", body);
        Assert.Contains("$100.00", body);
    }

    [Fact]
    public void RenderNotification_OrderConfirmation_IncludesCustomerName()
    {
        var notification = CreateTestNotification();
        notification.CustomerName = "Jane Doe";

        var (_, body) = _renderer.RenderNotification(notification);

        Assert.Contains("Jane Doe", body);
    }

    [Fact]
    public void RenderNotification_UnknownType_ReturnsFallback()
    {
        var notification = CreateTestNotification();
        notification.Type = NotificationType.OrderShipped;

        var (subject, body) = _renderer.RenderNotification(notification);

        Assert.Equal("Notification", subject);
        Assert.Contains(notification.OrderId.ToString(), body);
    }

    [Theory]
    [InlineData(0, "$0.00")]
    [InlineData(100, "$1.00")]
    [InlineData(1550, "$15.50")]
    [InlineData(99999, "$999.99")]
    public void RenderNotification_CurrencyFormatting_ConvertsFromCents(decimal cents, string expected)
    {
        var notification = CreateTestNotification(orderTotal: cents);

        var (subject, _) = _renderer.RenderNotification(notification);

        Assert.Contains(expected, subject);
    }

    private static OrderNotification CreateTestNotification(decimal orderTotal = 5000m)
    {
        return new OrderNotification
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            OrderTotal = orderTotal,
            CustomerEmail = "test@example.com",
            CustomerName = "Test User",
            Type = NotificationType.OrderConfirmation,
            Status = NotificationStatus.Pending,
            CreatedAt = new DateTime(2026, 1, 15, 14, 30, 0, DateTimeKind.Utc)
        };
    }
}
