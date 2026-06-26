using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Product.API.ViewModels;
using Product.Infrastructure.Data;
using ProductEntity = Product.Domain.Product;

namespace Product.API.Controllers;

[ApiController]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly ProductDbContext _db;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ProductDbContext db, ILogger<ProductController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // External: GET /api/products  ->  gateway strips prefix  ->  GET /
    [HttpGet("/")]
    [ProducesResponseType(typeof(IEnumerable<ProductVM>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var products = await _db.Products
            .Include(p => p.ProductCategory)
            .AsNoTracking()
            .ToListAsync();

        return Ok(products.Select(MapToVM));
    }

    // External: GET /api/products/{id}  ->  GET /{id}
    [HttpGet("/{id:int}")]
    [ProducesResponseType(typeof(ProductVM), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _db.Products
            .Include(p => p.ProductCategory)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound(id);

        return Ok(MapToVM(product));
    }

    // External: POST /api/products  ->  POST /
    [HttpPost("/")]
    [ProducesResponseType(typeof(ProductVM), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ProductVM model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest("Name is required.");

        var category = await ResolveCategoryAsync(model.ProductCategoryName);

        var product = new ProductEntity
        {
            Name = model.Name,
            Description = model.Description,
            Icon = model.Icon,
            BuyingPrice = model.BuyingPrice,
            SellingPrice = model.SellingPrice,
            UnitsInStock = model.UnitsInStock,
            IsActive = model.IsActive,
            IsDiscontinued = model.IsDiscontinued,
            ProductCategory = category,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, MapToVM(product));
    }

    // External: PUT /api/products/{id}  ->  PUT /{id}
    [HttpPut("/{id:int}")]
    [ProducesResponseType(typeof(ProductVM), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] ProductVM model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var product = await _db.Products
            .Include(p => p.ProductCategory)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound(id);

        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest("Name is required.");

        product.Name = model.Name;
        product.Description = model.Description;
        product.Icon = model.Icon;
        product.BuyingPrice = model.BuyingPrice;
        product.SellingPrice = model.SellingPrice;
        product.UnitsInStock = model.UnitsInStock;
        product.IsActive = model.IsActive;
        product.IsDiscontinued = model.IsDiscontinued;
        product.UpdatedDate = DateTime.UtcNow;

        var resolvedCategory = await ResolveCategoryAsync(model.ProductCategoryName);
        if (resolvedCategory.Name != product.ProductCategory?.Name)
        {
            product.ProductCategory = resolvedCategory;
        }

        await _db.SaveChangesAsync();

        return Ok(MapToVM(product));
    }

    // External: DELETE /api/products/{id}  ->  DELETE /{id}
    [HttpDelete("/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            return NotFound(id);

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // External: GET /api/products/categories  ->  GET /categories
    [HttpGet("/categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _db.ProductCategories
            .AsNoTracking()
            .Select(c => c.Name)
            .ToListAsync();

        categories.Sort(StringComparer.OrdinalIgnoreCase);

        return Ok(categories);
    }

    private async Task<Domain.ProductCategory> ResolveCategoryAsync(string? name)
    {
        var categoryName = string.IsNullOrWhiteSpace(name) ? "Uncategorized" : name;

        var existing = await _db.ProductCategories
            .FirstOrDefaultAsync(c => c.Name == categoryName);

        return existing ?? new Domain.ProductCategory
        {
            Name = categoryName,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
    }

    private static ProductVM MapToVM(ProductEntity product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Icon = product.Icon,
        BuyingPrice = product.BuyingPrice,
        SellingPrice = product.SellingPrice,
        UnitsInStock = product.UnitsInStock,
        IsActive = product.IsActive,
        IsDiscontinued = product.IsDiscontinued,
        ProductCategoryName = product.ProductCategory?.Name
    };
}
