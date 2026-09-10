namespace Order.API.ViewModels;

/// <summary>
/// Wire shape of an order, aligned with the monolith's <c>ViewModels/Shop/OrderVM</c> and
/// extended with the ids that replaced the cross-context navigations plus best-effort
/// enrichment (<see cref="Customer"/>, <see cref="Cashier"/>, <c>OrderDetails[].Product</c>),
/// which is null when the owning service cannot be reached.
/// </summary>
public class OrderVM
{
    public int Id { get; set; }
    public decimal Discount { get; set; }
    public string? Comments { get; set; }

    public int CustomerId { get; set; }
    public string? CashierId { get; set; }

    public decimal Total { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }

    public RelatedPartyVM? Customer { get; set; }
    public RelatedPartyVM? Cashier { get; set; }

    public List<OrderDetailVM> OrderDetails { get; set; } = [];
}

public class OrderDetailVM
{
    public int Id { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
    public int ProductId { get; set; }
    public RelatedPartyVM? Product { get; set; }
}

public class RelatedPartyVM
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
}
