using Microsoft.AspNetCore.Mvc;
using Product.Domain.Entities;
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
    public async Task<IActionResult> GetAll()
    {
        var products = await _repository.GetAllAsync();
        return Ok(products.Select(p => new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.StockQuantity,
            p.CreatedAt
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product is null)
            return NotFound();

        return Ok(new
        {
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Product name is required." });

        if (request.Price < 0)
            return BadRequest(new { error = "Price must be non-negative." });

        var product = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(product);
        _logger.LogInformation("Created product {ProductId}: {ProductName}", product.Id, product.Name);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, new
        {
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.CreatedAt
        });
    }
}

public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity);
