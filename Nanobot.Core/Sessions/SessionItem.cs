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
    public SessionItemType Type { get; init; }
    public string ThreadId { get; init; } = "";
    public string TurnId { get; init; } = "";
    public string? Role { get; init; }
    public string? Content { get; init; }
    public string? ToolName { get; init; }
    public string? ToolCallId { get; init; }
    public LLMUsage? Usage { get; init; }
    public string? Model { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public static SessionItem FromRuntimeEvent(RuntimeEvent evt)
    {
        return new SessionItem
        {
            Type = MapEventType(evt.Type),
            Content = evt.Content,
            ToolName = evt.ToolName,
            ToolCallId = evt.ToolCallId,
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
