using System.ComponentModel.DataAnnotations;

namespace Product.API.ViewModels;

/// <summary>
/// Wire shape of a product. Mirrors the monolith's ViewModels/Shop/ProductVM so the
/// Angular client can be repointed at this service unchanged; ProductCategoryId and
/// ParentId are additive so the category can be set over the API.
/// </summary>
public class ProductVM
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(256)]
    public string? Icon { get; set; }

    public decimal BuyingPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int UnitsInStock { get; set; }
    public bool IsActive { get; set; }
    public bool IsDiscontinued { get; set; }
    public int? ParentId { get; set; }
    public int ProductCategoryId { get; set; }
    public string? ProductCategoryName { get; set; }
}
