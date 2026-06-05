using Nanobot.Core.Tools;

namespace Nanobot.Core.Mcp;

public class McpToolProvider
{
    private readonly IMcpClient _client;

    public McpToolProvider(IMcpClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<ITool>> LoadToolsAsync(CancellationToken cancellationToken = default)
    {
        var definitions = await _client.ListToolsAsync(cancellationToken);
        return definitions
            .Select(definition => (ITool)new McpToolAdapter(_client, definition))
            .ToList();
    }
}
