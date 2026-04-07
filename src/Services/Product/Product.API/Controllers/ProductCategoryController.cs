using Microsoft.AspNetCore.Mvc;
using Product.API.DTOs;
using Product.Domain.Entities;
using Product.Domain.Interfaces;

namespace Product.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductCategoryController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductCategoryController> _logger;

    public ProductCategoryController(IProductRepository repository, ILogger<ProductCategoryController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
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

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _repository.GetCategoryByIdAsync(id);
        if (category == null)
            return NotFound();

        return Ok(new ProductCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Icon = category.Icon
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductCategoryDto dto)
    {
        var category = new ProductCategory
        {
            Name = dto.Name,
            Description = dto.Description,
            Icon = dto.Icon
        };

        var created = await _repository.AddCategoryAsync(category);
        _logger.LogInformation("ProductCategory {CategoryId} created: {CategoryName}", created.Id, created.Name);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, new ProductCategoryDto
        {
            Id = created.Id,
            Name = created.Name,
            Description = created.Description,
            Icon = created.Icon
        });
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductCategoryDto dto)
    {
        var existing = await _repository.GetCategoryByIdAsync(id);
        if (existing == null)
            return NotFound();

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Icon = dto.Icon;

        await _repository.UpdateCategoryAsync(existing);
        _logger.LogInformation("ProductCategory {CategoryId} updated: {CategoryName}", id, dto.Name);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _repository.GetCategoryByIdAsync(id);
        if (existing == null)
            return NotFound();

        await _repository.DeleteCategoryAsync(id);
        _logger.LogInformation("ProductCategory {CategoryId} deleted", id);

        return NoContent();
    }
}
