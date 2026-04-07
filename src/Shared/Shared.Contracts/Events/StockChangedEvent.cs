namespace Shared.Contracts.Events;

/// <summary>
/// Integration event published when product stock levels change.
/// Consumed by Order service for availability checks.
/// </summary>
public record StockChangedEvent(
    int ProductId,
    string ProductName,
    int PreviousStock,
    int NewStock,
    DateTime ChangedAt
);
