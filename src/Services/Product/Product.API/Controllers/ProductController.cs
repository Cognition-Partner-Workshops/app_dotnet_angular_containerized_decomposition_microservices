using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Product.Domain.DTOs;
using Product.Infrastructure.Data;

namespace Product.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly ProductDbContext _db;

    public ProductController(ProductDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var products = await _db.Products
            .Include(p => p.ProductCategory)
            .Select(p => new ProductDto
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
                ProductCategoryName = p.ProductCategory.Name
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _db.Products
            .Include(p => p.ProductCategory)
            .Where(p => p.Id == id)
            .Select(p => new ProductDto
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
                ProductCategoryName = p.ProductCategory.Name
            })
            .FirstOrDefaultAsync();

        if (product is null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var category = await _db.ProductCategories.FindAsync(request.ProductCategoryId);
        if (category is null) return BadRequest("Invalid ProductCategoryId");

        var entity = new Product.Domain.Entities.Product
        {
            Name = request.Name,
            Description = request.Description,
            Icon = request.Icon,
            BuyingPrice = request.BuyingPrice,
            SellingPrice = request.SellingPrice,
            UnitsInStock = request.UnitsInStock,
            IsActive = request.IsActive,
            IsDiscontinued = request.IsDiscontinued,
            ParentId = request.ParentId,
            ProductCategoryId = request.ProductCategoryId,
            ProductCategory = category,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _db.Products.Add(entity);
        await _db.SaveChangesAsync();

        var dto = new ProductDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Icon = entity.Icon,
            BuyingPrice = entity.BuyingPrice,
            SellingPrice = entity.SellingPrice,
            UnitsInStock = entity.UnitsInStock,
            IsActive = entity.IsActive,
            IsDiscontinued = entity.IsDiscontinued,
            ProductCategoryName = category.Name
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] CreateProductRequest request)
    {
        var entity = await _db.Products.FindAsync(id);
        if (entity is null) return NotFound();

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Icon = request.Icon;
        entity.BuyingPrice = request.BuyingPrice;
        entity.SellingPrice = request.SellingPrice;
        entity.UnitsInStock = request.UnitsInStock;
        entity.IsActive = request.IsActive;
        entity.IsDiscontinued = request.IsDiscontinued;
        entity.ParentId = request.ParentId;
        entity.ProductCategoryId = request.ProductCategoryId;
        entity.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Products.FindAsync(id);
        if (entity is null) return NotFound();

        _db.Products.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _db.ProductCategories
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Icon = c.Icon
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost("categories")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var entity = new Product.Domain.Entities.ProductCategory
        {
            Name = request.Name,
            Description = request.Description,
            Icon = request.Icon,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _db.ProductCategories.Add(entity);
        await _db.SaveChangesAsync();

        var dto = new CategoryDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Icon = entity.Icon
        };

        return CreatedAtAction(nameof(GetCategories), dto);
    }
}

public record CreateProductRequest(
    string Name,
    string? Description,
    string? Icon,
    decimal BuyingPrice,
    decimal SellingPrice,
    int UnitsInStock,
    bool IsActive,
    bool IsDiscontinued,
    int? ParentId,
    int ProductCategoryId
);

public record CreateCategoryRequest(
    string Name,
    string? Description,
    string? Icon
);
