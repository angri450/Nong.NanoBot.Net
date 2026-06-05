using System.Text.Json.Nodes;
using System.Runtime.CompilerServices;
using Nanobot.Core.Models;

namespace Nanobot.Core.Providers;

public class FallbackLLMProvider : IStreamingLLMProvider
{
    private readonly IReadOnlyList<ProviderRegistration> _providers;

    public FallbackLLMProvider(IEnumerable<ProviderRegistration> providers)
    {
        _providers = providers.ToList();
        if (_providers.Count == 0)
        {
            throw new ArgumentException("At least one fallback provider is required.", nameof(providers));
        }
    }

    public string GetDefaultModel() => _providers[0].Provider.GetDefaultModel();

    public async Task<LLMResponse> ChatAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7)
    {
        var failures = new List<string>();

        foreach (var registration in _providers)
        {
            try
            {
                var response = await registration.Provider.ChatAsync(messages, tools, model, maxTokens, temperature);
                if (!string.Equals(response.FinishReason, "error", StringComparison.OrdinalIgnoreCase))
                {
                    return response;
                }

                failures.Add($"{registration.Name}: {response.Content ?? "provider returned an error"}");
            }
            catch (Exception ex)
            {
                failures.Add($"{registration.Name}: {ex.Message}");
            }
        }

        return new LLMResponse($"Error: all fallback providers failed. {string.Join(" | ", failures)}")
        {
            FinishReason = "error"
        };
    }

    public async IAsyncEnumerable<LLMStreamChunk> ChatStreamAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var failures = new List<string>();

        foreach (var registration in _providers)
        {
            var chunks = new List<LLMStreamChunk>();
            LLMResponse? finalResponse = null;
            string? failure = null;

            try
            {
                if (registration.Provider is IStreamingLLMProvider streamingProvider)
                {
                    await foreach (var chunk in streamingProvider.ChatStreamAsync(
                        messages,
                        tools,
                        model,
                        maxTokens,
                        temperature,
                        cancellationToken))
                    {
                        if (chunk.FinalResponse is not null)
                        {
                            finalResponse = chunk.FinalResponse;
                            continue;
                        }

                        if (!string.IsNullOrEmpty(chunk.ContentDelta))
                        {
                            chunks.Add(chunk);
                        }
                    }
                }
                else
                {
                    finalResponse = await registration.Provider.ChatAsync(messages, tools, model, maxTokens, temperature);
                    if (!string.IsNullOrEmpty(finalResponse.Content))
                    {
                        chunks.Add(LLMStreamChunk.Delta(finalResponse.Content));
                    }
                }
            }
            catch (Exception ex)
            {
                failure = ex.Message;
            }

            if (failure is not null)
            {
                failures.Add($"{registration.Name}: {failure}");
                continue;
            }

            if (finalResponse is null)
            {
                failures.Add($"{registration.Name}: stream completed without a final response");
                continue;
            }

            if (string.Equals(finalResponse.FinishReason, "error", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{registration.Name}: {finalResponse.Content ?? "provider returned an error"}");
                continue;
            }

            foreach (var chunk in chunks)
            {
                yield return chunk;
            }

            yield return LLMStreamChunk.Final(finalResponse);
            yield break;
        }

        yield return LLMStreamChunk.Final(new LLMResponse($"Error: all fallback providers failed. {string.Join(" | ", failures)}")
        {
            FinishReason = "error"
        });
    }
}
