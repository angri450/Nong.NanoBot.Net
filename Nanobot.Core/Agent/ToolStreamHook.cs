namespace Nanobot.Core.Agent;

/// <summary>
/// Hook that emits streaming events for tool calls.
/// Use with Agent.RunStreamingAsync to get real-time tool call visibility.
/// </summary>
public class ToolStreamHook : IAgentHook
{
    private readonly Func<string, string, string?, CancellationToken, Task> _onToolEvent;
    private readonly Dictionary<string, DateTime> _toolStarts = new();

    public ToolStreamHook(Func<string, string, string?, CancellationToken, Task> onToolEvent)
    {
        _onToolEvent = onToolEvent;
    }

    public async Task BeforeToolAsync(AgentToolContext context)
    {
        var toolName = context.ToolCall.Name;
        var argsSummary = context.ToolCall.Arguments?.ToString() ?? "{}";
        if (argsSummary.Length > 200) argsSummary = argsSummary[..200] + "...";

        _toolStarts[toolName] = DateTime.UtcNow;
        await _onToolEvent("tool_start", toolName, argsSummary, CancellationToken.None);
    }

    public async Task AfterToolAsync(AgentToolContext context)
    {
        var toolName = context.ToolCall.Name;
        var duration = _toolStarts.TryGetValue(toolName, out var start)
            ? $"{(DateTime.UtcNow - start).TotalSeconds:F1}s"
            : "";
        var result = context.Result ?? "";
        var preview = result.Length > 300 ? result[..300] + "..." : result;

        _toolStarts.Remove(toolName);
        await _onToolEvent("tool_end", toolName, $"[{duration}] {preview}", CancellationToken.None);
    }

    public async Task OnToolErrorAsync(AgentToolContext context)
    {
        var toolName = context.ToolCall.Name;
        await _onToolEvent("tool_error", toolName, context.ErrorMessage ?? "Unknown error", CancellationToken.None);
    }
}
