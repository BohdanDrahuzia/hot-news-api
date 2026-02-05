using SantanderHnApi.Domain.Models;

namespace SantanderHnApi.Application.Interfaces;

public interface IBestStoriesService
{
    Task<IReadOnlyList<BestStory>> GetBestStoriesAsync(int count, CancellationToken cancellationToken);
}
