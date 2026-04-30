namespace AIMonitoringAgent.Models;

public class AlertRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public AlertCondition Condition { get; set; } = AlertCondition.GreaterThan;
    public double Threshold { get; set; }
    public int EvaluationWindowMinutes { get; set; } = 5;
    public AlertSeverityLevel Severity { get; set; } = AlertSeverityLevel.Warning;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum AlertCondition
{
    GreaterThan,
    LessThan,
    EqualTo,
    GreaterThanOrEqual,
    LessThanOrEqual
}

public enum AlertSeverityLevel
{
    Information,
    Warning,
    Error,
    Critical
}

public class AlertNotification
{
    public string AlertRuleId { get; set; } = string.Empty;
    public string AlertRuleName { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public double CurrentValue { get; set; }
    public double Threshold { get; set; }
    public AlertSeverityLevel Severity { get; set; }
    public DateTime FiredAt { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
}
