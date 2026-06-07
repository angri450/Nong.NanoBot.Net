namespace Nanobot.Web;

public sealed record AgentMessageRequest(string? SessionId, string? Message);

public sealed record AgentMessageResponse(string SessionId, string Answer);

public sealed record AgentStreamEvent(
    string Type,
    string SessionId,
    string? Content = null,
    string? Answer = null,
    string? Error = null);

public sealed record ApiErrorResponse(string Error);

public sealed record RuntimeStatusResponse(
    string Workspace,
    string Model,
    bool NongEnabled,
    string MemoryPreview,
    bool Ready,
    string? Error,
    string? Warning);

public sealed record CreateSessionRequest(string? Title);

public sealed record WebSessionSummary(
    string Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int MessageCount);

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
    DateTimeOffset CreatedAt);

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
