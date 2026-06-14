using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Nanobot.Core.Agent;
using Nanobot.Core.Config;
using Nanobot.Core.Events;
using Nanobot.Core.Memory;
using Nanobot.Core.Mcp;
using Nanobot.Core.Providers;
using Nanobot.Core.Sessions;
using Nanobot.Core.Tools;
using Nanobot.Core.Tools.Builtin;
using Nanobot.Core.Skills;
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
app.MapGet("/api/system/status", (NanobotWebRuntime runtime) => runtime.GetSystemStatus());

app.MapGet("/api/settings/model", (NanobotWebRuntime runtime) => runtime.GetModelSettings());

app.MapPost("/api/settings/model", (SaveModelSettingsRequest request, NanobotWebRuntime runtime) =>
{
    try
    {
        return Results.Ok(runtime.SaveModelSettings(request));
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new ApiErrorResponse(ex.Message));
    }
    catch (Exception ex)
    {
        return Results.Json(
            new ApiErrorResponse($"Model settings save failed: {ex.Message}"),
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

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

app.MapDelete("/api/sessions/{sessionId}", (string sessionId, NanobotWebRuntime runtime) =>
{
    var deleted = runtime.DeleteSession(sessionId);
    return deleted
        ? Results.Ok(new { sessionId, deleted = true })
        : Results.NotFound(new ApiErrorResponse("Session not found."));
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
        var turn = runtime.BeginChatTurn(request.SessionId, request.Message ?? "");
        var response = await runtime.SendMessageAsync(turn, cancellationToken);
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
    var turn = runtime.BeginChatTurn(request.SessionId, request.Message ?? "");

    try
    {
        await WriteNdjsonAsync(
            context,
            new AgentStreamEvent("session", turn.SessionId),
            cancellationToken);

        var response = await runtime.SendMessageStreamingAsync(
            turn,
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
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        turn.Cancel();
        return;
    }
    catch (InvalidOperationException ex)
    {
        await WriteNdjsonAsync(
            context,
            new AgentStreamEvent("error", turn.SessionId, Error: ex.Message),
            cancellationToken);
    }
    catch (Exception ex)
    {
        await WriteNdjsonAsync(
            context,
            new AgentStreamEvent("error", turn.SessionId, Error: $"Agent request failed: {ex.Message}"),
            cancellationToken);
    }
});

app.MapGet("/api/events", async (
    HttpContext context,
    NanobotWebRuntime runtime,
    CancellationToken cancellationToken) =>
{
    context.Response.Headers.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";

    // SSE replay from Last-Event-ID
    if (context.Request.Headers.TryGetValue("Last-Event-ID", out var lastEventIdValues)
        && long.TryParse(lastEventIdValues.FirstOrDefault(), out var sinceSequence)
        && sinceSequence > 0)
    {
        await foreach (var item in runtime.ReplayEventsAsync(sinceSequence, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested) break;
            await WriteSseEventAsync(context, item, cancellationToken);
        }
    }

    await foreach (var runtimeEvent in runtime.ListenAsync(cancellationToken))
    {
        if (cancellationToken.IsCancellationRequested) break;
        await WriteSseEventAsync(context, runtimeEvent, cancellationToken);
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

static async Task WriteSseEventAsync(
    HttpContext context,
    RuntimeEvent runtimeEvent,
    CancellationToken cancellationToken)
{
    await context.Response.WriteAsync($"id: {runtimeEvent.Sequence}\n", cancellationToken);
    await context.Response.WriteAsync("event: runtime\n", cancellationToken);
    await context.Response.WriteAsync(
        $"data: {JsonSerializer.Serialize(runtimeEvent, NanobotWebJson.EventOptions)}\n\n",
        cancellationToken);
    await context.Response.Body.FlushAsync(cancellationToken);
}

public sealed class NanobotWebRuntime
{
    private readonly object _reloadLock = new();
    private readonly string _nanoDir;
    private readonly string _configFile;
    private readonly string _modelsFile;
    private readonly string _secretsFile;
    private readonly string _workspace;
    private readonly RuntimeEventBus _eventBus;
    private readonly FileMemoryStore _memory;
    private readonly WebSessionStore _sessions;
    private readonly WorkspaceFileBrowser _files;
    private readonly JsonlSessionStore _jsonlStore;
    private readonly ModelSettingsStore _modelSettingsStore;
    private AppConfig _config = new();
    private ILLMProvider? _provider;
    private Agent? _agent;
    private string? _startupError;
    private string? _startupWarning;

    public NanobotWebRuntime()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _nanoDir = Path.Combine(home, ".nanobot");
        _configFile = Path.Combine(_nanoDir, "config.json");
        _modelsFile = Path.Combine(_nanoDir, "models.json");
        _secretsFile = Path.Combine(_nanoDir, "secrets.json");
        _workspace = Path.Combine(_nanoDir, "workspace");
        Directory.CreateDirectory(_nanoDir);
        Directory.CreateDirectory(_workspace);

        _eventBus = new RuntimeEventBus(JsonlSessionStore.ReadMaxSequence(_nanoDir));
        _modelSettingsStore = new ModelSettingsStore(_configFile, _modelsFile, _secretsFile);
        _memory = new FileMemoryStore(_workspace);
        _sessions = new WebSessionStore(_workspace);
        _files = new WorkspaceFileBrowser(_workspace);
        _jsonlStore = new JsonlSessionStore(_nanoDir, _eventBus);
        ReloadRuntime();
    }

    public RuntimeStatusResponse GetStatus()
    {
        return new RuntimeStatusResponse(
            Workspace: _workspace,
            Model: ResolveRuntimeModelLabel(),
            NongEnabled: _config.Tools.Nong.Enabled,
            MemoryPreview: Truncate(_memory.GetContext(), 1600),
            Ready: _agent is not null,
            Error: _startupError,
            Warning: _startupWarning
        );
    }

    public SystemStatusResponse GetSystemStatus()
    {
        var nong = SystemStatusProbe.ProbeNongStatus();
        var toolkit = ProbeToolkitStatus();
        return new SystemStatusResponse(GetStatus(), nong, toolkit);
    }

    static ToolkitStatusResponse? ProbeToolkitStatus()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var skillsDir = Path.Combine(home, ".nanobot", "workspace", "skills");
        if (!Directory.Exists(skillsDir)) return null;
        var skillNames = Directory.GetDirectories(skillsDir)
            .Select(Path.GetFileName)
            .Where(n => n != null)
            .Select(n => n!)
            .OrderBy(n => n)
            .ToList();
        return skillNames.Count > 0
            ? new ToolkitStatusResponse(true, skillNames.Count, skillNames)
            : null;
    }

    public ModelSettingsResponse GetModelSettings()
    {
        return _modelSettingsStore.Get(_config);
    }

    public SaveModelSettingsResponse SaveModelSettings(SaveModelSettingsRequest request)
    {
        _modelSettingsStore.Save(request, _config);

        ReloadRuntime();
        return new SaveModelSettingsResponse(
            Message: _agent is null ? "配置已保存，但运行时仍未就绪。" : "模型配置已保存并重载。",
            Status: GetStatus(),
            Settings: GetModelSettings()
        );
    }

    private void ReloadRuntime()
    {
        lock (_reloadLock)
        {
            _provider = null;
            _agent = null;
            _startupError = null;
            _startupWarning = null;

            LoadRuntimeCore();
        }
    }

    private void LoadRuntimeCore()
    {
        try
        {
            _config = File.Exists(_configFile) ? ConfigLoader.Load(_configFile) : new AppConfig();
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

            var nongHook = new NongConfirmationHook();
            _agent = new Agent(_provider, registry, _memory, _eventBus, hooks: new IAgentHook[] { nongHook });
        }
        catch (Exception ex)
        {
            _startupError = FormatStartupError(ex);
        }
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

    public bool DeleteSession(string sessionId)
    {
        return _sessions.Delete(sessionId);
    }

    public WorkspaceFileListResponse ListFiles(string? path)
    {
        return _files.List(path);
    }

    public WorkspaceFileContentResponse ReadFile(string? path)
    {
        return _files.Read(path);
    }

    public WebChatTurn BeginChatTurn(string? sessionId, string message)
    {
        return WebChatTurn.Begin(_sessions, sessionId, message);
    }

    public async Task<AgentMessageResponse> SendMessageAsync(WebChatTurn turn, CancellationToken cancellationToken)
    {
        if (_agent is null)
        {
            var message = _startupError ?? "Agent runtime is not ready.";
            turn.Fail(message);
            throw new InvalidOperationException(message);
        }

        var context = AgentExecutionContext.CreateRoot(_workspace) with
        {
            SessionId = turn.SessionId
        };

        try
        {
            var answer = await _agent.RunAsync(turn.UserMessage, context);
            return turn.Complete(answer);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            turn.Cancel();
            throw;
        }
        catch (Exception ex)
        {
            turn.Fail(ex.Message);
            throw;
        }
    }

    public async Task<AgentMessageResponse> SendMessageStreamingAsync(
        WebChatTurn turn,
        Func<AgentStreamEvent, CancellationToken, Task> onStreamEventAsync,
        CancellationToken cancellationToken)
    {
        if (_agent is null)
        {
            var message = _startupError ?? "Agent runtime is not ready.";
            turn.Fail(message);
            throw new InvalidOperationException(message);
        }

        var context = AgentExecutionContext.CreateRoot(_workspace) with
        {
            SessionId = turn.SessionId
        };

        try
        {
            var answer = await _agent.RunStreamingAsync(
                turn.UserMessage,
                context,
                async (delta, token) =>
                {
                    turn.AppendAssistantDelta(delta);
                    await onStreamEventAsync(new AgentStreamEvent("delta", turn.SessionId, Content: delta), token);
                },
                cancellationToken,
                onReasoningDeltaAsync: async (reasoning, token) =>
                {
                    turn.AppendAssistantReasoning(reasoning);
                    await onStreamEventAsync(new AgentStreamEvent("reasoning", turn.SessionId, Reasoning: reasoning), token);
                });

            return turn.Complete(answer);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            turn.Cancel();
            throw;
        }
        catch (Exception ex)
        {
            turn.Fail(ex.Message);
            throw;
        }
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

    public async IAsyncEnumerable<RuntimeEvent> ReplayEventsAsync(
        long sinceSequence,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in _jsonlStore.ReadItemsSinceSequenceAsync(sinceSequence))
        {
            if (cancellationToken.IsCancellationRequested) break;

            var sessionId = string.IsNullOrWhiteSpace(item.SessionId)
                ? item.ThreadId
                : item.SessionId;
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                continue;
            }

            yield return new RuntimeEvent
            {
                Type = item.EventType ?? MapItemType(item.Type),
                EventId = string.IsNullOrWhiteSpace(item.EventId) ? item.Id : item.EventId,
                Sequence = item.Sequence,
                RunId = string.IsNullOrWhiteSpace(item.RunId) ? item.ToolCallId ?? item.Id : item.RunId,
                SessionId = sessionId,
                ThreadId = item.ThreadId,
                Content = item.Content,
                ToolName = item.ToolName,
                ToolCallId = item.ToolCallId,
                ErrorMessage = item.ErrorMessage,
                Payload = item.Usage,
                Timestamp = item.Timestamp
            };
        }
    }

    private static RuntimeEventType MapItemType(SessionItemType itemType)
    {
        return itemType switch
        {
            SessionItemType.ToolCall => RuntimeEventType.ToolStarted,
            SessionItemType.ToolResult => RuntimeEventType.ToolCompleted,
            SessionItemType.Reasoning => RuntimeEventType.ReasoningCompleted,
            SessionItemType.AssistantMessage => RuntimeEventType.ContentCompleted,
            SessionItemType.UserMessage => RuntimeEventType.RunStarted,
            SessionItemType.Usage => RuntimeEventType.UsageUpdated,
            _ => RuntimeEventType.RunCompleted
        };
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
            var nongTool = new NongTool(_workspace, _config.Tools.Nong);
            registry.Register(nongTool);
            if (_config.Tools.Nong.DetailedTools)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var tools = await NongTool.DiscoverOpenAiToolsAsync(
                            _config.Tools.Nong.Command,
                            workspace: _workspace);
                        foreach (var t in tools)
                        {
                            registry.Register(new NongDiscoveredToolWrapper(
                                nongTool, t.Name, t.Args.ToArray(), t.Description, t.Parameters));
                        }
                        Console.WriteLine($"[nong] Discovered {tools.Count} command tools");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[nong] Command discovery skipped: {ex.Message}");
                    }
                });
            }
        }

        // Skill tools (2-phase progressive disclosure)
        var skillLoader = new SkillLoader();
        registry.Register(new GetSkillCatalogTool(skillLoader, _workspace));
        registry.Register(new LoadSkillTool(skillLoader, _workspace));
        registry.Register(new LoadSkillReferenceTool(skillLoader, _workspace));

        // Plugin manager
        var pluginManager = new PluginManager(_workspace);
        registry.Register(new PluginInstallTool(pluginManager));
        registry.Register(new PluginListTool(pluginManager));

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

    private ProviderSettings? ResolveProvider(string providerId)
    {
        return _config.Providers.TryGetValue(providerId, out var provider)
            ? provider
            : null;
    }

    private string ResolveRuntimeModelLabel()
    {
        if (!string.IsNullOrWhiteSpace(_config.Agents.Defaults.Model))
        {
            return _config.Agents.Defaults.Model!;
        }

        if (!string.IsNullOrWhiteSpace(_config.Agents.Defaults.Provider))
        {
            var providerId = _config.Agents.Defaults.Provider!;
            return $"{providerId}::{ResolveProviderModel(providerId, ResolveProvider(providerId))}";
        }

        return _provider?.GetDefaultModel() ?? "Not configured";
    }

    private static string ResolveProviderModel(string providerId, ProviderSettings? provider)
    {
        if (provider is null)
        {
            return DefaultProviderCatalog.GetDefaultModel(providerId);
        }

        if (!string.IsNullOrWhiteSpace(provider.DefaultModel))
        {
            return provider.DefaultModel!;
        }

        var model = provider.Models.FirstOrDefault(item => item.Enabled && !string.IsNullOrWhiteSpace(item.Id));
        if (model is not null)
        {
            return model.ApiModelId ?? model.Id!;
        }

        return "Not configured";
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
