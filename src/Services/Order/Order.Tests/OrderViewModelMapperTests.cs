using Order.API.Services;
using Order.API.ViewModels;
using Order.Domain.Entities;
using Order.Domain.Models;
using Xunit;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Tests;

public class OrderViewModelMapperTests
{
    private static OrderEntity NewOrder()
    {
        var order = new OrderEntity { Id = 1, CustomerId = 42, CashierId = "cashier-1", Discount = 5m, Comments = "rush" };
        order.OrderDetails.Add(new OrderDetail { Id = 1, ProductId = 10, Quantity = 2, UnitPrice = 25m });
        return order;
    }

    [Fact]
    public async Task Enrichment_fills_customer_cashier_and_product_names()
    {
        var enrichment = new StubEnrichmentClient();
        enrichment.Customers[42] = new RelatedParty("42", "Ada Lovelace", "ada@example.com");
        enrichment.Products[10] = new RelatedParty("10", "Widget");
        enrichment.Cashiers["cashier-1"] = new RelatedParty("cashier-1", "Grace Hopper");

        var model = await new OrderViewModelMapper(enrichment).ToEnrichedViewModelAsync(NewOrder());

        Assert.Equal("Ada Lovelace", model.Customer?.Name);
        Assert.Equal("Grace Hopper", model.Cashier?.Name);
        Assert.Equal("Widget", Assert.Single(model.OrderDetails).Product?.Name);
    }

    [Fact]
    public async Task Unavailable_services_degrade_to_ids_only()
    {
        var model = await new OrderViewModelMapper(new UnavailableEnrichmentClient()).ToEnrichedViewModelAsync(NewOrder());

        Assert.Null(model.Customer);
        Assert.Null(model.Cashier);
        Assert.Null(Assert.Single(model.OrderDetails).Product);
        Assert.Equal(42, model.CustomerId);
        Assert.Equal("cashier-1", model.CashierId);
        Assert.Equal(10, model.OrderDetails[0].ProductId);
    }

    [Fact]
    public async Task Mapping_keeps_the_monolith_order_fields_and_computes_the_total()
    {
        var model = await new OrderViewModelMapper(new UnavailableEnrichmentClient()).ToEnrichedViewModelAsync(NewOrder());

        Assert.Equal(1, model.Id);
        Assert.Equal(5m, model.Discount);
        Assert.Equal("rush", model.Comments);
        Assert.Equal(45m, model.Total);
    }

    [Fact]
    public void Save_model_maps_onto_the_entity_graph()
    {
        var save = new OrderSaveVM
        {
            CustomerId = 3,
            CashierId = "cashier-9",
            Discount = 2m,
            Comments = "gift",
            OrderDetails = [new OrderDetailSaveVM { ProductId = 5, Quantity = 4, UnitPrice = 7m, Discount = 1m }]
        };

        var entity = OrderViewModelMapper.ToEntity(save);

        Assert.Equal(3, entity.CustomerId);
        Assert.Equal("cashier-9", entity.CashierId);
        var line = Assert.Single(entity.OrderDetails);
        Assert.Equal(5, line.ProductId);
        Assert.Equal(4, line.Quantity);
    }
}
