namespace Order.Domain.Entities;

// Order aggregate root for the Order bounded context.
// Other bounded contexts are referenced by id only (e.g. CustomerId). The navigations and
// staff-id fields that the monolith carried for those contexts are intentionally dropped here.
public class Order : BaseEntity
{
    public decimal Discount { get; set; }
    public string? Comments { get; set; }

    public int CustomerId { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; } = [];
}
