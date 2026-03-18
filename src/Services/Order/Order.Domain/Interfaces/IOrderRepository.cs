using Order.Domain.Entities;

namespace Order.Domain.Interfaces;

public interface IOrderRepository
{
    Task<IEnumerable<OrderEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderEntity>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<OrderEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrderEntity> AddAsync(OrderEntity order, CancellationToken cancellationToken = default);
    Task<OrderEntity> UpdateAsync(OrderEntity order, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
