using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SantanderHnApi.Application.Interfaces;
using SantanderHnApi.Application.Models;
using SantanderHnApi.Application.Options;
using SantanderHnApi.Application.Services;
using Xunit;

namespace SantanderHnApi.Tests;

public sealed class BestStoriesServiceTests
{
    [Fact]
    public async Task GetBestStoriesAsync_SortsByScoreDescending()
    {
        var ids = new List<int> { 1, 2, 3 };
        var items = new Dictionary<int, HackerNewsItem>
        {
            { 1, new HackerNewsItem { Title = "t1", Url = "u1", By = "a", Time = 1, Score = 10, Descendants = 1, Type = "story" } },
            { 2, new HackerNewsItem { Title = "t2", Url = "u2", By = "b", Time = 2, Score = 30, Descendants = 2, Type = "story" } },
            { 3, new HackerNewsItem { Title = "t3", Url = "u3", By = "c", Time = 3, Score = 20, Descendants = 3, Type = "story" } }
        };

        var clientMock = new Mock<IHackerNewsClient>();
        clientMock.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ids);
        clientMock.Setup(c => c.GetItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => items.TryGetValue(id, out var item) ? item : null);

        var options = Options.Create(new HackerNewsOptions { MaxConcurrency = 3 });
        var service = new BestStoriesService(clientMock.Object, options, NullLogger<BestStoriesService>.Instance);

        var result = await service.GetBestStoriesAsync(3, CancellationToken.None);

        Assert.Equal(new[] { 30, 20, 10 }, result.Select(r => r.Score));
    }

    [Fact]
    public async Task GetBestStoriesAsync_UsesFirstNIdsOnly()
    {
        var ids = new List<int> { 1, 2, 3 };
        var items = new Dictionary<int, HackerNewsItem>
        {
            { 1, new HackerNewsItem { Title = "t1", Url = "u1", By = "a", Time = 1, Score = 10, Descendants = 1, Type = "story" } },
            { 2, new HackerNewsItem { Title = "t2", Url = "u2", By = "b", Time = 2, Score = 30, Descendants = 2, Type = "story" } },
            { 3, new HackerNewsItem { Title = "t3", Url = "u3", By = "c", Time = 3, Score = 20, Descendants = 3, Type = "story" } }
        };

        var clientMock = new Mock<IHackerNewsClient>();
        clientMock.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ids);
        clientMock.Setup(c => c.GetItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => items.TryGetValue(id, out var item) ? item : null);

        var options = Options.Create(new HackerNewsOptions { MaxConcurrency = 2 });
        var service = new BestStoriesService(clientMock.Object, options, NullLogger<BestStoriesService>.Instance);

        var result = await service.GetBestStoriesAsync(2, CancellationToken.None);

        Assert.Equal(2, result.Count);
        clientMock.Verify(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        clientMock.Verify(c => c.GetItemAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        clientMock.Verify(c => c.GetItemAsync(3, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBestStoriesAsync_RespectsMaxConcurrency()
    {
        var ids = new List<int> { 1, 2, 3, 4, 5 };
        var currentConcurrency = 0;
        var maxObserved = 0;

        var clientMock = new Mock<IHackerNewsClient>();
        clientMock.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ids);
        clientMock.Setup(c => c.GetItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>(async (id, ct) =>
            {
                var current = Interlocked.Increment(ref currentConcurrency);
                UpdateMax(ref maxObserved, current);

                try
                {
                    await Task.Delay(200, ct);
                }
                finally
                {
                    Interlocked.Decrement(ref currentConcurrency);
                }

                return new HackerNewsItem
                {
                    Title = $"t{id}",
                    Url = $"u{id}",
                    By = "user",
                    Time = 1,
                    Score = id,
                    Descendants = 0,
                    Type = "story"
                };
            });

        var options = Options.Create(new HackerNewsOptions { MaxConcurrency = 2 });
        var service = new BestStoriesService(clientMock.Object, options, NullLogger<BestStoriesService>.Instance);

        await service.GetBestStoriesAsync(5, CancellationToken.None);

        Assert.True(maxObserved <= 2);
    }

    private static void UpdateMax(ref int maxObserved, int current)
    {
        int initial;
        do
        {
            initial = maxObserved;
            if (current <= initial)
            {
                return;
            }
        } while (Interlocked.CompareExchange(ref maxObserved, current, initial) != initial);
    }
}
