namespace Product.API.DTOs;

public class ProductCategoryDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
}

public class CreateProductCategoryDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
}

public class UpdateProductCategoryDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
}
