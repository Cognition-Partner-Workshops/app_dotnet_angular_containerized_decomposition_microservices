using System.Collections.Concurrent;
using AIMonitoringAgent.Models;

namespace AIMonitoringAgent.Services;

public interface IAlertingService
{
    AlertRule AddRule(AlertRule rule);
    bool RemoveRule(string ruleId);
    AlertRule? GetRule(string ruleId);
    IReadOnlyList<AlertRule> GetAllRules();
    AlertNotification? EvaluateMetric(string metricName, string? serviceName, double currentValue);
    IReadOnlyList<AlertNotification> GetRecentAlerts(int count = 50);
}

public class AlertingService : IAlertingService
{
    private readonly IAppInsightsTelemetryService _telemetryService;
    private readonly ILogger<AlertingService> _logger;
    private readonly ConcurrentDictionary<string, AlertRule> _rules = new();
    private readonly ConcurrentQueue<AlertNotification> _recentAlerts = new();
    private const int MaxRecentAlerts = 500;

    public AlertingService(
        IAppInsightsTelemetryService telemetryService,
        ILogger<AlertingService> logger)
    {
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public AlertRule AddRule(AlertRule rule)
    {
        _rules[rule.Id] = rule;
        _logger.LogInformation("Alert rule added: {RuleName} ({RuleId})", rule.Name, rule.Id);
        return rule;
    }

    public bool RemoveRule(string ruleId)
    {
        var removed = _rules.TryRemove(ruleId, out _);
        if (removed)
            _logger.LogInformation("Alert rule removed: {RuleId}", ruleId);
        return removed;
    }

    public AlertRule? GetRule(string ruleId)
    {
        _rules.TryGetValue(ruleId, out var rule);
        return rule;
    }

    public IReadOnlyList<AlertRule> GetAllRules()
    {
        return _rules.Values.OrderBy(r => r.Name).ToList();
    }

    public AlertNotification? EvaluateMetric(string metricName, string? serviceName, double currentValue)
    {
        var matchingRules = _rules.Values
            .Where(r => r.IsEnabled
                && r.MetricName.Equals(metricName, StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrEmpty(r.ServiceName) || r.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var rule in matchingRules)
        {
            if (!IsConditionMet(rule.Condition, currentValue, rule.Threshold))
                continue;

            var notification = new AlertNotification
            {
                AlertRuleId = rule.Id,
                AlertRuleName = rule.Name,
                MetricName = metricName,
                ServiceName = serviceName,
                CurrentValue = currentValue,
                Threshold = rule.Threshold,
                Severity = rule.Severity,
                Message = $"Alert '{rule.Name}': {metricName} is {currentValue:F2} " +
                          $"({rule.Condition} threshold {rule.Threshold:F2})"
            };

            _telemetryService.TrackAlert(notification);

            _recentAlerts.Enqueue(notification);
            while (_recentAlerts.Count > MaxRecentAlerts)
                _recentAlerts.TryDequeue(out _);

            _logger.LogWarning("Alert fired: {Message}", notification.Message);
            return notification;
        }

        return null;
    }

    public IReadOnlyList<AlertNotification> GetRecentAlerts(int count = 50)
    {
        return _recentAlerts
            .OrderByDescending(a => a.FiredAt)
            .Take(count)
            .ToList();
    }

    private static bool IsConditionMet(AlertCondition condition, double current, double threshold)
    {
        return condition switch
        {
            AlertCondition.GreaterThan => current > threshold,
            AlertCondition.LessThan => current < threshold,
            AlertCondition.EqualTo => Math.Abs(current - threshold) < 0.001,
            AlertCondition.GreaterThanOrEqual => current >= threshold,
            AlertCondition.LessThanOrEqual => current <= threshold,
            _ => false
        };
    }
}
