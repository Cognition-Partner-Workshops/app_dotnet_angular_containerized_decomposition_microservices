namespace Shared.Contracts.Events;

/// <summary>
/// Integration event published when a product is updated in the Inventory service.
/// </summary>
public record ProductUpdatedEvent(
    int ProductId,
    string Name,
    decimal SellingPrice,
    int UnitsInStock,
    bool IsActive,
    bool IsDiscontinued,
    DateTime UpdatedAt
);
