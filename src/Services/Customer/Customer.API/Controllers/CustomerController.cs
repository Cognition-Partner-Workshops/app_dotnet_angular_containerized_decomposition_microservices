using Customer.API.ViewModels;
using Customer.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = Customer.Domain.Entities.Customer;
using GenderEnum = Customer.Domain.Entities.Gender;

namespace Customer.API.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
public class CustomerController : ControllerBase
{
    private readonly CustomerDbContext _dbContext;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(CustomerDbContext dbContext, ILogger<CustomerController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // External: GET /api/customers  ->  service GET /
    [HttpGet("/")]
    [ProducesResponseType(typeof(IEnumerable<CustomerVM>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<CustomerVM>>> GetAll()
    {
        var customers = await _dbContext.Customers
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(customers.Select(ToViewModel));
    }

    // External: GET /api/customers/{id}  ->  service GET /{id}
    [HttpGet("/{id:int}")]
    [ProducesResponseType(typeof(CustomerVM), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerVM>> GetById(int id)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer == null)
            return NotFound();

        return Ok(ToViewModel(customer));
    }

    // External: POST /api/customers  ->  service POST /
    [HttpPost("/")]
    [ProducesResponseType(typeof(CustomerVM), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerVM>> Create([FromBody] CustomerVM model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Customer name cannot be empty");
        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError(nameof(model.Email), "Email cannot be empty");
        if (string.IsNullOrWhiteSpace(model.Gender))
            ModelState.AddModelError(nameof(model.Gender), "Gender cannot be empty");
        else if (!Enum.TryParse<GenderEnum>(model.Gender, ignoreCase: true, out var parsedGender) || !Enum.IsDefined(parsedGender))
            ModelState.AddModelError(nameof(model.Gender), "Invalid gender value");
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var customer = new CustomerEntity
        {
            Name = model.Name!,
            Email = model.Email!,
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            City = model.City,
            Gender = ParseGender(model.Gender),
            CreatedBy = CurrentUser(),
            UpdatedBy = CurrentUser(),
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        var vm = ToViewModel(customer);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, vm);
    }

    // External: PUT /api/customers/{id}  ->  service PUT /{id}
    [HttpPut("/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerVM model)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Customer name cannot be empty");
        if (string.IsNullOrWhiteSpace(model.Email))
            ModelState.AddModelError(nameof(model.Email), "Email cannot be empty");
        if (string.IsNullOrWhiteSpace(model.Gender))
            ModelState.AddModelError(nameof(model.Gender), "Gender cannot be empty");
        else if (!Enum.TryParse<GenderEnum>(model.Gender, ignoreCase: true, out var parsedGender) || !Enum.IsDefined(parsedGender))
            ModelState.AddModelError(nameof(model.Gender), "Invalid gender value");
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        customer.Name = model.Name!;
        customer.Email = model.Email!;
        customer.PhoneNumber = model.PhoneNumber;
        customer.Address = model.Address;
        customer.City = model.City;
        customer.Gender = ParseGender(model.Gender);
        customer.UpdatedBy = CurrentUser();
        customer.UpdatedDate = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    // External: DELETE /api/customers/{id}  ->  service DELETE /{id}
    [HttpDelete("/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer == null)
            return NotFound();

        _dbContext.Customers.Remove(customer);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    private static CustomerVM ToViewModel(CustomerEntity c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Email = c.Email,
        PhoneNumber = c.PhoneNumber,
        Address = c.Address,
        City = c.City,
        Gender = c.Gender.ToString()
    };

    private static GenderEnum ParseGender(string? value) =>
        Enum.TryParse<GenderEnum>(value, ignoreCase: true, out var gender) ? gender : GenderEnum.None;

    // CreatedBy/UpdatedBy column is varchar(40); truncate the JWT identity to fit.
    private string? CurrentUser()
    {
        var name = User.Identity?.Name;
        if (string.IsNullOrEmpty(name))
            return null;
        return name.Length > 40 ? name[..40] : name;
    }
}
