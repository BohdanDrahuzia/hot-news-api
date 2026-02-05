using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SantanderHnApi.Application.Interfaces;
using SantanderHnApi.Application.Options;
using SantanderHnApi.Domain.Models;

namespace SantanderHnApi.Application.Services;

public sealed class BestStoriesService : IBestStoriesService
{
    private readonly IHackerNewsClient _client;
    private readonly HackerNewsOptions _options;
    private readonly ILogger<BestStoriesService> _logger;

    public BestStoriesService(
        IHackerNewsClient client,
        IOptions<HackerNewsOptions> options,
        ILogger<BestStoriesService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BestStory>> GetBestStoriesAsync(
        int count,
        CancellationToken cancellationToken)
    {
        var ids = await _client.GetBestStoryIdsAsync(cancellationToken);
        if (ids.Count == 0)
        {
            _logger.LogWarning("No best story IDs returned from Hacker News.");
            return Array.Empty<BestStory>();
        }

        var selectedIds = ids.Take(count).ToArray();
        var throttler = new SemaphoreSlim(_options.MaxConcurrency);

        try
        {
            var tasks = selectedIds.Select(async id =>
            {
                await throttler.WaitAsync(cancellationToken);
                try
                {
                    return await _client.GetItemAsync(id, cancellationToken);
                }
                finally
                {
                    throttler.Release();
                }
            });

            var items = await Task.WhenAll(tasks);

            return items
                .Where(item => item is { Type: "story" })
                .Select(item => new BestStory
                {
                    Title = item!.Title ?? string.Empty,
                    Uri = item.Url ?? string.Empty,
                    PostedBy = item.By ?? string.Empty,
                    Time = DateTimeOffset.FromUnixTimeSeconds(item.Time ?? 0),
                    Score = item.Score ?? 0,
                    CommentCount = item.Descendants ?? 0
                })
                .OrderByDescending(story => story.Score)
                .ToArray();
        }
        finally
        {
            throttler.Dispose();
        }
    }
}
