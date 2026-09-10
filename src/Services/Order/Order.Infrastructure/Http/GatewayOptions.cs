namespace Order.Infrastructure.Http;

public class GatewayOptions
{
    public const string SectionName = "Gateway";

    /// <summary>Base address of the API gateway on the compose network.</summary>
    public string BaseUrl { get; set; } = "http://api-gateway:5000";

    public string CustomerPath { get; set; } = "/api/customers";
    public string ProductPath { get; set; } = "/api/products";
    public string IdentityUserPath { get; set; } = "/api/identity/users";

    public int TimeoutSeconds { get; set; } = 3;
}
