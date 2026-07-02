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

    public async Task<OrderEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Orders.FindAsync(id);
    }

    public async Task<IReadOnlyList<OrderEntity>> GetAllAsync()
    {
        return await _context.Orders
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<OrderEntity> AddAsync(OrderEntity order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }
}
