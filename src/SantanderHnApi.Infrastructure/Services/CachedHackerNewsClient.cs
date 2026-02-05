using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SantanderHnApi.Application.Interfaces;
using SantanderHnApi.Application.Models;
using SantanderHnApi.Application.Options;

namespace SantanderHnApi.Infrastructure.Services;

public sealed class CachedHackerNewsClient : IHackerNewsClient
{
    private const string BestStoriesCacheKey = "hn:beststories";

    private readonly IHackerNewsClient _innerClient;
    private readonly IMemoryCache _cache;
    private readonly HackerNewsOptions _options;
    private readonly ILogger<CachedHackerNewsClient> _logger;

    public CachedHackerNewsClient(
        IHackerNewsClient innerClient,
        IMemoryCache cache,
        IOptions<HackerNewsOptions> options,
        ILogger<CachedHackerNewsClient> logger)
    {
        _innerClient = innerClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(BestStoriesCacheKey, out List<int>? cachedIds) && cachedIds is { Count: > 0 })
        {
            return cachedIds;
        }

        var ids = await _innerClient.GetBestStoryIdsAsync(cancellationToken);
        if (ids.Count == 0)
        {
            return ids;
        }

        _cache.Set(
            BestStoriesCacheKey,
            ids.ToList(),
            TimeSpan.FromSeconds(_options.BestStoriesCacheSeconds));

        _logger.LogDebug("Cached {Count} best story IDs.", ids.Count);
        return ids;
    }

    public async Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        var cacheKey = $"hn:item:{id}";

        if (_cache.TryGetValue(cacheKey, out HackerNewsItem? cachedItem))
        {
            return cachedItem;
        }

        var item = await _innerClient.GetItemAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        _cache.Set(
            cacheKey,
            item,
            TimeSpan.FromSeconds(_options.ItemCacheSeconds));

        return item;
    }
}
