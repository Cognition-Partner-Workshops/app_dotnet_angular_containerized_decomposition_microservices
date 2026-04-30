using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeAgent.Core.Interfaces;
using KnowledgeAgent.Core.Models;
using KnowledgeAgent.Infrastructure.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeAgent.Infrastructure.Services;

public class AppInsightsService : IAppInsightsService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly HttpClient _httpClient;
    private readonly AppInsightsOptions _options;
    private readonly ILogger<AppInsightsService> _logger;

    public AppInsightsService(
        TelemetryClient telemetryClient,
        IHttpClientFactory httpClientFactory,
        IOptions<AppInsightsOptions> options,
        ILogger<AppInsightsService> logger)
    {
        _telemetryClient = telemetryClient;
        _httpClient = httpClientFactory.CreateClient("AppInsights");
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.applicationinsights.io/v1/");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
    }

    public async Task<IEnumerable<AppInsightAlert>> GetRecentAlertsAsync(
        TimeSpan lookbackPeriod, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching alerts from the last {Minutes} minutes", lookbackPeriod.TotalMinutes);

        var alerts = new List<AppInsightAlert>();

        try
        {
            var query = $"exceptions | where timestamp > ago({lookbackPeriod.TotalMinutes}m) | order by timestamp desc | take 50";
            var response = await QueryAppInsightsAsync(query, cancellationToken);

            if (response != null)
            {
                alerts.AddRange(ParseExceptionResults(response));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching alerts from Application Insights");
        }

        return alerts;
    }

    public async Task<IEnumerable<AppInsightAlert>> GetActiveExceptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var lookbackMinutes = _options.AlertLookbackMinutes;
        return await GetRecentAlertsAsync(TimeSpan.FromMinutes(lookbackMinutes), cancellationToken);
    }

    public void TrackEvent(string eventName, Dictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackEvent(eventName, properties);
    }

    public void TrackException(Exception exception, Dictionary<string, string>? properties = null)
    {
        var telemetry = new ExceptionTelemetry(exception);
        if (properties != null)
        {
            foreach (var kvp in properties)
            {
                telemetry.Properties[kvp.Key] = kvp.Value;
            }
        }
        _telemetryClient.TrackException(telemetry);
    }

    private async Task<JsonDocument?> QueryAppInsightsAsync(string query, CancellationToken cancellationToken)
    {
        var requestUri = $"apps/{_options.ApplicationId}/query?query={Uri.EscapeDataString(query)}";

        var response = await _httpClient.GetAsync(requestUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Application Insights query failed with status: {StatusCode}", response.StatusCode);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonDocument.Parse(content);
    }

    private static IEnumerable<AppInsightAlert> ParseExceptionResults(JsonDocument response)
    {
        var alerts = new List<AppInsightAlert>();

        if (!response.RootElement.TryGetProperty("tables", out var tables))
            return alerts;

        foreach (var table in tables.EnumerateArray())
        {
            if (!table.TryGetProperty("rows", out var rows))
                continue;

            foreach (var row in rows.EnumerateArray())
            {
                var alert = new AppInsightAlert
                {
                    AlertId = Guid.NewGuid().ToString(),
                    AlertName = "Exception Detected",
                    Severity = "Error",
                    FiredAt = DateTime.UtcNow
                };

                if (row.GetArrayLength() > 0)
                    alert.ExceptionType = row[0].GetString() ?? string.Empty;
                if (row.GetArrayLength() > 1)
                    alert.ExceptionMessage = row[1].GetString() ?? string.Empty;
                if (row.GetArrayLength() > 2)
                    alert.StackTrace = row[2].GetString() ?? string.Empty;

                alert.Description = $"{alert.ExceptionType}: {alert.ExceptionMessage}";
                alerts.Add(alert);
            }
        }

        return alerts;
    }
}
