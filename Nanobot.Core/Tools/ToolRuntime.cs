using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Nanobot.Core.Tools;

public class ToolRuntime
{
    private readonly ToolRegistry _registry;
    private readonly ToolPermissionPolicy _policy;
    private readonly List<ToolAuditRecord> _auditLog = new();
    private readonly string _storeDir;
    private const int DefaultMaxOutputChars = 15000;

    public ToolRuntime(ToolRegistry registry, ToolPermissionPolicy policy, string? storeDir = null)
    {
        _registry = registry;
        _policy = policy;
        _storeDir = storeDir ?? Path.Combine(Path.GetTempPath(), "nanobot_tool_results");
    }

    public IReadOnlyList<ToolAuditRecord> AuditLog => _auditLog.AsReadOnly();

    public async Task<ToolExecutionResult> ExecuteAsync(
        string toolName,
        JsonNode? arguments,
        string sessionId,
        string runId)
    {
        var toolCallId = Guid.NewGuid().ToString("N");
        var startedAt = DateTimeOffset.UtcNow;
        var sw = Stopwatch.StartNew();

        if (!_policy.IsAllowed(toolName))
        {
            var reason = ToolPermissionPolicy.GetDenyReason(toolName, _policy.Mode);
            sw.Stop();
            _auditLog.Add(new ToolAuditRecord
            {
                ToolName = toolName,
                ToolCallId = toolCallId,
                SessionId = sessionId,
                RunId = runId,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Success = false,
                ErrorCode = "permission_denied",
                ErrorMessage = reason
            });

            return ToolExecutionResult.Error(toolName, "permission_denied", reason);
        }

        try
        {
            var result = await _registry.ExecuteWithResultAsync(toolName, arguments);
            sw.Stop();

            var output = result.Content ?? "";
            var handle = ToolResultHandle.Create(toolName, output, DefaultMaxOutputChars, _storeDir);
            var truncatedOutput = handle.IsTruncated
                ? output[..handle.TruncatedLength] + $"\n... (Result truncated from {output.Length} chars. Use handle {handle.HandleId} to retrieve full output)"
                : output;

            _auditLog.Add(new ToolAuditRecord
            {
                ToolName = toolName,
                ToolCallId = toolCallId,
                SessionId = sessionId,
                RunId = runId,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Success = result.Success,
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorMessage,
                OutputLength = output.Length,
                WasTruncated = handle.IsTruncated
            });

            return result.Success
                ? ToolExecutionResult.Ok(toolName, truncatedOutput)
                : ToolExecutionResult.Error(toolName, result.ErrorCode ?? "", result.ErrorMessage ?? "", result.Exception);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _auditLog.Add(new ToolAuditRecord
            {
                ToolName = toolName,
                ToolCallId = toolCallId,
                SessionId = sessionId,
                RunId = runId,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Success = false,
                ErrorCode = "tool_exception",
                ErrorMessage = ex.Message
            });

            return ToolExecutionResult.Error(toolName, "tool_exception", ex.Message, ex);
        }
    }
}
