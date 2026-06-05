using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Nanobot.Core.Models;

namespace Nanobot.Core.Channels;

public class DiscordChannel : HttpCallbackChannel
{
    private readonly string _botToken;
    private readonly HttpClient _httpClient;

    public DiscordChannel(
        string botToken,
        string listenPrefix,
        ChannelMessageHandler onMessage,
        HttpClient? httpClient = null)
        : base("discord", listenPrefix, onMessage)
    {
        _botToken = botToken;
        _httpClient = httpClient ?? new HttpClient();
    }

    public override async Task SendAsync(OutboundMessage message, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject
        {
            ["content"] = message.Content
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://discord.com/api/v10/channels/{Uri.EscapeDataString(message.ChatId)}/messages")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bot {_botToken}");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Discord returned {(int)response.StatusCode}: {content}");
        }
    }

    protected override Task<IReadOnlyList<InboundMessage>> ParseInboundAsync(
        HttpListenerRequest request,
        string body,
        CancellationToken cancellationToken)
    {
        var root = JsonNode.Parse(body) as JsonObject;
        if (root?["type"]?.ToString() != "MESSAGE_CREATE")
        {
            return Task.FromResult<IReadOnlyList<InboundMessage>>(Array.Empty<InboundMessage>());
        }

        var author = root["author"] as JsonObject;
        if (author?["bot"]?.GetValue<bool>() == true)
        {
            return Task.FromResult<IReadOnlyList<InboundMessage>>(Array.Empty<InboundMessage>());
        }

        var content = root["content"]?.ToString();
        var channelId = root["channel_id"]?.ToString();
        var senderId = author?["id"]?.ToString() ?? "unknown";
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(channelId))
        {
            return Task.FromResult<IReadOnlyList<InboundMessage>>(Array.Empty<InboundMessage>());
        }

        IReadOnlyList<InboundMessage> messages = new[]
        {
            new InboundMessage("discord", senderId, channelId, content)
        };
        return Task.FromResult(messages);
    }
}
