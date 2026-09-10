using System.ComponentModel.DataAnnotations;

namespace Order.API.ViewModels;

public class OrderSaveVM
{
    [Range(0, double.MaxValue)]
    public decimal Discount { get; set; }

    [MaxLength(500)]
    public string? Comments { get; set; }

    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    [MaxLength(450)]
    public string? CashierId { get; set; }

    public List<OrderDetailSaveVM> OrderDetails { get; set; } = [];
}

public class OrderDetailSaveVM
{
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Discount { get; set; }

    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }
}
