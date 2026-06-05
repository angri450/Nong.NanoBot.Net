using System.Text.Json;
using System.Text.Json.Nodes;
using Nanobot.Core.Events;

namespace Nanobot.Core.Gateway;

public static class WebSocketGatewayProtocol
{
    public static WebSocketAgentRequest ParseRequest(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new WebSocketAgentRequest { Message = string.Empty };
        }

        try
        {
            var json = JsonNode.Parse(payload) as JsonObject;
            var message = json?["message"]?.ToString();
            if (message is not null)
            {
                return new WebSocketAgentRequest
                {
                    Message = message,
                    SessionId = json?["sessionId"]?.ToString()
                };
            }
        }
        catch (JsonException)
        {
            // Plain text messages are accepted for simple clients.
        }

        return new WebSocketAgentRequest { Message = payload };
    }

    public static string FormatResponse(string content)
    {
        return new JsonObject
        {
            ["type"] = "response",
            ["content"] = content
        }.ToJsonString();
    }

    public static string FormatDelta(string content)
    {
        return new JsonObject
        {
            ["type"] = "delta",
            ["content"] = content
        }.ToJsonString();
    }

    public static string FormatError(string message)
    {
        return new JsonObject
        {
            ["type"] = "error",
            ["message"] = message
        }.ToJsonString();
    }

    public static string FormatEvent(RuntimeEvent runtimeEvent)
    {
        return new JsonObject
        {
            ["type"] = "event",
            ["eventType"] = runtimeEvent.Type.ToString(),
            ["runId"] = runtimeEvent.RunId,
            ["sessionId"] = runtimeEvent.SessionId,
            ["toolName"] = runtimeEvent.ToolName,
            ["toolCallId"] = runtimeEvent.ToolCallId,
            ["content"] = runtimeEvent.Content,
            ["errorMessage"] = runtimeEvent.ErrorMessage,
            ["timestamp"] = runtimeEvent.Timestamp.ToString("O")
        }.ToJsonString();
    }
}
