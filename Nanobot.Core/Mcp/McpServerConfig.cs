namespace Nanobot.Core.Mcp;

public record McpServerConfig
{
    public required string Command { get; init; }

    public IReadOnlyList<string> Arguments { get; init; } = Array.Empty<string>();

    public string? WorkingDirectory { get; init; }
}
