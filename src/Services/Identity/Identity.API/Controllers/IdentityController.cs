using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Identity.Domain.DTOs;
using Identity.Domain.Interfaces;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IdentityController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public IdentityController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AppUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _identityService.RegisterAsync(request);
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });
        return CreatedAtAction(nameof(GetById), new { id = result.User!.Id }, result.User);
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _identityService.LoginAsync(request);
        if (!result.Success)
            return Unauthorized(new { error = result.ErrorMessage });
        return Ok(new { token = result.Token });
    }

    [HttpGet("users")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<AppUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _identityService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("users/{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(AppUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _identityService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();
        return Ok(user);
    }
}
