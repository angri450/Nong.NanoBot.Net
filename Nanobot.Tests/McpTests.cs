using System.Text.Json.Nodes;
using System.Net;
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

    [Fact]
    public void McpClientFactory_CreatesHttpClientForRemoteConfig()
    {
        var client = McpClientFactory.Create(new McpServerConfig
        {
            Url = "https://mcp.example/rpc"
        });

        Assert.IsType<McpHttpClient>(client);
    }

    [Fact]
    public async Task McpHttpClient_ListsAndCallsToolsOverStreamableHttp()
    {
        var handler = new RecordingHandler((request, body) =>
        {
            Assert.Equal("Bearer token", request.Headers.GetValues("Authorization").Single());
            var json = JsonNode.Parse(body)!;
            var method = json["method"]?.ToString();
            return JsonResponse(method switch
            {
                "initialize" => """{"jsonrpc":"2.0","id":1,"result":{"capabilities":{}}}""",
                "notifications/initialized" => """{"jsonrpc":"2.0","result":{}}""",
                "tools/list" => """
                    {
                      "jsonrpc": "2.0",
                      "id": 2,
                      "result": {
                        "tools": [
                          {
                            "name": "remote_lookup",
                            "description": "Remote lookup",
                            "inputSchema": {"type":"object","properties":{"query":{"type":"string"}}}
                          }
                        ]
                      }
                    }
                    """,
                "tools/call" => """
                    {
                      "jsonrpc": "2.0",
                      "id": 3,
                      "result": {
                        "content": [{"type":"text","text":"remote-result"}]
                      }
                    }
                    """,
                _ => """{"jsonrpc":"2.0","result":{}}"""
            });
        });
        await using var client = new McpHttpClient(new McpServerConfig
        {
            Transport = "streamableHttp",
            Url = "https://mcp.example/rpc",
            Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer token" }
        }, new HttpClient(handler));

        var tools = await client.ListToolsAsync();
        var result = await client.CallToolAsync("remote_lookup", JsonNode.Parse("""{"query":"nano"}"""));

        Assert.Single(tools);
        Assert.Equal("remote_lookup", tools[0].Name);
        Assert.Equal("remote-result", result);
    }

    [Fact]
    public async Task McpHttpClient_DiscoversSseEndpoint()
    {
        var handler = new RecordingHandler((request, body) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                Assert.Equal("https://mcp.example/sse", request.RequestUri!.ToString());
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("event: endpoint\ndata: /messages\n\n")
                };
            }

            Assert.Equal("https://mcp.example/messages", request.RequestUri!.ToString());
            var method = JsonNode.Parse(body)?["method"]?.ToString();
            return JsonResponse(method == "tools/list"
                ? """{"jsonrpc":"2.0","id":2,"result":{"tools":[]}}"""
                : """{"jsonrpc":"2.0","id":1,"result":{}}""");
        });
        await using var client = new McpHttpClient(new McpServerConfig
        {
            Transport = "sse",
            Url = "https://mcp.example/sse"
        }, new HttpClient(handler));

        var tools = await client.ListToolsAsync();

        Assert.Empty(tools);
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

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string, HttpResponseMessage> _handler;

        public RecordingHandler(Func<HttpRequestMessage, string, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return _handler(request, body);
        }
    }
}
