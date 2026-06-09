namespace Nanobot.Core.Providers;

public record DeepSeekV4Options
{
    public string? Model { get; init; } = DeepSeekV4Models.Flash;

    public int MaxTokens { get; init; } = 4096;

    public ThinkingMode Thinking { get; init; } = ThinkingMode.Auto;

    public string ReasoningEffort { get; init; } = "high";

    public bool IncludeStreamUsage { get; init; } = true;

    public string? ApiKey { get; init; }

    public string? ApiBase { get; init; }

    public enum ThinkingMode
    {
        Off,
        High,
        Max,
        Auto
    }

    public bool IsThinkingEnabled => Thinking != ThinkingMode.Off;

    public static ThinkingMode ParseThinkingMode(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "off" or "disabled" or "关闭" => ThinkingMode.Off,
            "high" or "高" => ThinkingMode.High,
            "max" or "maximum" or "最大" => ThinkingMode.Max,
            _ => ThinkingMode.Auto
        };
    }
}
