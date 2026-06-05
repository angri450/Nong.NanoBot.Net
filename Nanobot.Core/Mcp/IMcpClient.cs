using System.Text.Json.Nodes;

namespace Nanobot.Core.Mcp;

public interface IMcpClient : IAsyncDisposable
{
    Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default);

    Task<string> CallToolAsync(string name, JsonNode? arguments, CancellationToken cancellationToken = default);
}
