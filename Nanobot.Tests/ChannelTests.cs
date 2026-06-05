using System.Net;
using System.Text.Json.Nodes;
using Nanobot.Core.Channels;
using Nanobot.Core.Config;
using Nanobot.Core.Models;

namespace Nanobot.Tests;

public class ChannelTests
{
    [Fact]
    public void ChannelFactory_CreatesConfiguredChannels()
    {
        var slack = ChannelFactory.Create("slack", new ChannelSettings
        {
            Enabled = true,
            Token = "xoxb-token",
            Endpoint = "http://localhost:9000/slack/"
        }, (_, _) => Task.FromResult<OutboundMessage?>(null));

        Assert.IsType<SlackChannel>(slack);
    }

    [Fact]
    public async Task SlackChannel_SendsChatPostMessage()
    {
        var handler = new RecordingHandler((request, body) =>
        {
            Assert.Equal("https://slack.com/api/chat.postMessage", request.RequestUri!.ToString());
            Assert.Equal("Bearer xoxb-token", request.Headers.GetValues("Authorization").Single());
            var json = JsonNode.Parse(body)!;
            Assert.Equal("C123", json["channel"]?.ToString());
            Assert.Equal("hello", json["text"]?.ToString());
            return JsonResponse("""{"ok":true}""");
        });
        var channel = new SlackChannel("xoxb-token", "http://localhost:9000/slack/", (_, _) => Task.FromResult<OutboundMessage?>(null), new HttpClient(handler));

        await channel.SendAsync(new OutboundMessage("slack", "C123", "hello"));
    }

    [Fact]
    public async Task DiscordChannel_SendsMessage()
    {
        var handler = new RecordingHandler((request, body) =>
        {
            Assert.Equal("https://discord.com/api/v10/channels/123/messages", request.RequestUri!.ToString());
            Assert.Equal("Bot bot-token", request.Headers.GetValues("Authorization").Single());
            Assert.Equal("hello", JsonNode.Parse(body)?["content"]?.ToString());
            return JsonResponse("""{"id":"m1"}""");
        });
        var channel = new DiscordChannel("bot-token", "http://localhost:9000/discord/", (_, _) => Task.FromResult<OutboundMessage?>(null), new HttpClient(handler));

        await channel.SendAsync(new OutboundMessage("discord", "123", "hello"));
    }

    [Fact]
    public async Task FeishuChannel_GetsTenantTokenAndSendsMessage()
    {
        var handler = new RecordingHandler((request, body) =>
        {
            if (request.RequestUri!.AbsolutePath.Contains("tenant_access_token"))
            {
                var tokenBody = JsonNode.Parse(body)!;
                Assert.Equal("app-id", tokenBody["app_id"]?.ToString());
                return JsonResponse("""{"tenant_access_token":"tenant-token","expire":7200}""");
            }

            Assert.Equal("Bearer tenant-token", request.Headers.GetValues("Authorization").Single());
            var json = JsonNode.Parse(body)!;
            Assert.Equal("oc-chat", json["receive_id"]?.ToString());
            Assert.Equal("text", json["msg_type"]?.ToString());
            Assert.Contains("hello", json["content"]?.ToString());
            return JsonResponse("""{"code":0}""");
        });
        var channel = new FeishuChannel("app-id", "secret", "http://localhost:9000/feishu/", (_, _) => Task.FromResult<OutboundMessage?>(null), new HttpClient(handler));

        await channel.SendAsync(new OutboundMessage("feishu", "oc-chat", "hello"));
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string, HttpResponseMessage> _handler;

        public RecordingHandler(Func<HttpRequestMessage, string, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return _handler(request, body);
        }
    }
}
