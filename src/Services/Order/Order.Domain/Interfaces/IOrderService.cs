using Order.Domain.Entities;

namespace Order.Domain.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<OrderEntity>> GetAllOrdersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<OrderEntity>> GetOrdersByCustomerAsync(int customerId, CancellationToken cancellationToken = default);
    Task<OrderEntity?> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrderEntity> CreateOrderAsync(OrderEntity order, CancellationToken cancellationToken = default);
    Task<OrderEntity> UpdateOrderAsync(OrderEntity order, CancellationToken cancellationToken = default);
    Task DeleteOrderAsync(int id, CancellationToken cancellationToken = default);
}
