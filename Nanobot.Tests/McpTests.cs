using System.Text.Json.Nodes;
using Nanobot.Core.Mcp;

namespace Nanobot.Tests;

public class McpTests
{
    [Fact]
    public async Task McpToolProvider_AdaptsListedTools()
    {
        var client = new FakeMcpClient();
        var provider = new McpToolProvider(client);

        var tools = await provider.LoadToolsAsync();

        Assert.Single(tools);
        Assert.Equal("lookup", tools[0].Name);
        Assert.Equal("Lookup a value", tools[0].Description);
    }

    [Fact]
    public async Task McpToolAdapter_CallsMcpClient()
    {
        var client = new FakeMcpClient();
        var tool = (await new McpToolProvider(client).LoadToolsAsync()).Single();

        var result = await tool.ExecuteAsync(JsonNode.Parse("{\"query\":\"nano\"}"));

        Assert.Equal("mcp-result", result);
        Assert.Equal("lookup", client.LastToolName);
        Assert.Equal("nano", client.LastArguments?["query"]?.ToString());
    }

    private sealed class FakeMcpClient : IMcpClient
    {
        public string? LastToolName { get; private set; }

        public JsonNode? LastArguments { get; private set; }

        public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<McpToolDefinition> tools = new[]
            {
                new McpToolDefinition(
                    "lookup",
                    "Lookup a value",
                    JsonNode.Parse("{\"type\":\"object\",\"properties\":{\"query\":{\"type\":\"string\"}}}")!
                )
            };
            return Task.FromResult(tools);
        }

        public Task<string> CallToolAsync(string name, JsonNode? arguments, CancellationToken cancellationToken = default)
        {
            LastToolName = name;
            LastArguments = arguments;
            return Task.FromResult("mcp-result");
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
