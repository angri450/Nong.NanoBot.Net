using System.Text.Json;
using Nanobot.Core.Events;

namespace Nanobot.Core.Sessions;

public class JsonlSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _basePath;
    private readonly RuntimeEventBus _eventBus;

    public JsonlSessionStore(string basePath, RuntimeEventBus eventBus)
    {
        _basePath = basePath;
        _eventBus = eventBus;
        _eventBus.Subscribe(OnRuntimeEventAsync);
    }

    public static long ReadMaxSequence(string basePath)
    {
        var sessionsPath = Path.Combine(basePath, "sessions");
        if (!Directory.Exists(sessionsPath))
        {
            return 0;
        }

        long maxSequence = 0;
        foreach (var eventsPath in Directory.EnumerateFiles(sessionsPath, "events.jsonl", SearchOption.AllDirectories))
        {
            foreach (var line in File.ReadLines(eventsPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                SessionItem? item;
                try
                {
                    item = JsonSerializer.Deserialize<SessionItem>(line, JsonOptions);
                }
                catch
                {
                    continue;
                }

                if (item?.Sequence > maxSequence)
                {
                    maxSequence = item.Sequence;
                }
            }
        }

        return maxSequence;
    }

    public string GetSessionPath(string sessionId)
    {
        return Path.Combine(_basePath, "sessions", Sanitize(sessionId));
    }

    public string GetThreadPath(string sessionId, string threadId)
    {
        return Path.Combine(GetSessionPath(sessionId), "threads", Sanitize(threadId));
    }

    public async Task SaveSessionJsonAsync(string sessionId, SessionThread thread)
    {
        var path = GetSessionPath(sessionId);
        Directory.CreateDirectory(path);
        var sessionJsonPath = Path.Combine(path, "session.json");
        await File.WriteAllTextAsync(sessionJsonPath,
            JsonSerializer.Serialize(new
            {
                id = sessionId,
                thread.Id,
                thread.Title,
                thread.CreatedAt
            }, JsonOptions));
    }

    public async Task AppendItemAsync(SessionItem item)
    {
        var sessionId = !string.IsNullOrWhiteSpace(item.SessionId)
            ? item.SessionId
            : item.ThreadId;
        var threadId = !string.IsNullOrWhiteSpace(item.ThreadId)
            ? item.ThreadId
            : sessionId;

        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(threadId))
        {
            return;
        }

        var sessionPath = GetSessionPath(sessionId);
        var actualThreadPath = Path.Combine(sessionPath, "threads", Sanitize(threadId));
        Directory.CreateDirectory(actualThreadPath);

        var eventsPath = Path.Combine(actualThreadPath, "events.jsonl");
        var normalizedItem = item with
        {
            SessionId = sessionId,
            ThreadId = threadId
        };
        var line = JsonSerializer.Serialize(normalizedItem, JsonOptions);
        await File.AppendAllTextAsync(eventsPath, line + Environment.NewLine);
    }

    public async IAsyncEnumerable<SessionItem> ReadItemsAsync(
        string sessionId,
        string threadId,
        long? sinceSequence = null)
    {
        var eventsPath = Path.Combine(GetThreadPath(sessionId, threadId), "events.jsonl");
        if (!File.Exists(eventsPath))
        {
            yield break;
        }

        using var reader = new StreamReader(eventsPath);
        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            SessionItem? item;
            try
            {
                item = JsonSerializer.Deserialize<SessionItem>(line, JsonOptions);
            }
            catch
            {
                continue;
            }

            if (item is null)
            {
                continue;
            }

            item = NormalizeItem(item, sessionId, threadId);
            if (!MatchesSinceSequence(item, sinceSequence))
            {
                continue;
            }

            yield return item;
        }
    }

    public async IAsyncEnumerable<SessionItem> ReadItemsSinceSequenceAsync(long sinceSequence)
    {
        var sessionsPath = Path.Combine(_basePath, "sessions");
        if (!Directory.Exists(sessionsPath))
        {
            yield break;
        }

        var items = new List<SessionItem>();
        foreach (var eventsPath in Directory.EnumerateFiles(sessionsPath, "events.jsonl", SearchOption.AllDirectories))
        {
            await foreach (var item in ReadItemsFromFileAsync(eventsPath, sinceSequence))
            {
                items.Add(item);
            }
        }

        var deduplicated = items
            .Where(item => item.Sequence > 0)
            .GroupBy(item => item.Sequence)
            .Select(group => group
                .OrderBy(item => item.Timestamp)
                .ThenBy(item => item.EventId, StringComparer.Ordinal)
                .Last())
            .Concat(items.Where(item => item.Sequence <= 0))
            .OrderBy(item => item.Sequence > 0 ? item.Sequence : long.MaxValue)
            .ThenBy(item => item.Timestamp)
            .ThenBy(item => item.EventId, StringComparer.Ordinal);

        foreach (var item in deduplicated)
        {
            yield return item;
        }
    }

    public async Task WriteSnapshotAsync(string sessionId, string threadId, IEnumerable<SessionItem> items)
    {
        var snapshotPath = Path.Combine(GetThreadPath(sessionId, threadId), "snapshot.json");
        var snapshot = JsonSerializer.Serialize(items, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(snapshotPath, snapshot);
    }

    private async Task OnRuntimeEventAsync(RuntimeEvent evt)
    {
        if (string.IsNullOrEmpty(evt.ThreadId))
        {
            evt = evt with { ThreadId = evt.SessionId };
        }

        var item = SessionItem.FromRuntimeEvent(evt);
        await AppendItemAsync(item);
    }

    private static SessionItem NormalizeItem(SessionItem item, string sessionId, string threadId)
    {
        return item with
        {
            SessionId = string.IsNullOrWhiteSpace(item.SessionId) ? sessionId : item.SessionId,
            ThreadId = string.IsNullOrWhiteSpace(item.ThreadId) ? threadId : item.ThreadId
        };
    }

    private static bool MatchesSinceSequence(SessionItem item, long? sinceSequence)
    {
        if (sinceSequence is null || sinceSequence <= 0)
        {
            return true;
        }

        return item.Sequence > 0 && item.Sequence > sinceSequence;
    }

    private async IAsyncEnumerable<SessionItem> ReadItemsFromFileAsync(string eventsPath, long? sinceSequence)
    {
        using var reader = new StreamReader(eventsPath);
        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            SessionItem? item;
            try
            {
                item = JsonSerializer.Deserialize<SessionItem>(line, JsonOptions);
            }
            catch
            {
                continue;
            }

            if (item is null || !MatchesSinceSequence(item, sinceSequence))
            {
                continue;
            }

            yield return item;
        }
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(sanitized) ? "default" : sanitized;
    }
}
