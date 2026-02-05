# Santander Hacker News API

ASP.NET Core API that returns the first `n` Hacker News best stories, sorted by score descending. Designed to be simple, resilient, and safe to call at scale.

## Quickstart

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

## Tests

```bash
dotnet test
```

## Endpoint

`GET /api/stories/best?n=10`

Ordering: take the first `n` IDs in the order returned by Hacker News, fetch those items, then sort those `n` stories by `score` descending.

Example:

```bash
curl "http://localhost:5123/api/stories/best?n=10"
```

Original prompt response shape (array), kept unchanged:

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

Our API response (adds `count` wrapper):

```json
{
  "count": 1,
  "stories": [
    {
      "title": "A uBlock Origin update was rejected from the Chrome Web Store",
      "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
      "postedBy": "ismaildonmez",
      "time": "2019-10-12T13:43:01+00:00",
      "score": 1716,
      "commentCount": 572
    }
  ]
}
```

## Configuration

Edit `src/SantanderHnApi.Api/appsettings.json` if needed:

- `BestStoriesCacheSeconds`: Cache TTL for the best stories ID list.
- `ItemCacheSeconds`: Cache TTL for each story item.
- `MaxConcurrency`: Maximum simultaneous calls to Hacker News.
- `MaxItems`: Upper bound for `n`.
- `HttpTimeoutSeconds`: Timeout per Hacker News request.
- `MaxRetries`: Retry count for transient failures (5xx/408/429).
- `RetryBaseDelaySeconds`: Base delay for exponential backoff.
- `RetryJitterMaxMilliseconds`: Max random jitter added per retry.
- `CircuitBreakerFailures`: Number of consecutive failures before opening the circuit.
- `CircuitBreakerBreakSeconds`: How long the circuit stays open before half‑open retry.

## Design Notes

- Caching via decorator: `CachedHackerNewsClient`.
- Parallel fetch uses `Parallel.ForEachAsync` with `MaxConcurrency`.
- Resilience: retry with backoff + jitter, plus circuit breaker.
- Validation: `n` bounds and startup validation for options.
- Error handling: external failures are logged; the API remains responsive.

## Assumptions

- Slightly stale data is acceptable to reduce external load.
- `n` is capped (`MaxItems`) to prevent extreme fan‑out.
- If a story is missing or not of type `story`, it is skipped.
- External failures may yield partial or empty results.

## Improvements with More Time

- Add distributed cache (Redis) for multi‑instance deployments.
- Add background refresh to keep caches warm.
- Add rate‑limiting and metrics/tracing (OpenTelemetry).
- Add integration tests.

## Architecture

- `src/SantanderHnApi.Api` — HTTP surface and composition root
- `src/SantanderHnApi.Application` — Use cases and interfaces
- `src/SantanderHnApi.Domain` — Core models
- `src/SantanderHnApi.Infrastructure` — Hacker News client, caching, and resilience
