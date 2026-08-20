using System.Globalization;
using FluentAssertions;
using Notification.API.Services;
using Notification.Domain.Entities;

namespace Notification.Tests;

public class NotificationRendererTests
{
    [Fact]
    public void RenderNotification_OrderConfirmation_UsesOrderConfirmationSubject()
    {
        using var culture = new CultureScope("en-US");
        var notification = CreateNotification(12345m);

        var result = new NotificationRenderer().RenderNotification(notification);

        result.subject.Should().Be("Order Confirmed — $123.45");
    }

    [Fact]
    public void RenderNotification_NonOrderConfirmation_UsesDefaultFallback()
    {
        var notification = CreateNotification(12345m);
        notification.Type = NotificationType.OrderShipped;

        var result = new NotificationRenderer().RenderNotification(notification);

        result.subject.Should().Be("Notification");
        result.body.Should().Be($"<p>Notification for order {notification.OrderId}</p>");
    }

    [Fact]
    public void RenderNotification_OrderConfirmation_FormatsCurrencyAndIncludesCustomerDetails()
    {
        using var culture = new CultureScope("en-US");
        var orderId = Guid.Parse("abcdef12-3456-7890-abcd-ef1234567890");
        var notification = CreateNotification(12345m);
        notification.OrderId = orderId;

        var result = new NotificationRenderer().RenderNotification(notification);

        result.body.Should().Contain("$123.45");
        result.body.Should().Contain(notification.CustomerName);
        result.body.Should().Contain(notification.CustomerEmail);
        result.body.Should().Contain("ABCDEF12");
    }

    private static OrderNotification CreateNotification(decimal orderTotal)
    {
        return new OrderNotification
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            OrderTotal = orderTotal,
            CustomerEmail = "alex@example.com",
            CustomerName = "Alex Customer",
            Type = NotificationType.OrderConfirmation,
            Status = NotificationStatus.Pending,
            CreatedAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        };
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _originalUICulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUICulture;
        }
    }
}
