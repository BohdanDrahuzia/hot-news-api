using SantanderHnApi.Application.Models;

namespace SantanderHnApi.Application.Interfaces;

public interface IHackerNewsClient
{
    Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken cancellationToken);
    Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken cancellationToken);
}
