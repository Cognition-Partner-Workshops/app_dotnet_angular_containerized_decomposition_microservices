using System.ComponentModel.DataAnnotations;

namespace Product.API.ViewModels;

public class ProductCategoryVM
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(256)]
    public string? Icon { get; set; }
}
