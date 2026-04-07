namespace Shared.Contracts.Events;

/// <summary>
/// Integration event published when a product is deleted from the Inventory service.
/// </summary>
public record ProductDeletedEvent(
    int ProductId,
    string Name,
    DateTime DeletedAt
);
