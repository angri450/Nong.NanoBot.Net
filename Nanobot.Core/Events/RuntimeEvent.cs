namespace Nanobot.Core.Events;

public enum RuntimeEventType
{
    RunStarted,
    ContentDelta,
    ContentCompleted,
    ReasoningDelta,
    ReasoningCompleted,
    ToolStarted,
    ToolDelta,
    ToolCompleted,
    ToolFailed,
    UsageUpdated,
    CacheMetricsUpdated,
    ApprovalRequested,
    UserInputRequested,
    RunInterrupted,
    RunFailed,
    RunCompleted,
    ModelSelected
}

public record RuntimeEvent
{
    public required RuntimeEventType Type { get; init; }

    public string EventId { get; init; } = Guid.NewGuid().ToString("N");

    public long Sequence { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public required string RunId { get; init; }

    public required string SessionId { get; init; }

    public string? ThreadId { get; init; }

    public string? TurnId { get; init; }

    public string? ToolName { get; init; }

    public string? ToolCallId { get; init; }

    public string? Content { get; init; }

    public string? ErrorMessage { get; init; }

    public object? Payload { get; init; }
}
