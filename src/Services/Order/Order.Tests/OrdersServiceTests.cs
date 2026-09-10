using Order.Domain.Entities;
using Order.Infrastructure.Repositories;
using Xunit;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Tests;

public class OrdersServiceTests
{
    private static OrderEntity NewOrder(int customerId = 1, string? cashierId = "cashier-1")
    {
        var order = new OrderEntity
        {
            CustomerId = customerId,
            CashierId = cashierId,
            Discount = 5m,
            Comments = "rush"
        };
        order.OrderDetails.Add(new OrderDetail { ProductId = 10, Quantity = 2, UnitPrice = 25m });
        order.OrderDetails.Add(new OrderDetail { ProductId = 11, Quantity = 1, UnitPrice = 40m, Discount = 4m });
        return order;
    }

    [Fact]
    public async Task CreateOrder_persists_order_with_its_lines_and_timestamps()
    {
        await using var context = TestDb.NewContext();
        var service = new OrdersService(context);

        var created = await service.CreateOrderAsync(NewOrder());

        Assert.True(created.Id > 0);
        Assert.NotEqual(default, created.CreatedDate);
        var stored = await service.GetOrderByIdAsync(created.Id);
        Assert.NotNull(stored);
        Assert.Equal(2, stored.OrderDetails.Count);
        Assert.Equal([10, 11], stored.OrderDetails.Select(d => d.ProductId).OrderBy(id => id));
    }

    [Fact]
    public async Task GetOrderById_returns_null_for_unknown_order()
    {
        await using var context = TestDb.NewContext();
        var service = new OrdersService(context);

        Assert.Null(await service.GetOrderByIdAsync(4242));
    }

    [Fact]
    public async Task GetAllOrders_pages_newest_first()
    {
        await using var context = TestDb.NewContext();
        var service = new OrdersService(context);
        for (var i = 0; i < 3; i++)
            await service.CreateOrderAsync(NewOrder(customerId: i + 1));

        var firstPage = await service.GetAllOrdersAsync(page: 1, pageSize: 2);
        var secondPage = await service.GetAllOrdersAsync(page: 2, pageSize: 2);

        Assert.Equal(2, firstPage.Count);
        Assert.Single(secondPage);
        Assert.True(firstPage[0].Id > firstPage[1].Id);
        Assert.True(firstPage[1].Id > secondPage[0].Id);
    }

    [Fact]
    public async Task GetOrdersByCustomer_only_returns_that_customers_orders()
    {
        await using var context = TestDb.NewContext();
        var service = new OrdersService(context);
        await service.CreateOrderAsync(NewOrder(customerId: 7));
        await service.CreateOrderAsync(NewOrder(customerId: 7));
        await service.CreateOrderAsync(NewOrder(customerId: 8));

        var orders = await service.GetOrdersByCustomerAsync(7);

        Assert.Equal(2, orders.Count);
        Assert.All(orders, o => Assert.Equal(7, o.CustomerId));
    }

    [Fact]
    public async Task UpdateOrder_replaces_lines_and_scalar_fields()
    {
        await using var context = TestDb.NewContext();
        var service = new OrdersService(context);
        var created = await service.CreateOrderAsync(NewOrder());

        var replacement = new OrderEntity { Id = created.Id, CustomerId = 9, CashierId = "cashier-2", Discount = 1m, Comments = "amended" };
        replacement.OrderDetails.Add(new OrderDetail { ProductId = 99, Quantity = 3, UnitPrice = 10m });

        var updated = await service.UpdateOrderAsync(replacement);

        Assert.NotNull(updated);
        Assert.Equal(9, updated.CustomerId);
        Assert.Equal("amended", updated.Comments);
        var line = Assert.Single(updated.OrderDetails);
        Assert.Equal(99, line.ProductId);
    }

    [Fact]
    public async Task UpdateOrder_returns_null_for_unknown_order()
    {
        await using var context = TestDb.NewContext();
        var service = new OrdersService(context);

        Assert.Null(await service.UpdateOrderAsync(new OrderEntity { Id = 123, CustomerId = 1 }));
    }

    [Fact]
    public async Task DeleteOrder_removes_the_order_and_its_lines()
    {
        await using var context = TestDb.NewContext();
        var service = new OrdersService(context);
        var created = await service.CreateOrderAsync(NewOrder());

        Assert.True(await service.DeleteOrderAsync(created.Id));
        Assert.False(await service.DeleteOrderAsync(created.Id));
        Assert.Null(await service.GetOrderByIdAsync(created.Id));
        Assert.Empty(context.OrderDetails);
    }
}
