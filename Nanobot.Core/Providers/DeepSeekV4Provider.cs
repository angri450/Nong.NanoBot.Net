using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nanobot.Core.Models;

namespace Nanobot.Core.Providers;

public class DeepSeekV4Provider : IStreamingLLMProvider
{
    private const string DefaultBaseUrl = "https://api.deepseek.com";

    private readonly string _apiKey;
    private readonly string _apiBase;
    private readonly HttpClient _httpClient;
    private readonly DeepSeekV4Options _options;
    private readonly DeepSeekV4Profile _profile;

    public DeepSeekV4Provider(
        string apiKey,
        string? apiBase = null,
        DeepSeekV4Options? options = null,
        HttpClient? httpClient = null)
    {
        _apiKey = apiKey;
        _apiBase = (apiBase ?? DefaultBaseUrl).TrimEnd('/');
        _httpClient = httpClient ?? new HttpClient();
        _options = options ?? new DeepSeekV4Options();
        _profile = DeepSeekV4Models.GetProfile(_options.Model ?? DeepSeekV4Models.Flash);
    }

    public string GetDefaultModel() => _options.Model ?? DeepSeekV4Models.Flash;

    public DeepSeekV4Profile Profile => _profile;

    public async Task<LLMResponse> ChatAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7)
    {
        var body = BuildRequestBody(messages, tools, model ?? GetDefaultModel(), maxTokens, temperature, stream: false);
        return await SendRequestAsync(body);
    }

    public async IAsyncEnumerable<LLMStreamChunk> ChatStreamAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var body = BuildRequestBody(messages, tools, model ?? GetDefaultModel(), maxTokens, temperature, stream: true);
        var request = CreateRequest(body);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            yield return LLMStreamChunk.Delta($"Error: DeepSeek returned {(int)response.StatusCode}: {errorBody}");
            yield return LLMStreamChunk.Final(new LLMResponse($"Error: DeepSeek returned {(int)response.StatusCode}: {errorBody}")
            {
                FinishReason = "error"
            });
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var parser = new DeepSeekStreamParser();

        await foreach (var data in ReadSseDataAsync(reader, cancellationToken))
        {
            if (data == "[DONE]")
            {
                break;
            }

            foreach (var chunk in parser.Apply(data))
            {
                yield return chunk;
            }
        }

        yield return LLMStreamChunk.Final(parser.BuildResponse(GetDefaultModel()));
    }

    private async Task<LLMResponse> SendRequestAsync(JsonObject body)
    {
        var request = CreateRequest(body);
        try
        {
            using var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return new LLMResponse($"Error: DeepSeek returned {(int)response.StatusCode}: {content}")
                {
                    FinishReason = "error"
                };
            }

            return ParseNonStreamingResponse(content);
        }
        catch (Exception ex)
        {
            return new LLMResponse($"Error: {ex.Message}") { FinishReason = "error" };
        }
    }

    private JsonObject BuildRequestBody(
        List<Message> messages,
        List<JsonNode>? tools,
        string model,
        int maxTokens,
        double temperature,
        bool stream)
    {
        var body = new JsonObject
        {
            ["model"] = model,
            ["messages"] = new JsonArray(messages.Select(MessageToJson).ToArray()),
            ["max_tokens"] = maxTokens,
            ["stream"] = stream
        };

        // Only send temperature when thinking is off (DeepSeek ignores it otherwise)
        if (_options.Thinking == DeepSeekV4Options.ThinkingMode.Off)
        {
            body["temperature"] = temperature;
        }

        // Thinking mode
        if (_options.Thinking != DeepSeekV4Options.ThinkingMode.Auto)
        {
            body["thinking"] = new JsonObject
            {
                ["type"] = _options.IsThinkingEnabled ? "enabled" : "disabled"
            };

            if (_options.IsThinkingEnabled)
            {
                body["reasoning_effort"] = _options.ReasoningEffort;
            }
        }

        // Stream options for usage in final chunk
        if (stream && _options.IncludeStreamUsage)
        {
            body["stream_options"] = new JsonObject
            {
                ["include_usage"] = true
            };
        }

        // Tools
        if (tools is { Count: > 0 })
        {
            var toolArray = new JsonArray();
            foreach (var tool in tools)
            {
                toolArray.Add(tool.DeepClone());
            }

            body["tools"] = toolArray;
        }

        return body;
    }

    private static JsonObject MessageToJson(Message message)
    {
        var node = new JsonObject
        {
            ["role"] = message.Role.ToLowerInvariant()
        };

        if (!string.IsNullOrEmpty(message.Content))
        {
            node["content"] = message.Content;
        }
        else
        {
            node["content"] = "";
        }

        // Handle tool result messages
        if (message.Role.Equals("tool", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(message.ToolCallId))
            {
                node["tool_call_id"] = message.ToolCallId;
            }

            return node;
        }

        // Handle assistant messages with tool calls
        if (message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            && message.ToolCalls is { Count: > 0 })
        {
            var toolCallsArray = new JsonArray();
            foreach (var tc in message.ToolCalls)
            {
                toolCallsArray.Add(new JsonObject
                {
                    ["id"] = tc.Id,
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = tc.Name,
                        ["arguments"] = tc.Arguments?.ToJsonString() ?? "{}"
                    }
                });
            }

            node["tool_calls"] = toolCallsArray;

            // Keep reasoning_content for tool-call history (required by DeepSeek V4)
            if (!string.IsNullOrEmpty(message.ReasoningContent))
            {
                node["reasoning_content"] = message.ReasoningContent;
            }
        }

        return node;
    }

    private HttpRequestMessage CreateRequest(JsonObject body)
    {
        var uri = $"{_apiBase}/v1/chat/completions";
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
            body["stream"]?.GetValue<bool>() == true ? "text/event-stream" : "application/json"));
        return request;
    }

    private LLMResponse ParseNonStreamingResponse(string json)
    {
        var root = JsonNode.Parse(json) as JsonObject ?? new JsonObject();
        var choice = root["choices"]?[0] as JsonObject;
        var message = choice?["message"] as JsonObject;

        var response = new LLMResponse
        {
            Content = message?["content"]?.ToString(),
            FinishReason = choice?["finish_reason"]?.ToString() ?? "stop",
            ReasoningContent = message?["reasoning_content"]?.ToString(),
            Model = root["model"]?.ToString(),
            Usage = ParseDeepSeekUsage(root["usage"] as JsonObject)
        };

        if (message?["tool_calls"] is JsonArray toolCalls)
        {
            foreach (var tc in toolCalls.OfType<JsonObject>())
            {
                var function = tc["function"] as JsonObject;
                JsonNode? args = null;
                try { args = JsonNode.Parse(function?["arguments"]?.ToString() ?? "{}"); }
                catch { /* keep null */ }

                response.ToolCalls.Add(new ToolCallRequest(
                    tc["id"]?.ToString() ?? Guid.NewGuid().ToString("N"),
                    function?["name"]?.ToString() ?? "unknown",
                    args));
            }
        }

        return response;
    }

    public static LLMUsage ParseDeepSeekUsage(JsonObject? usage)
    {
        if (usage is null)
        {
            return LLMUsage.Basic(0, 0);
        }

        var promptTokens = usage["prompt_tokens"]?.GetValue<int>() ?? 0;
        var completionTokens = usage["completion_tokens"]?.GetValue<int>() ?? 0;
        var cacheHitTokens = usage["prompt_cache_hit_tokens"]?.GetValue<int>() ?? 0;
        var cacheMissTokens = usage["prompt_cache_miss_tokens"]?.GetValue<int>() ?? 0;
        var reasoningTokens = usage["completion_tokens_details"] is JsonObject details
            ? details["reasoning_tokens"]?.GetValue<int>() ?? 0
            : 0;

        return LLMUsage.FromDeepSeekResponse(promptTokens, cacheHitTokens, cacheMissTokens, completionTokens, reasoningTokens);
    }

    private static async IAsyncEnumerable<string> ReadSseDataAsync(
        StreamReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var data = new StringBuilder();
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (line.Length == 0)
            {
                if (data.Length > 0)
                {
                    yield return data.ToString();
                    data.Clear();
                }

                continue;
            }

            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                if (data.Length > 0)
                {
                    data.Append('\n');
                }

                data.Append(line["data:".Length..].Trim());
            }
        }

        if (data.Length > 0)
        {
            yield return data.ToString();
        }
    }

    private sealed class DeepSeekStreamParser
    {
        private readonly StringBuilder _content = new();
        private readonly StringBuilder _reasoning = new();
        private readonly Dictionary<int, ToolCallAccumulator> _toolCallsByIndex = new();
        private string _finishReason = "stop";
        private LLMUsage? _usage;

        public IReadOnlyList<LLMStreamChunk> Apply(string json)
        {
            var chunks = new List<LLMStreamChunk>();
            var root = JsonNode.Parse(json) as JsonObject;
            if (root is null) return chunks;

            var choices = root["choices"];
            if (choices is null) return chunks;

            var choice = choices[0] as JsonObject;
            if (choice is null) return chunks;

            var delta = choice["delta"] as JsonObject;

            // Reasoning delta
            if (delta is not null)
            {
                var reasoningDelta = delta["reasoning_content"]?.ToString();
                if (!string.IsNullOrEmpty(reasoningDelta))
                {
                    _reasoning.Append(reasoningDelta);
                    chunks.Add(LLMStreamChunk.Reasoning(reasoningDelta));
                }

                // Content delta
                var contentDelta = delta["content"]?.ToString();
                if (!string.IsNullOrEmpty(contentDelta))
                {
                    _content.Append(contentDelta);
                    chunks.Add(LLMStreamChunk.Delta(contentDelta));
                }

                // Tool call deltas
                if (delta["tool_calls"] is JsonArray toolCalls)
                {
                    foreach (var item in toolCalls.OfType<JsonObject>())
                    {
                        var index = item["index"]?.GetValue<int>() ?? 0;
                        if (!_toolCallsByIndex.TryGetValue(index, out var accumulator))
                        {
                            accumulator = new ToolCallAccumulator();
                            _toolCallsByIndex[index] = accumulator;
                        }

                        if (!string.IsNullOrWhiteSpace(item["id"]?.ToString()))
                        {
                            accumulator.Id = item["id"]!.ToString();
                        }

                        var function = item["function"] as JsonObject;
                        if (!string.IsNullOrWhiteSpace(function?["name"]?.ToString()))
                        {
                            accumulator.Name = function!["name"]!.ToString();
                        }

                        if (function?["arguments"] is JsonNode args)
                        {
                            accumulator.Arguments.Append(args.ToString());
                        }
                    }
                }
            }

            // Finish reason
            var finishReason = choice["finish_reason"]?.ToString();
            if (!string.IsNullOrWhiteSpace(finishReason))
            {
                _finishReason = finishReason;
            }

            // Usage (from stream_options.include_usage final chunk)
            var usage = root["usage"] as JsonObject;
            if (usage is not null)
            {
                _usage = ParseDeepSeekUsage(usage);
            }

            return chunks;
        }

        public LLMResponse BuildResponse(string model)
        {
            var response = new LLMResponse
            {
                Content = _content.Length == 0 ? null : _content.ToString(),
                ReasoningContent = _reasoning.Length == 0 ? null : _reasoning.ToString(),
                FinishReason = _finishReason,
                Usage = _usage,
                Model = model,
                Provider = "deepseek"
            };

            foreach (var accumulator in _toolCallsByIndex.Values)
            {
                if (string.IsNullOrWhiteSpace(accumulator.Name))
                {
                    continue;
                }

                JsonNode? args = null;
                try
                {
                    args = JsonNode.Parse(accumulator.Arguments.Length == 0 ? "{}" : accumulator.Arguments.ToString());
                }
                catch { /* keep null */ }

                response.ToolCalls.Add(new ToolCallRequest(
                    string.IsNullOrWhiteSpace(accumulator.Id) ? Guid.NewGuid().ToString("N") : accumulator.Id,
                    accumulator.Name,
                    args));
            }

            return response;
        }
    }

    private sealed class ToolCallAccumulator
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public StringBuilder Arguments { get; } = new();
    }
}
