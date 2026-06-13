using Nanobot.Core.Models;
using Nanobot.Core.Tools.Builtin;

namespace Nanobot.Core.Agent;

/// <summary>
/// Hook that intercepts high-risk Nong commands before execution.
/// Install/token commands require explicit user approval every time.
/// Write commands require one-time session approval for each module group.
/// </summary>
public class NongConfirmationHook : IAgentHook
{
    // Commands that MUST be confirmed every single time
    private const string InstallConfirmPrefix = "nong_ocr_install_model";
    private const string CloudConfirmPrefix = "nong_ocr_cloud";
    private const string ToWordConfirmPrefix = "nong_ocr_to_word";
    private const string CameraConfirmPrefix = "nong_ocr_camera";

    // Write-risk command prefixes (confirm once per session per group)
    private static readonly HashSet<string> WriteCommandPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "nong_word_create", "nong_word_fill", "nong_word_crop", "nong_word_fit_images",
        "nong_word_compact_tables", "nong_word_regroup_images", "nong_word_merge",
        "nong_word_academic_format", "nong_word_format_gongwen", "nong_word_protect",
        "nong_word_add_", "nong_word_rebuild", "nong_word_fix_order",
        "nong_pdf_merge", "nong_pdf_split", "nong_pdf_ocr", "nong_pdf_compress",
        "nong_pptx_create",
        "nong_excel_create", "nong_excel_style", "nong_excel_formula", "nong_excel_pivot",
        "nong_inspect_write_paper", "nong_inspect_write_official",
        "nong_chart_", "nong_diagram_"
    };

    // Session-level tracking: group → confirmed
    private readonly Dictionary<string, HashSet<string>> _confirmedWriteGroups = new();

    public Task BeforeToolAsync(AgentToolContext context)
    {
        var toolName = context.ToolCall.Name;

        // Only intercept Nong discovered tools (they start with "nong_")
        if (!toolName.StartsWith("nong_", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        // --- Install / Token commands: always confirm ---
        if (toolName.StartsWith(InstallConfirmPrefix, StringComparison.OrdinalIgnoreCase) ||
            toolName.StartsWith(CameraConfirmPrefix, StringComparison.OrdinalIgnoreCase))
        {
            context.Reject($"[Nong guard] '{toolName}' requires explicit user confirmation. This command downloads large files (install-model) or accesses hardware (camera). Reply \"confirm\" to proceed, or explain what you want.");
            return Task.CompletedTask;
        }

        if (toolName.StartsWith(CloudConfirmPrefix, StringComparison.OrdinalIgnoreCase) ||
            toolName.StartsWith(ToWordConfirmPrefix, StringComparison.OrdinalIgnoreCase))
        {
            context.Reject($"[Nong guard] '{toolName}' consumes PaddleOCR API token. Reply \"confirm\" to proceed.");
            return Task.CompletedTask;
        }

        // --- Write commands: confirm once per session per group ---
        if (!IsWriteCommand(toolName))
            return Task.CompletedTask;

        var group = ResolveWriteGroup(toolName);
        var sessionKey = context.Execution.SessionId;

        if (!_confirmedWriteGroups.TryGetValue(sessionKey, out var confirmed))
        {
            confirmed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _confirmedWriteGroups[sessionKey] = confirmed;
        }

        if (confirmed.Contains(group))
            return Task.CompletedTask; // Already confirmed this group in this session

        // First write in this group this session → confirm
        context.Reject($"[Nong guard] '{toolName}' will write/modify files (group: {group}). Reply \"confirm\" to allow all {group} write commands for this session.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// After a tool runs successfully, mark its group as confirmed if it was a write command.
    /// (Called from the LLM flow when user typed "confirm" and the tool was re-invoked.)
    /// </summary>
    public Task AfterToolAsync(AgentToolContext context)
    {
        var toolName = context.ToolCall.Name;
        if (!IsWriteCommand(toolName) || context.IsRejected)
            return Task.CompletedTask;

        var group = ResolveWriteGroup(toolName);
        var sessionKey = context.Execution.SessionId;

        if (!_confirmedWriteGroups.TryGetValue(sessionKey, out var confirmed))
        {
            confirmed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _confirmedWriteGroups[sessionKey] = confirmed;
        }

        confirmed.Add(group);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Allow the user to explicitly confirm a tool group before first use.
    /// </summary>
    public void ConfirmGroup(string sessionId, string group)
    {
        if (!_confirmedWriteGroups.TryGetValue(sessionId, out var confirmed))
        {
            confirmed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _confirmedWriteGroups[sessionId] = confirmed;
        }
        confirmed.Add(group);
    }

    private static bool IsWriteCommand(string toolName)
    {
        foreach (var prefix in WriteCommandPrefixes)
        {
            if (toolName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string ResolveWriteGroup(string toolName)
    {
        var second = toolName.IndexOf('_');
        if (second < 0) return "unknown";
        var third = toolName.IndexOf('_', second + 1);
        return third > second + 1
            ? toolName[(second + 1)..third]
            : toolName[(second + 1)..];
    }
}
