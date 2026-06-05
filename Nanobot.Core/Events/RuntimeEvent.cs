namespace Nanobot.Core.Events;

public enum RuntimeEventType
{
    RunStarted,
    RunCompleted,
    RunFailed,
    ToolStarted,
    ToolCompleted,
    ToolFailed
}

public record RuntimeEvent
{
    public required RuntimeEventType Type { get; init; }

    public required string RunId { get; init; }

    public required string SessionId { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public string? ToolName { get; init; }

    public string? ToolCallId { get; init; }

    public string? Content { get; init; }

    public string? ErrorMessage { get; init; }
}
