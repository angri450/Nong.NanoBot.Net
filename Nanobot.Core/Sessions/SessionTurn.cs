namespace Nanobot.Core.Sessions;

public record SessionTurn
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string ThreadId { get; init; } = "";
    public int Index { get; init; }
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; init; }
    public string? UserInput { get; init; }
}
