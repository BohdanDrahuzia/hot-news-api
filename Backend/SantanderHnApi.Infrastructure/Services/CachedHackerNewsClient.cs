using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SantanderHnApi.Application.Interfaces;
using SantanderHnApi.Application.Models;
using SantanderHnApi.Application.Options;

namespace SantanderHnApi.Infrastructure.Services;

public sealed class CachedHackerNewsClient(
    IHackerNewsClient innerClient,
    IMemoryCache cache,
    IOptions<HackerNewsOptions> options,
    ILogger<CachedHackerNewsClient> logger)
    : IHackerNewsClient
{
    private const string BestStoriesCacheKey = "hn:beststories";

    private readonly HackerNewsOptions _options = options.Value;

    public async Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(BestStoriesCacheKey, out List<int>? cachedIds) && cachedIds is { Count: > 0 })
        {
            return cachedIds;
        }

        var ids = await innerClient.GetBestStoryIdsAsync(cancellationToken);
        if (ids.Count == 0)
        {
            return ids;
        }

        cache.Set(
            BestStoriesCacheKey,
            ids.ToList(),
            TimeSpan.FromSeconds(_options.BestStoriesCacheSeconds));

        logger.LogDebug("Cached {Count} best story IDs.", ids.Count);
        return ids;
    }

    public async Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        var cacheKey = $"hn:item:{id}";

        if (cache.TryGetValue(cacheKey, out HackerNewsItem? cachedItem))
        {
            return cachedItem;
        }

        var item = await innerClient.GetItemAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        cache.Set(
            cacheKey,
            item,
            TimeSpan.FromSeconds(_options.ItemCacheSeconds));

        return item;
    }
}
