using Order.Domain.Entities;
using Order.Domain.Interfaces;

namespace Order.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<OrderEntity>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<IEnumerable<OrderEntity>> GetOrdersByCustomerAsync(int customerId, CancellationToken cancellationToken = default)
        => _repository.GetByCustomerIdAsync(customerId, cancellationToken);

    public Task<OrderEntity?> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<OrderEntity> CreateOrderAsync(OrderEntity order, CancellationToken cancellationToken = default)
        => _repository.AddAsync(order, cancellationToken);

    public Task<OrderEntity> UpdateOrderAsync(OrderEntity order, CancellationToken cancellationToken = default)
        => _repository.UpdateAsync(order, cancellationToken);

    public Task DeleteOrderAsync(int id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);
}
