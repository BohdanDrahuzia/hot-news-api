using System.Text.Json.Serialization;

namespace SantanderHnApi.Application.Models;

public sealed class HackerNewsItem
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("by")]
    public string? By { get; init; }

    [JsonPropertyName("time")]
    public long? Time { get; init; }

    [JsonPropertyName("score")]
    public int? Score { get; init; }

    [JsonPropertyName("descendants")]
    public int? Descendants { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("id")]
    public int? Id { get; init; }
}
