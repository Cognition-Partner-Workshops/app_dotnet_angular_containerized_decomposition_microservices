using Microsoft.AspNetCore.Mvc;
using Product.API.ViewModels;
using Product.Domain.Interfaces;
using ProductEntity = Product.Domain.Entities.Product;

namespace Product.API.Controllers;

/// <summary>
/// Products. Served both at the service root (the gateway strips the /api/products
/// prefix before forwarding) and at /api/products for direct calls to the service.
/// </summary>
[ApiController]
[Route("")]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;

    public ProductsController(IProductRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductVM>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await _repository.GetAllAsync(cancellationToken);
        return Ok(products.Select(ToViewModel));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductVM>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return NotFound();

        return Ok(ToViewModel(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductVM>> Create([FromBody] ProductVM model, CancellationToken cancellationToken)
    {
        if (!await _repository.CategoryExistsAsync(model.ProductCategoryId, cancellationToken))
            return BadRequest($"Product category {model.ProductCategoryId} does not exist.");

        var product = new ProductEntity { Name = model.Name! };
        Apply(model, product);

        var created = await _repository.AddAsync(product, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToViewModel(created));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductVM>> Update(int id, [FromBody] ProductVM model, CancellationToken cancellationToken)
    {
        if (!await _repository.CategoryExistsAsync(model.ProductCategoryId, cancellationToken))
            return BadRequest($"Product category {model.ProductCategoryId} does not exist.");

        var product = new ProductEntity { Id = id, Name = model.Name! };
        Apply(model, product);

        var updated = await _repository.UpdateAsync(product, cancellationToken);
        if (updated is null)
            return NotFound();

        return Ok(ToViewModel(updated));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        return await _repository.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    private static void Apply(ProductVM model, ProductEntity product)
    {
        product.Name = model.Name!;
        product.Description = model.Description;
        product.Icon = model.Icon;
        product.BuyingPrice = model.BuyingPrice;
        product.SellingPrice = model.SellingPrice;
        product.UnitsInStock = model.UnitsInStock;
        product.IsActive = model.IsActive;
        product.IsDiscontinued = model.IsDiscontinued;
        product.ParentId = model.ParentId;
        product.ProductCategoryId = model.ProductCategoryId;
    }

    private static ProductVM ToViewModel(ProductEntity product) => new()
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
        ParentId = product.ParentId,
        ProductCategoryId = product.ProductCategoryId,
        ProductCategoryName = product.ProductCategory?.Name
    };
}
