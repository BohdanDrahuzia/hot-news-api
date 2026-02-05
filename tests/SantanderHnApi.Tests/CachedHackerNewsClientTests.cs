using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SantanderHnApi.Application.Interfaces;
using SantanderHnApi.Application.Models;
using SantanderHnApi.Application.Options;
using SantanderHnApi.Infrastructure.Services;
using Xunit;

namespace SantanderHnApi.Tests;

public sealed class CachedHackerNewsClientTests
{
    [Fact]
    public async Task GetBestStoryIdsAsync_CachesIds()
    {
        var inner = new CountingHackerNewsClient(
            ids: new List<int> { 1, 2, 3 },
            items: new Dictionary<int, HackerNewsItem>());

        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new HackerNewsOptions { BestStoriesCacheSeconds = 60 });

        var client = new CachedHackerNewsClient(inner, cache, options, NullLogger<CachedHackerNewsClient>.Instance);

        var first = await client.GetBestStoryIdsAsync(CancellationToken.None);
        var second = await client.GetBestStoryIdsAsync(CancellationToken.None);

        Assert.Equal(first, second);
        Assert.Equal(1, inner.BestStoriesCalls);
    }

    [Fact]
    public async Task GetItemAsync_CachesItem()
    {
        var inner = new CountingHackerNewsClient(
            ids: new List<int> { 1 },
            items: new Dictionary<int, HackerNewsItem>
            {
                { 1, new HackerNewsItem { Title = "t1", Url = "u1", By = "a", Time = 1, Score = 10, Descendants = 1, Type = "story" } }
            });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new HackerNewsOptions { ItemCacheSeconds = 60 });

        var client = new CachedHackerNewsClient(inner, cache, options, NullLogger<CachedHackerNewsClient>.Instance);

        var first = await client.GetItemAsync(1, CancellationToken.None);
        var second = await client.GetItemAsync(1, CancellationToken.None);

        Assert.NotNull(first);
        Assert.Equal(first?.Title, second?.Title);
        Assert.Equal(1, inner.ItemCalls);
    }

    [Fact]
    public async Task GetItemAsync_UsesCacheWhenAvailable()
    {
        const int storyId = 42;
        var cachedItem = new HackerNewsItem
        {
            Title = "cached",
            Url = "cached-url",
            By = "cached-user",
            Time = 1,
            Score = 99,
            Descendants = 5,
            Type = "story"
        };

        var inner = new CountingHackerNewsClient(
            ids: new List<int> { storyId },
            items: new Dictionary<int, HackerNewsItem>
            {
                { storyId, new HackerNewsItem { Title = "live", Url = "live-url", By = "live", Time = 2, Score = 1, Descendants = 0, Type = "story" } }
            });

        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set($"hn:item:{storyId}", cachedItem);

        var options = Options.Create(new HackerNewsOptions { ItemCacheSeconds = 60 });
        var client = new CachedHackerNewsClient(inner, cache, options, NullLogger<CachedHackerNewsClient>.Instance);

        var result = await client.GetItemAsync(storyId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("cached", result?.Title);
        Assert.Equal(0, inner.ItemCalls);
    }

    private sealed class CountingHackerNewsClient(
        IReadOnlyList<int> ids,
        IReadOnlyDictionary<int, HackerNewsItem> items)
        : IHackerNewsClient
    {
        public int BestStoriesCalls { get; private set; }
        public int ItemCalls { get; private set; }

        public Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
        {
            BestStoriesCalls++;
            return Task.FromResult(ids);
        }

        public Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken)
        {
            ItemCalls++;
            items.TryGetValue(id, out var item);
            return Task.FromResult(item);
        }
    }
}
