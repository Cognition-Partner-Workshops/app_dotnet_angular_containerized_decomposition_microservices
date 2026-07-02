using Order.Domain.Entities;

namespace Order.Domain.Interfaces;

public interface IOrderRepository
{
    Task<OrderEntity?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<OrderEntity>> GetAllAsync();
    Task<OrderEntity> AddAsync(OrderEntity order);
}
