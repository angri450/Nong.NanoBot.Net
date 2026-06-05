using System.Text.Json.Nodes;
using Nanobot.Core.Tools;

namespace Nanobot.Core.Mcp;

public class McpToolAdapter : ITool
{
    private readonly IMcpClient _client;

    public McpToolAdapter(IMcpClient client, McpToolDefinition definition)
    {
        _client = client;
        Name = definition.Name;
        Description = definition.Description;
        Parameters = definition.Parameters;
    }

    public string Name { get; }

    public string Description { get; }

    public JsonNode Parameters { get; }

    public Task<string> ExecuteAsync(JsonNode? arguments)
    {
        return _client.CallToolAsync(Name, arguments);
    }
}
