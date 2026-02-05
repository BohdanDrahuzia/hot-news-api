using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SantanderHnApi.Application.Interfaces;
using SantanderHnApi.Application.Models;
using SantanderHnApi.Application.Options;
using SantanderHnApi.Application.Services;
using SantanderHnApi.Domain.Models;
using Xunit;

namespace SantanderHnApi.Tests;

public sealed class BestStoriesServiceTests
{
    [Fact]
    public async Task GetBestStoriesAsync_SortsByScoreDescending()
    {
        var client = new FakeHackerNewsClient(
            new List<int> { 1, 2, 3 },
            new Dictionary<int, HackerNewsItem>
            {
                { 1, new HackerNewsItem { Title = "t1", Url = "u1", By = "a", Time = 1, Score = 10, Descendants = 1, Type = "story" } },
                { 2, new HackerNewsItem { Title = "t2", Url = "u2", By = "b", Time = 2, Score = 30, Descendants = 2, Type = "story" } },
                { 3, new HackerNewsItem { Title = "t3", Url = "u3", By = "c", Time = 3, Score = 20, Descendants = 3, Type = "story" } }
            });

        var options = Options.Create(new HackerNewsOptions { MaxConcurrency = 3 });
        var service = new BestStoriesService(client, options, NullLogger<BestStoriesService>.Instance);

        var result = await service.GetBestStoriesAsync(3, CancellationToken.None);

        Assert.Equal(new[] { 30, 20, 10 }, result.Select(r => r.Score));
    }

    [Fact]
    public async Task GetBestStoriesAsync_UsesFirstNIdsOnly()
    {
        var client = new FakeHackerNewsClient(
            new List<int> { 1, 2, 3 },
            new Dictionary<int, HackerNewsItem>
            {
                { 1, new HackerNewsItem { Title = "t1", Url = "u1", By = "a", Time = 1, Score = 10, Descendants = 1, Type = "story" } },
                { 2, new HackerNewsItem { Title = "t2", Url = "u2", By = "b", Time = 2, Score = 30, Descendants = 2, Type = "story" } },
                { 3, new HackerNewsItem { Title = "t3", Url = "u3", By = "c", Time = 3, Score = 20, Descendants = 3, Type = "story" } }
            });

        var options = Options.Create(new HackerNewsOptions { MaxConcurrency = 2 });
        var service = new BestStoriesService(client, options, NullLogger<BestStoriesService>.Instance);

        var result = await service.GetBestStoriesAsync(2, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { 1, 2 }, client.RequestedIds.OrderBy(id => id));
    }

    [Fact]
    public async Task GetBestStoriesAsync_RespectsMaxConcurrency()
    {
        var client = new DelayHackerNewsClient(new List<int> { 1, 2, 3, 4, 5 }, delayMilliseconds: 200);
        var options = Options.Create(new HackerNewsOptions { MaxConcurrency = 2 });
        var service = new BestStoriesService(client, options, NullLogger<BestStoriesService>.Instance);

        await service.GetBestStoriesAsync(5, CancellationToken.None);

        Assert.True(client.MaxObservedConcurrency <= 2);
    }

    private sealed class FakeHackerNewsClient(IReadOnlyList<int> ids, IReadOnlyDictionary<int, HackerNewsItem> items)
        : IHackerNewsClient
    {
        public List<int> RequestedIds { get; } = new();

        public Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult(ids);

        public Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken)
        {
            RequestedIds.Add(id);
            items.TryGetValue(id, out var item);
            return Task.FromResult(item);
        }
    }

    private sealed class DelayHackerNewsClient(IReadOnlyList<int> ids, int delayMilliseconds) : IHackerNewsClient
    {
        private int _currentConcurrency;
        private int _maxObserved;

        public int MaxObservedConcurrency => Volatile.Read(ref _maxObserved);

        public Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
            => Task.FromResult(ids);

        public async Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken)
        {
            var current = Interlocked.Increment(ref _currentConcurrency);
            UpdateMax(current);

            try
            {
                await Task.Delay(delayMilliseconds, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _currentConcurrency);
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
        }

        private void UpdateMax(int current)
        {
            int initial;
            do
            {
                initial = _maxObserved;
                if (current <= initial)
                {
                    return;
                }
            } while (Interlocked.CompareExchange(ref _maxObserved, current, initial) != initial);
        }
    }
}
