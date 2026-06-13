namespace Nanobot.Web;

public sealed record AgentMessageRequest(string? SessionId, string? Message);

public sealed record AgentMessageResponse(string SessionId, string Answer);

public sealed record AgentStreamEvent(
    string Type,
    string SessionId,
    string? Content = null,
    string? Answer = null,
    string? Error = null,
    string? Reasoning = null,
    string? ToolName = null,
    string? ToolCallId = null,
    double? CacheHitRate = null,
    int? InputTokens = null,
    int? OutputTokens = null,
    int? CachedTokens = null);

public sealed record ApiErrorResponse(string Error);

public sealed record RuntimeStatusResponse(
    string Workspace,
    string Model,
    bool NongEnabled,
    string MemoryPreview,
    bool Ready,
    string? Error,
    string? Warning,
    double? CacheHitRate = null,
    int? ContextTokens = null,
    int? ContextWindow = null,
    string? ThinkingMode = null);

public sealed record ModelSettingsResponse(
    string ProviderId,
    string ApiBase,
    string Model,
    bool HasApiKey,
    string ApiKeyPreview,
    string KeySource,
    string ConfigPath,
    string? ThinkingMode = null);

public sealed record SaveModelSettingsRequest(
    string? ProviderId,
    string? ApiKey,
    string? ApiBase,
    string? Model,
    bool ClearApiKey);

public sealed record SaveModelSettingsResponse(
    string Message,
    RuntimeStatusResponse Status,
    ModelSettingsResponse Settings);

public sealed record CreateSessionRequest(string? Title);

public sealed record WebSessionSummary(
    string Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int MessageCount,
    bool Resumed = false);

public sealed record WebSessionDto(
    string Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<WebChatMessageDto> Messages);

public sealed record WebChatMessageDto(
    string Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    string? Reasoning = null);

public sealed record WorkspaceFileListResponse(
    string Path,
    IReadOnlyList<WorkspaceFileEntry> Entries);

public sealed record WorkspaceFileEntry(
    string Name,
    string Path,
    string Kind,
    long? Size,
    DateTimeOffset ModifiedAt);

public sealed record WorkspaceFileContentResponse(
    string Path,
    string Name,
    string Content,
    long Size,
    bool Truncated,
    DateTimeOffset ModifiedAt);

public sealed record SystemStatusResponse(
    RuntimeStatusResponse Runtime,
    NongStatusResponse? Nong,
    ToolkitStatusResponse? Toolkit);

public sealed record NongStatusResponse(
    bool Installed,
    string? Version,
    int CommandCount,
    IReadOnlyList<string> AvailableRoots,
    IReadOnlyList<ExternalToolStatus>? ExternalTools = null,
    OcrModelStatus? OcrModels = null);

public sealed record ExternalToolStatus(
    string Name,
    string PackageId,
    bool Installed,
    string? Version);

public sealed record OcrModelStatus(
    bool V6Available,
    string? V6Size,
    string? V6CachePath,
    bool V5Available);

public sealed record ToolkitStatusResponse(
    bool Installed,
    int SkillCount,
    IReadOnlyList<string> SkillNames);
