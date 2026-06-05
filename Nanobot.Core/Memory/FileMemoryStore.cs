using System.Text.Json;

namespace Nanobot.Core.Memory;

public class FileMemoryStore : IWritableMemory
{
    private readonly string _memoryFile;
    private readonly string _soulFile;
    private readonly string _userFile;
    private readonly string _historyFile;
    private readonly string _dreamCursorFile;
    private readonly object _fileLock = new();
    private static readonly JsonSerializerOptions HistoryJsonOptions = new(JsonSerializerDefaults.Web);

    public FileMemoryStore(string workspace)
    {
        if (string.IsNullOrWhiteSpace(workspace))
        {
            throw new ArgumentException("Workspace path is required.", nameof(workspace));
        }

        Workspace = workspace;
        MemoryDirectory = Path.Combine(workspace, "memory");
        _memoryFile = Path.Combine(MemoryDirectory, "MEMORY.md");
        _soulFile = Path.Combine(workspace, "SOUL.md");
        _userFile = Path.Combine(workspace, "USER.md");
        _historyFile = Path.Combine(MemoryDirectory, "history.jsonl");
        _dreamCursorFile = Path.Combine(MemoryDirectory, ".dream_cursor");

        Directory.CreateDirectory(MemoryDirectory);
    }

    public string Workspace { get; }

    public string MemoryDirectory { get; }

    public string MemoryFile => _memoryFile;

    public string HistoryFile => _historyFile;

    public string GetContext()
    {
        var sections = new List<string>();
        AddContextSection(sections, "Soul", _soulFile);
        AddContextSection(sections, "User", _userFile);

        var memory = GetMemoryContent();
        if (!string.IsNullOrWhiteSpace(memory))
        {
            sections.Add($"## Long-term Memory\n{memory}");
        }

        return sections.Count == 0 ? string.Empty : string.Join("\n\n", sections);
    }

    public string GetMemoryContent()
    {
        lock (_fileLock)
        {
            if (!File.Exists(_memoryFile))
            {
                return string.Empty;
            }

            var memory = File.ReadAllText(_memoryFile);
            return string.IsNullOrWhiteSpace(memory) ? string.Empty : memory.Trim();
        }
    }

    public void WriteMemory(string content)
    {
        WriteText(_memoryFile, content);
    }

    public void AppendMemory(string content)
    {
        var normalized = NormalizeMemoryBlock(content);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        lock (_fileLock)
        {
            var existing = File.Exists(_memoryFile)
                ? File.ReadAllText(_memoryFile).TrimEnd()
                : string.Empty;
            var separator = string.IsNullOrWhiteSpace(existing) ? string.Empty : "\n\n";
            WriteTextLocked(_memoryFile, existing + separator + normalized + "\n");
        }
    }

    public void WriteSoul(string content)
    {
        WriteText(_soulFile, content);
    }

    public void WriteUser(string content)
    {
        WriteText(_userFile, content);
    }

    public void AppendHistory(MemoryHistoryEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Content))
        {
            return;
        }

        lock (_fileLock)
        {
            Directory.CreateDirectory(MemoryDirectory);
            var json = JsonSerializer.Serialize(entry, HistoryJsonOptions);
            File.AppendAllText(_historyFile, json + Environment.NewLine);
        }
    }

    public MemoryHistoryWindow ReadHistoryAfterCursor(long cursor, int maxEntries = 20)
    {
        if (cursor < 0)
        {
            cursor = 0;
        }

        lock (_fileLock)
        {
            if (!File.Exists(_historyFile))
            {
                return new MemoryHistoryWindow(Array.Empty<MemoryHistoryEntry>(), cursor);
            }

            var entries = new List<MemoryHistoryEntry>();
            long nextCursor = 0;
            foreach (var line in File.ReadLines(_historyFile))
            {
                nextCursor++;
                if (nextCursor <= cursor || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    var entry = JsonSerializer.Deserialize<MemoryHistoryEntry>(line, HistoryJsonOptions);
                    if (entry is not null)
                    {
                        entries.Add(entry);
                    }
                }
                catch
                {
                    // Ignore malformed historical rows so one bad append does not stop Dream.
                }

                if (entries.Count >= maxEntries)
                {
                    break;
                }
            }

            return new MemoryHistoryWindow(entries, nextCursor);
        }
    }

    public long GetDreamCursor()
    {
        lock (_fileLock)
        {
            if (!File.Exists(_dreamCursorFile))
            {
                return 0;
            }

            return long.TryParse(File.ReadAllText(_dreamCursorFile).Trim(), out var cursor)
                ? Math.Max(0, cursor)
                : 0;
        }
    }

    public void SetDreamCursor(long cursor)
    {
        lock (_fileLock)
        {
            WriteTextLocked(_dreamCursorFile, Math.Max(0, cursor).ToString());
        }
    }

    private void WriteText(string path, string content)
    {
        lock (_fileLock)
        {
            WriteTextLocked(path, content);
        }
    }

    private static void WriteTextLocked(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, content ?? string.Empty);
        if (File.Exists(path))
        {
            File.Replace(tempPath, path, null);
        }
        else
        {
            File.Move(tempPath, path);
        }
    }

    private static string NormalizeMemoryBlock(string content)
    {
        var trimmed = content.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        return trimmed.StartsWith("-", StringComparison.Ordinal)
            || trimmed.StartsWith("#", StringComparison.Ordinal)
            ? trimmed
            : "- " + trimmed;
    }

    private static void AddContextSection(List<string> sections, string title, string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var content = File.ReadAllText(path).Trim();
        if (!string.IsNullOrWhiteSpace(content))
        {
            sections.Add($"## {title}\n{content}");
        }
    }
}
