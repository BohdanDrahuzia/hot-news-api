using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SantanderHnApi.Application.Interfaces;
using SantanderHnApi.Application.Models;

namespace SantanderHnApi.Infrastructure.Services;

public sealed class HackerNewsClient : IHackerNewsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HackerNewsClient> _logger;

    public HackerNewsClient(
        HttpClient httpClient,
        ILogger<HackerNewsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var ids = await _httpClient.GetFromJsonAsync<List<int>>(
                          "v0/beststories.json",
                          cancellationToken)
                      ?? new List<int>();

            return ids;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to fetch best stories from Hacker News.");
            return Array.Empty<int>();
        }
    }

    public async Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _httpClient.GetFromJsonAsync<HackerNewsItem>(
                $"v0/item/{id}.json",
                cancellationToken);
            return item;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to fetch story {StoryId} from Hacker News.", id);
            return null;
        }
    }
}
