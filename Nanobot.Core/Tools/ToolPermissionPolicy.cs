namespace Nanobot.Core.Tools;

public enum PermissionMode
{
    Default,
    Plan,
    AcceptEdits,
    DenySideEffects
}

public class ToolPermissionPolicy
{
    private readonly PermissionMode _mode;
    private readonly string _workspaceRoot;

    private static readonly HashSet<string> SideEffectTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "write_file", "edit_file", "shell", "nong", "github"
    };

    private static readonly HashSet<string> PlanAllowedTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "read_file", "list_dir", "web_search", "web_fetch", "memory", "summarize", "weather", "stock"
    };

    public ToolPermissionPolicy(PermissionMode mode, string workspaceRoot)
    {
        _mode = mode;
        _workspaceRoot = workspaceRoot;
    }

    public PermissionMode Mode => _mode;

    public bool IsAllowed(string toolName, IDictionary<string, object?>? arguments = null)
    {
        return _mode switch
        {
            PermissionMode.Plan => PlanAllowedTools.Contains(toolName),
            PermissionMode.DenySideEffects => !SideEffectTools.Contains(toolName),
            _ => true
        };
    }

    public bool IsPathInWorkspace(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        var workspaceFull = Path.GetFullPath(_workspaceRoot);
        return fullPath.StartsWith(workspaceFull, StringComparison.OrdinalIgnoreCase);
    }

    public bool RequiresReadBeforeEdit(string toolName)
    {
        return toolName is "write_file" or "edit_file";
    }

    public static string GetDenyReason(string toolName, PermissionMode mode)
    {
        return mode switch
        {
            PermissionMode.Plan => $"Tool '{toolName}' is not allowed in plan mode (side effects disabled).",
            PermissionMode.DenySideEffects => $"Tool '{toolName}' is a side-effect tool and is currently denied.",
            _ => $"Tool '{toolName}' denied."
        };
    }
}
