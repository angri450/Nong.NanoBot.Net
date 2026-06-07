using System.Text.Json;
using System.Text.Json.Serialization;
using Nanobot.Core.Agent;
using Nanobot.Core.Config;
using Nanobot.Core.Events;
using Nanobot.Core.Memory;
using Nanobot.Core.Mcp;
using Nanobot.Core.Providers;
using Nanobot.Core.Tools;
using Nanobot.Core.Tools.Builtin;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<NanobotWebRuntime>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/runtime/status", (NanobotWebRuntime runtime) => runtime.GetStatus());

app.MapPost("/api/agent/message", async Task<IResult> (
    AgentMessageRequest request,
    NanobotWebRuntime runtime,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new ApiErrorResponse("Message is required."));
    }

    try
    {
        var response = await runtime.SendMessageAsync(request, cancellationToken);
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(
            new ApiErrorResponse(ex.Message),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        return Results.Json(
            new ApiErrorResponse($"Agent request failed: {ex.Message}"),
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapGet("/api/events", async (
    HttpContext context,
    NanobotWebRuntime runtime,
    CancellationToken cancellationToken) =>
{
    context.Response.Headers.ContentType = "text/event-stream";
    await foreach (var runtimeEvent in runtime.ListenAsync(cancellationToken))
    {
        await context.Response.WriteAsync($"event: runtime\n", cancellationToken);
        await context.Response.WriteAsync(
            $"data: {JsonSerializer.Serialize(runtimeEvent, NanobotWebJson.EventOptions)}\n\n",
            cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
});

app.MapGet("/favicon.ico", () => Results.NoContent());

app.Run();

public sealed class NanobotWebRuntime
{
    private readonly string _workspace;
    private readonly AppConfig _config;
    private readonly RuntimeEventBus _eventBus = new();
    private readonly FileMemoryStore _memory;
    private readonly ILLMProvider? _provider;
    private readonly Agent? _agent;
    private readonly string? _startupError;
    private readonly string? _startupWarning;

    public NanobotWebRuntime()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var nanoDir = Path.Combine(home, ".nanobot");
        var configFile = Path.Combine(nanoDir, "config.json");
        _workspace = Path.Combine(nanoDir, "workspace");
        Directory.CreateDirectory(_workspace);

        _memory = new FileMemoryStore(_workspace);

        try
        {
            _config = File.Exists(configFile) ? ConfigLoader.Load(configFile) : new AppConfig();
        }
        catch (Exception ex)
        {
            _config = new AppConfig();
            _startupError = $"Failed to load ~/.nanobot/config.json: {ex.Message}";
            return;
        }

        try
        {
            var providerSetup = ProviderConfigurationFactory.Create(_config);
            _provider = providerSetup.Provider;

            var registry = CreateToolRegistry(_provider);
            var mcpWarnings = RegisterMcpToolsAsync(registry, _config).GetAwaiter().GetResult();
            if (mcpWarnings.Count > 0)
            {
                _startupWarning = string.Join(Environment.NewLine, mcpWarnings);
            }

            _agent = new Agent(_provider, registry, _memory, _eventBus);
        }
        catch (Exception ex)
        {
            _startupError = FormatStartupError(ex);
        }
    }

    public RuntimeStatusResponse GetStatus()
    {
        return new RuntimeStatusResponse(
            Workspace: _workspace,
            Model: _provider?.GetDefaultModel() ?? "Not configured",
            NongEnabled: _config.Tools.Nong.Enabled,
            MemoryPreview: Truncate(_memory.GetContext(), 1600),
            Ready: _agent is not null,
            Error: _startupError,
            Warning: _startupWarning
        );
    }

    public async Task<AgentMessageResponse> SendMessageAsync(AgentMessageRequest request, CancellationToken cancellationToken)
    {
        if (_agent is null)
        {
            throw new InvalidOperationException(_startupError ?? "Agent runtime is not ready.");
        }

        var sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? "web-default" : request.SessionId;
        var context = AgentExecutionContext.CreateRoot(_workspace) with
        {
            SessionId = sessionId
        };
        var answer = await _agent.RunAsync(request.Message, context);
        return new AgentMessageResponse(sessionId, answer);
    }

    public async IAsyncEnumerable<RuntimeEvent> ListenAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queue = System.Threading.Channels.Channel.CreateUnbounded<RuntimeEvent>();
        using var subscription = _eventBus.Subscribe(runtimeEvent => queue.Writer.TryWrite(runtimeEvent));
        while (!cancellationToken.IsCancellationRequested)
        {
            RuntimeEvent runtimeEvent;
            try
            {
                runtimeEvent = await queue.Reader.ReadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            yield return runtimeEvent;
        }
    }

    private ToolRegistry CreateToolRegistry(ILLMProvider provider)
    {
        var registry = new ToolRegistry();
        registry.Register(new ReadFileTool());
        registry.Register(new WriteFileTool());
        registry.Register(new EditFileTool());
        registry.Register(new ListDirTool());
        registry.Register(new ShellTool(_workspace));
        registry.Register(new WebSearchTool(Environment.GetEnvironmentVariable("BRAVE_API_KEY") ?? _config.WebSearch?.ApiKey ?? ""));
        registry.Register(new WebFetchTool());
        registry.Register(new WeatherTool());
        registry.Register(new StockTool());
        registry.Register(new SummarizeTool(provider));
        registry.Register(new MemoryTool(_memory));

        if (_config.Tools.Nong.Enabled)
        {
            registry.Register(new NongTool(_workspace, _config.Tools.Nong));
        }

        var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? (_config.Providers.TryGetValue("github", out var gh) ? gh.ApiKey : null);
        registry.Register(new GitHubTool(githubToken));

        return registry;
    }

    private static async Task<IReadOnlyList<string>> RegisterMcpToolsAsync(ToolRegistry registry, AppConfig config)
    {
        var warnings = new List<string>();
        foreach (var (name, serverConfig) in config.Tools.McpServers)
        {
            try
            {
                var client = McpClientFactory.Create(serverConfig);
                var tools = await new McpToolProvider(client).LoadToolsAsync();
                foreach (var tool in tools)
                {
                    registry.Register(tool);
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"MCP server '{name}' unavailable: {ex.Message}");
            }
        }

        return warnings;
    }

    private static string FormatStartupError(Exception ex)
    {
        var message = ex.Message;
        if (ex is ProviderConfigurationException)
        {
            message += " Configure ~/.nanobot/config.json or set provider environment variables.";
        }

        return message;
    }

    private static string Truncate(string value, int maxChars)
    {
        return value.Length <= maxChars ? value : value[..maxChars] + "\n... (truncated)";
    }
}

public sealed record AgentMessageRequest(string SessionId, string Message);

public sealed record AgentMessageResponse(string SessionId, string Answer);

public sealed record ApiErrorResponse(string Error);

public sealed record RuntimeStatusResponse(
    string Workspace,
    string Model,
    bool NongEnabled,
    string MemoryPreview,
    bool Ready,
    string? Error,
    string? Warning);

public static class NanobotWebJson
{
    public static JsonSerializerOptions EventOptions { get; } = CreateEventOptions();

    private static JsonSerializerOptions CreateEventOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
