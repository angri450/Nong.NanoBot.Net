namespace Nanobot.Core.Agent;

public record AgentExecutionContext
{
    public const string DefaultSessionId = "default";

    public string SessionId { get; init; } = DefaultSessionId;

    public string Workspace { get; init; } = Environment.CurrentDirectory;

    public bool IsEphemeral { get; init; }

    public string? ParentRunId { get; init; }

    public IReadOnlyCollection<string>? AllowedTools { get; init; }

    public static AgentExecutionContext CreateRoot(string workspace)
    {
        return new AgentExecutionContext
        {
            SessionId = DefaultSessionId,
            Workspace = string.IsNullOrWhiteSpace(workspace) ? Environment.CurrentDirectory : workspace
        };
    }

    public bool IsToolAllowed(string toolName)
    {
        return AllowedTools is null || AllowedTools.Contains(toolName, StringComparer.Ordinal);
    }
}
