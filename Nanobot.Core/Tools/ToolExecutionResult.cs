using System.Text.Json;

namespace Nanobot.Core.Tools;

public record ToolExecutionResult
{
    public required string ToolName { get; init; }

    public required bool Success { get; init; }

    public string? Content { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public Exception? Exception { get; init; }

    public static ToolExecutionResult Ok(string toolName, string? content)
    {
        return new ToolExecutionResult
        {
            ToolName = toolName,
            Success = true,
            Content = content ?? "Tool execution returned no result."
        };
    }

    public static ToolExecutionResult Error(string toolName, string code, string message, Exception? exception = null)
    {
        return new ToolExecutionResult
        {
            ToolName = toolName,
            Success = false,
            ErrorCode = code,
            ErrorMessage = message,
            Exception = exception,
            Content = FormatError(toolName, code, message)
        };
    }

    public static string FormatError(string toolName, string code, string message)
    {
        return JsonSerializer.Serialize(new
        {
            error = new
            {
                tool = toolName,
                code,
                message
            }
        });
    }
}
