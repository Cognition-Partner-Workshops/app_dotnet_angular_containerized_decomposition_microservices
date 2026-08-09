using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace IntegrationTests;

public class ScaffoldFlowTests
{
    private static readonly Uri OrderUrl = new(
        Environment.GetEnvironmentVariable("ORDER_URL") ?? "http://localhost:5003");
    private static readonly Uri ProductUrl = new(
        Environment.GetEnvironmentVariable("PRODUCT_URL") ?? "http://localhost:5004");

    [Fact]
    public async Task OrderEndpointReportsScaffoldStatus()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(new Uri(OrderUrl, "/api/order"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Order", json.RootElement.GetProperty("service").GetString());
        Assert.Equal("scaffold", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ProductEndpointReportsScaffoldStatus()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(new Uri(ProductUrl, "/api/product"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Product", json.RootElement.GetProperty("service").GetString());
        Assert.Equal("scaffold", json.RootElement.GetProperty("status").GetString());
    }
}
