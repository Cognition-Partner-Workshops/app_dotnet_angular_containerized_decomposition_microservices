using Microsoft.Extensions.Logging.Abstractions;
using Order.Domain;
using Order.Domain.Entities;
using Order.Infrastructure.Orders;
using Order.Infrastructure.Repositories;
using Xunit;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Tests;

public class OrderPlacementServiceTests
{
    private static OrderEntity NewOrder()
    {
        var order = new OrderEntity { CustomerId = 42, CashierId = "cashier-1", Discount = 5m };
        order.OrderDetails.Add(new OrderDetail { ProductId = 10, Quantity = 2, UnitPrice = 25m });
        order.OrderDetails.Add(new OrderDetail { ProductId = 11, Quantity = 1, UnitPrice = 40m, Discount = 4m });
        return order;
    }

    [Fact]
    public async Task PlaceOrder_publishes_OrderPlacedEvent_for_the_saved_order()
    {
        await using var context = TestDb.NewContext();
        var publisher = new RecordingEventPublisher();
        var placement = new OrderPlacementService(new OrdersService(context), publisher, NullLogger<OrderPlacementService>.Instance);

        var placed = await placement.PlaceOrderAsync(NewOrder());

        var published = Assert.Single(publisher.Published);
        Assert.Equal(OrderIdentifiers.ToOrderGuid(placed.Id), published.OrderId);
        Assert.Equal(OrderIdentifiers.ToCustomerGuid(42), published.CustomerId);
        Assert.Equal(81m, published.TotalAmount);
        Assert.Equal(placed.CreatedDate, published.PlacedAt);
    }

    [Fact]
    public async Task PlaceOrder_keeps_the_order_when_publishing_fails()
    {
        await using var context = TestDb.NewContext();
        var orders = new OrdersService(context);
        var placement = new OrderPlacementService(orders, new ThrowingEventPublisher(), NullLogger<OrderPlacementService>.Instance);

        var placed = await placement.PlaceOrderAsync(NewOrder());

        Assert.NotNull(await orders.GetOrderByIdAsync(placed.Id));
    }

    [Fact]
    public void Order_guids_are_stable_and_distinct_per_id_and_context()
    {
        Assert.Equal(OrderIdentifiers.ToOrderGuid(7), OrderIdentifiers.ToOrderGuid(7));
        Assert.NotEqual(OrderIdentifiers.ToOrderGuid(7), OrderIdentifiers.ToOrderGuid(8));
        Assert.NotEqual(OrderIdentifiers.ToOrderGuid(7), OrderIdentifiers.ToCustomerGuid(7));
    }

    [Fact]
    public void Total_never_goes_below_zero()
    {
        var order = new OrderEntity { CustomerId = 1, Discount = 1000m };
        order.OrderDetails.Add(new OrderDetail { ProductId = 1, Quantity = 1, UnitPrice = 10m });

        Assert.Equal(0m, OrderPlacementService.CalculateTotal(order));
    }
}
