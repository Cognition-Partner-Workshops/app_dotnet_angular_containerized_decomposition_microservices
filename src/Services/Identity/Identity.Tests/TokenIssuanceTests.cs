using System.Net;
using System.Net.Http.Json;

namespace Identity.Tests;

public class TokenIssuanceTests(IdentityWebApplicationFactory factory) : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly IdentityWebApplicationFactory _factory = factory;

    [Fact]
    public async Task PasswordGrant_WithSeededCredentials_IssuesAccessAndRefreshTokens()
    {
        var client = _factory.CreateClient();

        var response = await client.RequestPasswordTokenAsync(
            TestClientExtensions.AdminUserName, TestClientExtensions.DefaultPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();

        Assert.NotNull(token);
        Assert.False(string.IsNullOrWhiteSpace(token!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(token.RefreshToken));
        Assert.Equal("Bearer", token.TokenType);
    }

    [Fact]
    public async Task PasswordGrant_WithInvalidCredentials_IsRejected()
    {
        var client = _factory.CreateClient();

        var response = await client.RequestPasswordTokenAsync(
            TestClientExtensions.AdminUserName, "wrong-password");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = (await response.Content.ReadAsStringAsync()).ReadJson();

        Assert.Equal("invalid_grant", payload.GetProperty("error").GetString());
    }

    [Fact]
    public async Task RefreshTokenGrant_WithIssuedRefreshToken_IssuesNewAccessToken()
    {
        var client = _factory.CreateClient();
        var token = await client.GetTokenAsync();

        var response = await client.RequestRefreshTokenAsync(token.RefreshToken!);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var refreshed = await response.Content.ReadFromJsonAsync<TokenResponse>();

        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrWhiteSpace(refreshed!.AccessToken));
    }

    [Fact]
    public async Task AccessToken_AuthenticatesAgainstAccountEndpoints()
    {
        var client = await _factory.CreateClient().AuthenticateAsync();

        var response = await client.GetAsync("/api/account/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = (await response.Content.ReadAsStringAsync()).ReadJson();

        Assert.Equal(TestClientExtensions.AdminUserName, payload.GetProperty("userName").GetString());
    }

    [Fact]
    public async Task AccountEndpoints_WithoutToken_AreUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/account/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
