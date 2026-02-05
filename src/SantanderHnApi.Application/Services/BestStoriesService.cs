using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SantanderHnApi.Application.Interfaces;
using SantanderHnApi.Application.Models;
using SantanderHnApi.Application.Options;
using SantanderHnApi.Domain.Models;

namespace SantanderHnApi.Application.Services;

public sealed class BestStoriesService(
    IHackerNewsClient client,
    IOptions<HackerNewsOptions> options,
    ILogger<BestStoriesService> logger)
    : IBestStoriesService
{
    private readonly HackerNewsOptions _options = options.Value;

    public async Task<IReadOnlyList<BestStory>> GetBestStoriesAsync(
        int count,
        CancellationToken cancellationToken)
    {
        var ids = await client.GetBestStoryIdsAsync(cancellationToken);
        if (ids.Count == 0)
        {
            logger.LogWarning("No best story IDs returned from Hacker News.");
            return Array.Empty<BestStory>();
        }

        var selectedIds = ids.Take(count).ToArray();

        // Safe: each worker writes to a unique index.
        var items = new HackerNewsItem?[selectedIds.Length];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, selectedIds.Length),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.MaxConcurrency,
                CancellationToken = cancellationToken
            },
            async (index, ct) =>
            {
                var id = selectedIds[index];
                items[index] = await client.GetItemAsync(id, ct);
            });

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
}
