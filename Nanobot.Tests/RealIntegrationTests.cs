using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using Nanobot.Core.Agent;
using Nanobot.Core.Events;
using Nanobot.Core.Gateway;
using Nanobot.Core.Memory;
using Nanobot.Core.Models;
using Nanobot.Core.Providers;
using Nanobot.Core.Tools;

namespace Nanobot.Tests;

public class RealIntegrationTests
{
    [Fact]
    public async Task OpenAICompatibleProvider_CanCallRealModel_WhenEnabled()
    {
        if (!RunIntegrationTests())
        {
            return;
        }

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Assert.False(string.IsNullOrWhiteSpace(apiKey), "OPENAI_API_KEY is required when NANOBOT_RUN_INTEGRATION_TESTS=1.");

        var provider = new OpenAICompatibleProvider(
            apiKey!,
            Environment.GetEnvironmentVariable("OPENAI_API_BASE"),
            ResolveOpenAIModel()
        );

        var response = await provider.ChatAsync(
            new List<Message> { new("user", "Reply with exactly: ok") },
            maxTokens: 16,
            temperature: 0
        );

        Assert.NotEqual("error", response.FinishReason);
        Assert.Contains("ok", response.Content ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenAICompatibleProvider_CanStreamRealModel_WhenEnabled()
    {
        if (!RunIntegrationTests())
        {
            return;
        }

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Assert.False(string.IsNullOrWhiteSpace(apiKey), "OPENAI_API_KEY is required when NANOBOT_RUN_INTEGRATION_TESTS=1.");

        var provider = new OpenAICompatibleProvider(
            apiKey!,
            Environment.GetEnvironmentVariable("OPENAI_API_BASE"),
            ResolveOpenAIModel()
        );
        var content = new StringBuilder();
        LLMResponse? finalResponse = null;

        await foreach (var chunk in provider.ChatStreamAsync(
            new List<Message> { new("user", "Reply with exactly: ok") },
            maxTokens: 16,
            temperature: 0))
        {
            if (!string.IsNullOrEmpty(chunk.ContentDelta))
            {
                content.Append(chunk.ContentDelta);
            }

            finalResponse = chunk.FinalResponse ?? finalResponse;
        }

        Assert.NotNull(finalResponse);
        Assert.NotEqual("error", finalResponse.FinishReason);
        Assert.Contains("ok", content.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WebSocketGateway_RequiresTokenAndReturnsResponse_WhenEnabled()
    {
        if (!RunIntegrationTests())
        {
            return;
        }

        var port = GetFreePort();
        var prefix = $"http://127.0.0.1:{port}/ws/";
        var workspace = CreateWorkspace();
        var agent = new Agent(new StaticProvider("gateway-ok"), new ToolRegistry(), new StaticMemory(), new RuntimeEventBus());
        var eventBus = new RuntimeEventBus();
        var gateway = new WebSocketAgentGateway(agent, eventBus, workspace, prefix, "secret", streamingEnabled: false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var gatewayTask = gateway.StartAsync(cts.Token);

        await Task.Delay(300, cts.Token);

        using var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("Authorization", "Bearer secret");
        await socket.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/ws/"), cts.Token);
        await SendTextAsync(socket, "{\"message\":\"hello\",\"sessionId\":\"integration\"}", cts.Token);

        string? responsePayload = null;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var payload = await ReceiveTextAsync(socket, cts.Token);
            var json = JsonNode.Parse(payload)!;
            if (json["type"]?.ToString() == "response")
            {
                responsePayload = payload;
                break;
            }
        }

        Assert.NotNull(responsePayload);
        Assert.Equal("gateway-ok", JsonNode.Parse(responsePayload!)?["content"]?.ToString());

        cts.Cancel();
        try
        {
            await gatewayTask;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static bool RunIntegrationTests()
    {
        var value = Environment.GetEnvironmentVariable("NANOBOT_RUN_INTEGRATION_TESTS");
        return value is "1" or "true" or "TRUE" or "yes" or "YES";
    }

    private static string ResolveOpenAIModel()
    {
        var raw = Environment.GetEnvironmentVariable("OPENAI_MODEL");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "gpt-4o";
        }

        var separator = raw.IndexOf("::", StringComparison.Ordinal);
        return separator < 0 ? raw : raw[(separator + 2)..];
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string CreateWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "nanobot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task SendTextAsync(ClientWebSocket socket, string payload, CancellationToken cancellationToken)
    {
        await socket.SendAsync(
            Encoding.UTF8.GetBytes(payload),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken
        );
    }

    private static async Task<string> ReceiveTextAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);
            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }

    private sealed class StaticProvider : ILLMProvider
    {
        private readonly string _content;

        public StaticProvider(string content)
        {
            _content = content;
        }

        public Task<LLMResponse> ChatAsync(
            List<Message> messages,
            List<JsonNode>? tools = null,
            string? model = null,
            int maxTokens = 4096,
            double temperature = 0.7)
        {
            return Task.FromResult(new LLMResponse(_content));
        }

        public string GetDefaultModel() => "static";
    }

    private sealed class StaticMemory : IMemory
    {
        public string GetContext() => string.Empty;
    }
}
