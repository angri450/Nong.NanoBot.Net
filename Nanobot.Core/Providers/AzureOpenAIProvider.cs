using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Nanobot.Core.Models;

namespace Nanobot.Core.Providers;

public class AzureOpenAIProvider : ILLMProvider
{
    public const string DefaultApiVersion = "2024-10-21";

    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deployment;
    private readonly string _apiVersion;
    private readonly HttpClient _httpClient;

    public AzureOpenAIProvider(
        string endpoint,
        string apiKey,
        string deployment,
        string apiVersion = DefaultApiVersion,
        HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Azure OpenAI endpoint is required.", nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(deployment))
        {
            throw new ArgumentException("Azure OpenAI deployment is required.", nameof(deployment));
        }

        _endpoint = endpoint.TrimEnd('/');
        _apiKey = apiKey;
        _deployment = deployment;
        _apiVersion = apiVersion;
        _httpClient = httpClient ?? new HttpClient();
    }

    public string GetDefaultModel() => _deployment;

    public async Task<LLMResponse> ChatAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7)
    {
        var deployment = model ?? _deployment;
        var uri = $"{_endpoint}/openai/deployments/{Uri.EscapeDataString(deployment)}/chat/completions?api-version={Uri.EscapeDataString(_apiVersion)}";
        var body = BuildRequestBody(messages, tools, maxTokens, temperature);

        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("api-key", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return new LLMResponse($"Error: Azure OpenAI returned {(int)response.StatusCode}: {content}")
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
        int maxTokens,
        double temperature)
    {
        var body = new JsonObject
        {
            ["messages"] = new JsonArray(messages.Select(ToOpenAIMessage).Where(message => message is not null).ToArray()),
            ["max_tokens"] = maxTokens,
            ["temperature"] = temperature
        };

        if (tools is { Count: > 0 })
        {
            body["tools"] = new JsonArray(tools.Select(tool => tool.DeepClone()).ToArray());
        }

        return body;
    }

    private static JsonNode? ToOpenAIMessage(Message message)
    {
        if (message.Role.Equals("tool", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(message.ToolCallId))
            {
                return null;
            }

            return new JsonObject
            {
                ["role"] = "tool",
                ["tool_call_id"] = message.ToolCallId,
                ["content"] = message.Content ?? ""
            };
        }

        var node = new JsonObject
        {
            ["role"] = message.Role.ToLowerInvariant(),
            ["content"] = message.Content ?? ""
        };

        if (message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) && message.ToolCalls?.Count > 0)
        {
            node["tool_calls"] = new JsonArray(message.ToolCalls.Select(toolCall => new JsonObject
            {
                ["id"] = toolCall.Id,
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = toolCall.Name,
                    ["arguments"] = toolCall.Arguments?.ToJsonString() ?? "{}"
                }
            }).ToArray());
        }

        return node;
    }

    private static LLMResponse ParseResponse(string json)
    {
        var root = JsonNode.Parse(json) as JsonObject ?? new JsonObject();
        var choice = root["choices"]?[0] as JsonObject;
        var message = choice?["message"] as JsonObject;
        var result = new LLMResponse
        {
            Content = message?["content"]?.ToString(),
            FinishReason = choice?["finish_reason"]?.ToString() ?? "stop"
        };

        if (message?["tool_calls"] is JsonArray toolCalls)
        {
            foreach (var toolCall in toolCalls.OfType<JsonObject>())
            {
                var function = toolCall["function"] as JsonObject;
                JsonNode? arguments = null;
                try
                {
                    arguments = JsonNode.Parse(function?["arguments"]?.ToString() ?? "{}");
                }
                catch
                {
                    arguments = null;
                }

                result.ToolCalls.Add(new ToolCallRequest(
                    toolCall["id"]?.ToString() ?? Guid.NewGuid().ToString("N"),
                    function?["name"]?.ToString() ?? "unknown",
                    arguments
                ));
            }
        }

        if (root["usage"] is JsonObject usage)
        {
            result.Usage["prompt_tokens"] = usage["prompt_tokens"]?.GetValue<int>() ?? 0;
            result.Usage["completion_tokens"] = usage["completion_tokens"]?.GetValue<int>() ?? 0;
            result.Usage["total_tokens"] = usage["total_tokens"]?.GetValue<int>() ?? 0;
        }

        return result;
    }
}
