using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Nanobot.Core.Mcp;

public class McpStdioClient : IMcpClient
{
    private readonly McpServerConfig _config;
    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private Process? _process;
    private int _nextId;
    private bool _initialized;

    public McpStdioClient(McpServerConfig config)
    {
        _config = config;
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

    public async ValueTask DisposeAsync()
    {
        _requestLock.Dispose();
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
        }
        catch
        {
            // Process disposal must not throw during agent shutdown.
        }
        finally
        {
            _process.Dispose();
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        StartProcess();
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

    private void StartProcess()
    {
        if (_process is not null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.Command))
        {
            throw new InvalidOperationException("MCP stdio server requires a command.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _config.Command!,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in _config.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (!string.IsNullOrWhiteSpace(_config.WorkingDirectory))
        {
            startInfo.WorkingDirectory = _config.WorkingDirectory;
        }

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start MCP server process.");
    }

    private async Task<JsonObject> SendRequestAsync(string method, JsonNode parameters, CancellationToken cancellationToken)
    {
        await _requestLock.WaitAsync(cancellationToken);
        try
        {
            StartProcess();
            var id = Interlocked.Increment(ref _nextId);
            var request = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["method"] = method,
                ["params"] = parameters.DeepClone()
            };

            await _process!.StandardInput.WriteLineAsync(request.ToJsonString());
            await _process.StandardInput.FlushAsync();

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await _process.StandardOutput.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    throw new InvalidOperationException("MCP server closed stdout.");
                }

                var response = JsonNode.Parse(line) as JsonObject;
                if (response?["id"]?.GetValue<int>() != id)
                {
                    continue;
                }

                if (response["error"] is JsonObject error)
                {
                    throw new InvalidOperationException(error["message"]?.ToString() ?? "MCP server returned an error.");
                }

                return response["result"] as JsonObject ?? new JsonObject();
            }

            throw new OperationCanceledException(cancellationToken);
        }
        finally
        {
            _requestLock.Release();
        }
    }

    private async Task SendNotificationAsync(string method, CancellationToken cancellationToken)
    {
        StartProcess();
        var request = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = new JsonObject()
        };

        await _process!.StandardInput.WriteLineAsync(request.ToJsonString());
        await _process.StandardInput.FlushAsync(cancellationToken);
    }
}
