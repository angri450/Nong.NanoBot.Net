namespace Nanobot.Core.Tools;

public record ToolAuditRecord
{
    public string ToolName { get; init; } = "";
    public string? ToolCallId { get; init; }
    public string SessionId { get; init; } = "";
    public string RunId { get; init; } = "";
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; init; }
    public int DurationMs { get; init; }
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public int OutputLength { get; init; }
    public bool WasTruncated { get; init; }
}
