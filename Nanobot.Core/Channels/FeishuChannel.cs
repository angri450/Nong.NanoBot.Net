using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Nanobot.Core.Models;

namespace Nanobot.Core.Channels;

public class FeishuChannel : HttpCallbackChannel
{
    private readonly string _appId;
    private readonly string _appSecret;
    private readonly HttpClient _httpClient;
    private string? _tenantAccessToken;
    private DateTimeOffset _tokenExpiresAt;

    public FeishuChannel(
        string appId,
        string appSecret,
        string listenPrefix,
        ChannelMessageHandler onMessage,
        HttpClient? httpClient = null)
        : base("feishu", listenPrefix, onMessage)
    {
        _appId = appId;
        _appSecret = appSecret;
        _httpClient = httpClient ?? new HttpClient();
    }

    public override async Task SendAsync(OutboundMessage message, CancellationToken cancellationToken = default)
    {
        var token = await GetTenantAccessTokenAsync(cancellationToken);
        var body = new JsonObject
        {
            ["receive_id"] = message.ChatId,
            ["msg_type"] = "text",
            ["content"] = new JsonObject
            {
                ["text"] = message.Content
            }.ToJsonString()
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://open.feishu.cn/open-apis/im/v1/messages?receive_id_type=chat_id")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Feishu returned {(int)response.StatusCode}: {content}");
        }
    }

    protected override Task<IReadOnlyList<InboundMessage>> ParseInboundAsync(
        HttpListenerRequest request,
        string body,
        CancellationToken cancellationToken)
    {
        var root = JsonNode.Parse(body) as JsonObject;
        var eventNode = root?["event"] as JsonObject;
        var message = eventNode?["message"] as JsonObject;
        if (message is null)
        {
            return Task.FromResult<IReadOnlyList<InboundMessage>>(Array.Empty<InboundMessage>());
        }

        var chatId = message["chat_id"]?.ToString();
        var senderId = eventNode?["sender"]?["sender_id"]?["open_id"]?.ToString() ?? "unknown";
        var contentJson = message["content"]?.ToString();
        var text = string.Empty;
        if (!string.IsNullOrWhiteSpace(contentJson))
        {
            try
            {
                text = JsonNode.Parse(contentJson)?["text"]?.ToString() ?? "";
            }
            catch
            {
                text = contentJson;
            }
        }

        if (string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult<IReadOnlyList<InboundMessage>>(Array.Empty<InboundMessage>());
        }

        IReadOnlyList<InboundMessage> messages = new[]
        {
            new InboundMessage("feishu", senderId, chatId, text)
        };
        return Task.FromResult(messages);
    }

    protected override async Task<bool> TryHandleControlRequestAsync(
        HttpListenerContext context,
        string body,
        CancellationToken cancellationToken)
    {
        var root = JsonNode.Parse(body) as JsonObject;
        if (root?["challenge"] is null)
        {
            return false;
        }

        await RespondAsync(context, HttpStatusCode.OK, new JsonObject
        {
            ["challenge"] = root["challenge"]?.ToString()
        }.ToJsonString(), cancellationToken);
        return true;
    }

    private async Task<string> GetTenantAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_tenantAccessToken)
            && _tokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return _tenantAccessToken;
        }

        var body = new JsonObject
        {
            ["app_id"] = _appId,
            ["app_secret"] = _appSecret
        };
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Feishu token request returned {(int)response.StatusCode}: {content}");
        }

        var root = JsonNode.Parse(content);
        _tenantAccessToken = root?["tenant_access_token"]?.ToString()
            ?? throw new InvalidOperationException("Feishu token response did not include tenant_access_token.");
        var expiresIn = root["expire"]?.GetValue<int>() ?? 7200;
        _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        return _tenantAccessToken;
    }
}
