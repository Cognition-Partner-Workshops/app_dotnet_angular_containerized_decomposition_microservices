using Microsoft.EntityFrameworkCore;
using Order.Domain.Entities;
using Order.Domain.Interfaces;
using Order.Infrastructure.Data;

namespace Order.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OrderEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.OrderDetails)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OrderEntity>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.OrderDetails)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<OrderEntity> AddAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        order.CreatedDate = DateTime.UtcNow;
        order.UpdatedDate = DateTime.UtcNow;
        foreach (var detail in order.OrderDetails)
        {
            detail.CreatedDate = DateTime.UtcNow;
            detail.UpdatedDate = DateTime.UtcNow;
        }
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<OrderEntity> UpdateAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        order.UpdatedDate = DateTime.UtcNow;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders.FindAsync([id], cancellationToken);
        if (order is not null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
