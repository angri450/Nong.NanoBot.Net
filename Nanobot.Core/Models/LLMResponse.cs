using Nanobot.Core.Providers;

namespace Nanobot.Core.Models;

public record LLMResponse
{
    public string? Content { get; init; }
    public List<ToolCallRequest> ToolCalls { get; init; } = new();
    public string FinishReason { get; init; } = "stop";
    public LLMUsage? Usage { get; init; }
    public string? ReasoningContent { get; init; }
    public string? Model { get; init; }
    public string? Provider { get; init; }

    public bool HasToolCalls => ToolCalls.Count > 0;

    public LLMResponse(string? content)
    {
        Content = content;
    }

    public LLMResponse() { }
}
