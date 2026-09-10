using Order.Domain.Models;

namespace Order.Domain.Interfaces;

/// <summary>
/// Best-effort lookups of data owned by other services, over HTTP through the gateway.
/// Every method returns null when the lookup fails; callers degrade instead of erroring.
/// </summary>
public interface IOrderEnrichmentClient
{
    Task<RelatedParty?> GetCustomerAsync(int customerId, CancellationToken cancellationToken = default);
    Task<RelatedParty?> GetProductAsync(int productId, CancellationToken cancellationToken = default);
    Task<RelatedParty?> GetCashierAsync(string cashierId, CancellationToken cancellationToken = default);
}
