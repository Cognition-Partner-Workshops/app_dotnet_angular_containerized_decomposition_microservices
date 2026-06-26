using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Customer.SmokeTests;

public class CustomerSmokeTests
{
    private readonly HttpClient _client;
    private readonly string _gatewayUrl;

    public CustomerSmokeTests()
    {
        _gatewayUrl = Environment.GetEnvironmentVariable("GATEWAY_URL") ?? "http://localhost:5000";
        _client = new HttpClient { BaseAddress = new Uri(_gatewayUrl) };
    }

    [Fact]
    public async Task GetCustomers_ThroughGateway_Returns200WithArray()
    {
        var response = await _client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var customers = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.Equal(JsonValueKind.Array, customers.ValueKind);
    }

    [Fact]
    public async Task GetCustomerById_ThroughGateway_ReturnsSuccessOrNotFound()
    {
        var response = await _client.GetAsync("/api/customers/1");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200 or 404 but got {(int)response.StatusCode}");
    }
}
