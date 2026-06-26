using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Identity.API.Services;

/// <summary>
/// Issues HS256-signed JWT access tokens. The values produced here form the canonical
/// JWT contract that every other QuickApp microservice validates against.
/// </summary>
public class JwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(JwtSettings settings)
    {
        _settings = settings;
    }

    public (string AccessToken, int ExpiresInSeconds) CreateAccessToken(
        ApplicationUser user, IEnumerable<string> roles)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_settings.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new("sub", user.Id),
            new("name", string.IsNullOrWhiteSpace(user.FullName) ? user.UserName ?? user.Id : user.FullName),
            new("email", user.Email ?? string.Empty),
        };

        foreach (var role in roles)
            claims.Add(new Claim("role", role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        var handler = new JwtSecurityTokenHandler();
        handler.OutboundClaimTypeMap.Clear();
        var jwt = handler.WriteToken(token);

        return (jwt, (int)(expires - now).TotalSeconds);
    }
}
