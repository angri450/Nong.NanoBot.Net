using Nanobot.Core.Events;
using Nanobot.Core.Providers;

namespace Nanobot.Core.Sessions;

public enum SessionItemType
{
    UserMessage,
    AssistantMessage,
    Reasoning,
    ToolCall,
    ToolResult,
    Usage,
    Approval,
    UserInput,
    SystemNote
}

public record SessionItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string SessionId { get; init; } = "";
    public SessionItemType Type { get; init; }
    public RuntimeEventType? EventType { get; init; }
    public string EventId { get; init; } = "";
    public long Sequence { get; init; }
    public string RunId { get; init; } = "";
    public string ThreadId { get; init; } = "";
    public string TurnId { get; init; } = "";
    public string? Role { get; init; }
    public string? Content { get; init; }
    public string? ToolName { get; init; }
    public string? ToolCallId { get; init; }
    public string? ErrorMessage { get; init; }
    public LLMUsage? Usage { get; init; }
    public string? Model { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public static SessionItem FromRuntimeEvent(RuntimeEvent evt)
    {
        return new SessionItem
        {
            SessionId = evt.SessionId,
            Type = MapEventType(evt.Type),
            EventType = evt.Type,
            EventId = evt.EventId,
            Sequence = evt.Sequence,
            RunId = evt.RunId,
            ThreadId = evt.ThreadId ?? evt.SessionId,
            TurnId = evt.TurnId ?? "",
            Content = evt.Content,
            ToolName = evt.ToolName,
            ToolCallId = evt.ToolCallId,
            ErrorMessage = evt.ErrorMessage,
            Usage = evt.Payload as LLMUsage,
            Timestamp = evt.Timestamp
        };
    }

    private static SessionItemType MapEventType(RuntimeEventType type)
    {
        return type switch
        {
            RuntimeEventType.ContentDelta or RuntimeEventType.ContentCompleted => SessionItemType.AssistantMessage,
            RuntimeEventType.ReasoningDelta or RuntimeEventType.ReasoningCompleted => SessionItemType.Reasoning,
            RuntimeEventType.ToolStarted or RuntimeEventType.ToolDelta or RuntimeEventType.ToolCompleted => SessionItemType.ToolCall,
            RuntimeEventType.ToolFailed => SessionItemType.ToolResult,
            RuntimeEventType.UsageUpdated or RuntimeEventType.CacheMetricsUpdated => SessionItemType.Usage,
            RuntimeEventType.ApprovalRequested => SessionItemType.Approval,
            RuntimeEventType.UserInputRequested => SessionItemType.UserInput,
            _ => SessionItemType.SystemNote
        };
    }
}
