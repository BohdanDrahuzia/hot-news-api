using System.Text.Json.Serialization;

namespace SantanderHnApi.Client.Models;

public sealed class BestStory
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("uri")]
    public string Uri { get; init; } = string.Empty;

    [JsonPropertyName("postedBy")]
    public string PostedBy { get; init; } = string.Empty;

    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; init; }

    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("commentCount")]
    public int CommentCount { get; init; }

    [JsonPropertyName("id")]
    public int Id { get; init; }
}
