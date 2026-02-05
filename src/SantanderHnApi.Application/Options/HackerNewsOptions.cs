namespace SantanderHnApi.Application.Options;

public sealed class HackerNewsOptions
{
    public const string SectionName = "HackerNews";

    public string BaseUrl { get; init; } = "https://hacker-news.firebaseio.com/";

    // Cache the best stories list for a short time to reduce HN traffic.
    public int BestStoriesCacheSeconds { get; init; } = 120;

    // Cache individual items a little longer for overlap across requests.
    public int ItemCacheSeconds { get; init; } = 600;

    // Prevent excessive request fan-out.
    public int MaxConcurrency { get; init; } = 10;

    // Guardrails for large client requests.
    public int MaxItems { get; init; } = 500;

    public int HttpTimeoutSeconds { get; init; } = 5;

    // Retry settings for transient failures.
    public int MaxRetries { get; init; } = 3;
    public double RetryBaseDelaySeconds { get; init; } = 0.5;
    public int RetryJitterMaxMilliseconds { get; init; } = 250;

    // Circuit breaker settings.
    public int CircuitBreakerFailures { get; init; } = 5;
    public int CircuitBreakerBreakSeconds { get; init; } = 30;
}
