using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Nanobot.Core.Agent;
using Nanobot.Core.Auth;
using Nanobot.Core.CodingPlan;
using Nanobot.Core.Config;
using Nanobot.Core.Events;
using Nanobot.Core.Memory;
using Nanobot.Core.Mcp;
using Nanobot.Core.Providers;
using Nanobot.Core.Sessions;
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
    context.Response.Headers.CacheControl = "no-cache";

    // SSE replay from Last-Event-ID
    if (context.Request.Headers.TryGetValue("Last-Event-ID", out var lastEventIdValues)
        && long.TryParse(lastEventIdValues.FirstOrDefault(), out var sinceSequence)
        && sinceSequence > 0)
    {
        await foreach (var item in runtime.ReplayEventsAsync(sinceSequence, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested) break;
            await context.Response.WriteAsync($"id: {item.EventId}\n", cancellationToken);
            await context.Response.WriteAsync($"event: runtime\n", cancellationToken);
            await context.Response.WriteAsync(
                $"data: {JsonSerializer.Serialize(item, NanobotWebJson.EventOptions)}\n\n",
                cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }

    await foreach (var runtimeEvent in runtime.ListenAsync(cancellationToken))
    {
        if (cancellationToken.IsCancellationRequested) break;
        await context.Response.WriteAsync($"id: {runtimeEvent.Sequence}\n", cancellationToken);
        await context.Response.WriteAsync($"event: runtime\n", cancellationToken);
        await context.Response.WriteAsync(
            $"data: {JsonSerializer.Serialize(runtimeEvent, NanobotWebJson.EventOptions)}\n\n",
            cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
});

app.MapGet("/favicon.ico", () => Results.NoContent());

// GitCode Auth endpoints
app.MapGet("/api/gitcode/auth/status", (NanobotWebRuntime runtime) => runtime.GetGitCodeAuthStatus());

app.MapPost("/api/gitcode/auth/login/start", async (NanobotWebRuntime runtime) =>
{
    try
    {
        var state = await runtime.StartGitCodeLoginAsync();
        return Results.Ok(new
        {
            loginId = state.LoginId,
            loginUrl = state.LoginUrl,
            status = state.Status.ToString().ToLowerInvariant()
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new ApiErrorResponse($"Login start failed: {ex.Message}"),
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.MapPost("/api/gitcode/auth/login/{loginId}/poll", async (string loginId, NanobotWebRuntime runtime) =>
{
    var result = await runtime.PollGitCodeLoginAsync(loginId);
    if (result is null)
    {
        return Results.NotFound(new ApiErrorResponse("Login session not found."));
    }

    return Results.Ok(new
    {
        loginId = result.LoginId,
        status = result.Status.ToString().ToLowerInvariant()
    });
});

app.MapDelete("/api/gitcode/auth/login/{loginId}", (string loginId, NanobotWebRuntime runtime) =>
{
    runtime.CancelGitCodeLogin(loginId);
    return Results.Ok(new { loginId, cancelled = true });
});

app.MapPost("/api/gitcode/auth/logout", (NanobotWebRuntime runtime) =>
{
    runtime.LogoutGitCode();
    return Results.Ok(new { loggedOut = true });
});

// GitCode CodingPlan endpoints
app.MapPost("/api/gitcode/codingplan/setup", async (NanobotWebRuntime runtime) =>
{
    try
    {
        var result = await runtime.SetupCodingPlanAsync();
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Json(new ApiErrorResponse($"CodingPlan setup failed: {ex.Message}"),
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.MapGet("/api/gitcode/codingplan/models", async (NanobotWebRuntime runtime) =>
{
    var models = await runtime.GetCodingPlanModelsAsync();
    return Results.Ok(models);
});

app.MapGet("/api/gitcode/codingplan/status", async (NanobotWebRuntime runtime) =>
{
    var status = await runtime.GetCodingPlanStatusAsync();
    return Results.Ok(status);
});

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
    private const string DmxProviderId = "dmx";
    private const string DmxDefaultApiBase = "https://www.dmxapi.cn/v1/";
    private const string DmxDefaultModel = "deepseek-v4-pro-guan";

    private readonly object _reloadLock = new();
    private readonly string _nanoDir;
    private readonly string _configFile;
    private readonly string _workspace;
    private readonly RuntimeEventBus _eventBus = new();
    private readonly FileMemoryStore _memory;
    private readonly WebSessionStore _sessions;
    private readonly WorkspaceFileBrowser _files;
    private readonly JsonlSessionStore _jsonlStore;
    private readonly GitCodeAuthStore _gitCodeAuthStore;
    private readonly GitCodeAuthService _gitCodeAuthService;
    private readonly GitCodeCodingPlanService _gitCodeCodingPlanService;
    private readonly Dictionary<string, GitCodeLoginState> _gitCodeLoginStates = new();
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
        _workspace = Path.Combine(_nanoDir, "workspace");
        Directory.CreateDirectory(_nanoDir);
        Directory.CreateDirectory(_workspace);

        _memory = new FileMemoryStore(_workspace);
        _sessions = new WebSessionStore(_workspace);
        _files = new WorkspaceFileBrowser(_workspace);
        _jsonlStore = new JsonlSessionStore(_nanoDir, _eventBus);
        _gitCodeAuthStore = new GitCodeAuthStore(_nanoDir);
        _gitCodeAuthService = new GitCodeAuthService(_gitCodeAuthStore);
        _gitCodeCodingPlanService = new GitCodeCodingPlanService(
            new GitCodeCodingPlanClient(() => _gitCodeAuthStore.GetValidAccessToken()),
            _gitCodeAuthStore,
            _configFile);
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

    public ModelSettingsResponse GetModelSettings()
    {
        var providerId = DmxProviderId;
        var provider = ResolveProvider(providerId);
        var envKey = Environment.GetEnvironmentVariable("DMX_API_KEY");
        var configuredKey = provider?.ApiKey;
        var effectiveKey = !string.IsNullOrWhiteSpace(envKey) ? envKey : configuredKey;
        var keySource = !string.IsNullOrWhiteSpace(envKey)
            ? "environment"
            : !string.IsNullOrWhiteSpace(configuredKey)
                ? "config"
                : "none";

        return new ModelSettingsResponse(
            ProviderId: providerId,
            ApiBase: provider?.ApiBase ?? provider?.BaseUrl ?? DmxDefaultApiBase,
            Model: ResolveProviderModel(providerId, provider),
            HasApiKey: !string.IsNullOrWhiteSpace(effectiveKey),
            ApiKeyPreview: MaskSecret(effectiveKey),
            KeySource: keySource,
            ConfigPath: _configFile
        );
    }

    public SaveModelSettingsResponse SaveModelSettings(SaveModelSettingsRequest request)
    {
        var providerId = string.IsNullOrWhiteSpace(request.ProviderId)
            ? DmxProviderId
            : request.ProviderId.Trim();
        if (!providerId.Equals(DmxProviderId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("当前 WebUI 设置面板先支持 DMX provider。");
        }

        var apiBase = string.IsNullOrWhiteSpace(request.ApiBase)
            ? DmxDefaultApiBase
            : request.ApiBase.Trim();
        var model = string.IsNullOrWhiteSpace(request.Model)
            ? DmxDefaultModel
            : request.Model.Trim();
        if (!Uri.TryCreate(apiBase, UriKind.Absolute, out var apiBaseUri)
            || (apiBaseUri.Scheme != Uri.UriSchemeHttp && apiBaseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("API 地址必须是 http 或 https URL。");
        }

        Directory.CreateDirectory(_nanoDir);
        var configJson = LoadConfigJson();
        var providers = EnsureObject(configJson, "providers");
        var provider = EnsureObject(providers, providerId);
        provider["kind"] = "openai-compatible";
        provider["apiBase"] = apiBase;
        provider["defaultModel"] = model;
        provider["models"] = new JsonArray
        {
            new JsonObject
            {
                ["id"] = model,
                ["apiModelId"] = model,
                ["supportsStreaming"] = true,
                ["supportsTools"] = true,
                ["providerModelFamily"] = DeepSeekV4Models.IsDeepSeekV4(model) ? "deepseek-v4" : null,
                ["supportsReasoning"] = DeepSeekV4Models.IsDeepSeekV4(model),
                ["supportsPromptCacheMetrics"] = DeepSeekV4Models.IsDeepSeekV4(model),
                ["contextWindow"] = DeepSeekV4Models.IsDeepSeekV4(model) ? 1_000_000 : null,
                ["maxOutputTokens"] = DeepSeekV4Models.IsDeepSeekV4(model) ? 384_000 : null
            }
        };

        if (request.ClearApiKey)
        {
            provider["apiKey"] = "";
        }
        else if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            provider["apiKey"] = request.ApiKey.Trim();
        }
        else if (!provider.ContainsKey("apiKey"))
        {
            provider["apiKey"] = "";
        }

        var agents = EnsureObject(configJson, "agents");
        var defaults = EnsureObject(agents, "defaults");
        defaults["provider"] = providerId;
        defaults["model"] = $"{providerId}::{model}";
        defaults["fallbackModels"] = new JsonArray($"{providerId}::{model}");

        var streaming = EnsureObject(configJson, "streaming");
        streaming["enabled"] = true;

        File.WriteAllText(
            _configFile,
            configJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

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

            _agent = new Agent(_provider, registry, _memory, _eventBus);
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
            cancellationToken,
            onReasoningDeltaAsync: async (reasoning, token) =>
            {
                await onStreamEventAsync(new AgentStreamEvent("reasoning", sessionId, Reasoning: reasoning), token);
            });

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

    public async IAsyncEnumerable<RuntimeEvent> ReplayEventsAsync(
        long sinceSequence,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Read from JSONL store for the active session
        // Since events are session-scoped, we find the most recent session
        var sessions = _sessions.List();
        var sessionId = sessions.FirstOrDefault()?.Id;
        if (sessionId is null) yield break;

        await foreach (var item in _jsonlStore.ReadItemsAsync(sessionId, sessionId))
        {
            if (cancellationToken.IsCancellationRequested) break;

            yield return new RuntimeEvent
            {
                Type = MapItemType(item.Type),
                RunId = item.ToolCallId ?? item.Id,
                SessionId = sessionId,
                ThreadId = item.ThreadId,
                Content = item.Content,
                ToolName = item.ToolName,
                ToolCallId = item.ToolCallId,
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

    public object GetGitCodeAuthStatus()
    {
        var data = _gitCodeAuthService.GetStatus();
        return new
        {
            loggedIn = data.IsLoggedIn,
            login = data.User?.Login,
            name = data.User?.Name,
            avatarUrl = data.User?.AvatarUrl,
            tokenValid = data.Token?.IsValid ?? false,
            expiresAt = data.Token?.ExpiresAt,
            needsRefresh = data.NeedsRefresh
        };
    }

    public async Task<GitCodeLoginState> StartGitCodeLoginAsync()
    {
        var state = await _gitCodeAuthService.StartLoginAsync();
        _gitCodeLoginStates[state.LoginId] = state;
        return state;
    }

    public async Task<GitCodeLoginState?> PollGitCodeLoginAsync(string loginId)
    {
        if (!_gitCodeLoginStates.TryGetValue(loginId, out var state))
        {
            return null;
        }

        var updated = await _gitCodeAuthService.PollLoginAsync(state);
        if (updated.Status == GitCodeLoginStatus.Authorized)
        {
            await _gitCodeAuthService.FinishLoginAsync(updated);
            _gitCodeLoginStates.Remove(loginId);
        }
        else if (updated.Status is GitCodeLoginStatus.Expired or GitCodeLoginStatus.Failed)
        {
            _gitCodeLoginStates.Remove(loginId);
        }

        _gitCodeLoginStates[loginId] = updated;
        return updated;
    }

    public void CancelGitCodeLogin(string loginId)
    {
        _gitCodeLoginStates.Remove(loginId);
    }

    public void LogoutGitCode()
    {
        _gitCodeAuthService.Logout();
        _gitCodeLoginStates.Clear();
    }

    public async Task<CodingPlanSetupResult> SetupCodingPlanAsync()
    {
        var result = await _gitCodeCodingPlanService.SetupAsync();
        if (result.Success)
        {
            ReloadRuntime();
        }

        return result;
    }

    public async Task<List<GitCodeModelEntry>> GetCodingPlanModelsAsync()
    {
        var client = new GitCodeCodingPlanClient(() => _gitCodeAuthStore.GetValidAccessToken());
        var models = new List<GitCodeModelEntry>();
        foreach (var planType in new[] { PlanType.Max, PlanType.Pro, PlanType.Lite })
        {
            try
            {
                models.AddRange(await client.ListModelsAsync(planType));
            }
            catch { }
        }

        return models.GroupBy(m => m.DisplayModelName).Select(g => g.First()).ToList();
    }

    public async Task<GitCodeCodingPlanStatus> GetCodingPlanStatusAsync()
    {
        var client = new GitCodeCodingPlanClient(() => _gitCodeAuthStore.GetValidAccessToken());
        return await client.GetStatusAsync();
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

    private string ResolveSettingsProviderId()
    {
        return DmxProviderId;
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
            return providerId.Equals(DmxProviderId, StringComparison.OrdinalIgnoreCase)
                ? DmxDefaultModel
                : "Not configured";
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

        return providerId.Equals(DmxProviderId, StringComparison.OrdinalIgnoreCase)
            ? DmxDefaultModel
            : "Not configured";
    }

    private static string MaskSecret(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return "";
        }

        var value = secret.Trim();
        if (value.Length <= 10)
        {
            return "已配置";
        }

        return $"{value[..6]}...{value[^4..]}";
    }

    private JsonObject LoadConfigJson()
    {
        if (!File.Exists(_configFile))
        {
            return new JsonObject();
        }

        var text = File.ReadAllText(_configFile);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidOperationException("config.json 根节点必须是 JSON object。");
    }

    private static JsonObject EnsureObject(JsonObject parent, string propertyName)
    {
        if (parent[propertyName] is JsonObject existing)
        {
            return existing;
        }

        var created = new JsonObject();
        parent[propertyName] = created;
        return created;
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
