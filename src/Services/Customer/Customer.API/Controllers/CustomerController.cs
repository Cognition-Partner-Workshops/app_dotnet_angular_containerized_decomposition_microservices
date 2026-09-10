using Customer.API.Mapping;
using Customer.API.ViewModels;
using Customer.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Customer.API.Controllers;

/// <summary>
/// Served both at the service root (the gateway's /api/customers route strips
/// its prefix before forwarding) and at /api/customers for direct calls.
/// </summary>
[ApiController]
[Route("/")]
[Route("api/customers")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerRepository _repository;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(ICustomerRepository repository, ILogger<CustomerController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerVM>>> GetAll()
    {
        var customers = await _repository.GetAllCustomersDataAsync();
        return Ok(CustomerMapper.ToViewModels(customers));
    }

    [HttpGet("top-active/{count:int}")]
    public async Task<ActionResult<IEnumerable<CustomerVM>>> GetTopActiveCustomers(int count)
    {
        if (count <= 0)
            return BadRequest("Count must be greater than zero");

        var customers = await _repository.GetTopActiveCustomersAsync(count);
        return Ok(CustomerMapper.ToViewModels(customers));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerVM>> GetById(int id)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer is null)
            return NotFound();

        return Ok(CustomerMapper.ToViewModel(customer));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerVM>> Post([FromBody] CustomerVM model)
    {
        if (Validate(model) is { } error)
            return BadRequest(error);

        var created = await _repository.AddAsync(CustomerMapper.ToEntity(model));
        _logger.LogInformation("Created customer {CustomerId}", created.Id);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, CustomerMapper.ToViewModel(created));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerVM>> Put(int id, [FromBody] CustomerVM model)
    {
        if (Validate(model) is { } error)
            return BadRequest(error);

        var updated = await _repository.UpdateAsync(id, CustomerMapper.ToEntity(model));
        if (updated is null)
            return NotFound();

        return Ok(CustomerMapper.ToViewModel(updated));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Mirrors the monolith's CustomerViewModelValidator rules.
    /// </summary>
    private static string? Validate(CustomerVM? model)
    {
        if (model is null)
            return "Customer payload cannot be empty";

        if (string.IsNullOrWhiteSpace(model.Name))
            return "Customer name cannot be empty";

        if (string.IsNullOrWhiteSpace(model.Gender))
            return "Gender cannot be empty";

        if (CustomerMapper.ParseGender(model.Gender) is null)
            return $"'{model.Gender}' is not a valid gender";

        return null;
    }
}
