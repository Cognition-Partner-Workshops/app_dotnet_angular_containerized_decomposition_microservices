using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Notification.API.Services;
using Notification.Domain.Entities;
using Notification.Domain.Interfaces;
using Shared.Contracts.Events;

namespace Notification.Tests;

public class OrderEventConsumerTests
{
    [Fact]
    public async Task HandleOrderPlaced_CreatesRenderedNotificationAndPersistsIt()
    {
        var repository = new Mock<INotificationRepository>();
        OrderNotification? addedNotification = null;
        repository
            .Setup(x => x.AddAsync(It.IsAny<OrderNotification>()))
            .Callback<OrderNotification>(notification => addedNotification = notification)
            .ReturnsAsync((OrderNotification notification) => notification);
        var consumer = new OrderEventConsumer(
            repository.Object,
            new NotificationRenderer(),
            Mock.Of<ILogger<OrderEventConsumer>>());
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var orderEvent = new OrderPlacedEvent(orderId, customerId, 12345m, DateTime.UtcNow);

        var result = await consumer.HandleOrderPlaced(orderEvent);

        result.Should().BeSameAs(addedNotification);
        result.OrderId.Should().Be(orderId);
        result.CustomerId.Should().Be(customerId);
        result.OrderTotal.Should().Be(12345m);
        result.Status.Should().Be(NotificationStatus.Rendered);
        result.RenderedSubject.Should().NotBeNullOrEmpty();
        result.RenderedBody.Should().NotBeNullOrEmpty();
        repository.Verify(
            x => x.AddAsync(It.IsAny<OrderNotification>()),
            Times.Once);
    }
}
