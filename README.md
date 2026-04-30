# Decomposed .NET Microservices — Target State

This repository is the **target scaffolding** for decomposing the monolithic QuickApp application into cloud-native .NET microservices deployed on Kubernetes.

## Source Monolith

The before-state monolith lives in [`app_dotnet_angular_containerized_decomposition_monolith`](https://github.com/Cognition-Partner-Workshops/app_dotnet_angular_containerized_decomposition_monolith).

## Architecture

The monolith's bounded contexts are decomposed into the following independently deployable microservices:

```
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  Identity    │  │  Customer    │  │   Order      │
│  Service     │  │  Service     │  │   Service    │
│  (.NET 10)   │  │  (.NET 10)   │  │  (.NET 10)   │
└──────┬───────┘  └──────┬───────┘  └──────┬───────┘
       │                 │                 │
       └────────────┬────┘─────────────────┘
                    │
              ┌─────┴──────┐
              │  API       │
              │  Gateway   │
              └─────┬──────┘
                    │
┌──────────────┐  ┌┴─────────────┐
│  Product     │  │ Notification │
│  Service     │  │  Service     │
│  (.NET 10)   │  │  (.NET 10)   │
└──────────────┘  └──────────────┘
```

## Services

| Service | Port | Description | Monolith Origin |
|---------|------|-------------|-----------------|
| `identity-service` | 5001 | Authentication, authorization, user/role management | `AuthorizationController`, `UserAccountController`, `UserRoleController` |
| `customer-service` | 5002 | Customer CRUD and lookup | `CustomerController`, customer models |
| `order-service` | 5003 | Order management and processing | `OrdersController`, order models |
| `product-service` | 5004 | Product catalog management | `ProductsController`, product models |
| `notification-service` | 5005 | Email and in-app notifications | `NotificationService`, notification models |
| `api-gateway` | 5000 | YARP reverse proxy, request routing, rate limiting | New — replaces monolith's single entry point |
| `monitoring-agent` | 7071 | AI-powered App Insights monitoring Azure Function | New — observability and anomaly detection |

## Project Structure

```
src/
├── ApiGateway/                    # YARP-based API gateway
│   ├── Program.cs
│   ├── appsettings.json
│   ├── ApiGateway.csproj
│   └── Dockerfile
├── Services/
│   ├── Identity/
│   │   ├── Identity.API/          # ASP.NET Core Web API
│   │   ├── Identity.Domain/       # Domain entities and interfaces
│   │   └── Identity.Infrastructure/ # EF Core, external integrations
│   ├── Customer/
│   │   ├── Customer.API/
│   │   ├── Customer.Domain/
│   │   └── Customer.Infrastructure/
│   ├── Order/
│   │   ├── Order.API/
│   │   ├── Order.Domain/
│   │   └── Order.Infrastructure/
│   ├── Product/
│   │   ├── Product.API/
│   │   ├── Product.Domain/
│   │   └── Product.Infrastructure/
│   └── Notification/
│       ├── Notification.API/
│       ├── Notification.Domain/
│       └── Notification.Infrastructure/
├── Functions/
│   └── Monitoring/
│       └── Monitoring.Functions/   # Azure Function — AI monitoring agent
│           ├── Functions/          # Timer & HTTP-triggered functions
│           ├── Services/           # App Insights query, AI analysis, alerting
│           ├── Models/             # Telemetry, anomaly, alert models
│           ├── Configuration/      # Monitoring options
│           ├── Program.cs
│           ├── host.json
│           └── Dockerfile
├── Shared/
│   ├── Shared.Contracts/          # Shared DTOs, events, interfaces
│   └── Shared.Infrastructure/     # Common middleware, logging, health checks
├── docker-compose.yml
├── docker-compose.override.yml
└── Microservices.sln
```

## Technology Stack

- **.NET 10** — ASP.NET Core Web API per service
- **Entity Framework Core** — per-service database (database-per-service pattern)
- **YARP** — API gateway / reverse proxy
- **RabbitMQ** — async messaging between services
- **Azure Functions v4** — serverless monitoring agent (isolated worker)
- **Azure Application Insights** — telemetry collection and querying
- **Azure OpenAI** — AI-powered anomaly analysis and health insights
- **Docker** — containerized services
- **Kubernetes** — orchestration (see `app_dotnet_angular_containerized_decomposition_iac` for Helm charts)

## Getting Started

Each service can be run independently:

```bash
# Run all services with Docker Compose
docker compose up --build

# Run a single service
cd src/Services/Identity/Identity.API
dotnet run
```

## AI Monitoring Agent

The `monitoring-agent` is an Azure Function (C#, .NET 10, isolated worker) that provides AI-powered observability for all microservices via Azure Application Insights.

### Functions

| Function | Trigger | Schedule | Description |
|----------|---------|----------|-------------|
| `AnomalyDetector` | Timer | Every 5 min | Queries App Insights telemetry and detects anomalies using rule-based + statistical (z-score) + AI analysis |
| `HealthMonitor` | Timer | Every 30 min | Generates comprehensive health reports with AI summaries and sends webhook notifications |
| `GetHealthReport` | HTTP GET | On-demand | Returns full platform health report with per-service AI insights (`/api/monitoring/health`) |
| `GetServiceTelemetry` | HTTP GET | On-demand | Returns detailed telemetry and AI analysis for a specific service (`/api/monitoring/services/{name}`) |
| `GetAnomalies` | HTTP GET | On-demand | Lists all detected anomalies across services (`/api/monitoring/anomalies`) |
| `AlertWebhook` | HTTP POST | On-demand | Receives App Insights alert webhooks and enriches with AI analysis (`/api/monitoring/alerts/webhook`) |
| `TriggerManualAnalysis` | HTTP POST | On-demand | Triggers on-demand AI analysis for a specific service (`/api/monitoring/analyze/{name}`) |

### Capabilities

- **Anomaly Detection**: Rule-based thresholds (failure rate, response time) + statistical z-score analysis on time-series data
- **AI-Powered Insights**: Azure OpenAI generates root cause analysis, health summaries, and exception pattern analysis
- **Webhook Alerts**: Sends adaptive card notifications to Microsoft Teams or Slack
- **KQL Queries**: Queries App Insights via Azure Monitor Query SDK (requests, exceptions, dependencies)

### Configuration

Set the following environment variables (or `local.settings.json` values):

| Variable | Description |
|----------|-------------|
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights connection string |
| `Monitoring__WorkspaceId` | Log Analytics workspace ID |
| `Monitoring__AzureOpenAIEndpoint` | Azure OpenAI endpoint URL |
| `Monitoring__AzureOpenAIDeployment` | Model deployment name (default: `gpt-4o`) |
| `Monitoring__AlertWebhookUrl` | Teams/Slack incoming webhook URL |
| `Monitoring__FailureRateThresholdPercent` | Failure rate alert threshold (default: `5.0`) |
| `Monitoring__ResponseTimeThresholdMs` | P95 response time threshold (default: `2000`) |
| `Monitoring__MonitoredServices` | Comma-separated service names to monitor |

## Related Repositories

| Repo | Purpose |
|------|---------|
| [`app_dotnet_angular_containerized_decomposition_monolith`](https://github.com/Cognition-Partner-Workshops/app_dotnet_angular_containerized_decomposition_monolith) | Before-state monolith |
| [`app_dotnet_angular_containerized_decomposition_microfrontends`](https://github.com/Cognition-Partner-Workshops/app_dotnet_angular_containerized_decomposition_microfrontends) | Angular micro-frontends target |
| [`app_dotnet_angular_containerized_decomposition_iac`](https://github.com/Cognition-Partner-Workshops/app_dotnet_angular_containerized_decomposition_iac) | App-specific Helm charts |
| [`platform-engineering-shared-services`](https://github.com/Cognition-Partner-Workshops/platform-engineering-shared-services) | Shared EKS cluster and platform infra |
