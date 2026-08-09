using System.Net;
using System.Text;
using System.Text.Json;

namespace IntegrationTests;

/// <summary>
/// Gap checks for the current Order/Product scaffolds. These should fail
/// loudly once the services are implemented and then be deleted.
/// </summary>
public class ScaffoldGapTests
{
    private static readonly Uri OrderUrl = new(
        Environment.GetEnvironmentVariable("ORDER_URL") ?? "http://localhost:5003");
    private static readonly Uri ProductUrl = new(
        Environment.GetEnvironmentVariable("PRODUCT_URL") ?? "http://localhost:5004");

    [Fact]
    public async Task OrderCreateEndpointIsNotYetAvailable()
    {
        using var client = new HttpClient();
        var response = await client.PostAsync(
            new Uri(OrderUrl, "/api/order"),
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task ProductCreateEndpointIsNotYetAvailable()
    {
        using var client = new HttpClient();
        var response = await client.PostAsync(
            new Uri(ProductUrl, "/api/product"),
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task OrderGetStillExposesScaffoldMarker()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(new Uri(OrderUrl, "/api/order"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Order", json.RootElement.GetProperty("service").GetString());
        Assert.Equal("scaffold", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ProductGetStillExposesScaffoldMarker()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(new Uri(ProductUrl, "/api/product"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Product", json.RootElement.GetProperty("service").GetString());
        Assert.Equal("scaffold", json.RootElement.GetProperty("status").GetString());
    }
}
