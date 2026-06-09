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
        if (string.IsNullOrEmpty(item.ThreadId))
        {
            return;
        }

        var threadPath = GetThreadPath(item.ThreadId, item.ThreadId);
        // TODO: derive sessionId from thread — for now use threadId as sessionId
        var sessionPath = GetSessionPath(item.ThreadId);
        var actualThreadPath = Path.Combine(sessionPath, "threads", Sanitize(item.ThreadId));
        Directory.CreateDirectory(actualThreadPath);

        var eventsPath = Path.Combine(actualThreadPath, "events.jsonl");
        var line = JsonSerializer.Serialize(item, JsonOptions);
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
            // For now, use SessionId as ThreadId when ThreadId is not set
            evt = evt with { ThreadId = evt.ThreadId ?? evt.SessionId };
        }

        var item = SessionItem.FromRuntimeEvent(evt);
        await AppendItemAsync(item);
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(sanitized) ? "default" : sanitized;
    }
}
