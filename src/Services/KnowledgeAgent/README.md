# SharePoint Knowledge Agent

An AI-powered agent built with **C# .NET 8** that reads documents from SharePoint, creates Knowledge Articles (KA), stores them in a vector database (Qdrant), and integrates with Azure Application Insights to automatically retrieve relevant knowledge when issues are detected and send email notifications.

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    SharePoint Knowledge Agent                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────┐    ┌──────────────────┐    ┌───────────────────┐  │
│  │  SharePoint   │───▶│ Document Processor│───▶│  Embedding Service │  │
│  │  Service      │    │ (Extract & Chunk) │    │  (Azure OpenAI)    │  │
│  └──────────────┘    └──────────────────┘    └─────────┬─────────┘  │
│         │                                               │            │
│         │ (Microsoft Graph API)                         ▼            │
│         │                                    ┌───────────────────┐  │
│         │                                    │   Vector Store     │  │
│         │                                    │   (Qdrant)         │  │
│         │                                    └─────────┬─────────┘  │
│         │                                               │            │
│  ┌──────────────┐                            ┌─────────▼─────────┐  │
│  │ App Insights  │───────────────────────────▶│ Knowledge Article │  │
│  │ Monitor       │    (Search for relevant    │ Service            │  │
│  └──────┬───────┘     articles on alerts)    └───────────────────┘  │
│         │                                                            │
│         ▼                                                            │
│  ┌──────────────┐                                                    │
│  │    Email      │ (Send alerts with relevant Knowledge Articles)     │
│  │ Notification  │                                                    │
│  └──────────────┘                                                    │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

## Features

- **SharePoint Document Ingestion**: Connects to SharePoint via Microsoft Graph API to read documents from specified sites/drives
- **Document Processing**: Extracts text from various document formats (PDF, DOCX, TXT, HTML, CSV, etc.) and chunks them for embedding
- **Vector Storage**: Stores document embeddings in Qdrant vector database for semantic search
- **Knowledge Article Management**: Creates, indexes, and retrieves knowledge articles
- **Application Insights Integration**: Monitors for exceptions/alerts and correlates them with knowledge articles
- **Email Notifications**: Sends rich HTML emails with alert details and relevant knowledge articles
- **Background Processing**: Automated periodic syncing from SharePoint and alert monitoring
- **Webhook Support**: Accepts Application Insights alert webhooks for real-time processing

## Prerequisites

- .NET 8 SDK
- Qdrant vector database (included in docker-compose)
- Azure AD App Registration (for SharePoint access via Microsoft Graph)
- Azure OpenAI or OpenAI API key (for embeddings)
- Azure Application Insights instance
- SMTP server for email notifications

## Configuration

### Required Azure AD Permissions (Microsoft Graph)

Register an application in Azure AD with the following permissions:
- `Sites.Read.All` - Read SharePoint sites
- `Files.Read.All` - Read files in SharePoint

### Environment Variables

| Variable | Description |
|----------|-------------|
| `SHAREPOINT_TENANT_ID` | Azure AD tenant ID |
| `SHAREPOINT_CLIENT_ID` | Azure AD app client ID |
| `SHAREPOINT_CLIENT_SECRET` | Azure AD app client secret |
| `SHAREPOINT_SITE_ID` | Target SharePoint site ID |
| `SHAREPOINT_DRIVE_ID` | Target SharePoint drive ID |
| `OPENAI_API_KEY` | Azure OpenAI / OpenAI API key |
| `OPENAI_ENDPOINT` | Azure OpenAI endpoint URL |
| `APPINSIGHTS_CONNECTION_STRING` | App Insights connection string |
| `APPINSIGHTS_API_KEY` | App Insights REST API key |
| `APPINSIGHTS_APPLICATION_ID` | App Insights application ID |
| `SMTP_HOST` | SMTP server host |
| `SMTP_USERNAME` | SMTP username |
| `SMTP_PASSWORD` | SMTP password |
| `EMAIL_FROM_ADDRESS` | Sender email address |

## Getting Started

### 1. Run with Docker Compose

```bash
# Set environment variables in .env file
cp .env.example .env
# Edit .env with your configuration

# Start the services
docker compose up --build
```

### 2. Run Locally

```bash
# Ensure Qdrant is running
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant:latest

# Restore and run
cd KnowledgeAgent.API
dotnet restore
dotnet run
```

### 3. Access the API

- Swagger UI: `http://localhost:5010` (Docker) or `http://localhost:5000` (local)
- Health Check: `GET /health`

## API Endpoints

### Knowledge Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/knowledge/search` | Semantic search across knowledge articles |
| `GET` | `/api/knowledge/articles` | List all knowledge articles |
| `GET` | `/api/knowledge/articles/{id}` | Get a specific article |

### Agent Operations

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/agent/sync` | Trigger manual SharePoint sync |
| `POST` | `/api/agent/monitor` | Trigger manual monitoring cycle |
| `POST` | `/api/agent/alerts/webhook` | Receive Application Insights alert webhook |

### Health

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/health` | Health check endpoint |
| `GET` | `/api/health` | Detailed health status |

## How It Works

### Document Ingestion Flow

1. **SharePoint Sync Job** runs periodically (configurable interval)
2. Fetches new/modified documents from the configured SharePoint site
3. **Document Processor** extracts text content and splits into chunks
4. **Embedding Service** generates vector embeddings using Azure OpenAI
5. **Vector Store** (Qdrant) indexes the embeddings for semantic search
6. Knowledge Articles are created with metadata linking back to source documents

### Alert Processing Flow

1. **Alert Monitoring Job** polls Application Insights for new exceptions
2. Alternatively, Application Insights sends an alert via webhook
3. Agent builds a search query from the exception details
4. **Semantic Search** finds relevant Knowledge Articles from the vector store
5. **Email Notification** is sent with alert details and related knowledge articles

## Project Structure

```
KnowledgeAgent/
├── KnowledgeAgent.API/           # ASP.NET Core Web API
│   ├── Controllers/              # API endpoints
│   ├── DTOs/                     # Request/Response models
│   └── Program.cs               # Application entry point
├── KnowledgeAgent.Core/          # Domain layer
│   ├── Models/                   # Domain entities
│   ├── Enums/                    # Status enumerations
│   └── Interfaces/               # Service contracts
├── KnowledgeAgent.Infrastructure/# Implementation layer
│   ├── Services/                 # Service implementations
│   ├── Configuration/            # Options/settings classes
│   └── BackgroundJobs/           # Hosted services
├── Dockerfile                    # Container build
├── docker-compose.yml            # Multi-container setup
└── README.md                     # This file
```

## Integration with Application Insights AI Agent

To integrate with an existing Application Insights monitoring setup:

1. **Configure Alert Action Group**: In Azure Portal, create an Action Group that sends a webhook to `/api/agent/alerts/webhook`
2. **Set up Alert Rules**: Configure alert rules in Application Insights for exceptions, performance issues, etc.
3. **The agent automatically**:
   - Receives the alert
   - Searches the knowledge base for relevant documentation
   - Sends an email with both the alert details and relevant knowledge articles

## Technology Stack

- **Runtime**: .NET 8
- **Web Framework**: ASP.NET Core
- **SharePoint Access**: Microsoft Graph SDK v5
- **Vector Database**: Qdrant
- **Embeddings**: Azure OpenAI (text-embedding-ada-002)
- **Monitoring**: Azure Application Insights
- **Email**: SMTP (System.Net.Mail)
- **Authentication**: Azure Identity (ClientSecretCredential)
- **Containerization**: Docker
