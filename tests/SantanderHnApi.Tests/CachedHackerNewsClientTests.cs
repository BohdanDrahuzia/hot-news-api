using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
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
        var ids = new List<int> { 1, 2, 3 };
        var innerMock = new Mock<IHackerNewsClient>();
        innerMock.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ids);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new HackerNewsOptions { BestStoriesCacheSeconds = 60 });

        var client = new CachedHackerNewsClient(innerMock.Object, cache, options, NullLogger<CachedHackerNewsClient>.Instance);

        var first = await client.GetBestStoryIdsAsync(CancellationToken.None);
        var second = await client.GetBestStoryIdsAsync(CancellationToken.None);

        Assert.Equal(first, second);
        innerMock.Verify(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetItemAsync_CachesItem()
    {
        var item = new HackerNewsItem
        {
            Title = "t1",
            Url = "u1",
            By = "a",
            Time = 1,
            Score = 10,
            Descendants = 1,
            Type = "story"
        };

        var innerMock = new Mock<IHackerNewsClient>();
        innerMock.Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new HackerNewsOptions { ItemCacheSeconds = 60 });

        var client = new CachedHackerNewsClient(innerMock.Object, cache, options, NullLogger<CachedHackerNewsClient>.Instance);

        var first = await client.GetItemAsync(1, CancellationToken.None);
        var second = await client.GetItemAsync(1, CancellationToken.None);

        Assert.NotNull(first);
        Assert.Equal(first?.Title, second?.Title);
        innerMock.Verify(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()), Times.Once);
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

        var innerMock = new Mock<IHackerNewsClient>();
        innerMock.Setup(c => c.GetItemAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HackerNewsItem
            {
                Title = "live",
                Url = "live-url",
                By = "live",
                Time = 2,
                Score = 1,
                Descendants = 0,
                Type = "story"
            });

        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set($"hn:item:{storyId}", cachedItem);

        var options = Options.Create(new HackerNewsOptions { ItemCacheSeconds = 60 });
        var client = new CachedHackerNewsClient(innerMock.Object, cache, options, NullLogger<CachedHackerNewsClient>.Instance);

        var result = await client.GetItemAsync(storyId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("cached", result?.Title);
        innerMock.Verify(c => c.GetItemAsync(storyId, It.IsAny<CancellationToken>()), Times.Never);
    }
}
