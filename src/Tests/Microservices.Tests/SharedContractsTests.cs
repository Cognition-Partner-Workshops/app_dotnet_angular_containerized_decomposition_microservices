using Shared.Contracts.Events;
using Shared.Contracts.DTOs;

namespace Microservices.Tests;

public class SharedContractsTests
{
    [Fact]
    public void OrderPlacedEvent_RecordProperties_AreAssignedCorrectly()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var amount = 2500m;
        var placedAt = DateTime.UtcNow;

        var evt = new OrderPlacedEvent(orderId, customerId, amount, placedAt);

        Assert.Equal(orderId, evt.OrderId);
        Assert.Equal(customerId, evt.CustomerId);
        Assert.Equal(amount, evt.TotalAmount);
        Assert.Equal(placedAt, evt.PlacedAt);
    }

    [Fact]
    public void OrderPlacedEvent_RecordEquality_WorksCorrectly()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var a = new OrderPlacedEvent(orderId, customerId, 100m, now);
        var b = new OrderPlacedEvent(orderId, customerId, 100m, now);

        Assert.Equal(a, b);
    }

    [Fact]
    public void ServiceHealthDto_RecordProperties_AreAssignedCorrectly()
    {
        var now = DateTime.UtcNow;
        var dto = new ServiceHealthDto("identity-service", "Healthy", now);

        Assert.Equal("identity-service", dto.ServiceName);
        Assert.Equal("Healthy", dto.Status);
        Assert.Equal(now, dto.CheckedAt);
    }
}
