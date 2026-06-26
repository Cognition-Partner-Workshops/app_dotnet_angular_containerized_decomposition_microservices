using System.Security.Claims;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Protected Identity resource. Reachable through the gateway at external GET /api/identity
/// (the gateway strips /api/identity, so this service exposes it at root /).
/// An unauthenticated request returns 401.
/// </summary>
[ApiController]
[Authorize]
public class IdentityController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("/")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirstValue("sub");
        var name = User.FindFirstValue("name");
        var email = User.FindFirstValue("email");
        var roles = User.FindAll("role").Select(c => c.Value).ToArray();

        return Ok(new
        {
            id = userId,
            name,
            email,
            roles,
        });
    }

    [HttpGet("/users")]
    public IActionResult GetUsers()
    {
        var users = _userManager.Users
            .Select(u => new { id = u.Id, userName = u.UserName, email = u.Email, fullName = u.FullName })
            .ToList();

        return Ok(users);
    }
}
