using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using Nanobot.Core.Models;
using OpenAI;
using OpenAI.Chat;

namespace Nanobot.Core.Providers;

public class OpenAICompatibleProvider : IStreamingLLMProvider
{
    private readonly string _defaultModel;
    private readonly OpenAIClient _client;

    public OpenAICompatibleProvider(string apiKey, string? baseUrl = null, string defaultModel = "gpt-4o")
    {
        _defaultModel = defaultModel;

        var options = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(baseUrl))
        {
            options.Endpoint = new Uri(baseUrl);
        }

        _client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
    }

    public string GetDefaultModel() => _defaultModel;

    public async Task<LLMResponse> ChatAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7)
    {
        var targetModel = model ?? _defaultModel;
        var chatClient = _client.GetChatClient(targetModel);

        var chatMessages = messages.Select(ToOpenAIMessage).Where(message => message is not null).ToList();
        var options = CreateOptions(tools, maxTokens, temperature);

        try
        {
            var completion = await chatClient.CompleteChatAsync(chatMessages!, options);
            return ParseResponse(completion);
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
        var targetModel = model ?? _defaultModel;
        var chatClient = _client.GetChatClient(targetModel);
        var chatMessages = messages.Select(ToOpenAIMessage).Where(message => message is not null).ToList();
        var options = CreateOptions(tools, maxTokens, temperature);
        var content = new StringBuilder();
        var toolCalls = new List<StreamingToolCallBuilder>();
        StreamingToolCallBuilder? lastToolCall = null;
        string? finishReason = null;
        int promptTokens = 0, completionTokens = 0;

        IAsyncEnumerable<StreamingChatCompletionUpdate>? updates = null;
        LLMResponse? startupError = null;
        try
        {
            updates = chatClient.CompleteChatStreamingAsync(chatMessages!, options, cancellationToken);
        }
        catch (Exception ex)
        {
            startupError = new LLMResponse($"Error: {ex.Message}") { FinishReason = "error" };
        }

        if (startupError is not null)
        {
            yield return LLMStreamChunk.Delta(startupError.Content!);
            yield return LLMStreamChunk.Final(startupError);
            yield break;
        }

        var completionUpdates = updates ?? throw new InvalidOperationException("Streaming updates were not created.");
        await foreach (var update in completionUpdates.WithCancellation(cancellationToken))
        {
            foreach (var contentPart in update.ContentUpdate)
            {
                if (string.IsNullOrEmpty(contentPart.Text))
                {
                    continue;
                }

                content.Append(contentPart.Text);
                yield return LLMStreamChunk.Delta(contentPart.Text);
            }

            foreach (var toolCallUpdate in update.ToolCallUpdates)
            {
                lastToolCall = ApplyToolCallUpdate(toolCalls, lastToolCall, toolCallUpdate);
            }

            var updateFinishReason = update.FinishReason?.ToString();
            if (!string.IsNullOrEmpty(updateFinishReason))
            {
                finishReason = updateFinishReason.ToLowerInvariant();
            }

            var updateUsage = update.Usage;
            if (updateUsage is not null)
            {
                promptTokens = updateUsage.InputTokenCount;
                completionTokens = updateUsage.OutputTokenCount;
            }
        }

        var response = new LLMResponse
        {
            Content = content.Length == 0 ? null : content.ToString(),
            FinishReason = finishReason ?? "stop",
            Usage = LLMUsage.Basic(promptTokens, completionTokens)
        };

        foreach (var toolCall in toolCalls)
        {
            if (string.IsNullOrWhiteSpace(toolCall.Name))
            {
                continue;
            }

            JsonNode? argsNode = null;
            try
            {
                argsNode = JsonNode.Parse(toolCall.Arguments.ToString());
            }
            catch
            {
                // Keep malformed provider arguments as null so the tool layer decides how to handle them.
            }

            response.ToolCalls.Add(new ToolCallRequest(
                string.IsNullOrWhiteSpace(toolCall.Id) ? Guid.NewGuid().ToString("N") : toolCall.Id,
                toolCall.Name,
                argsNode
            ));
        }

        yield return LLMStreamChunk.Final(response);
    }

    private static ChatCompletionOptions CreateOptions(
        List<JsonNode>? tools,
        int maxTokens,
        double temperature)
    {
        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = maxTokens,
            Temperature = (float)temperature
        };

        if (tools != null)
        {
            foreach (var toolNode in tools)
            {
                var funcNode = toolNode["function"];
                if (funcNode == null)
                {
                    continue;
                }

                var name = funcNode["name"]?.ToString() ?? "unknown";
                var description = funcNode["description"]?.ToString() ?? "";
                var parameters = funcNode["parameters"];
                var paramSchema = parameters is null ? null : BinaryData.FromString(parameters.ToJsonString());

                options.Tools.Add(ChatTool.CreateFunctionTool(name, description, paramSchema));
            }
        }

        return options;
    }

    private static ChatMessage? ToOpenAIMessage(Message message)
    {
        var content = message.Content ?? "";
        return message.Role.ToLowerInvariant() switch
        {
            "system" => new SystemChatMessage(content),
            "user" => new UserChatMessage(content),
            "assistant" => ToAssistantMessage(message, content),
            "tool" when !string.IsNullOrEmpty(message.ToolCallId) => new ToolChatMessage(message.ToolCallId, content),
            "tool" => null,
            _ => new UserChatMessage(content)
        };
    }

    private static AssistantChatMessage ToAssistantMessage(Message message, string content)
    {
        if (message.ToolCalls is null || message.ToolCalls.Count == 0)
        {
            return new AssistantChatMessage(content);
        }

        var toolCalls = message.ToolCalls.Select(toolCall =>
            ChatToolCall.CreateFunctionToolCall(
                toolCall.Id,
                toolCall.Name,
                BinaryData.FromString(toolCall.Arguments?.ToJsonString() ?? "{}")
            )
        ).ToList();

        var assistantMessage = new AssistantChatMessage(toolCalls);
        if (!string.IsNullOrEmpty(content))
        {
            assistantMessage.Content.Add(ChatMessageContentPart.CreateTextPart(content));
        }

        return assistantMessage;
    }

    private static LLMResponse ParseResponse(ChatCompletion completion)
    {
        var result = new LLMResponse
        {
            Content = completion.Content?.Count > 0 ? completion.Content[0].Text : null,
            FinishReason = completion.FinishReason.ToString().ToLowerInvariant(),
            Usage = completion.Usage != null
                ? LLMUsage.Basic(completion.Usage.InputTokenCount, completion.Usage.OutputTokenCount)
                : null
        };

        if (completion.ToolCalls != null && completion.ToolCalls.Count > 0)
        {
            foreach (var toolCall in completion.ToolCalls)
            {
                JsonNode? argsNode = null;
                try
                {
                    argsNode = JsonNode.Parse(toolCall.FunctionArguments.ToString());
                }
                catch
                {
                    // Keep malformed provider arguments as null so the tool layer decides how to handle them.
                }

                result.ToolCalls.Add(new ToolCallRequest(toolCall.Id, toolCall.FunctionName, argsNode));
            }
        }

        return result;
    }

    private static StreamingToolCallBuilder ApplyToolCallUpdate(
        List<StreamingToolCallBuilder> toolCalls,
        StreamingToolCallBuilder? lastToolCall,
        StreamingChatToolCallUpdate update)
    {
        var builder = ResolveToolCallBuilder(toolCalls, lastToolCall, update.ToolCallId);
        if (!string.IsNullOrWhiteSpace(update.ToolCallId))
        {
            builder.Id = update.ToolCallId;
        }

        if (!string.IsNullOrWhiteSpace(update.FunctionName))
        {
            builder.Name = update.FunctionName;
        }

        if (update.FunctionArgumentsUpdate is not null)
        {
            builder.Arguments.Append(update.FunctionArgumentsUpdate);
        }

        return builder;
    }

    private static StreamingToolCallBuilder ResolveToolCallBuilder(
        List<StreamingToolCallBuilder> toolCalls,
        StreamingToolCallBuilder? lastToolCall,
        string? toolCallId)
    {
        if (!string.IsNullOrWhiteSpace(toolCallId))
        {
            var existing = toolCalls.FirstOrDefault(call =>
                string.Equals(call.Id, toolCallId, StringComparison.Ordinal));
            if (existing is not null)
            {
                return existing;
            }

            var created = new StreamingToolCallBuilder { Id = toolCallId };
            toolCalls.Add(created);
            return created;
        }

        if (lastToolCall is not null)
        {
            return lastToolCall;
        }

        var fallback = new StreamingToolCallBuilder();
        toolCalls.Add(fallback);
        return fallback;
    }

    private sealed class StreamingToolCallBuilder
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public StringBuilder Arguments { get; } = new();
    }
}
