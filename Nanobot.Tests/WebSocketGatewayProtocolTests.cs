using System.Text.Json.Nodes;
using Nanobot.Core.Events;
using Nanobot.Core.Gateway;

namespace Nanobot.Tests;

public class WebSocketGatewayProtocolTests
{
    [Fact]
    public void ParseRequest_AcceptsPlainText()
    {
        var request = WebSocketGatewayProtocol.ParseRequest("hello");

        Assert.Equal("hello", request.Message);
        Assert.Null(request.SessionId);
    }

    [Fact]
    public void ParseRequest_AcceptsJsonPayload()
    {
        var request = WebSocketGatewayProtocol.ParseRequest("{\"message\":\"hello\",\"sessionId\":\"s1\"}");

        Assert.Equal("hello", request.Message);
        Assert.Equal("s1", request.SessionId);
    }

    [Fact]
    public void FormatEvent_IncludesRuntimeEventFields()
    {
        var payload = WebSocketGatewayProtocol.FormatEvent(new RuntimeEvent
        {
            Type = RuntimeEventType.ToolStarted,
            RunId = "run-1",
            SessionId = "s1",
            ToolName = "lookup",
            ToolCallId = "call-1"
        });

        var json = JsonNode.Parse(payload)!;

        Assert.Equal("event", json["type"]?.ToString());
        Assert.Equal("ToolStarted", json["eventType"]?.ToString());
        Assert.Equal("lookup", json["toolName"]?.ToString());
    }

    [Fact]
    public void FormatDelta_IncludesDeltaContent()
    {
        var payload = WebSocketGatewayProtocol.FormatDelta("hello");

        var json = JsonNode.Parse(payload)!;

        Assert.Equal("delta", json["type"]?.ToString());
        Assert.Equal("hello", json["content"]?.ToString());
    }

    [Fact]
    public void WebSocketGatewayAuth_AcceptsBearerOrQueryToken()
    {
        Assert.True(WebSocketGatewayAuth.IsAuthorized("secret", "Bearer secret", null));
        Assert.True(WebSocketGatewayAuth.IsAuthorized("secret", null, "secret"));
        Assert.True(WebSocketGatewayAuth.IsAuthorized(null, null, null));
    }

    [Fact]
    public void WebSocketGatewayAuth_RejectsMissingOrWrongToken()
    {
        Assert.False(WebSocketGatewayAuth.IsAuthorized("secret", null, null));
        Assert.False(WebSocketGatewayAuth.IsAuthorized("secret", "Bearer wrong", null));
        Assert.False(WebSocketGatewayAuth.IsAuthorized("secret", "Basic secret", null));
    }
}
