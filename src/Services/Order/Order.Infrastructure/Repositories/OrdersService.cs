using Microsoft.EntityFrameworkCore;
using Order.Domain.Interfaces;
using Order.Infrastructure.Data;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Infrastructure.Repositories;

public class OrdersService : IOrdersService
{
    private readonly OrderDbContext _context;

    public OrdersService(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OrderEntity>> GetAllOrdersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails)
            .OrderByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderEntity?> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderEntity>> GetOrdersByCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderEntity> CreateOrderAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        order.CreatedDate = now;
        order.UpdatedDate = now;
        foreach (var detail in order.OrderDetails)
        {
            detail.CreatedDate = now;
            detail.UpdatedDate = now;
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<OrderEntity?> UpdateOrderAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Orders
            .Include(o => o.OrderDetails)
            .SingleOrDefaultAsync(o => o.Id == order.Id, cancellationToken);

        if (existing is null)
            return null;

        existing.Discount = order.Discount;
        existing.Comments = order.Comments;
        existing.CashierId = order.CashierId;
        existing.CustomerId = order.CustomerId;
        existing.UpdatedBy = order.UpdatedBy;
        existing.UpdatedDate = DateTime.UtcNow;

        _context.OrderDetails.RemoveRange(existing.OrderDetails);
        foreach (var detail in order.OrderDetails)
        {
            detail.Id = 0;
            detail.OrderId = existing.Id;
            detail.CreatedDate = DateTime.UtcNow;
            detail.UpdatedDate = DateTime.UtcNow;
            existing.OrderDetails.Add(detail);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Orders
            .Include(o => o.OrderDetails)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (existing is null)
            return false;

        _context.Orders.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
