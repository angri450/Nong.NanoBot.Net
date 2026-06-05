using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using Nanobot.Core.Models;

namespace Nanobot.Core.Providers;

public class AnthropicProvider : IStreamingLLMProvider
{
    private const string DefaultBaseUrl = "https://api.anthropic.com";
    private const string AnthropicVersion = "2023-06-01";

    private readonly string _apiKey;
    private readonly string _defaultModel;
    private readonly HttpClient _httpClient;
    private readonly Uri _messagesUri;

    public AnthropicProvider(
        string apiKey,
        string defaultModel = "claude-sonnet-4-5",
        HttpClient? httpClient = null,
        string? baseUrl = null)
    {
        _apiKey = apiKey;
        _defaultModel = defaultModel;
        _httpClient = httpClient ?? new HttpClient();
        _messagesUri = new Uri(new Uri((baseUrl ?? DefaultBaseUrl).TrimEnd('/') + "/"), "v1/messages");
    }

    public string GetDefaultModel() => _defaultModel;

    public async Task<LLMResponse> ChatAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7)
    {
        var body = BuildRequestBody(messages, tools, model ?? _defaultModel, maxTokens, temperature);
        using var request = new HttpRequestMessage(HttpMethod.Post, _messagesUri)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("x-api-key", _apiKey);
        request.Headers.TryAddWithoutValidation("anthropic-version", AnthropicVersion);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return new LLMResponse($"Error: Anthropic returned {(int)response.StatusCode}: {content}")
                {
                    FinishReason = "error"
                };
            }

            return ParseResponse(content);
        }
        catch (Exception ex)
        {
            return new LLMResponse($"Error: {ex.Message}") { FinishReason = "error" };
        }
    }

    public async IAsyncEnumerable<LLMStreamChunk> ChatStreamAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var body = BuildRequestBody(messages, tools, model ?? _defaultModel, maxTokens, temperature);
        body["stream"] = true;
        using var request = new HttpRequestMessage(HttpMethod.Post, _messagesUri)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("x-api-key", _apiKey);
        request.Headers.TryAddWithoutValidation("anthropic-version", AnthropicVersion);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        var rawError = response.IsSuccessStatusCode
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);
        if (rawError is not null)
        {
            yield return LLMStreamChunk.Final(new LLMResponse($"Error: Anthropic returned {(int)response.StatusCode}: {rawError}")
            {
                FinishReason = "error"
            });
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var parser = new AnthropicStreamParser();
        await foreach (var data in ReadSseDataAsync(reader, cancellationToken))
        {
            var chunks = parser.Apply(data);
            foreach (var chunk in chunks)
            {
                yield return chunk;
            }
        }

        yield return LLMStreamChunk.Final(parser.BuildResponse());
    }

    private static JsonObject BuildRequestBody(
        IReadOnlyList<Message> messages,
        List<JsonNode>? tools,
        string model,
        int maxTokens,
        double temperature)
    {
        var body = new JsonObject
        {
            ["model"] = model,
            ["max_tokens"] = maxTokens,
            ["temperature"] = temperature,
            ["messages"] = BuildMessages(messages)
        };

        var system = string.Join("\n\n", messages
            .Where(message => message.Role.Equals("system", StringComparison.OrdinalIgnoreCase))
            .Select(message => message.Content)
            .Where(content => !string.IsNullOrWhiteSpace(content)));
        if (!string.IsNullOrWhiteSpace(system))
        {
            body["system"] = system;
        }

        var anthropicTools = BuildTools(tools);
        if (anthropicTools.Count > 0)
        {
            body["tools"] = anthropicTools;
        }

        return body;
    }

    private static JsonArray BuildMessages(IReadOnlyList<Message> messages)
    {
        var array = new JsonArray();
        foreach (var message in messages.Where(message => !message.Role.Equals("system", StringComparison.OrdinalIgnoreCase)))
        {
            if (message.Role.Equals("tool", StringComparison.OrdinalIgnoreCase))
            {
                array.Add(new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "tool_result",
                            ["tool_use_id"] = message.ToolCallId ?? "",
                            ["content"] = message.Content ?? ""
                        }
                    }
                });
                continue;
            }

            if (message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) && message.ToolCalls?.Count > 0)
            {
                var content = new JsonArray();
                if (!string.IsNullOrWhiteSpace(message.Content))
                {
                    content.Add(new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = message.Content
                    });
                }

                foreach (var toolCall in message.ToolCalls)
                {
                    content.Add(new JsonObject
                    {
                        ["type"] = "tool_use",
                        ["id"] = toolCall.Id,
                        ["name"] = toolCall.Name,
                        ["input"] = toolCall.Arguments?.DeepClone() ?? new JsonObject()
                    });
                }

                array.Add(new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = content
                });
                continue;
            }

            var role = message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
            array.Add(new JsonObject
            {
                ["role"] = role,
                ["content"] = message.Content ?? ""
            });
        }

        return array;
    }

    private static JsonArray BuildTools(List<JsonNode>? tools)
    {
        var array = new JsonArray();
        if (tools is null)
        {
            return array;
        }

        foreach (var tool in tools)
        {
            var function = tool["function"];
            if (function is null)
            {
                continue;
            }

            array.Add(new JsonObject
            {
                ["name"] = function["name"]?.ToString() ?? "unknown",
                ["description"] = function["description"]?.ToString() ?? "",
                ["input_schema"] = function["parameters"]?.DeepClone() ?? new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject()
                }
            });
        }

        return array;
    }

    private static LLMResponse ParseResponse(string json)
    {
        var root = JsonNode.Parse(json) as JsonObject ?? new JsonObject();
        var textParts = new List<string>();
        var toolCalls = new List<ToolCallRequest>();
        if (root["content"] is JsonArray content)
        {
            foreach (var item in content.OfType<JsonObject>())
            {
                var type = item["type"]?.ToString();
                if (type == "text")
                {
                    var text = item["text"]?.ToString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        textParts.Add(text);
                    }
                }
                else if (type == "tool_use")
                {
                    toolCalls.Add(new ToolCallRequest(
                        item["id"]?.ToString() ?? Guid.NewGuid().ToString("N"),
                        item["name"]?.ToString() ?? "unknown",
                        item["input"]?.DeepClone()
                    ));
                }
            }
        }

        var result = new LLMResponse
        {
            FinishReason = root["stop_reason"]?.ToString() ?? "stop",
            Content = textParts.Count == 0 ? null : string.Join("\n", textParts)
        };

        result.ToolCalls.AddRange(toolCalls);

        if (root["usage"] is JsonObject usage)
        {
            result.Usage["prompt_tokens"] = usage["input_tokens"]?.GetValue<int>() ?? 0;
            result.Usage["completion_tokens"] = usage["output_tokens"]?.GetValue<int>() ?? 0;
            result.Usage["total_tokens"] = result.Usage["prompt_tokens"] + result.Usage["completion_tokens"];
        }

        return result;
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

    private sealed class AnthropicStreamParser
    {
        private readonly StringBuilder _content = new();
        private readonly Dictionary<int, ToolUseBuilder> _toolUseByIndex = new();
        private string _finishReason = "stop";

        public IReadOnlyList<LLMStreamChunk> Apply(string json)
        {
            var chunks = new List<LLMStreamChunk>();
            var root = JsonNode.Parse(json) as JsonObject;
            var type = root?["type"]?.ToString();
            if (type == "content_block_start")
            {
                var index = root?["index"]?.GetValue<int>() ?? 0;
                var block = root?["content_block"] as JsonObject;
                if (block?["type"]?.ToString() == "tool_use")
                {
                    _toolUseByIndex[index] = new ToolUseBuilder
                    {
                        Id = block["id"]?.ToString() ?? Guid.NewGuid().ToString("N"),
                        Name = block["name"]?.ToString() ?? "unknown"
                    };
                }
            }
            else if (type == "content_block_delta")
            {
                var index = root?["index"]?.GetValue<int>() ?? 0;
                var delta = root?["delta"] as JsonObject;
                if (delta?["type"]?.ToString() == "text_delta")
                {
                    var text = delta["text"]?.ToString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        _content.Append(text);
                        chunks.Add(LLMStreamChunk.Delta(text));
                    }
                }
                else if (delta?["type"]?.ToString() == "input_json_delta"
                    && _toolUseByIndex.TryGetValue(index, out var toolUse))
                {
                    toolUse.Arguments.Append(delta["partial_json"]?.ToString());
                }
            }
            else if (type == "message_delta")
            {
                if (root?["delta"] is JsonObject delta
                    && !string.IsNullOrWhiteSpace(delta["stop_reason"]?.ToString()))
                {
                    _finishReason = delta["stop_reason"]!.ToString();
                }
            }

            return chunks;
        }

        public LLMResponse BuildResponse()
        {
            var response = new LLMResponse
            {
                Content = _content.Length == 0 ? null : _content.ToString(),
                FinishReason = _finishReason == "stop" && _toolUseByIndex.Count > 0
                    ? "tool_use"
                    : _finishReason
            };

            foreach (var toolUse in _toolUseByIndex.Values)
            {
                JsonNode? arguments = null;
                try
                {
                    arguments = JsonNode.Parse(toolUse.Arguments.Length == 0 ? "{}" : toolUse.Arguments.ToString());
                }
                catch
                {
                    arguments = null;
                }

                response.ToolCalls.Add(new ToolCallRequest(toolUse.Id, toolUse.Name, arguments));
            }

            return response;
        }
    }

    private sealed class ToolUseBuilder
    {
        public string Id { get; init; } = Guid.NewGuid().ToString("N");

        public string Name { get; init; } = "unknown";

        public StringBuilder Arguments { get; } = new();
    }
}
