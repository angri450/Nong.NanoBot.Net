namespace Nanobot.Core.Mcp;

public record McpServerConfig
{
    public string? Type { get; init; }

    public string? Transport { get; init; }

    public string? Command { get; init; }

    public IReadOnlyList<string> Arguments { get; init; } = Array.Empty<string>();

    public string? WorkingDirectory { get; init; }

    public string? Url { get; init; }

    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
}
