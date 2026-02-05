# Santander Hacker News API

A simple ASP.NET Core API that returns the first `n` Hacker News “best stories,” sorted by score descending. It uses in-memory caching and capped concurrency to avoid overloading the Hacker News API.

## How to Run

1. Install the .NET SDK (10.0+).
2. From the repo root:

```bash
dotnet restore

dotnet run --project src/SantanderHnApi.Api/SantanderHnApi.Api.csproj
```

The API will be available at:

- `http://localhost:5123`
- `https://localhost:7123`

Swagger UI is enabled in Development at `/swagger`.

To run tests:

```bash
dotnet test
```

## API Usage

`GET /api/stories/best?n=10`

The API takes the first `n` IDs in the order provided by Hacker News, then sorts those `n` stories by `score` descending before returning them.

Example:

```bash
curl "http://localhost:5123/api/stories/best?n=10"
```

Response:

```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "score": 1716,
    "commentCount": 572
  }
]
```

## Configuration

Edit `src/SantanderHnApi.Api/appsettings.json` if needed:

- `BestStoriesCacheSeconds`: Cache TTL for the best stories ID list.
- `ItemCacheSeconds`: Cache TTL for each story item.
- `MaxConcurrency`: Maximum simultaneous calls to Hacker News (parallel fetch limit).
- `MaxItems`: Upper bound for `n`.
- `HttpTimeoutSeconds`: Timeout per Hacker News request.
- `MaxRetries`: Retry count for transient failures (5xx/408/429).
- `RetryBaseDelaySeconds`: Base delay for exponential backoff.
- `RetryJitterMaxMilliseconds`: Max random jitter added per retry.
- `CircuitBreakerFailures`: Number of consecutive failures before opening the circuit.
- `CircuitBreakerBreakSeconds`: How long the circuit stays open before half‑open retry.

## Assumptions

- Slightly stale data is acceptable to reduce external load.
- `n` is capped (`MaxItems`) to prevent extreme fan‑out.
- If a story is missing or not of type `story`, it is skipped.
- Hacker News transient failures are handled with retries and exponential backoff.
- External errors/timeouts are logged and do not crash the API; callers may see partial or empty results.

## Improvements with More Time

- Add distributed cache (Redis) for multi‑instance deployments.
- Add circuit breaker policies for sustained Hacker News outages.
- Warm cache in the background to reduce cold‑start latency.
- Add request rate‑limiting and metrics/tracing (OpenTelemetry).
- Add unit tests for service logic and controller validation.

## Architecture

This solution uses a lightweight Clean Architecture split to keep boundaries clear:

- `SantanderHnApi.Api` depends on `Application` and `Infrastructure` and contains the HTTP surface.
- `SantanderHnApi.Application` holds use cases, interfaces, and orchestration logic.
- `SantanderHnApi.Domain` contains core models.
- `SantanderHnApi.Infrastructure` integrates with Hacker News, caching, and HTTP policies.

## Project Structure

- `src/SantanderHnApi.Api` — API endpoints and composition root
- `src/SantanderHnApi.Application` — Use cases and interfaces
- `src/SantanderHnApi.Domain` — Core domain models
- `src/SantanderHnApi.Infrastructure` — Hacker News client + caching + HTTP policies
