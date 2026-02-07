using System.Text.Json.Serialization;

namespace SantanderHnApi.Client.Models;

public sealed class BestStoriesResponse
{
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("stories")]
    public IReadOnlyList<BestStory> Stories { get; init; } = Array.Empty<BestStory>();
}
