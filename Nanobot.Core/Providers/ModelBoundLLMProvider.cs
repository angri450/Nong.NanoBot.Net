using System.Text.Json.Nodes;
using Nanobot.Core.Models;

namespace Nanobot.Core.Providers;

public class ModelBoundLLMProvider : IStreamingLLMProvider
{
    private readonly ILLMProvider _inner;
    private readonly string _model;

    public ModelBoundLLMProvider(ILLMProvider inner, string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model is required.", nameof(model));
        }

        _inner = inner;
        _model = model;
    }

    public string GetDefaultModel() => _model;

    public Task<LLMResponse> ChatAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7)
    {
        return _inner.ChatAsync(messages, tools, model ?? _model, maxTokens, temperature);
    }

    public IAsyncEnumerable<LLMStreamChunk> ChatStreamAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7,
        CancellationToken cancellationToken = default)
    {
        if (_inner is IStreamingLLMProvider streamingProvider)
        {
            return streamingProvider.ChatStreamAsync(
                messages,
                tools,
                model ?? _model,
                maxTokens,
                temperature,
                cancellationToken
            );
        }

        return ChatStreamFallbackAsync(messages, tools, model, maxTokens, temperature, cancellationToken);
    }

    private async IAsyncEnumerable<LLMStreamChunk> ChatStreamFallbackAsync(
        List<Message> messages,
        List<JsonNode>? tools,
        string? model,
        int maxTokens,
        double temperature,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var response = await ChatAsync(messages, tools, model, maxTokens, temperature);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrEmpty(response.Content))
        {
            yield return LLMStreamChunk.Delta(response.Content);
        }

        yield return LLMStreamChunk.Final(response);
    }
}
