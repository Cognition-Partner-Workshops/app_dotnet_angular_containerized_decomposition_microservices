using Order.API.ViewModels;
using OrderEntity = Order.Domain.Entities.Order;
using OrderDetailEntity = Order.Domain.Entities.OrderDetail;

namespace Order.API.Mapping;

public static class OrderMappingExtensions
{
    public static OrderVM ToViewModel(this OrderEntity entity) => new()
    {
        Id = entity.Id,
        Discount = entity.Discount,
        Comments = entity.Comments,
        CustomerId = entity.CustomerId,
        OrderDetails = entity.OrderDetails
            .Select(d => d.ToViewModel())
            .ToList()
    };

    public static OrderDetailVM ToViewModel(this OrderDetailEntity entity) => new()
    {
        Id = entity.Id,
        ProductId = entity.ProductId,
        Quantity = entity.Quantity,
        UnitPrice = entity.UnitPrice,
        Discount = entity.Discount
    };

    public static OrderEntity ToEntity(this OrderVM vm) => new()
    {
        Discount = vm.Discount,
        Comments = vm.Comments,
        CustomerId = vm.CustomerId
    };

    public static void ApplyTo(this OrderVM vm, OrderEntity entity)
    {
        entity.Discount = vm.Discount;
        entity.Comments = vm.Comments;
        entity.CustomerId = vm.CustomerId;
    }

    public static OrderDetailEntity ToEntity(this OrderDetailVM vm) => new()
    {
        ProductId = vm.ProductId,
        Quantity = vm.Quantity,
        UnitPrice = vm.UnitPrice,
        Discount = vm.Discount
    };
}
