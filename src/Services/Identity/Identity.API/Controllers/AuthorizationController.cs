using Identity.API.Services;
using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// OAuth2-style token endpoint. Reachable through the gateway at
/// external POST /api/identity/connect/token (the gateway strips /api/identity,
/// so this service exposes it at /connect/token).
/// </summary>
[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _tokenService;

    public AuthorizationController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokenService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _tokenService = tokenService;
    }

    [HttpPost("/connect/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange([FromForm] TokenRequest request)
    {
        if (!string.Equals(request.grant_type, "password", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "unsupported_grant_type" });

        if (string.IsNullOrWhiteSpace(request.username) || string.IsNullOrWhiteSpace(request.password))
            return BadRequest(new { error = "invalid_request", error_description = "Username or password cannot be empty." });

        var user = await _userManager.FindByNameAsync(request.username)
                   ?? await _userManager.FindByEmailAsync(request.username);

        if (user is null || !user.IsEnabled)
            return Unauthorized(new { error = "invalid_grant", error_description = "Invalid username or password." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return Unauthorized(new { error = "invalid_grant", error_description = "Invalid username or password." });

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresIn) = _tokenService.CreateAccessToken(user, roles);

        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = expiresIn,
        });
    }

    public class TokenRequest
    {
        public string? grant_type { get; set; }
        public string? username { get; set; }
        public string? password { get; set; }
        public string? scope { get; set; }
    }
}
