using System.Text.Json;

namespace Nanobot.Web;

public sealed class WebSessionStore
{
    private const string DefaultSessionTitle = "NanoBot Session";
    private const int MaxSessions = 50;
    private const int MaxMessagesPerSession = 200;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly object _lock = new();
    private readonly string _storageFile;
    private readonly List<StoredSession> _sessions;

    public WebSessionStore(string workspace)
    {
        var storageDirectory = Path.Combine(workspace, ".webui");
        Directory.CreateDirectory(storageDirectory);
        _storageFile = Path.Combine(storageDirectory, "sessions.json");
        _sessions = LoadSessions();
    }

    public IReadOnlyList<WebSessionSummary> List()
    {
        lock (_lock)
        {
            return _sessions
                .OrderByDescending(session => session.UpdatedAt)
                .Select(ToSummary)
                .ToList();
        }
    }

    public WebSessionDto Create(string? title = null)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var session = new StoredSession
            {
                Id = $"web-{now.ToUnixTimeMilliseconds()}-{Guid.NewGuid():N}",
                Title = string.IsNullOrWhiteSpace(title) ? DefaultSessionTitle : title.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            _sessions.Insert(0, session);
            TrimSessions();
            SaveLocked();
            return ToDto(session);
        }
    }

    public WebSessionDto GetOrCreate(string? sessionId)
    {
        lock (_lock)
        {
            var session = string.IsNullOrWhiteSpace(sessionId)
                ? null
                : _sessions.FirstOrDefault(item => item.Id.Equals(sessionId, StringComparison.Ordinal));

            if (session is not null)
            {
                return ToDto(session);
            }
        }

        return Create();
    }

    public bool Delete(string sessionId)
    {
        lock (_lock)
        {
            var session = _sessions.FirstOrDefault(item => item.Id.Equals(sessionId, StringComparison.Ordinal));
            if (session is null) return false;
            _sessions.Remove(session);
            SaveLocked();
            return true;
        }
    }

    public WebSessionDto? Get(string sessionId)
    {
        lock (_lock)
        {
            var session = _sessions.FirstOrDefault(item => item.Id.Equals(sessionId, StringComparison.Ordinal));
            return session is null ? null : ToDto(session);
        }
    }

    public WebSessionDto AppendMessage(string sessionId, string role, string content, string? reasoning = null)
    {
        lock (_lock)
        {
            var session = GetOrCreateStoredSessionLocked(sessionId);
            var timestamp = DateTimeOffset.UtcNow;
            session.Messages.Add(new StoredMessage
            {
                Id = Guid.NewGuid().ToString("N"),
                Role = role,
                Content = content,
                Reasoning = reasoning,
                CreatedAt = timestamp
            });

            if (role.Equals("user", StringComparison.OrdinalIgnoreCase) && ShouldRetitle(session))
            {
                session.Title = CreateTitle(content);
            }

            TouchSessionLocked(session, timestamp);
            return ToDto(session);
        }
    }

    public WebChatMessageDto CreateMessage(string sessionId, string role, string content = "", string? reasoning = null)
    {
        lock (_lock)
        {
            var session = GetOrCreateStoredSessionLocked(sessionId);
            var timestamp = DateTimeOffset.UtcNow;
            var message = new StoredMessage
            {
                Id = Guid.NewGuid().ToString("N"),
                Role = role,
                Content = content,
                Reasoning = reasoning,
                CreatedAt = timestamp
            };
            session.Messages.Add(message);
            TouchSessionLocked(session, timestamp);
            return ToDto(message);
        }
    }

    public WebSessionDto AppendToMessage(
        string sessionId,
        string messageId,
        string? contentDelta = null,
        string? reasoningDelta = null)
    {
        lock (_lock)
        {
            var session = GetExistingSessionLocked(sessionId);
            var message = GetExistingMessageLocked(session, messageId);
            if (!string.IsNullOrEmpty(contentDelta))
            {
                message.Content += contentDelta;
            }

            if (!string.IsNullOrEmpty(reasoningDelta))
            {
                message.Reasoning = (message.Reasoning ?? string.Empty) + reasoningDelta;
            }

            TouchSessionLocked(session, DateTimeOffset.UtcNow);
            return ToDto(session);
        }
    }

    public WebSessionDto UpdateMessage(
        string sessionId,
        string messageId,
        string role,
        string content,
        string? reasoning = null)
    {
        lock (_lock)
        {
            var session = GetExistingSessionLocked(sessionId);
            var message = GetExistingMessageLocked(session, messageId);
            message.Role = role;
            message.Content = content;
            message.Reasoning = reasoning;
            TouchSessionLocked(session, DateTimeOffset.UtcNow);
            return ToDto(session);
        }
    }

    private List<StoredSession> LoadSessions()
    {
        if (!File.Exists(_storageFile))
        {
            return new List<StoredSession>();
        }

        try
        {
            var store = JsonSerializer.Deserialize<StoredSessionStore>(
                File.ReadAllText(_storageFile),
                JsonOptions);
            return store?.Sessions ?? new List<StoredSession>();
        }
        catch
        {
            return new List<StoredSession>();
        }
    }

    private void SaveLocked()
    {
        var store = new StoredSessionStore { Sessions = _sessions };
        var tempFile = _storageFile + ".tmp";
        File.WriteAllText(tempFile, JsonSerializer.Serialize(store, JsonOptions));
        if (File.Exists(_storageFile))
        {
            File.Replace(tempFile, _storageFile, null);
        }
        else
        {
            File.Move(tempFile, _storageFile);
        }
    }

    private void TrimSessions()
    {
        if (_sessions.Count > MaxSessions)
        {
            _sessions.RemoveRange(MaxSessions, _sessions.Count - MaxSessions);
        }
    }

    private StoredSession GetOrCreateStoredSessionLocked(string sessionId)
    {
        var session = _sessions.FirstOrDefault(item => item.Id.Equals(sessionId, StringComparison.Ordinal));
        if (session is not null)
        {
            return session;
        }

        var now = DateTimeOffset.UtcNow;
        session = new StoredSession
        {
            Id = sessionId,
            Title = DefaultSessionTitle,
            CreatedAt = now,
            UpdatedAt = now
        };
        _sessions.Insert(0, session);
        return session;
    }

    private StoredSession GetExistingSessionLocked(string sessionId)
    {
        return _sessions.FirstOrDefault(item => item.Id.Equals(sessionId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");
    }

    private static StoredMessage GetExistingMessageLocked(StoredSession session, string messageId)
    {
        return session.Messages.FirstOrDefault(item => item.Id.Equals(messageId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Message '{messageId}' was not found.");
    }

    private void TouchSessionLocked(StoredSession session, DateTimeOffset timestamp)
    {
        session.UpdatedAt = timestamp;

        if (session.Messages.Count > MaxMessagesPerSession)
        {
            session.Messages.RemoveRange(0, session.Messages.Count - MaxMessagesPerSession);
        }

        _sessions.Remove(session);
        _sessions.Insert(0, session);
        TrimSessions();
        SaveLocked();
    }

    private static bool ShouldRetitle(StoredSession session)
    {
        return session.Messages.Count <= 1
            || session.Title.Equals(DefaultSessionTitle, StringComparison.OrdinalIgnoreCase)
            || session.Title.Equals("Nong Session", StringComparison.OrdinalIgnoreCase)
            || session.Title.StartsWith("Session ", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTitle(string content)
    {
        var normalized = string.Join(
            " ",
            content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return DefaultSessionTitle;
        }

        return normalized.Length <= 48 ? normalized : normalized[..45] + "...";
    }

    private static WebSessionSummary ToSummary(StoredSession session)
    {
        return new WebSessionSummary(
            session.Id,
            NormalizeTitle(session.Title),
            session.CreatedAt,
            session.UpdatedAt,
            session.Messages.Count);
    }

    private static WebSessionDto ToDto(StoredSession session)
    {
        return new WebSessionDto(
            session.Id,
            NormalizeTitle(session.Title),
            session.CreatedAt,
            session.UpdatedAt,
            session.Messages
                .Select(message => new WebChatMessageDto(
                    message.Id,
                    message.Role,
                    message.Content,
                    message.CreatedAt,
                    message.Reasoning))
                .ToList());
    }

    private static WebChatMessageDto ToDto(StoredMessage message)
    {
        return new WebChatMessageDto(
            message.Id,
            message.Role,
            message.Content,
            message.CreatedAt,
            message.Reasoning);
    }

    private static string NormalizeTitle(string title)
    {
        return title.Equals("Nong Session", StringComparison.OrdinalIgnoreCase)
            ? DefaultSessionTitle
            : title;
    }

    private sealed class StoredSessionStore
    {
        public List<StoredSession> Sessions { get; set; } = new();
    }

    private sealed class StoredSession
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = DefaultSessionTitle;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public List<StoredMessage> Messages { get; set; } = new();
    }

    private sealed class StoredMessage
    {
        public string Id { get; set; } = "";
        public string Role { get; set; } = "system";
        public string Content { get; set; } = "";
        public string? Reasoning { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
