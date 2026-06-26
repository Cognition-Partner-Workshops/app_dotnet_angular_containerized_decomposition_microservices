using Customer.API.DTOs;
using Customer.Domain.Entities;
using Customer.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Customer.API.Controllers;

[ApiController]
[Route("")]
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
    public async Task<IActionResult> GetAll()
    {
        var customers = await _repository.GetAllAsync();
        return Ok(customers.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer is null)
            return NotFound();

        return Ok(ToDto(customer));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var customer = new Domain.Entities.Customer
        {
            Name = request.Name,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            City = request.City,
            Gender = Enum.TryParse<Gender>(request.Gender, true, out var g) ? g : Gender.None,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        var created = await _repository.AddAsync(customer);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        existing.Name = request.Name;
        existing.Email = request.Email;
        existing.PhoneNumber = request.PhoneNumber;
        existing.Address = request.Address;
        existing.City = request.City;
        existing.Gender = Enum.TryParse<Gender>(request.Gender, true, out var g) ? g : Gender.None;
        existing.UpdatedDate = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);
        return Ok(ToDto(existing));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        await _repository.DeleteAsync(id);
        return NoContent();
    }

    private static CustomerDto ToDto(Domain.Entities.Customer c) =>
        new(c.Id, c.Name, c.Email, c.PhoneNumber, c.Address, c.City,
            c.Gender.ToString(), c.CreatedDate, c.UpdatedDate);
}

public record CreateCustomerRequest(
    string Name,
    string Email,
    string? PhoneNumber,
    string? Address,
    string? City,
    string? Gender);

public record UpdateCustomerRequest(
    string Name,
    string Email,
    string? PhoneNumber,
    string? Address,
    string? City,
    string? Gender);
