using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Order.Domain.Interfaces;
using Order.Domain.Models;

namespace Order.Infrastructure.Http;

/// <summary>
/// Reads related data owned by the Customer, Product and Identity services over HTTP through
/// the gateway. Never throws: a failed or unrecognised response yields null so the order
/// response degrades to ids only.
/// </summary>
public class GatewayEnrichmentClient : IOrderEnrichmentClient
{
    public const string HttpClientName = "gateway";

    private static readonly string[] NameKeys = ["name", "fullName", "productName", "customerName", "displayName", "userName", "title"];
    private static readonly string[] EmailKeys = ["email", "emailAddress"];

    private readonly HttpClient _httpClient;
    private readonly GatewayOptions _options;
    private readonly ILogger<GatewayEnrichmentClient> _logger;

    public GatewayEnrichmentClient(HttpClient httpClient, IOptions<GatewayOptions> options, ILogger<GatewayEnrichmentClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task<RelatedParty?> GetCustomerAsync(int customerId, CancellationToken cancellationToken = default) =>
        GetAsync($"{_options.CustomerPath}/{customerId}", customerId.ToString(), cancellationToken);

    public Task<RelatedParty?> GetProductAsync(int productId, CancellationToken cancellationToken = default) =>
        GetAsync($"{_options.ProductPath}/{productId}", productId.ToString(), cancellationToken);

    public Task<RelatedParty?> GetCashierAsync(string cashierId, CancellationToken cancellationToken = default) =>
        GetAsync($"{_options.IdentityUserPath}/{Uri.EscapeDataString(cashierId)}", cashierId, cancellationToken);

    private async Task<RelatedParty?> GetAsync(string path, string id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Enrichment lookup {Path} returned {StatusCode}", path, (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return null;

            var name = FindString(root, NameKeys);
            var email = FindString(root, EmailKeys);
            if (name is null && email is null)
                return null;

            return new RelatedParty(id, name, email);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogDebug(ex, "Enrichment lookup {Path} failed; degrading response", path);
            return null;
        }
    }

    private static string? FindString(JsonElement element, string[] keys)
    {
        foreach (var key in keys)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (!string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (property.Value.ValueKind == JsonValueKind.String)
                    return property.Value.GetString();
            }
        }

        return null;
    }
}
