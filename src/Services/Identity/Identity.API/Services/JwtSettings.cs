namespace Identity.API.Services;

/// <summary>
/// Canonical JWT contract values, bound from the "Jwt" configuration section.
/// </summary>
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "quickapp-identity";
    public string Audience { get; set; } = "quickapp";
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
