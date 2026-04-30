using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monitoring.Functions.Configuration;
using Monitoring.Functions.Models;

namespace Monitoring.Functions.Services;

public sealed class AlertService : IAlertService
{
    private readonly MonitoringOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        IOptions<MonitoringOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<AlertService> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<MonitoringAlert> CreateAlertAsync(
        string serviceName,
        AlertLevel level,
        string title,
        string summary,
        List<AnomalyResult> anomalies,
        CancellationToken cancellationToken = default)
    {
        var alert = new MonitoringAlert(
            AlertId: Guid.NewGuid(),
            ServiceName: serviceName,
            Level: level,
            Title: title,
            Summary: summary,
            DetailedAnalysis: BuildDetailedAnalysis(anomalies),
            Anomalies: anomalies,
            RecommendedActions: anomalies
                .Select(a => a.RecommendedAction)
                .Where(a => !string.IsNullOrEmpty(a))
                .Distinct()
                .ToList(),
            CreatedAt: DateTime.UtcNow,
            Status: AlertStatus.Active
        );

        _logger.LogWarning(
            "Alert created: [{Level}] {Title} for {Service} - {Summary}",
            level, title, serviceName, summary);

        return Task.FromResult(alert);
    }

    public async Task SendAlertNotificationAsync(
        MonitoringAlert alert, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AlertWebhookUrl))
        {
            _logger.LogInformation("No webhook URL configured; alert logged only: {AlertId}", alert.AlertId);
            return;
        }

        var payload = BuildWebhookPayload(alert);
        await PostWebhookAsync(payload, cancellationToken);
    }

    public async Task SendHealthReportNotificationAsync(
        HealthReport report, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AlertWebhookUrl))
        {
            _logger.LogInformation("No webhook URL configured; health report logged only");
            return;
        }

        var payload = BuildHealthReportPayload(report);
        await PostWebhookAsync(payload, cancellationToken);
    }

    private async Task PostWebhookAsync(object payload, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var response = await client.PostAsync(
                _options.AlertWebhookUrl,
                new StringContent(json, Encoding.UTF8, "application/json"),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook notification sent successfully");
            }
            else
            {
                _logger.LogWarning(
                    "Webhook notification failed: {StatusCode} {Reason}",
                    response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook notification");
        }
    }

    private static object BuildWebhookPayload(MonitoringAlert alert)
    {
        var color = alert.Level switch
        {
            AlertLevel.Critical => "#FF0000",
            AlertLevel.Error => "#FF6600",
            AlertLevel.Warning => "#FFAA00",
            _ => "#00AA00"
        };

        var icon = alert.Level switch
        {
            AlertLevel.Critical => "🔴",
            AlertLevel.Error => "🟠",
            AlertLevel.Warning => "🟡",
            _ => "🟢"
        };

        return new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = new object[]
                        {
                            new
                            {
                                type = "TextBlock",
                                text = $"{icon} {alert.Title}",
                                weight = "Bolder",
                                size = "Large",
                                color = "Attention"
                            },
                            new
                            {
                                type = "TextBlock",
                                text = $"**Service:** {alert.ServiceName} | **Level:** {alert.Level}",
                                isSubtle = true
                            },
                            new
                            {
                                type = "TextBlock",
                                text = alert.Summary,
                                wrap = true
                            },
                            new
                            {
                                type = "TextBlock",
                                text = $"**Anomalies:** {alert.Anomalies.Count} detected",
                                spacing = "Medium"
                            },
                            new
                            {
                                type = "TextBlock",
                                text = string.Join("\n",
                                    alert.RecommendedActions.Select(a => $"- {a}")),
                                wrap = true,
                                spacing = "Small"
                            },
                            new
                            {
                                type = "TextBlock",
                                text = $"Alert ID: {alert.AlertId} | {alert.CreatedAt:u}",
                                isSubtle = true,
                                size = "Small"
                            }
                        }
                    }
                }
            }
        };
    }

    private static object BuildHealthReportPayload(HealthReport report)
    {
        var statusIcon = report.OverallStatus switch
        {
            OverallHealthStatus.Healthy => "🟢",
            OverallHealthStatus.Degraded => "🟡",
            _ => "🔴"
        };

        var serviceLines = report.Services
            .Select(s =>
            {
                var sIcon = s.State switch
                {
                    HealthState.Healthy => "🟢",
                    HealthState.Degraded => "🟡",
                    HealthState.Unhealthy => "🔴",
                    _ => "⚪"
                };
                return $"{sIcon} **{s.ServiceName}** — {s.FailureRatePercent:F1}% errors, " +
                       $"{s.AverageResponseTimeMs:F0}ms avg";
            });

        return new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = new object[]
                        {
                            new
                            {
                                type = "TextBlock",
                                text = $"{statusIcon} Platform Health Report",
                                weight = "Bolder",
                                size = "Large"
                            },
                            new
                            {
                                type = "TextBlock",
                                text = report.AiSummary,
                                wrap = true
                            },
                            new
                            {
                                type = "TextBlock",
                                text = string.Join("\n", serviceLines),
                                wrap = true,
                                spacing = "Medium"
                            },
                            new
                            {
                                type = "TextBlock",
                                text = $"Active Alerts: {report.ActiveAlerts.Count} | " +
                                       $"Generated: {report.GeneratedAt:u}",
                                isSubtle = true,
                                size = "Small"
                            }
                        }
                    }
                }
            }
        };
    }

    private static string BuildDetailedAnalysis(List<AnomalyResult> anomalies)
    {
        if (anomalies.Count == 0)
            return "No anomalies detected.";

        var sb = new StringBuilder();
        foreach (var anomaly in anomalies)
        {
            sb.AppendLine($"[{anomaly.Severity}] {anomaly.Type}: {anomaly.Description}");
            if (!string.IsNullOrEmpty(anomaly.AiAnalysis))
                sb.AppendLine($"  Analysis: {anomaly.AiAnalysis}");
            sb.AppendLine($"  Action: {anomaly.RecommendedAction}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
