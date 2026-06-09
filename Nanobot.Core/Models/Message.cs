using System.Text.Json.Nodes;

namespace Nanobot.Core.Models;

public record Message
{
    public string Role { get; init; }
    public string? Content { get; init; }
    public List<ToolCallRequest>? ToolCalls { get; init; }
    public string? ToolCallId { get; init; }
    public string? ReasoningContent { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }

    public Message(string role, string? content)
    {
        Role = role;
        Content = content;
    }
}