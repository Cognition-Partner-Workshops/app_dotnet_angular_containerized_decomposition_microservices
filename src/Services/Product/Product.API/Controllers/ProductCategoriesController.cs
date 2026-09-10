using Microsoft.AspNetCore.Mvc;
using Product.API.ViewModels;
using Product.Domain.Entities;
using Product.Domain.Interfaces;

namespace Product.API.Controllers;

[ApiController]
[Route("categories")]
[Route("api/products/categories")]
[Produces("application/json")]
public class ProductCategoriesController : ControllerBase
{
    private readonly IProductCategoryRepository _repository;

    public ProductCategoriesController(IProductCategoryRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductCategoryVM>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await _repository.GetAllAsync(cancellationToken);
        return Ok(categories.Select(ToViewModel));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductCategoryVM>> GetById(int id, CancellationToken cancellationToken)
    {
        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return NotFound();

        return Ok(ToViewModel(category));
    }

    [HttpPost]
    public async Task<ActionResult<ProductCategoryVM>> Create([FromBody] ProductCategoryVM model, CancellationToken cancellationToken)
    {
        var created = await _repository.AddAsync(
            new ProductCategory
            {
                Name = model.Name!,
                Description = model.Description,
                Icon = model.Icon
            },
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToViewModel(created));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductCategoryVM>> Update(int id, [FromBody] ProductCategoryVM model, CancellationToken cancellationToken)
    {
        var updated = await _repository.UpdateAsync(
            new ProductCategory
            {
                Id = id,
                Name = model.Name!,
                Description = model.Description,
                Icon = model.Icon
            },
            cancellationToken);

        if (updated is null)
            return NotFound();

        return Ok(ToViewModel(updated));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        return await _repository.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    private static ProductCategoryVM ToViewModel(ProductCategory category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        Icon = category.Icon
    };
}
