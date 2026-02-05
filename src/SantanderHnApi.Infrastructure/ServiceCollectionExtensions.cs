using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using SantanderHnApi.Application.Interfaces;
using SantanderHnApi.Application.Options;
using SantanderHnApi.Infrastructure.Services;

namespace SantanderHnApi.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddOptions<HackerNewsOptions>()
            .Bind(configuration.GetSection(HackerNewsOptions.SectionName))
            .Validate(options =>
                options.MaxItems > 0 &&
                options.MaxConcurrency > 0 &&
                options.BestStoriesCacheSeconds > 0 &&
                options.ItemCacheSeconds > 0 &&
                options.HttpTimeoutSeconds > 0 &&
                options.CircuitBreakerFailures > 0 &&
                options.CircuitBreakerBreakSeconds > 0,
                "HackerNews options must be positive values.")
            .ValidateOnStart();

        var hnOptions = configuration
            .GetSection(HackerNewsOptions.SectionName)
            .Get<HackerNewsOptions>() ?? new HackerNewsOptions();

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == (HttpStatusCode)429)
            .WaitAndRetryAsync(
                hnOptions.MaxRetries,
                retryAttempt =>
                {
                    var baseDelay = TimeSpan.FromSeconds(hnOptions.RetryBaseDelaySeconds);
                    var exponential = TimeSpan.FromSeconds(baseDelay.TotalSeconds * Math.Pow(2, retryAttempt - 1));
                    var jitterMs = Random.Shared.Next(0, hnOptions.RetryJitterMaxMilliseconds + 1);
                    return exponential + TimeSpan.FromMilliseconds(jitterMs);
                });

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == (HttpStatusCode)429)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: hnOptions.CircuitBreakerFailures,
                durationOfBreak: TimeSpan.FromSeconds(hnOptions.CircuitBreakerBreakSeconds));

        var resiliencePolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

        services.AddHttpClient<HackerNewsClient>(client =>
            {
                client.BaseAddress = new Uri(hnOptions.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(hnOptions.HttpTimeoutSeconds);
            })
            .AddPolicyHandler(resiliencePolicy);

        services.AddScoped<IHackerNewsClient>(sp =>
            new CachedHackerNewsClient(
                sp.GetRequiredService<HackerNewsClient>(),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<IOptions<HackerNewsOptions>>(),
                sp.GetRequiredService<ILogger<CachedHackerNewsClient>>()));

        return services;
    }
}
