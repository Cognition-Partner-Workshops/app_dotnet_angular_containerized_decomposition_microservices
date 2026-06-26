using System.ComponentModel.DataAnnotations;

namespace Order.API.ViewModels;

public class OrderVM
{
    public int Id { get; set; }

    public decimal Discount { get; set; }

    [StringLength(500)]
    public string? Comments { get; set; }

    public int CustomerId { get; set; }

    public List<OrderDetailVM> OrderDetails { get; set; } = [];
}

public class OrderDetailVM
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }
}
