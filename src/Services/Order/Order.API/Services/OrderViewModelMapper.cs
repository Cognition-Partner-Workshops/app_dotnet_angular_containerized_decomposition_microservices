using Order.API.ViewModels;
using Order.Domain.Entities;
using Order.Domain.Interfaces;
using Order.Domain.Models;
using Order.Infrastructure.Orders;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.API.Services;

/// <summary>
/// Maps order entities to their wire shape, filling in related data owned by other services
/// where the gateway answers. Lookups are deduplicated per mapping call.
/// </summary>
public class OrderViewModelMapper
{
    private readonly IOrderEnrichmentClient _enrichment;

    public OrderViewModelMapper(IOrderEnrichmentClient enrichment)
    {
        _enrichment = enrichment;
    }

    public static OrderVM ToViewModel(OrderEntity order) => new()
    {
        Id = order.Id,
        Discount = order.Discount,
        Comments = order.Comments,
        CustomerId = order.CustomerId,
        CashierId = order.CashierId,
        Total = OrderPlacementService.CalculateTotal(order),
        CreatedDate = order.CreatedDate,
        UpdatedDate = order.UpdatedDate,
        OrderDetails = order.OrderDetails.Select(ToViewModel).ToList()
    };

    public static OrderDetailVM ToViewModel(OrderDetail detail) => new()
    {
        Id = detail.Id,
        UnitPrice = detail.UnitPrice,
        Quantity = detail.Quantity,
        Discount = detail.Discount,
        ProductId = detail.ProductId
    };

    public static OrderEntity ToEntity(OrderSaveVM model, int id = 0)
    {
        var order = new OrderEntity
        {
            Id = id,
            Discount = model.Discount,
            Comments = model.Comments,
            CustomerId = model.CustomerId,
            CashierId = model.CashierId
        };

        foreach (var detail in model.OrderDetails)
        {
            order.OrderDetails.Add(new OrderDetail
            {
                UnitPrice = detail.UnitPrice,
                Quantity = detail.Quantity,
                Discount = detail.Discount,
                ProductId = detail.ProductId
            });
        }

        return order;
    }

    public async Task<OrderVM> ToEnrichedViewModelAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        var models = await ToEnrichedViewModelsAsync([order], cancellationToken);
        return models[0];
    }

    public async Task<IReadOnlyList<OrderVM>> ToEnrichedViewModelsAsync(IReadOnlyList<OrderEntity> orders, CancellationToken cancellationToken = default)
    {
        var models = orders.Select(ToViewModel).ToList();

        var customerIds = orders.Select(o => o.CustomerId).Distinct().ToArray();
        var cashierIds = orders.Select(o => o.CashierId).OfType<string>().Where(id => id.Length > 0).Distinct().ToArray();
        var productIds = orders.SelectMany(o => o.OrderDetails).Select(d => d.ProductId).Distinct().ToArray();

        var customers = await LookupAsync(customerIds, id => _enrichment.GetCustomerAsync(id, cancellationToken));
        var products = await LookupAsync(productIds, id => _enrichment.GetProductAsync(id, cancellationToken));
        var cashiers = await LookupAsync(cashierIds, id => _enrichment.GetCashierAsync(id, cancellationToken));

        foreach (var model in models)
        {
            model.Customer = ToViewModel(customers.GetValueOrDefault(model.CustomerId));
            if (model.CashierId is not null)
                model.Cashier = ToViewModel(cashiers.GetValueOrDefault(model.CashierId));

            foreach (var detail in model.OrderDetails)
                detail.Product = ToViewModel(products.GetValueOrDefault(detail.ProductId));
        }

        return models;
    }

    private static async Task<Dictionary<TKey, RelatedParty?>> LookupAsync<TKey>(
        IReadOnlyCollection<TKey> keys,
        Func<TKey, Task<RelatedParty?>> lookup) where TKey : notnull
    {
        var results = new Dictionary<TKey, RelatedParty?>(keys.Count);
        if (keys.Count == 0)
            return results;

        var tasks = keys.Select(async key => (key, value: await lookup(key))).ToArray();
        foreach (var (key, value) in await Task.WhenAll(tasks))
            results[key] = value;

        return results;
    }

    private static RelatedPartyVM? ToViewModel(RelatedParty? party) =>
        party is null ? null : new RelatedPartyVM { Id = party.Id, Name = party.Name, Email = party.Email };
}
