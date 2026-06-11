using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace Nanobot.Core.Mcp;

public class McpHttpClient : IMcpClient
{
    private readonly HttpClient _httpClient;
    private readonly McpServerConfig _config;
    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private readonly bool _useSseEndpointDiscovery;
    private Uri? _rpcUri;
    private int _nextId;
    private bool _initialized;

    public McpHttpClient(McpServerConfig config, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(config.Url))
        {
            throw new ArgumentException("MCP HTTP/SSE server requires a URL.", nameof(config));
        }

        _config = config;
        _httpClient = httpClient ?? new HttpClient();
        _rpcUri = new Uri(config.Url, UriKind.Absolute);
        _useSseEndpointDiscovery = ResolveTransport(config) == "sse";
    }

    public async Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var result = await SendRequestAsync("tools/list", new JsonObject(), cancellationToken);
        var tools = result["tools"] as JsonArray ?? new JsonArray();

        return tools.OfType<JsonObject>().Select(tool => new McpToolDefinition(
            tool["name"]?.ToString() ?? "unknown",
            tool["description"]?.ToString() ?? "",
            tool["inputSchema"]?.DeepClone() ?? new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject()
            }
        )).ToList();
    }

    public async Task<string> CallToolAsync(string name, JsonNode? arguments, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var result = await SendRequestAsync(
            "tools/call",
            new JsonObject
            {
                ["name"] = name,
                ["arguments"] = arguments?.DeepClone() ?? new JsonObject()
            },
            cancellationToken
        );

        var textParts = new List<string>();
        if (result["content"] is JsonArray content)
        {
            foreach (var item in content.OfType<JsonObject>())
            {
                if (item["type"]?.ToString() == "text")
                {
                    var text = item["text"]?.ToString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        textParts.Add(text);
                    }
                }
            }
        }

        var output = textParts.Count == 0 ? result.ToJsonString() : string.Join("\n", textParts);
        return result["isError"]?.GetValue<bool>() == true ? $"Error: {output}" : output;
    }

    public ValueTask DisposeAsync()
    {
        _requestLock.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        if (_useSseEndpointDiscovery)
        {
            _rpcUri = await DiscoverSseEndpointAsync(cancellationToken);
        }

        await SendRequestAsync(
            "initialize",
            new JsonObject
            {
                ["protocolVersion"] = "2024-11-05",
                ["capabilities"] = new JsonObject(),
                ["clientInfo"] = new JsonObject
                {
                    ["name"] = "Nong.NanoBot.Net",
                    ["version"] = "0.1.0"
                }
            },
            cancellationToken
        );

        await SendNotificationAsync("notifications/initialized", cancellationToken);
        _initialized = true;
    }

    private async Task<JsonObject> SendRequestAsync(string method, JsonNode parameters, CancellationToken cancellationToken)
    {
        await _requestLock.WaitAsync(cancellationToken);
        try
        {
            var id = Interlocked.Increment(ref _nextId);
            var request = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["method"] = method,
                ["params"] = parameters.DeepClone()
            };

            var response = await SendJsonRpcAsync(request, cancellationToken);
            if (response["error"] is JsonObject error)
            {
                throw new InvalidOperationException(error["message"]?.ToString() ?? "MCP server returned an error.");
            }

            return response["result"] as JsonObject ?? new JsonObject();
        }
        finally
        {
            _requestLock.Release();
        }
    }

    private async Task SendNotificationAsync(string method, CancellationToken cancellationToken)
    {
        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = new JsonObject()
        };

        await SendJsonRpcAsync(request, cancellationToken);
    }

    private async Task<JsonObject> SendJsonRpcAsync(JsonObject payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _rpcUri)
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
        };
        ApplyHeaders(request);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"MCP server returned {(int)response.StatusCode}: {body}");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(body) as JsonObject ?? new JsonObject();
    }

    private async Task<Uri> DiscoverSseEndpointAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _rpcUri);
        ApplyHeaders(request);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? eventName = null;
        var data = new StringBuilder();
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (line.Length == 0)
            {
                if (eventName == "endpoint" && data.Length > 0)
                {
                    return ResolveEndpoint(data.ToString().Trim());
                }

                eventName = null;
                data.Clear();
                continue;
            }

            if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
            {
                eventName = line["event:".Length..].Trim();
            }
            else if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                if (data.Length > 0)
                {
                    data.Append('\n');
                }

                data.Append(line["data:".Length..].Trim());
            }
        }

        throw new InvalidOperationException("MCP SSE endpoint event was not received.");
    }

    private Uri ResolveEndpoint(string endpoint)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        return new Uri(_rpcUri!, endpoint);
    }

    private void ApplyHeaders(HttpRequestMessage request)
    {
        foreach (var (name, value) in _config.Headers)
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }
    }

    private static string ResolveTransport(McpServerConfig config)
    {
        var value = config.Transport ?? config.Type;
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim().ToLowerInvariant();
        }

        return config.Url?.TrimEnd('/').EndsWith("/sse", StringComparison.OrdinalIgnoreCase) == true
            ? "sse"
            : "streamablehttp";
    }
}
