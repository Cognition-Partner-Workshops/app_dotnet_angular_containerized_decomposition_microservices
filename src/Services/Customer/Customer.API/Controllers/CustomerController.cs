using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Customer.API.DTOs;
using Customer.Domain.Interfaces;
using Customer.Domain.Enums;
using Entities = Customer.Domain.Entities;

namespace Customer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(ICustomerService customerService, ILogger<CustomerController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers.Select(MapToDto));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer is null) return NotFound();
        return Ok(MapToDto(customer));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CustomerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest("Name and Email are required.");

        var customer = new Entities.Customer
        {
            Name = dto.Name,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            City = dto.City,
            Gender = Enum.TryParse<Gender>(dto.Gender, true, out var g) ? g : Gender.None
        };

        var created = await _customerService.CreateAsync(customer);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerDto dto)
    {
        var existing = await _customerService.GetByIdAsync(id);
        if (existing is null) return NotFound();

        existing.Name = dto.Name ?? existing.Name;
        existing.Email = dto.Email ?? existing.Email;
        existing.PhoneNumber = dto.PhoneNumber;
        existing.Address = dto.Address;
        existing.City = dto.City;
        existing.Gender = Enum.TryParse<Gender>(dto.Gender, true, out var g) ? g : existing.Gender;

        await _customerService.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _customerService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    private static CustomerDto MapToDto(Entities.Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Email = c.Email,
        PhoneNumber = c.PhoneNumber,
        Address = c.Address,
        City = c.City,
        Gender = c.Gender.ToString()
    };
}
