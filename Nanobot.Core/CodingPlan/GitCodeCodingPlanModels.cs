using System.Text.Json.Serialization;

namespace Nanobot.Core.CodingPlan;

public enum PlanType { Lite, Pro, Max }

public sealed record GitCodeCodingPlanClaim
{
    [JsonPropertyName("plan_type")]
    public string PlanType { get; init; } = "";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "";

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

public sealed record GitCodeCodingPlanStatus
{
    [JsonPropertyName("plan_type")]
    public string? PlanType { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "";

    [JsonPropertyName("used_tokens")]
    public long? UsedTokens { get; init; }

    [JsonPropertyName("total_tokens")]
    public long? TotalTokens { get; init; }

    [JsonPropertyName("quota_reset_at")]
    public string? QuotaResetAt { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

public sealed record GitCodeModelEntry
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("display_model_name")]
    public string DisplayModelName { get; init; } = "";

    [JsonPropertyName("model_name")]
    public string? ModelName { get; init; }

    [JsonPropertyName("base_url")]
    public string? BaseUrl { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = "openai";

    [JsonPropertyName("context_window")]
    public int ContextWindow { get; init; }

    [JsonPropertyName("plan_available")]
    public bool PlanAvailable { get; init; } = true;

    [JsonPropertyName("is_infinity")]
    public int IsInfinity { get; init; }

    [JsonPropertyName("is_atomcode_exclusive")]
    public int IsAtomcodeExclusive { get; init; }
}

public class CodingPlanSetupResult
{
    public bool Success { get; set; }
    public Dictionary<string, CodingPlanStep> Steps { get; set; } = new();
    public string? DefaultProvider { get; set; }
    public List<GitCodeModelEntry> Models { get; set; } = new();
}

public class CodingPlanStep
{
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
}
