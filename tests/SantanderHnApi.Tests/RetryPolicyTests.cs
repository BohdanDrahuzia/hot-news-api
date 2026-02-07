using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System;
using Polly.CircuitBreaker;
using SantanderHnApi.Application.Options;
using SantanderHnApi.Infrastructure;
using Xunit;

namespace SantanderHnApi.Tests;

public sealed class RetryPolicyTests
{
    [Fact]
    public async Task RetryPolicy_Retries_OnTransientFailures()
    {
        var options = new HackerNewsOptions
        {
            MaxRetries = 2,
            RetryBaseDelaySeconds = 0,
            RetryJitterMaxMilliseconds = 0,
            CircuitBreakerFailures = 10,
            CircuitBreakerBreakSeconds = 1
        };

        var policy = ServiceCollectionExtensions.CreateResiliencePolicy(options);
        var attempts = 0;

        Task<HttpResponseMessage> Action()
        {
            attempts++;
            var status = attempts <= 2 ? HttpStatusCode.InternalServerError : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status));
        }

        var response = await policy.ExecuteAsync(Action);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task RetryPolicy_Retries_OnTooManyRequests()
    {
        var options = new HackerNewsOptions
        {
            MaxRetries = 1,
            RetryBaseDelaySeconds = 0,
            RetryJitterMaxMilliseconds = 0,
            CircuitBreakerFailures = 10,
            CircuitBreakerBreakSeconds = 1
        };

        var policy = ServiceCollectionExtensions.CreateResiliencePolicy(options);
        var attempts = 0;

        Task<HttpResponseMessage> Action()
        {
            attempts++;
            var status = attempts == 1 ? (HttpStatusCode)429 : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status));
        }

        var response = await policy.ExecuteAsync(Action);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task RetryPolicy_DoesNotRetry_OnNotFound()
    {
        var options = new HackerNewsOptions
        {
            MaxRetries = 3,
            RetryBaseDelaySeconds = 0,
            RetryJitterMaxMilliseconds = 0,
            CircuitBreakerFailures = 10,
            CircuitBreakerBreakSeconds = 1
        };

        var policy = ServiceCollectionExtensions.CreateResiliencePolicy(options);
        var attempts = 0;

        Task<HttpResponseMessage> Action()
        {
            attempts++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        var response = await policy.ExecuteAsync(Action);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task CircuitBreaker_Opens_AfterConsecutiveFailures()
    {
        var options = new HackerNewsOptions
        {
            MaxRetries = 0,
            RetryBaseDelaySeconds = 0,
            RetryJitterMaxMilliseconds = 0,
            CircuitBreakerFailures = 2,
            CircuitBreakerBreakSeconds = 30
        };

        var policy = ServiceCollectionExtensions.CreateResiliencePolicy(options);

        Task<HttpResponseMessage> Action()
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }

        await policy.ExecuteAsync(Action);
        await policy.ExecuteAsync(Action);

        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(() => policy.ExecuteAsync(Action));
    }

    [Fact]
    public async Task CircuitBreaker_AllowsRecovery_AfterBreakDuration()
    {
        var options = new HackerNewsOptions
        {
            MaxRetries = 0,
            RetryBaseDelaySeconds = 0,
            RetryJitterMaxMilliseconds = 0,
            CircuitBreakerFailures = 2,
            CircuitBreakerBreakSeconds = 1
        };

        var policy = ServiceCollectionExtensions.CreateResiliencePolicy(options);
        var executions = 0;

        Task<HttpResponseMessage> Action()
        {
            var current = Interlocked.Increment(ref executions);
            var status = current <= 2 ? HttpStatusCode.InternalServerError : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status));
        }

        await policy.ExecuteAsync(Action);
        await policy.ExecuteAsync(Action);

        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(() => policy.ExecuteAsync(Action));

        await Task.Delay(TimeSpan.FromSeconds(options.CircuitBreakerBreakSeconds + 0.1));

        var recovered = await policy.ExecuteAsync(Action);
        var followUp = await policy.ExecuteAsync(Action);

        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        Assert.Equal(HttpStatusCode.OK, followUp.StatusCode);
        Assert.Equal(4, executions);
    }
}
