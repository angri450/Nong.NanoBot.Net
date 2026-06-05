using System.Text.Json.Nodes;

namespace Nanobot.Core.Mcp;

public record McpToolDefinition(
    string Name,
    string Description,
    JsonNode Parameters
);
