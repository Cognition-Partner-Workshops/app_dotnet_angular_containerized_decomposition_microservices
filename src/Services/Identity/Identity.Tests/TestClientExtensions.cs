using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Identity.Tests;

public record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("token_type")] string TokenType);

public static class TestClientExtensions
{
    public const string AdminUserName = "admin";
    public const string StandardUserName = "user";
    public const string DefaultPassword = "tempP@ss123";

    public static async Task<HttpResponseMessage> RequestPasswordTokenAsync(
        this HttpClient client, string userName, string password)
    {
        return await client.PostAsync("/connect/token", new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", userName),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("scope", "openid email phone profile roles offline_access"),
            new KeyValuePair<string, string>("client_id", "quickapp_spa")
        ]));
    }

    public static async Task<HttpResponseMessage> RequestRefreshTokenAsync(
        this HttpClient client, string refreshToken)
    {
        return await client.PostAsync("/connect/token", new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id", "quickapp_spa")
        ]));
    }

    public static async Task<TokenResponse> GetTokenAsync(
        this HttpClient client, string userName = AdminUserName, string password = DefaultPassword)
    {
        var response = await client.RequestPasswordTokenAsync(userName, password);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    public static async Task<HttpClient> AuthenticateAsync(
        this HttpClient client, string userName = AdminUserName, string password = DefaultPassword)
    {
        var token = await client.GetTokenAsync(userName, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        return client;
    }

    public static JsonElement ReadJson(this string content) => JsonDocument.Parse(content).RootElement;
}
