using Shared.Contracts.Events;

namespace Order.Domain.Interfaces;

public interface IOrderEventPublisher
{
    Task PublishOrderPlacedAsync(OrderPlacedEvent orderPlaced, CancellationToken cancellationToken = default);
}
