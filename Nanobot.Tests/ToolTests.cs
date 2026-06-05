using Nanobot.Core.Tools;
using Nanobot.Core.Tools.Builtin;
using Nanobot.Core.Security;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace Nanobot.Tests;

public class ToolTests
{
    [Fact]
    public void ToolRegistry_CanRegisterAndGet()
    {
        var registry = new ToolRegistry();
        var tool = new ReadFileTool();
        
        registry.Register(tool);
        
        Assert.True(registry.Has(tool.Name));
        Assert.Equal(tool, registry.Get(tool.Name));
    }

    [Fact]
    public void ToolRegistry_GetDefinitions_ReturnsValidOpenAISchema()
    {
        var registry = new ToolRegistry();
        registry.Register(new ReadFileTool());
        
        var definitions = registry.GetDefinitions();
        
        Assert.Single(definitions);
        Assert.Equal("function", definitions[0]["type"]?.ToString());
        Assert.Equal("read_file", definitions[0]["function"]?["name"]?.ToString());
    }

    [Fact]
    public async Task ShellTool_CanExecuteEcho()
    {
        var tool = new ShellTool();
        var args = JsonNode.Parse("{\"command\": \"echo hello\"}");
        
        var result = await tool.ExecuteAsync(args);
        var json = JsonNode.Parse(result);
        
        Assert.Contains("hello", json?["stdout"]?.ToString().ToLower());
        Assert.Equal(0, json?["exitCode"]?.GetValue<int>());
    }

    [Fact]
    public async Task ToolRegistry_ExecuteWithResult_ReturnsStructuredMissingToolError()
    {
        var registry = new ToolRegistry();

        var result = await registry.ExecuteWithResultAsync("missing", JsonNode.Parse("{}"));

        Assert.False(result.Success);
        Assert.Equal("tool_not_found", result.ErrorCode);
        var json = JsonNode.Parse(result.Content!);
        Assert.Equal("missing", json?["error"]?["tool"]?.ToString());
    }

    [Fact]
    public async Task ShellTool_RejectsWorkingDirectoryOutsideWorkspace()
    {
        var workspace = CreateWorkspace();
        var outside = Directory.GetParent(workspace)!.FullName;
        var tool = new ShellTool(workspace);
        var args = JsonNode.Parse($$"""
            {
              "command": "echo should-not-run",
              "workingDirectory": "{{JsonEscape(outside)}}"
            }
            """);

        var result = await tool.ExecuteAsync(args);

        var json = JsonNode.Parse(result);
        Assert.Equal("shell_execution_error", json?["error"]?["code"]?.ToString());
        Assert.Contains("outside workspace", json?["error"]?["message"]?.ToString());
    }

    [Fact]
    public async Task ShellTool_TimesOutLongRunningCommand()
    {
        var tool = new ShellTool(CreateWorkspace());
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "ping 127.0.0.1 -n 6 > nul"
            : "sleep 5";
        var args = JsonNode.Parse($$"""
            {
              "command": "{{JsonEscape(command)}}",
              "timeoutMs": 100
            }
            """);

        var result = await tool.ExecuteAsync(args);

        var json = JsonNode.Parse(result);
        Assert.True(json?["timedOut"]?.GetValue<bool>());
        Assert.Null(json?["exitCode"]);
    }

    [Fact]
    public async Task ShellTool_TruncatesLargeOutput()
    {
        var tool = new ShellTool(CreateWorkspace());
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "powershell -NoProfile -Command \"$s='x'*150; Write-Output $s\""
            : "i=0; while [ $i -lt 150 ]; do printf x; i=$((i+1)); done";
        var args = JsonNode.Parse($$"""
            {
              "command": "{{JsonEscape(command)}}",
              "maxOutputChars": 100
            }
            """);

        var result = await tool.ExecuteAsync(args);

        var json = JsonNode.Parse(result);
        Assert.True(json?["truncated"]?.GetValue<bool>());
        Assert.Contains("truncated", json?["stdout"]?.ToString());
    }

    [Fact]
    public async Task WebFetchTool_BlocksPrivateAddressBeforeRequest()
    {
        var handler = new CountingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var tool = new WebFetchTool(
            httpClient: new HttpClient(handler),
            networkGuard: new NetworkSecurityGuard()
        );

        var result = await tool.ExecuteAsync(JsonNode.Parse("{\"url\":\"http://127.0.0.1/secret\"}"));

        var json = JsonNode.Parse(result);
        Assert.Contains("restricted address", json?["error"]?.ToString());
        Assert.Equal(0, handler.Count);
    }

    [Fact]
    public async Task WebFetchTool_BlocksRedirectToPrivateAddress()
    {
        var handler = new CountingHandler(_ => new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers = { Location = new Uri("http://127.0.0.1/secret") }
        });
        var resolver = new StaticResolver(IPAddress.Parse("93.184.216.34"));
        var tool = new WebFetchTool(
            httpClient: new HttpClient(handler),
            networkGuard: new NetworkSecurityGuard(resolver)
        );

        var result = await tool.ExecuteAsync(JsonNode.Parse("{\"url\":\"http://example.com/start\"}"));

        var json = JsonNode.Parse(result);
        Assert.Contains("restricted address", json?["error"]?.ToString());
        Assert.Equal(1, handler.Count);
    }

    private static string CreateWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "nanobot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string JsonEscape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public CountingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        public int Count { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++;
            return Task.FromResult(_handler(request));
        }
    }

    private sealed class StaticResolver : IHostResolver
    {
        private readonly IPAddress[] _addresses;

        public StaticResolver(params IPAddress[] addresses)
        {
            _addresses = addresses;
        }

        public Task<IPAddress[]> GetHostAddressesAsync(string host)
        {
            return Task.FromResult(_addresses);
        }
    }
}
