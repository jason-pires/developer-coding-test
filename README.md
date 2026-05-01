# Hacker News Orchestration API

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Docker](https://img.shields.io/badge/Docker-Alpine-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![CI/CD](https://img.shields.io/badge/CI%2FCD-GitHub_Actions-2088FF?logo=githubactions&logoColor=white)](.github/workflows/build-and-deploy.yml)
[![Architecture](https://img.shields.io/badge/Architecture-Clean-1f6feb)](#-architecture)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](#-license)

> A production-grade .NET 9 REST API that orchestrates the public **[Hacker News API](https://github.com/HackerNews/API)** — built around Clean Architecture, resilience pipelines, full observability, and container-first deployment.

---

## ✨ Highlights

- ⚡ **High-performance orchestration** of the Hacker News public API with concurrency throttling and in-memory caching
- 🧱 **Clean Architecture** — clear separation between `Domain`, `Application`, `Infrastructure`, `Common`, and `Core.API`
- 🛡️ **Resilience-first** — Polly v8 pipeline with retries, timeouts, and circuit breaker via `Microsoft.Extensions.Http.Resilience`
- 📜 **OpenAPI / Swagger** documentation with API versioning (`/api/v{version}/...`)
- 🔭 **Full observability** — OpenTelemetry traces/metrics + Serilog structured logging with `TraceId` / `SpanId` correlation
- 🚦 **Centralized error handling** via `ExceptionsHandlingMiddleware` returning RFC 7807 `ProblemDetails`
- 🧪 **Tested** — xUnit + NSubstitute unit tests and Stryker.NET mutation testing
- 🐳 **Container-ready** — multi-stage Alpine Dockerfile running as non-root on port `8080`
- 🚀 **CI/CD scaffold** — GitHub Actions workflow with build, test, and ECS Fargate deployment stages

---

## 📡 The API

A single, focused endpoint:

```http
GET /api/v1/News/top/{n}
```

| Param | Type | Description |
|-------|------|-------------|
| `n`   | `int` (1 — `MaxStoriesPerRequest`) | Number of best stories to return, sorted by score |

**Sample response (`200 OK`):**

```json
[
  {
    "title": "Show HN: A Faster JSON Parser",
    "uri": "https://example.com/post",
    "postedBy": "pg",
    "time": "2026-05-01T01:23:45Z",
    "score": 482,
    "commentCount": 137
  }
]
```

**Error responses** follow [RFC 7807 ProblemDetails](https://datatracker.ietf.org/doc/html/rfc7807):

| Status | Meaning |
|--------|---------|
| `400 Bad Request`            | `n <= 0` or `n > MaxStoriesPerRequest` |
| `499 Client Closed Request`  | Caller cancelled the request |
| `500 Internal Server Error`  | Unhandled failure |
| `503 Service Unavailable`    | Upstream Hacker News API unreachable / circuit open |

Interactive docs available at **`/swagger`** when running locally.

---

## 🏛️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│                       Core.API                          │
│  Controllers · Middleware · DI · Versioning · Swagger   │
└────────────────────────┬────────────────────────────────┘
                         │ depends on
┌────────────────────────▼────────────────────────────────┐
│                     Application                         │
│           Use cases · Orchestration · Interfaces        │
│                  HackerNewsService                      │
└──────────────┬─────────────────────────┬────────────────┘
               │                         │
┌──────────────▼──────────┐  ┌───────────▼────────────────┐
│      Infrastructure     │  │           Domain           │
│  HTTP clients · Polly · │  │   DTOs · Responses · POCO  │
│  Cache · External I/O   │  │                            │
└─────────────────────────┘  └────────────────────────────┘
                         ▲
                         │ shared utilities
                  ┌──────┴──────┐
                  │   Common    │
                  │ Config etc. │
                  └─────────────┘
```

**Dependency rule:** outer layers depend on inner layers — never the inverse. `Domain` has zero dependencies; `Application` defines interfaces (e.g. `IHackerNewsService`, `IHackerNewsGateway`, `ICacheProvider`) implemented by `Infrastructure`.

### Key cross-cutting concerns

| Concern | Where | What |
|---------|-------|------|
| **Resilience** | `Core.API/Config/ApiConfigAndResilience.cs` | Polly retry + timeout + circuit breaker on the typed `HttpClient` |
| **Mapping**    | `Core.API/Config/MappingConfigurations.cs`  | Mapster configurations |
| **Errors**     | `Core.API/Middleware/ExceptionsHandlingMiddleware` | Catches all exceptions, emits `ProblemDetails` |
| **Tracing**    | `Program.cs` (OpenTelemetry)                | OTLP exporter to `OpenTelemetry:OtlpEndpoint` |
| **Logging**    | `Program.cs` (Serilog)                      | Console sink, enriched with `TraceId`/`SpanId` |

---

## 🚀 Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://docs.docker.com/get-docker/) *(optional — for containerized run)*

### Run locally

```bash
git clone <repo-url>
cd developer-coding-test

dotnet restore
dotnet run --project Core.API/API.csproj
```

API listens on the URL printed by Kestrel (e.g. `https://localhost:5001`).
Open **`/swagger`** for the interactive UI.

### Run in Docker

```bash
docker build -t hackernews-api .
docker run -p 8080:8080 hackernews-api
```

Then hit `http://localhost:8080/api/v1/News/top/10`.

### Run the full stack with Docker Compose 🧩

The repository ships a [`docker-compose.yml`](docker-compose.yml) that spins up the API **and** a Jaeger all-in-one container to receive OpenTelemetry traces:

```bash
docker compose up --build
```

| Service | URL | Purpose |
|---------|-----|---------|
| **API**         | http://localhost:8080/swagger | Swagger UI |
| **API**         | http://localhost:8080/api/v1/News/top/10 | Endpoint sample |
| **Jaeger UI**   | http://localhost:16686 | Distributed traces |
| **OTLP gRPC**   | `localhost:4317` | OpenTelemetry ingest |

Tear it down with:

```bash
docker compose down
```

---

## ⚙️ Configuration

Configurable via `appsettings.json`, environment variables, or any standard ASP.NET Core configuration source.

```jsonc
{
  "GenericHttpClientOptions": {
    "BaseUrl": "https://hacker-news.firebaseio.com/v0",
    "TimeoutSeconds": 30
  },
  "HackerNewsOptions": {
    "MaxStoriesPerRequest": 50,        // Hard cap on `n`
    "MaxConcurrentStoryRequests": 10,  // Concurrency throttle for fan-out
    "BestStoriesCacheSeconds": 60,     // TTL for the best-stories ID list
    "StoryDetailsCacheSeconds": 300    // TTL for individual story details
  },
  "OpenTelemetry": {
    "ServiceName": "hacker-news-api",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

Override any value via environment variable using ASP.NET Core conventions, e.g.:
```bash
HackerNewsOptions__MaxStoriesPerRequest=100
```

---

## 🧪 Testing

### Unit tests (xUnit + NSubstitute)

```bash
dotnet test Tests/UnitTests/UnitTests.csproj
```

21 tests covering caching behavior, concurrency throttling, ordering, and error paths.

### Mutation testing (Stryker.NET)

```bash
dotnet tool restore
cd Tests/MutationTests
dotnet stryker -f stryker-config.json
```

Generates an HTML report under `Tests/MutationTests/StrykerOutput/.../reports/mutation-report.html`.

---

## 🐳 Docker

The included [`Dockerfile`](Dockerfile) is a multi-stage Alpine build:

1. **`build`** stage — `mcr.microsoft.com/dotnet/sdk:9.0-alpine` restores and publishes
2. **`runtime`** stage — `mcr.microsoft.com/dotnet/aspnet:9.0-alpine`, runs as non-root `app` user, exposes `8080`

```bash
docker build -t hackernews-api .
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  hackernews-api
```

---

## 🚢 CI/CD

The pipeline at [`.github/workflows/build-and-deploy.yml`](.github/workflows/build-and-deploy.yml) runs on every push and PR, performing:

1. ✅ Restore, build, and run unit tests
2. 🐳 Build and push the Docker image to **Amazon ECR**
3. 🚀 Deploy a new task revision to **AWS ECS Fargate**

Provide these GitHub Action **secrets** to enable deployment:

| Secret | Purpose |
|--------|---------|
| `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` | AWS credentials |
| `AWS_REGION`, `ECR_REPOSITORY`, `ECS_CLUSTER`, `ECS_SERVICE`, `ECS_TASK_DEFINITION` | Target infrastructure |

---

## 📁 Project Structure

```
.
├── Core.API/             # ASP.NET Core entrypoint (Controllers, Middleware, DI, Swagger)
├── Application/          # Use cases & service orchestration
│   ├── Interfaces/
│   └── Services/         # HackerNewsService
├── Infrastructure/       # External I/O: HTTP clients, cache, gateways
├── Domain/               # DTOs, response contracts, pure domain types
├── Common/               # Cross-cutting helpers, options classes
├── Tests/
│   ├── UnitTests/        # xUnit + NSubstitute
│   └── MutationTests/    # Stryker.NET configuration
├── .github/workflows/    # CI/CD pipelines
├── Dockerfile            # Multi-stage Alpine build
├── docker-compose.yml    # API + Jaeger (OTLP) local stack
└── Core.sln
```

---

## 📜 License

MIT — feel free to use, learn from, and adapt.

---

<p align="center"><sub>Built with ❤️ and a healthy dose of resilience patterns.</sub></p>
