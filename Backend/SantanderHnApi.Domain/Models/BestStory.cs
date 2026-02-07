namespace SantanderHnApi.Domain.Models;

public sealed class BestStory
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Uri { get; init; } = string.Empty;
    public string PostedBy { get; init; } = string.Empty;
    public DateTimeOffset Time { get; init; }
    public int Score { get; init; }
    public int CommentCount { get; init; }
}
