using System.Net;
using System.Text.Json.Nodes;
using Nanobot.Core.Models;
using Nanobot.Core.Providers;

namespace Nanobot.Tests;

public class ProviderTests
{
    [Fact]
    public void ProviderRegistry_CanRegisterResolveAndOverwriteProvider()
    {
        var registry = new ProviderRegistry();
        var first = new FakeProvider("first");
        var second = new FakeProvider("second");

        registry.Register("main", first);
        registry.Register("main", second);

        Assert.Same(second, registry.Resolve("main"));
        Assert.Single(registry.GetRegistrations());
    }

    [Fact]
    public void ProviderRegistry_ThrowsClearErrorForUnknownProvider()
    {
        var registry = new ProviderRegistry();

        var error = Assert.Throws<InvalidOperationException>(() => registry.Resolve("missing"));

        Assert.Contains("missing", error.Message);
    }

    [Fact]
    public async Task FallbackProvider_StopsAtFirstSuccessfulProvider()
    {
        var first = new FakeProvider("first", new LLMResponse("ok"));
        var second = new FakeProvider("second", new LLMResponse("unused"));
        var fallback = new FallbackLLMProvider(new[]
        {
            Registration("first", first),
            Registration("second", second)
        });

        var response = await fallback.ChatAsync(new List<Message> { new("user", "hello") });

        Assert.Equal("ok", response.Content);
        Assert.Equal(1, first.CallCount);
        Assert.Equal(0, second.CallCount);
    }

    [Fact]
    public async Task FallbackProvider_UsesNextProviderOnErrorResponse()
    {
        var first = new FakeProvider("first", new LLMResponse("bad") { FinishReason = "error" });
        var second = new FakeProvider("second", new LLMResponse("ok"));
        var fallback = new FallbackLLMProvider(new[]
        {
            Registration("first", first),
            Registration("second", second)
        });

        var response = await fallback.ChatAsync(new List<Message> { new("user", "hello") });

        Assert.Equal("ok", response.Content);
        Assert.Equal(1, first.CallCount);
        Assert.Equal(1, second.CallCount);
    }

    [Fact]
    public async Task FallbackProvider_ReturnsFailureChainWhenAllProvidersFail()
    {
        var first = new FakeProvider("first", new LLMResponse("bad") { FinishReason = "error" });
        var second = new FakeProvider("second", exception: new InvalidOperationException("boom"));
        var fallback = new FallbackLLMProvider(new[]
        {
            Registration("first", first),
            Registration("second", second)
        });

        var response = await fallback.ChatAsync(new List<Message> { new("user", "hello") });

        Assert.Equal("error", response.FinishReason);
        Assert.Contains("first", response.Content);
        Assert.Contains("second", response.Content);
        Assert.Contains("boom", response.Content);
    }

    [Fact]
    public void OpenAIProvider_RemainsCompatibleWrapper()
    {
        var provider = new OpenAIProvider("test-key", defaultModel: "model-a");

        Assert.IsAssignableFrom<OpenAICompatibleProvider>(provider);
        Assert.Equal("model-a", provider.GetDefaultModel());
    }

    private static ProviderRegistration Registration(string name, ILLMProvider provider)
    {
        return new ProviderRegistration(
            new ProviderDescriptor(name, "fake", provider.GetDefaultModel(), ProviderCapabilities.Chat),
            provider
        );
    }

    private sealed class FakeProvider : ILLMProvider
    {
        private readonly LLMResponse? _response;
        private readonly Exception? _exception;
        private readonly string _model;

        public FakeProvider(string model, LLMResponse? response = null, Exception? exception = null)
        {
            _model = model;
            _response = response;
            _exception = exception;
        }

        public int CallCount { get; private set; }

        public Task<LLMResponse> ChatAsync(
            List<Message> messages,
            List<JsonNode>? tools = null,
            string? model = null,
            int maxTokens = 4096,
            double temperature = 0.7)
        {
            CallCount++;
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_response ?? new LLMResponse(_model));
        }

        public string GetDefaultModel() => _model;
    }
}

public class HttpProviderTests
{
    [Fact]
    public async Task AnthropicProvider_BuildsMessagesRequestAndParsesToolUseResponse()
    {
        var handler = new RecordingHandler((request, body) =>
        {
            var json = JsonNode.Parse(body)!;
            Assert.Equal("claude-test", json["model"]?.ToString());
            Assert.Equal("system prompt", json["system"]?.ToString());
            Assert.Equal("lookup", json["tools"]?[0]?["name"]?.ToString());
            Assert.Equal("tool_result", json["messages"]?[1]?["content"]?[0]?["type"]?.ToString());
            Assert.Equal("test-key", request.Headers.GetValues("x-api-key").Single());

            return JsonResponse("""
                {
                  "stop_reason": "tool_use",
                  "content": [
                    {"type": "text", "text": "I will call a tool."},
                    {"type": "tool_use", "id": "tool-1", "name": "lookup", "input": {"query": "nano"}}
                  ],
                  "usage": {"input_tokens": 3, "output_tokens": 4}
                }
                """);
        });
        var provider = new AnthropicProvider("test-key", "claude-test", new HttpClient(handler));

        var response = await provider.ChatAsync(
            new List<Message>
            {
                new("system", "system prompt"),
                new("user", "hello"),
                new("tool", "tool output") { ToolCallId = "tool-previous" }
            },
            ToolDefinitions()
        );

        Assert.Equal("tool_use", response.FinishReason);
        Assert.Equal("I will call a tool.", response.Content);
        Assert.Single(response.ToolCalls);
        Assert.Equal("lookup", response.ToolCalls[0].Name);
        Assert.Equal("nano", response.ToolCalls[0].Arguments?["query"]?.ToString());
        Assert.Equal(7, response.Usage?.TotalTokens);
    }

    [Fact]
    public async Task AzureOpenAIProvider_BuildsChatRequestAndParsesToolCalls()
    {
        var handler = new RecordingHandler((request, body) =>
        {
            Assert.Contains("/openai/deployments/deploy-a/chat/completions", request.RequestUri!.ToString());
            Assert.Contains("api-version=2024-10-21", request.RequestUri!.ToString());
            Assert.Equal("azure-key", request.Headers.GetValues("api-key").Single());

            var json = JsonNode.Parse(body)!;
            Assert.Equal("lookup", json["tools"]?[0]?["function"]?["name"]?.ToString());
            Assert.Equal("user", json["messages"]?[0]?["role"]?.ToString());

            return JsonResponse("""
                {
                  "choices": [
                    {
                      "finish_reason": "tool_calls",
                      "message": {
                        "content": null,
                        "tool_calls": [
                          {
                            "id": "call-1",
                            "type": "function",
                            "function": {
                              "name": "lookup",
                              "arguments": "{\"query\":\"nano\"}"
                            }
                          }
                        ]
                      }
                    }
                  ],
                  "usage": {"prompt_tokens": 2, "completion_tokens": 5, "total_tokens": 7}
                }
                """);
        });
        var provider = new AzureOpenAIProvider("https://example.openai.azure.com", "azure-key", "deploy-a", httpClient: new HttpClient(handler));

        var response = await provider.ChatAsync(new List<Message> { new("user", "hello") }, ToolDefinitions());

        Assert.Equal("tool_calls", response.FinishReason);
        Assert.Single(response.ToolCalls);
        Assert.Equal("lookup", response.ToolCalls[0].Name);
        Assert.Equal("nano", response.ToolCalls[0].Arguments?["query"]?.ToString());
        Assert.Equal(7, response.Usage?.TotalTokens);
    }

    [Fact]
    public async Task AnthropicProvider_StreamsTextAndToolCalls()
    {
        var handler = new RecordingHandler((request, body) =>
        {
            var json = JsonNode.Parse(body)!;
            Assert.True(json["stream"]?.GetValue<bool>());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    event: content_block_delta
                    data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"hel"}}

                    event: content_block_delta
                    data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"lo"}}

                    event: content_block_start
                    data: {"type":"content_block_start","index":1,"content_block":{"type":"tool_use","id":"tool-1","name":"lookup","input":{}}}

                    event: content_block_delta
                    data: {"type":"content_block_delta","index":1,"delta":{"type":"input_json_delta","partial_json":"{\"query\":\"nano\"}"}}

                    event: message_delta
                    data: {"type":"message_delta","delta":{"stop_reason":"tool_use"}}

                    """)
            };
        });
        var provider = new AnthropicProvider("test-key", "claude-test", new HttpClient(handler));
        var deltas = new List<string>();
        LLMResponse? final = null;

        await foreach (var chunk in provider.ChatStreamAsync(new List<Message> { new("user", "hello") }))
        {
            if (chunk.ContentDelta is not null)
            {
                deltas.Add(chunk.ContentDelta);
            }

            final = chunk.FinalResponse ?? final;
        }

        Assert.Equal(new[] { "hel", "lo" }, deltas);
        Assert.NotNull(final);
        Assert.Equal("hello", final!.Content);
        Assert.Equal("tool_use", final.FinishReason);
        Assert.Single(final.ToolCalls);
        Assert.Equal("lookup", final.ToolCalls[0].Name);
        Assert.Equal("nano", final.ToolCalls[0].Arguments?["query"]?.ToString());
    }

    [Fact]
    public async Task AzureOpenAIProvider_StreamsTextAndToolCalls()
    {
        var handler = new RecordingHandler((request, body) =>
        {
            var json = JsonNode.Parse(body)!;
            Assert.True(json["stream"]?.GetValue<bool>());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    data: {"choices":[{"delta":{"content":"hel"}}]}

                    data: {"choices":[{"delta":{"content":"lo"}}]}

                    data: {"choices":[{"delta":{"tool_calls":[{"index":0,"id":"call-1","function":{"name":"lookup","arguments":"{\"query\":"}}]}}]}

                    data: {"choices":[{"delta":{"tool_calls":[{"index":0,"function":{"arguments":"\"nano\"}"}}]},"finish_reason":"tool_calls"}]}

                    data: [DONE]

                    """)
            };
        });
        var provider = new AzureOpenAIProvider("https://example.openai.azure.com", "azure-key", "deploy-a", httpClient: new HttpClient(handler));
        var deltas = new List<string>();
        LLMResponse? final = null;

        await foreach (var chunk in provider.ChatStreamAsync(new List<Message> { new("user", "hello") }))
        {
            if (chunk.ContentDelta is not null)
            {
                deltas.Add(chunk.ContentDelta);
            }

            final = chunk.FinalResponse ?? final;
        }

        Assert.Equal(new[] { "hel", "lo" }, deltas);
        Assert.NotNull(final);
        Assert.Equal("hello", final!.Content);
        Assert.Equal("tool_calls", final.FinishReason);
        Assert.Single(final.ToolCalls);
        Assert.Equal("lookup", final.ToolCalls[0].Name);
        Assert.Equal("nano", final.ToolCalls[0].Arguments?["query"]?.ToString());
    }

    private static List<JsonNode> ToolDefinitions()
    {
        return new List<JsonNode>
        {
            JsonNode.Parse("""
                {
                  "type": "function",
                  "function": {
                    "name": "lookup",
                    "description": "Lookup a value",
                    "parameters": {
                      "type": "object",
                      "properties": {
                        "query": {"type": "string"}
                      }
                    }
                  }
                }
                """)!
        };
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

public class LLMUsageTests
{
    [Fact]
    public void CacheHitRate_CalculatesCorrectly()
    {
        var usage = LLMUsage.FromDeepSeekResponse(
            promptTokens: 1000,
            cacheHitTokens: 800,
            cacheMissTokens: 200,
            completionTokens: 300,
            reasoningTokens: 150);

        Assert.Equal(1000, usage.InputTokens);
        Assert.Equal(800, usage.CachedInputTokens);
        Assert.Equal(200, usage.UncachedInputTokens);
        Assert.Equal(300, usage.OutputTokens);
        Assert.Equal(150, usage.ReasoningTokens);
        Assert.Equal(1300, usage.TotalTokens);
        Assert.Equal(0.8, usage.CacheHitRate, 3);
    }

    [Fact]
    public void CacheHitRate_ZeroInput_ReturnsZero()
    {
        var usage = LLMUsage.Basic(0, 100);
        Assert.Equal(0, usage.CacheHitRate);
    }

    [Fact]
    public void FromLegacy_ConvertsDictionaryCorrectly()
    {
        var legacy = new Dictionary<string, int>
        {
            ["prompt_tokens"] = 500,
            ["completion_tokens"] = 200,
            ["total_tokens"] = 700,
            ["prompt_cache_hit_tokens"] = 400,
            ["prompt_cache_miss_tokens"] = 100,
            ["reasoning_tokens"] = 50
        };

        var usage = LLMUsage.FromLegacy(legacy);

        Assert.Equal(500, usage.InputTokens);
        Assert.Equal(400, usage.CachedInputTokens);
        Assert.Equal(100, usage.UncachedInputTokens);
        Assert.Equal(200, usage.OutputTokens);
        Assert.Equal(50, usage.ReasoningTokens);
        Assert.Equal(700, usage.TotalTokens);
    }
}

public class DeepSeekV4ProviderTests
{
    [Fact]
    public void ParseDeepSeekUsage_ExtractsAllFields()
    {
        var usageJson = new JsonObject
        {
            ["prompt_tokens"] = 100000,
            ["completion_tokens"] = 5000,
            ["total_tokens"] = 105000,
            ["prompt_cache_hit_tokens"] = 95000,
            ["prompt_cache_miss_tokens"] = 5000,
            ["completion_tokens_details"] = new JsonObject
            {
                ["reasoning_tokens"] = 2000
            }
        };

        var usage = DeepSeekV4Provider.ParseDeepSeekUsage(usageJson);

        Assert.Equal(100000, usage.InputTokens);
        Assert.Equal(95000, usage.CachedInputTokens);
        Assert.Equal(5000, usage.UncachedInputTokens);
        Assert.Equal(5000, usage.OutputTokens);
        Assert.Equal(2000, usage.ReasoningTokens);
        Assert.Equal(0.95, usage.CacheHitRate, 3);
    }

    [Fact]
    public void ParseDeepSeekUsage_NullUsage_ReturnsZeroed()
    {
        var usage = DeepSeekV4Provider.ParseDeepSeekUsage(null);
        Assert.Equal(0, usage.TotalTokens);
    }

    [Fact]
    public void DeepSeekV4Models_DetectsKnownModels()
    {
        Assert.True(DeepSeekV4Models.IsDeepSeekV4("deepseek-v4-flash"));
        Assert.True(DeepSeekV4Models.IsDeepSeekV4("deepseek-v4-pro"));
        Assert.True(DeepSeekV4Models.IsDeepSeekV4("deepseek-v4-pro-guan"));
        Assert.False(DeepSeekV4Models.IsDeepSeekV4("gpt-4o"));
        Assert.False(DeepSeekV4Models.IsDeepSeekV4(null));
    }

    [Fact]
    public void DeepSeekV4Profile_HasExpectedValues()
    {
        var profile = DeepSeekV4Models.GetProfile("deepseek-v4-flash");

        Assert.Equal(1_000_000, profile.ContextWindow);
        Assert.Equal(384_000, profile.MaxOutput);
        Assert.True(profile.SupportsStreaming);
        Assert.True(profile.SupportsTools);
        Assert.True(profile.SupportsReasoning);
        Assert.True(profile.SupportsInterleavedThinking);
        Assert.True(profile.SupportsPromptCacheMetrics);
        Assert.Equal("high", profile.DefaultReasoningEffort);
    }

    [Fact]
    public async Task DeepSeekV4Provider_StreamsReasoningContent()
    {
        var handler = new FakeDeepSeekHandler(ss =>
        {
            ss.WriteLine("data: {\"choices\":[{\"delta\":{\"reasoning_content\":\"Thinking...\"}}]}");
            ss.WriteLine();
            ss.WriteLine("data: {\"choices\":[{\"delta\":{\"content\":\"Hello\"}}]}");
            ss.WriteLine();
            ss.WriteLine("data: {\"choices\":[{\"delta\":{\"content\":\" world\"}}]}");
            ss.WriteLine();
            ss.WriteLine("data: {\"choices\":[{\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":5,\"total_tokens\":15}}");
            ss.WriteLine();
            ss.WriteLine("data: [DONE]");
            ss.WriteLine();
        });

        var provider = new DeepSeekV4Provider("test-key", httpClient: new HttpClient(handler));
        var deltas = new List<string>();
        var reasoning = new List<string>();
        LLMResponse? final = null;

        await foreach (var chunk in provider.ChatStreamAsync(new List<Message> { new("user", "hi") }))
        {
            if (chunk.ReasoningDelta is not null) reasoning.Add(chunk.ReasoningDelta);
            if (chunk.ContentDelta is not null) deltas.Add(chunk.ContentDelta);
            final = chunk.FinalResponse ?? final;
        }

        Assert.Equal(new[] { "Thinking..." }, reasoning);
        Assert.Equal(new[] { "Hello", " world" }, deltas);
        Assert.NotNull(final);
        Assert.Equal("Hello world", final!.Content);
        Assert.Equal("deepseek", final.Provider);
    }

    [Fact]
    public async Task DeepSeekV4Provider_ParsesStreamingToolCalls()
    {
        var handler = new FakeDeepSeekHandler(ss =>
        {
            ss.WriteLine("""data: {"choices":[{"delta":{"content":"I'll look it up."}}]}""");
            ss.WriteLine();
            ss.WriteLine("""data: {"choices":[{"delta":{"tool_calls":[{"index":0,"id":"call-1","function":{"name":"read_file","arguments":"{\"path\": \"/tmp/test.txt\"}"}}]}}]}""");
            ss.WriteLine();
            ss.WriteLine("""data: {"choices":[{"finish_reason":"tool_calls"}]}""");
            ss.WriteLine();
            ss.WriteLine("""data: [DONE]""");
            ss.WriteLine();
        });

        var provider = new DeepSeekV4Provider("test-key", httpClient: new HttpClient(handler));
        LLMResponse? final = null;

        await foreach (var chunk in provider.ChatStreamAsync(new List<Message> { new("user", "read file") }))
        {
            final = chunk.FinalResponse ?? final;
        }

        Assert.NotNull(final);
        Assert.Equal("tool_calls", final!.FinishReason);
        Assert.Single(final.ToolCalls);
        Assert.Equal("read_file", final.ToolCalls[0].Name);
        Assert.Equal("call-1", final.ToolCalls[0].Id);
    }

    [Fact]
    public async Task DeepSeekV4Provider_StreamsCacheUsage()
    {
        var handler = new FakeDeepSeekHandler(ss =>
        {
            ss.WriteLine("data: {\"choices\":[{\"delta\":{\"content\":\"Done.\"}}]}");
            ss.WriteLine();
            ss.WriteLine("data: {\"choices\":[{\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":1000,\"completion_tokens\":50,\"total_tokens\":1050,\"prompt_cache_hit_tokens\":980,\"prompt_cache_miss_tokens\":20,\"completion_tokens_details\":{\"reasoning_tokens\":30}}}");
            ss.WriteLine();
            ss.WriteLine("data: [DONE]");
            ss.WriteLine();
        });

        var provider = new DeepSeekV4Provider("test-key", httpClient: new HttpClient(handler));
        LLMResponse? final = null;

        await foreach (var chunk in provider.ChatStreamAsync(new List<Message> { new("user", "hi") }))
        {
            final = chunk.FinalResponse ?? final;
        }

        Assert.NotNull(final);
        Assert.NotNull(final!.Usage);
        Assert.Equal(1000, final.Usage!.InputTokens);
        Assert.Equal(980, final.Usage.CachedInputTokens);
        Assert.Equal(20, final.Usage.UncachedInputTokens);
        Assert.Equal(50, final.Usage.OutputTokens);
        Assert.Equal(30, final.Usage.ReasoningTokens);
        Assert.Equal(0.98, final.Usage.CacheHitRate, 3);
    }

    private sealed class FakeDeepSeekHandler : HttpMessageHandler
    {
        private readonly Action<StreamWriter> _writeResponse;

        public FakeDeepSeekHandler(Action<StreamWriter> writeResponse)
        {
            _writeResponse = writeResponse;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            _writeResponse(writer);
            await writer.FlushAsync(cancellationToken);
            ms.Position = 0;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(ms)
            };
        }
    }
}
