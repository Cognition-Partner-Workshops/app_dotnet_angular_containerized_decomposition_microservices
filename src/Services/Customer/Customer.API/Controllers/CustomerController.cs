using AutoMapper;
using Customer.API.ViewModels;
using Customer.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Customer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(ICustomerService customerService, IMapper mapper, ILogger<CustomerController> logger)
    {
        _customerService = customerService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerVM>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllCustomersAsync();
        return Ok(_mapper.Map<IEnumerable<CustomerVM>>(customers));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerVM), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null) return NotFound();
        return Ok(_mapper.Map<CustomerVM>(customer));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustomerVM), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CustomerVM customerVm)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var customer = _mapper.Map<Domain.Entities.Customer>(customerVm);
        var created = await _customerService.CreateCustomerAsync(customer);
        var result = _mapper.Map<CustomerVM>(created);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CustomerVM), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerVM customerVm)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var customer = _mapper.Map<Domain.Entities.Customer>(customerVm);
        customer.Id = id;
        var updated = await _customerService.UpdateCustomerAsync(customer);
        if (updated == null) return NotFound();
        return Ok(_mapper.Map<CustomerVM>(updated));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _customerService.DeleteCustomerAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
