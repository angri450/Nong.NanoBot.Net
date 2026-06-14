namespace Nanobot.Core.Memory;

public static class WorkspaceBootstrapper
{
    public static void EnsureInitialized(string workspace)
    {
        if (string.IsNullOrWhiteSpace(workspace))
        {
            throw new ArgumentException("Workspace path is required.", nameof(workspace));
        }

        var workspaceRoot = Path.GetFullPath(workspace);
        var memoryDirectory = Path.Combine(workspaceRoot, "memory");

        Directory.CreateDirectory(workspaceRoot);
        Directory.CreateDirectory(memoryDirectory);

        EnsureFile(Path.Combine(workspaceRoot, "SOUL.md"));
        EnsureFile(Path.Combine(workspaceRoot, "USER.md"));
        EnsureFile(Path.Combine(workspaceRoot, "HEARTBEAT.md"));
        EnsureFile(Path.Combine(memoryDirectory, "MEMORY.md"));
        EnsureFile(Path.Combine(memoryDirectory, "history.jsonl"));
        EnsureFile(Path.Combine(memoryDirectory, ".dream_cursor"), "0");
    }

    private static void EnsureFile(string path, string defaultContent = "")
    {
        if (File.Exists(path))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, defaultContent);
    }
}
