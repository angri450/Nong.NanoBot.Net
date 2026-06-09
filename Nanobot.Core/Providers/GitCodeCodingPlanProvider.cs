using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Nanobot.Core.Auth;
using Nanobot.Core.Models;

namespace Nanobot.Core.Providers;

public class GitCodeCodingPlanProvider : IStreamingLLMProvider
{
    private readonly GitCodeAuthStore _authStore;
    private readonly string? _defaultModel;
    private readonly string? _defaultBaseUrl;
    private IStreamingLLMProvider? _delegateProvider;

    public GitCodeCodingPlanProvider(
        GitCodeAuthStore authStore,
        string? defaultModel = null,
        string? defaultBaseUrl = null)
    {
        _authStore = authStore;
        _defaultModel = defaultModel;
        _defaultBaseUrl = defaultBaseUrl;
    }

    public string GetDefaultModel() => _defaultModel ?? DeepSeekV4Models.Flash;

    private IStreamingLLMProvider EnsureProvider()
    {
        if (_delegateProvider is not null) return _delegateProvider;

        var token = _authStore.GetValidAccessToken()
            ?? throw new InvalidOperationException("GitCode access token is not available. Please login first.");

        var model = _defaultModel ?? DeepSeekV4Models.Flash;
        var baseUrl = _defaultBaseUrl ?? "https://api-ai.gitcode.com/v1";

        if (DeepSeekV4Models.IsDeepSeekV4(model))
        {
            _delegateProvider = new DeepSeekV4Provider(token, baseUrl, new DeepSeekV4Options
            {
                Model = model,
                ApiBase = baseUrl
            });
        }
        else
        {
            _delegateProvider = new OpenAICompatibleProvider(token, baseUrl, model);
        }

        return _delegateProvider;
    }

    public async Task<LLMResponse> ChatAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7)
    {
        return await EnsureProvider().ChatAsync(messages, tools, model ?? _defaultModel, maxTokens, temperature);
    }

    public async IAsyncEnumerable<LLMStreamChunk> ChatStreamAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var provider = EnsureProvider();
        if (provider is IStreamingLLMProvider streaming)
        {
            await foreach (var chunk in streaming.ChatStreamAsync(messages, tools, model ?? _defaultModel, maxTokens, temperature, cancellationToken))
            {
                yield return chunk;
            }
        }
        else
        {
            var response = await provider.ChatAsync(messages, tools, model ?? _defaultModel, maxTokens, temperature);
            if (!string.IsNullOrEmpty(response.Content))
            {
                yield return LLMStreamChunk.Delta(response.Content);
            }

            yield return LLMStreamChunk.Final(response);
        }
    }
}
