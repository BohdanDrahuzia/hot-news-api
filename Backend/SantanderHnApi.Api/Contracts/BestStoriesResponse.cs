using SantanderHnApi.Domain.Models;

namespace SantanderHnApi.Api.Contracts;

public sealed class BestStoriesResponse
{
    public int Count { get; init; }
    public IReadOnlyList<BestStory> Stories { get; init; } = Array.Empty<BestStory>();
}
