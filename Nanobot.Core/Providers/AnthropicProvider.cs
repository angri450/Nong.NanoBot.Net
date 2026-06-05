using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Nanobot.Core.Models;

namespace Nanobot.Core.Providers;

public class AnthropicProvider : ILLMProvider
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
}
