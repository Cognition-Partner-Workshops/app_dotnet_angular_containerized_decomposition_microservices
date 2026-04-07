namespace Shared.Contracts.Events;

/// <summary>
/// Integration event published when a new product is created in the Inventory service.
/// </summary>
public record ProductCreatedEvent(
    int ProductId,
    string Name,
    decimal SellingPrice,
    int UnitsInStock,
    int ProductCategoryId,
    DateTime CreatedAt
);
