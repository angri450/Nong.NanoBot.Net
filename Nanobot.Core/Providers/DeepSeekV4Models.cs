namespace Nanobot.Core.Providers;

public static class DeepSeekV4Models
{
    public const string Flash = "deepseek-v4-flash";
    public const string Pro = "deepseek-v4-pro";
    public const string DmxGuan = "deepseek-v4-pro-guan";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Flash, Pro, DmxGuan
    };

    public static DeepSeekV4Profile GetProfile(string modelId)
    {
        return modelId switch
        {
            Flash or Pro => new DeepSeekV4Profile
            {
                Id = modelId,
                ContextWindow = 1_000_000,
                MaxOutput = 384_000,
                SupportsStreaming = true,
                SupportsTools = true,
                SupportsReasoning = true,
                SupportsInterleavedThinking = true,
                SupportsPromptCacheMetrics = true,
                DefaultReasoningEffort = "high"
            },
            DmxGuan => new DeepSeekV4Profile
            {
                Id = DmxGuan,
                ContextWindow = 1_000_000,
                MaxOutput = 384_000,
                SupportsStreaming = true,
                SupportsTools = true,
                SupportsReasoning = true,
                SupportsInterleavedThinking = true,
                SupportsPromptCacheMetrics = true,
                DefaultReasoningEffort = "high"
            },
            _ => throw new ArgumentException($"Unknown DeepSeek V4 model: {modelId}", nameof(modelId))
        };
    }

    public static bool IsDeepSeekV4(string? modelId)
    {
        return modelId is not null && All.Contains(modelId);
    }
}

public record DeepSeekV4Profile
{
    public string Id { get; init; } = "";
    public int ContextWindow { get; init; }
    public int MaxOutput { get; init; }
    public bool SupportsStreaming { get; init; }
    public bool SupportsTools { get; init; }
    public bool SupportsReasoning { get; init; }
    public bool SupportsInterleavedThinking { get; init; }
    public bool SupportsPromptCacheMetrics { get; init; }
    public string DefaultReasoningEffort { get; init; } = "high";
}
