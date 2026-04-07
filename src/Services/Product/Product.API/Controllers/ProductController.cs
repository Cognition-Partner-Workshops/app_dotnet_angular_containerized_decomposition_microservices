using Microsoft.AspNetCore.Mvc;
using Product.API.DTOs;
using Product.Domain.Interfaces;

namespace Product.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductController> _logger;

    public ProductController(IProductRepository repository, ILogger<ProductController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var products = await _repository.GetAllAsync();
        var dtos = products.Select(p => MapToDto(p));
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        return Ok(MapToDto(product));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var product = new Domain.Entities.Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Icon = dto.Icon,
            BuyingPrice = dto.BuyingPrice,
            SellingPrice = dto.SellingPrice,
            UnitsInStock = dto.UnitsInStock,
            IsActive = dto.IsActive,
            IsDiscontinued = dto.IsDiscontinued,
            ProductCategoryId = dto.ProductCategoryId,
            ParentId = dto.ParentId
        };

        var created = await _repository.AddAsync(product);
        _logger.LogInformation("Product {ProductId} created: {ProductName}", created.Id, created.Name);

        // Reload with category
        var result = await _repository.GetByIdAsync(created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(result!));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Icon = dto.Icon;
        existing.BuyingPrice = dto.BuyingPrice;
        existing.SellingPrice = dto.SellingPrice;
        existing.UnitsInStock = dto.UnitsInStock;
        existing.IsActive = dto.IsActive;
        existing.IsDiscontinued = dto.IsDiscontinued;
        existing.ProductCategoryId = dto.ProductCategoryId;
        existing.ParentId = dto.ParentId;

        await _repository.UpdateAsync(existing);
        _logger.LogInformation("Product {ProductId} updated: {ProductName}", id, dto.Name);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        await _repository.DeleteAsync(id);
        _logger.LogInformation("Product {ProductId} deleted", id);

        return NoContent();
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<ProductCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _repository.GetAllCategoriesAsync();
        var dtos = categories.Select(c => new ProductCategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Icon = c.Icon
        });
        return Ok(dtos);
    }

    private static ProductDto MapToDto(Domain.Entities.Product p)
    {
        return new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Icon = p.Icon,
            BuyingPrice = p.BuyingPrice,
            SellingPrice = p.SellingPrice,
            UnitsInStock = p.UnitsInStock,
            IsActive = p.IsActive,
            IsDiscontinued = p.IsDiscontinued,
            ProductCategoryId = p.ProductCategoryId,
            ProductCategoryName = p.ProductCategory?.Name,
            ParentId = p.ParentId
        };
    }
}
