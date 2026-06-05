namespace Nanobot.Core.Mcp;

public static class McpClientFactory
{
    public static IMcpClient Create(McpServerConfig config, HttpClient? httpClient = null)
    {
        var transport = ResolveTransport(config);
        return transport switch
        {
            "stdio" => new McpStdioClient(config),
            "sse" or "http" or "streamablehttp" or "streamable-http" => new McpHttpClient(config, httpClient),
            _ => throw new InvalidOperationException($"Unsupported MCP transport '{transport}'.")
        };
    }

    private static string ResolveTransport(McpServerConfig config)
    {
        var transport = config.Transport ?? config.Type;
        if (!string.IsNullOrWhiteSpace(transport))
        {
            return transport.Trim().ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(config.Url))
        {
            return config.Url.TrimEnd('/').EndsWith("/sse", StringComparison.OrdinalIgnoreCase)
                ? "sse"
                : "streamablehttp";
        }

        return "stdio";
    }
}
