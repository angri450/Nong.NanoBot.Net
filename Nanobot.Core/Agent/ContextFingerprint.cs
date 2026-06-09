namespace Nanobot.Core.Agent;

public record ContextFingerprint
{
    public string SystemHash { get; init; } = "";
    public string ProjectGuidanceHash { get; init; } = "";
    public string ToolSchemaHash { get; init; } = "";
    public string FrozenHistoryPrefixHash { get; init; } = "";
    public string DynamicTailHash { get; init; } = "";
    public int SentTokensEstimate { get; init; }

    public bool IsValid => !string.IsNullOrEmpty(SystemHash);

    public string FingerprintKey =>
        $"{SystemHash}|{ProjectGuidanceHash}|{ToolSchemaHash}|{FrozenHistoryPrefixHash}";

    public string? DescribeChange(ContextFingerprint previous)
    {
        if (previous.SystemHash != SystemHash) return "系统指令变化";
        if (previous.ToolSchemaHash != ToolSchemaHash) return "工具 schema 发生变化";
        if (previous.FrozenHistoryPrefixHash != FrozenHistoryPrefixHash) return "历史消息被压缩";
        if (previous.ProjectGuidanceHash != ProjectGuidanceHash) return "项目指导变化";
        return null;
    }
}
