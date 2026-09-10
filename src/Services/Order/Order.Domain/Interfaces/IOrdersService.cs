using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Domain.Interfaces;

public interface IOrdersService
{
    Task<IReadOnlyList<OrderEntity>> GetAllOrdersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<OrderEntity?> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderEntity>> GetOrdersByCustomerAsync(int customerId, CancellationToken cancellationToken = default);
    Task<OrderEntity> CreateOrderAsync(OrderEntity order, CancellationToken cancellationToken = default);
    Task<OrderEntity?> UpdateOrderAsync(OrderEntity order, CancellationToken cancellationToken = default);
    Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken = default);
}
