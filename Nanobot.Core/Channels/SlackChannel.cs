using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Nanobot.Core.Models;

namespace Nanobot.Core.Channels;

public class SlackChannel : HttpCallbackChannel
{
    private readonly string _botToken;
    private readonly HttpClient _httpClient;

    public SlackChannel(
        string botToken,
        string listenPrefix,
        ChannelMessageHandler onMessage,
        HttpClient? httpClient = null)
        : base("slack", listenPrefix, onMessage)
    {
        _botToken = botToken;
        _httpClient = httpClient ?? new HttpClient();
    }

    public override async Task SendAsync(OutboundMessage message, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject
        {
            ["channel"] = message.ChatId,
            ["text"] = message.Content
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/chat.postMessage")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_botToken}");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Slack returned {(int)response.StatusCode}: {content}");
        }
    }

    protected override Task<IReadOnlyList<InboundMessage>> ParseInboundAsync(
        HttpListenerRequest request,
        string body,
        CancellationToken cancellationToken)
    {
        var root = JsonNode.Parse(body) as JsonObject;
        var eventNode = root?["event"] as JsonObject;
        if (eventNode is null
            || eventNode["type"]?.ToString() != "message"
            || eventNode["bot_id"] is not null)
        {
            return Task.FromResult<IReadOnlyList<InboundMessage>>(Array.Empty<InboundMessage>());
        }

        var text = eventNode["text"]?.ToString();
        var channel = eventNode["channel"]?.ToString();
        var user = eventNode["user"]?.ToString() ?? "unknown";
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(channel))
        {
            return Task.FromResult<IReadOnlyList<InboundMessage>>(Array.Empty<InboundMessage>());
        }

        IReadOnlyList<InboundMessage> messages = new[]
        {
            new InboundMessage("slack", user, channel, text)
        };
        return Task.FromResult(messages);
    }

    protected override async Task<bool> TryHandleControlRequestAsync(
        HttpListenerContext context,
        string body,
        CancellationToken cancellationToken)
    {
        var root = JsonNode.Parse(body) as JsonObject;
        if (root?["type"]?.ToString() != "url_verification")
        {
            return false;
        }

        await RespondAsync(context, HttpStatusCode.OK, root["challenge"]?.ToString() ?? "", cancellationToken);
        return true;
    }
}
