namespace Nanobot.Core.Memory;

public interface IWritableMemory : IWorkspaceMemory
{
    string MemoryFile { get; }

    string HistoryFile { get; }

    string GetMemoryContent();

    void WriteMemory(string content);

    void AppendMemory(string content);

    void WriteSoul(string content);

    void WriteUser(string content);

    void AppendHistory(MemoryHistoryEntry entry);

    MemoryHistoryWindow ReadHistoryAfterCursor(long cursor, int maxEntries = 20);

    long GetDreamCursor();

    void SetDreamCursor(long cursor);
}

public record MemoryHistoryEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public string SessionId { get; init; } = "default";

    public string Role { get; init; } = "user";

    public string Content { get; init; } = "";
}

public record MemoryHistoryWindow(
    IReadOnlyList<MemoryHistoryEntry> Entries,
    long NextCursor
);
