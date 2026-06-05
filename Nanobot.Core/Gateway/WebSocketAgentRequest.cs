namespace Nanobot.Core.Gateway;

public record WebSocketAgentRequest
{
    public required string Message { get; init; }

    public string? SessionId { get; init; }
}
