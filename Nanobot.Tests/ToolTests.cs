using Nanobot.Core.Tools;
using Nanobot.Core.Tools.Builtin;
using Nanobot.Core.Security;
using Nanobot.Core.Config;
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
    public async Task StockTool_UsesCsvApiAndParsesQuote()
    {
        var handler = new RecordingHandler((request, body) =>
        {
            Assert.Contains("s=aapl.us", request.RequestUri!.Query);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    Symbol,Date,Time,Open,High,Low,Close,Volume
                    AAPL.US,2026-06-05,22:00:12,190.00,192.00,189.50,191.25,123456
                    """)
            };
        });
        var tool = new StockTool(new HttpClient(handler), "https://example.test/q/l/");

        var result = await tool.ExecuteAsync(JsonNode.Parse("""{"symbol":"AAPL"}"""));

        Assert.Contains("股票: AAPL.US", result);
        Assert.Contains("收盘/最新: 191.25", result);
        Assert.DoesNotContain("Google", result);
    }

    [Fact]
    public async Task StockTool_ReturnsHelpfulMessageForMissingQuote()
    {
        var handler = new RecordingHandler((request, body) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                Symbol,Date,Time,Open,High,Low,Close,Volume
                UNKNOWN.US,N/D,N/D,N/D,N/D,N/D,N/D,N/D
                """)
        });
        var tool = new StockTool(new HttpClient(handler), "https://example.test/q/l/");

        var result = await tool.ExecuteAsync(JsonNode.Parse("""{"symbol":"UNKNOWN"}"""));

        Assert.Contains("未能获取股票 UNKNOWN 的报价", result);
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
    public async Task NongTool_AppendsJsonAndPassesArgumentArray()
    {
        var workspace = CreateWorkspace();
        var docs = Path.Combine(workspace, "docs");
        Directory.CreateDirectory(docs);
        var runner = new FakeNongRunner(new NongCommandResult(0, false, "{\"status\":\"ok\"}", ""));
        var tool = new NongTool(
            workspace,
            new NongToolSettings
            {
                Command = "fake-nong",
                AllowedRoots = new List<string> { "pdf" }
            },
            runner
        );

        var result = await tool.ExecuteAsync(JsonNode.Parse("""
            {
              "args": ["pdf", "check", "paper.pdf"],
              "workingDirectory": "docs",
              "timeoutMs": 1500
            }
            """));

        var json = JsonNode.Parse(result);
        Assert.Single(runner.Requests);
        Assert.Equal("fake-nong", runner.Requests[0].Command);
        Assert.Equal(new[] { "pdf", "check", "paper.pdf", "--json" }, runner.Requests[0].Args);
        Assert.Equal(docs, runner.Requests[0].WorkingDirectory);
        Assert.Equal(1500, runner.Requests[0].TimeoutMs);
        Assert.Equal(0, json?["exitCode"]?.GetValue<int>());
        Assert.Contains("\"status\":\"ok\"", json?["stdout"]?.ToString());
    }

    [Fact]
    public async Task NongTool_DoesNotDuplicateJsonFlag()
    {
        var runner = new FakeNongRunner(new NongCommandResult(0, false, "ok", ""));
        var tool = new NongTool(CreateWorkspace(), new NongToolSettings(), runner);

        await tool.ExecuteAsync(JsonNode.Parse("""
            {
              "args": ["--verbose", "commands", "--json"]
            }
            """));

        Assert.Single(runner.Requests);
        Assert.Equal(new[] { "--verbose", "commands", "--json" }, runner.Requests[0].Args);
    }

    [Fact]
    public async Task NongTool_RejectsDisallowedRootWithoutRunning()
    {
        var runner = new FakeNongRunner(new NongCommandResult(0, false, "unused", ""));
        var tool = new NongTool(
            CreateWorkspace(),
            new NongToolSettings
            {
                AllowedRoots = new List<string> { "pdf" }
            },
            runner
        );

        var result = await tool.ExecuteAsync(JsonNode.Parse("""
            {
              "args": ["lit", "search"]
            }
            """));

        var json = JsonNode.Parse(result);
        Assert.Equal("nong_root_not_allowed", json?["error"]?["code"]?.ToString());
        Assert.Empty(runner.Requests);
    }

    [Fact]
    public async Task NongTool_RejectsAllRootsWhenAllowlistIsExplicitlyEmpty()
    {
        var runner = new FakeNongRunner(new NongCommandResult(0, false, "unused", ""));
        var tool = new NongTool(
            CreateWorkspace(),
            new NongToolSettings
            {
                AllowedRoots = new List<string>()
            },
            runner
        );

        var result = await tool.ExecuteAsync(JsonNode.Parse("""
            {
              "args": ["pdf", "check", "paper.pdf"]
            }
            """));

        var json = JsonNode.Parse(result);
        Assert.Equal("nong_root_not_allowed", json?["error"]?["code"]?.ToString());
        Assert.Empty(runner.Requests);
    }

    [Fact]
    public async Task NongTool_RejectsWorkingDirectoryOutsideWorkspace()
    {
        var workspace = CreateWorkspace();
        var outside = Directory.GetParent(workspace)!.FullName;
        var runner = new FakeNongRunner(new NongCommandResult(0, false, "unused", ""));
        var tool = new NongTool(workspace, new NongToolSettings(), runner);

        var result = await tool.ExecuteAsync(JsonNode.Parse($$"""
            {
              "args": ["pdf", "check", "paper.pdf"],
              "workingDirectory": "{{JsonEscape(outside)}}"
            }
            """));

        var json = JsonNode.Parse(result);
        Assert.Equal("nong_execution_error", json?["error"]?["code"]?.ToString());
        Assert.Contains("outside workspace", json?["error"]?["message"]?.ToString());
        Assert.Empty(runner.Requests);
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

    private sealed class FakeNongRunner : INongCommandRunner
    {
        private readonly NongCommandResult _result;

        public FakeNongRunner(NongCommandResult result)
        {
            _result = result;
        }

        public List<NongCommandRequest> Requests { get; } = new();

        public Task<NongCommandResult> RunAsync(NongCommandRequest request)
        {
            Requests.Add(request);
            return Task.FromResult(_result);
        }
    }
}

public sealed class RecordingHandler : HttpMessageHandler
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
