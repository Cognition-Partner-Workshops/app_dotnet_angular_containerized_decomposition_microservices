# AI Monitoring Agent

Azure Application Insights-powered monitoring agent for the microservices platform. Provides AI model performance tracking, service health monitoring, anomaly detection, and configurable alerting.

## Features

- **AI Model Performance Tracking** — Track latency, token usage, success/failure rates for AI/ML model invocations
- **Service Health Monitoring** — Periodic health checks against all microservice endpoints with availability telemetry
- **Anomaly Detection** — Statistical anomaly detection using rolling-window standard deviation analysis
- **Configurable Alerting** — Define alert rules with thresholds, conditions, and severity levels
- **Application Insights Integration** — All telemetry (events, metrics, availability, exceptions) sent to Azure App Insights
- **Dashboard API** — Aggregated view of AI models, service health, and recent anomalies

## API Endpoints

### Monitoring

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/monitoring/ai-model/track` | Track an AI model invocation |
| `GET` | `/api/monitoring/ai-model/summary` | Get summaries for all tracked models |
| `GET` | `/api/monitoring/ai-model/summary/{modelName}` | Get summary for a specific model |
| `GET` | `/api/monitoring/ai-model/invocations` | Get recent invocations |
| `GET` | `/api/monitoring/health/services` | Get latest health statuses |
| `POST` | `/api/monitoring/health/check` | Trigger health check for all services |
| `POST` | `/api/monitoring/health/check/{serviceName}` | Check a specific service |
| `GET` | `/api/monitoring/anomalies` | Get recent anomalies |
| `POST` | `/api/monitoring/metrics/custom` | Track a custom metric |
| `GET` | `/api/monitoring/dashboard` | Get aggregated dashboard data |

### Alerts

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/alerts/rules` | List all alert rules |
| `GET` | `/api/alerts/rules/{ruleId}` | Get a specific alert rule |
| `POST` | `/api/alerts/rules` | Create an alert rule |
| `DELETE` | `/api/alerts/rules/{ruleId}` | Delete an alert rule |
| `POST` | `/api/alerts/evaluate` | Evaluate a metric against rules |
| `GET` | `/api/alerts/recent` | Get recent alert notifications |

## Configuration

Set the App Insights connection string in `appsettings.json` or via environment variable:

```json
{
  "Monitoring": {
    "ApplicationInsightsConnectionString": "InstrumentationKey=...;IngestionEndpoint=...",
    "HealthCheckIntervalSeconds": 30,
    "AnomalySensitivityMultiplier": 2.0,
    "ServiceEndpoints": {
      "identity-service": "http://localhost:5001/healthz"
    },
    "DefaultAlertRules": [
      {
        "Name": "High Response Time",
        "MetricName": "ResponseTime",
        "Condition": "GreaterThan",
        "Threshold": 5000,
        "Severity": "Warning"
      }
    ]
  }
}
```

## Running

```bash
# Standalone
cd src/Services/Monitoring/AIMonitoringAgent
dotnet run

# With Docker Compose (all services)
cd src
docker compose up --build
```

The agent runs on port **5006** and exposes Swagger UI at `/swagger` in development mode.

## Usage Example

Track an AI model invocation:

```bash
curl -X POST http://localhost:5006/api/monitoring/ai-model/track \
  -H "Content-Type: application/json" \
  -d '{
    "modelName": "gpt-4",
    "modelVersion": "0613",
    "latency": 1250.5,
    "tokensUsed": 450,
    "promptTokens": 200,
    "completionTokens": 250,
    "isSuccessful": true
  }'
```

Create an alert rule:

```bash
curl -X POST http://localhost:5006/api/alerts/rules \
  -H "Content-Type: application/json" \
  -d '{
    "name": "High Latency Alert",
    "metricName": "ResponseTime",
    "condition": "GreaterThan",
    "threshold": 3000,
    "severity": "Warning"
  }'
```
