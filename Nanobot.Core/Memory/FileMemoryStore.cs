namespace Nanobot.Core.Memory;

public class FileMemoryStore : IWorkspaceMemory
{
    private readonly string _memoryFile;

    public FileMemoryStore(string workspace)
    {
        if (string.IsNullOrWhiteSpace(workspace))
        {
            throw new ArgumentException("Workspace path is required.", nameof(workspace));
        }

        Workspace = workspace;
        MemoryDirectory = Path.Combine(workspace, "memory");
        _memoryFile = Path.Combine(MemoryDirectory, "MEMORY.md");

        Directory.CreateDirectory(MemoryDirectory);
    }

    public string Workspace { get; }

    public string MemoryDirectory { get; }

    public string GetContext()
    {
        if (!File.Exists(_memoryFile))
        {
            return string.Empty;
        }

        var memory = File.ReadAllText(_memoryFile);
        if (string.IsNullOrWhiteSpace(memory))
        {
            return string.Empty;
        }

        return $"## Long-term Memory\n{memory}";
    }
}
