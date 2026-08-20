using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Notification.API.Controllers;
using Notification.API.Services;
using Notification.Domain.Entities;
using Notification.Domain.Interfaces;
using Shared.Contracts.Events;

namespace Notification.Tests;

public class NotificationControllerTests
{
    [Fact]
    public async Task GetById_ReturnsNotFoundWhenNotificationIsMissing()
    {
        var repository = new Mock<INotificationRepository>();
        repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((OrderNotification?)null);
        var controller = CreateController(repository.Object);

        var result = await controller.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_ReturnsOkWhenNotificationExists()
    {
        var notification = CreateNotification();
        var repository = new Mock<INotificationRepository>();
        repository
            .Setup(x => x.GetByIdAsync(notification.Id))
            .ReturnsAsync(notification);
        var controller = CreateController(repository.Object);

        var result = await controller.GetById(notification.Id);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPreview_ReturnsNotFoundWhenRenderedBodyIsEmpty()
    {
        var notification = CreateNotification();
        notification.RenderedBody = string.Empty;
        var repository = new Mock<INotificationRepository>();
        repository
            .Setup(x => x.GetByIdAsync(notification.Id))
            .ReturnsAsync(notification);
        var controller = CreateController(repository.Object);

        var result = await controller.GetPreview(notification.Id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ReceiveOrderPlacedEvent_MapsDtoAndReturnsCreatedAtAction()
    {
        var repository = new Mock<INotificationRepository>();
        OrderNotification? addedNotification = null;
        repository
            .Setup(x => x.AddAsync(It.IsAny<OrderNotification>()))
            .Callback<OrderNotification>(notification => addedNotification = notification)
            .ReturnsAsync((OrderNotification notification) => notification);
        var controller = CreateController(repository.Object);
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var placedAt = new DateTime(2025, 4, 5, 6, 7, 8, DateTimeKind.Utc);
        var dto = new OrderPlacedEventDto(orderId, customerId, 12345m, placedAt);

        var result = await controller.ReceiveOrderPlacedEvent(dto);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(NotificationController.GetPreview));
        created.RouteValues.Should().ContainKey("id");
        addedNotification.Should().NotBeNull();
        addedNotification!.OrderId.Should().Be(orderId);
        addedNotification.CustomerId.Should().Be(customerId);
        addedNotification.OrderTotal.Should().Be(12345m);
        addedNotification.Status.Should().Be(NotificationStatus.Rendered);
        repository.Verify(x => x.AddAsync(It.IsAny<OrderNotification>()), Times.Once);
    }

    private static NotificationController CreateController(INotificationRepository repository)
    {
        var consumer = new OrderEventConsumer(
            repository,
            new NotificationRenderer(),
            Mock.Of<ILogger<OrderEventConsumer>>());
        return new NotificationController(
            repository,
            consumer,
            Mock.Of<ILogger<NotificationController>>());
    }

    private static OrderNotification CreateNotification()
    {
        return new OrderNotification
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            OrderTotal = 12345m,
            CustomerEmail = "customer@example.com",
            CustomerName = "Valued Customer",
            Type = NotificationType.OrderConfirmation,
            Status = NotificationStatus.Rendered,
            RenderedSubject = "Order Confirmed",
            RenderedBody = "<p>Rendered</p>",
            CreatedAt = DateTime.UtcNow
        };
    }
}
