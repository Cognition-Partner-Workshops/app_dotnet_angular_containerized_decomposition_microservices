using Microsoft.Extensions.Logging;
using Order.Domain;
using Order.Domain.Interfaces;
using Shared.Contracts.Events;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Infrastructure.Orders;

/// <summary>
/// Persists a new order and announces it with <see cref="OrderPlacedEvent"/>. Publishing is a
/// side effect of a committed order: a broker failure is logged, it does not undo the order.
/// </summary>
public class OrderPlacementService
{
    private readonly IOrdersService _orders;
    private readonly IOrderEventPublisher _publisher;
    private readonly ILogger<OrderPlacementService> _logger;

    public OrderPlacementService(IOrdersService orders, IOrderEventPublisher publisher, ILogger<OrderPlacementService> logger)
    {
        _orders = orders;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<OrderEntity> PlaceOrderAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        var created = await _orders.CreateOrderAsync(order, cancellationToken);

        var orderPlaced = new OrderPlacedEvent(
            OrderIdentifiers.ToOrderGuid(created.Id),
            OrderIdentifiers.ToCustomerGuid(created.CustomerId),
            CalculateTotal(created),
            created.CreatedDate);

        try
        {
            await _publisher.PublishOrderPlacedAsync(orderPlaced, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order {OrderId} was saved but OrderPlacedEvent could not be published", created.Id);
        }

        return created;
    }

    public static decimal CalculateTotal(OrderEntity order)
    {
        var lines = order.OrderDetails.Sum(d => (d.UnitPrice * d.Quantity) - d.Discount);
        return Math.Max(0m, lines - order.Discount);
    }
}
