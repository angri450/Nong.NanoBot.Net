namespace Nanobot.Core.Providers;

public record LLMUsage
{
    public int InputTokens { get; init; }
    public int CachedInputTokens { get; init; }
    public int UncachedInputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int ReasoningTokens { get; init; }
    public int TotalTokens { get; init; }

    public double CacheHitRate => InputTokens > 0
        ? (double)CachedInputTokens / InputTokens
        : 0;

    public static LLMUsage FromLegacy(Dictionary<string, int> legacy)
    {
        legacy.TryGetValue("prompt_tokens", out var prompt);
        legacy.TryGetValue("completion_tokens", out var completion);
        legacy.TryGetValue("total_tokens", out var total);
        legacy.TryGetValue("prompt_cache_hit_tokens", out var cacheHit);
        legacy.TryGetValue("prompt_cache_miss_tokens", out var cacheMiss);
        legacy.TryGetValue("reasoning_tokens", out var reasoning);

        return new LLMUsage
        {
            InputTokens = prompt,
            CachedInputTokens = cacheHit,
            UncachedInputTokens = cacheMiss > 0 ? cacheMiss : prompt - cacheHit,
            OutputTokens = completion,
            ReasoningTokens = reasoning,
            TotalTokens = total > 0 ? total : prompt + completion
        };
    }

    public static LLMUsage FromDeepSeekResponse(
        int promptTokens,
        int cacheHitTokens,
        int cacheMissTokens,
        int completionTokens,
        int reasoningTokens)
    {
        return new LLMUsage
        {
            InputTokens = promptTokens,
            CachedInputTokens = cacheHitTokens,
            UncachedInputTokens = cacheMissTokens,
            OutputTokens = completionTokens,
            ReasoningTokens = reasoningTokens,
            TotalTokens = promptTokens + completionTokens
        };
    }

    public static LLMUsage Basic(int inputTokens, int outputTokens)
    {
        return new LLMUsage
        {
            InputTokens = inputTokens,
            UncachedInputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = inputTokens + outputTokens
        };
    }
}
