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
using Nanobot.Web;

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

app.MapGet("/api/sessions", (NanobotWebRuntime runtime) => runtime.ListSessions());

app.MapPost("/api/sessions", (CreateSessionRequest request, NanobotWebRuntime runtime) =>
    runtime.CreateSession(request));

app.MapGet("/api/sessions/{sessionId}", (string sessionId, NanobotWebRuntime runtime) =>
{
    var session = runtime.GetSession(sessionId);
    return session is null
        ? Results.NotFound(new ApiErrorResponse("Session not found."))
        : Results.Ok(session);
});

app.MapGet("/api/workspace/files", (string? path, NanobotWebRuntime runtime) =>
{
    try
    {
        return Results.Ok(runtime.ListFiles(path));
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new ApiErrorResponse(ex.Message));
    }
});

app.MapGet("/api/workspace/file", (string? path, NanobotWebRuntime runtime) =>
{
    try
    {
        return Results.Ok(runtime.ReadFile(path));
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new ApiErrorResponse(ex.Message));
    }
});

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

app.MapPost("/api/agent/stream", async (
    AgentMessageRequest request,
    NanobotWebRuntime runtime,
    HttpContext context,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(
            new ApiErrorResponse("Message is required."),
            cancellationToken);
        return;
    }

    context.Response.Headers.ContentType = "application/x-ndjson";
    context.Response.Headers.CacheControl = "no-cache";

    try
    {
        var response = await runtime.SendMessageStreamingAsync(
            request,
            async (streamEvent, token) =>
            {
                await WriteNdjsonAsync(context, streamEvent, token);
            },
            cancellationToken);

        await WriteNdjsonAsync(
            context,
            new AgentStreamEvent("complete", response.SessionId, Answer: response.Answer),
            cancellationToken);
    }
    catch (InvalidOperationException ex)
    {
        await WriteNdjsonAsync(
            context,
            new AgentStreamEvent("error", runtime.ResolveSessionId(request.SessionId), Error: ex.Message),
            cancellationToken);
    }
    catch (Exception ex)
    {
        await WriteNdjsonAsync(
            context,
            new AgentStreamEvent("error", runtime.ResolveSessionId(request.SessionId), Error: $"Agent request failed: {ex.Message}"),
            cancellationToken);
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

static async Task WriteNdjsonAsync(
    HttpContext context,
    AgentStreamEvent streamEvent,
    CancellationToken cancellationToken)
{
    await context.Response.WriteAsync(
        JsonSerializer.Serialize(streamEvent, NanobotWebJson.EventOptions) + "\n",
        cancellationToken);
    await context.Response.Body.FlushAsync(cancellationToken);
}

public sealed class NanobotWebRuntime
{
    private readonly string _workspace;
    private readonly AppConfig _config;
    private readonly RuntimeEventBus _eventBus = new();
    private readonly FileMemoryStore _memory;
    private readonly WebSessionStore _sessions;
    private readonly WorkspaceFileBrowser _files;
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
        _sessions = new WebSessionStore(_workspace);
        _files = new WorkspaceFileBrowser(_workspace);

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

    public IReadOnlyList<WebSessionSummary> ListSessions()
    {
        var sessions = _sessions.List();
        return sessions.Count == 0
            ? new[] { _sessions.Create() }.Select(session => new WebSessionSummary(
                session.Id,
                session.Title,
                session.CreatedAt,
                session.UpdatedAt,
                session.Messages.Count)).ToList()
            : sessions;
    }

    public WebSessionDto CreateSession(CreateSessionRequest request)
    {
        return _sessions.Create(request.Title);
    }

    public WebSessionDto? GetSession(string sessionId)
    {
        return _sessions.Get(sessionId);
    }

    public WorkspaceFileListResponse ListFiles(string? path)
    {
        return _files.List(path);
    }

    public WorkspaceFileContentResponse ReadFile(string? path)
    {
        return _files.Read(path);
    }

    public string ResolveSessionId(string? sessionId)
    {
        return _sessions.GetOrCreate(sessionId).Id;
    }

    public async Task<AgentMessageResponse> SendMessageAsync(AgentMessageRequest request, CancellationToken cancellationToken)
    {
        if (_agent is null)
        {
            throw new InvalidOperationException(_startupError ?? "Agent runtime is not ready.");
        }

        var sessionId = ResolveSessionId(request.SessionId);
        _sessions.AppendMessage(sessionId, "user", request.Message ?? "");

        var context = AgentExecutionContext.CreateRoot(_workspace) with
        {
            SessionId = sessionId
        };
        var answer = await _agent.RunAsync(request.Message ?? "", context);
        _sessions.AppendMessage(sessionId, "assistant", answer);
        return new AgentMessageResponse(sessionId, answer);
    }

    public async Task<AgentMessageResponse> SendMessageStreamingAsync(
        AgentMessageRequest request,
        Func<AgentStreamEvent, CancellationToken, Task> onStreamEventAsync,
        CancellationToken cancellationToken)
    {
        if (_agent is null)
        {
            throw new InvalidOperationException(_startupError ?? "Agent runtime is not ready.");
        }

        var sessionId = ResolveSessionId(request.SessionId);
        var message = request.Message ?? "";
        _sessions.AppendMessage(sessionId, "user", message);

        await onStreamEventAsync(new AgentStreamEvent("session", sessionId), cancellationToken);

        var context = AgentExecutionContext.CreateRoot(_workspace) with
        {
            SessionId = sessionId
        };

        var answer = await _agent.RunStreamingAsync(
            message,
            context,
            async (delta, token) =>
            {
                await onStreamEventAsync(new AgentStreamEvent("delta", sessionId, Content: delta), token);
            },
            cancellationToken);

        _sessions.AppendMessage(sessionId, "assistant", answer);
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
