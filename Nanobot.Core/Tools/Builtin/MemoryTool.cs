using System.Text.Json.Nodes;
using Nanobot.Core.Memory;

namespace Nanobot.Core.Tools.Builtin;

public class MemoryTool : ITool
{
    private readonly IWritableMemory _memory;

    public MemoryTool(IWritableMemory memory)
    {
        _memory = memory;
    }

    public string Name => "remember";

    public string Description => "Persist durable user or agent memory. Use only for stable preferences, facts, or decisions worth remembering across sessions.";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "content": {
                "type": "string",
                "description": "The memory note to persist. Keep it concise and factual."
            },
            "mode": {
                "type": "string",
                "enum": ["append", "replace"],
                "description": "append adds a note to MEMORY.md; replace overwrites MEMORY.md.",
                "default": "append"
            }
        },
        "required": ["content"]
    }
    """)!;

    public Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var content = arguments?["content"]?.ToString();
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult("Error: content is required.");
        }

        var mode = arguments?["mode"]?.ToString();
        if (string.Equals(mode, "replace", StringComparison.OrdinalIgnoreCase))
        {
            _memory.WriteMemory(content.Trim() + Environment.NewLine);
            return Task.FromResult("Memory replaced.");
        }

        _memory.AppendMemory(content);
        return Task.FromResult("Memory saved.");
    }
}
