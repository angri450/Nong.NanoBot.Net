using System.Text;
using Nanobot.Core.Models;
using Nanobot.Core.Providers;

namespace Nanobot.Core.Memory;

public class DreamConsolidator
{
    private const int DefaultMaxEntries = 20;

    private readonly IWritableMemory _memory;
    private readonly ILLMProvider _provider;

    public DreamConsolidator(IWritableMemory memory, ILLMProvider provider)
    {
        _memory = memory;
        _provider = provider;
    }

    public async Task<DreamResult> RunOnceAsync(
        int maxEntries = DefaultMaxEntries,
        CancellationToken cancellationToken = default)
    {
        var cursor = _memory.GetDreamCursor();
        var window = _memory.ReadHistoryAfterCursor(cursor, maxEntries);
        if (window.Entries.Count == 0)
        {
            return DreamResult.Skipped(cursor);
        }

        var prompt = BuildPrompt(window.Entries, _memory.GetMemoryContent());
        var response = await _provider.ChatAsync(new List<Message>
        {
            new("system", "You consolidate chat history into durable Markdown memory. Return only MEMORY.md content."),
            new("user", prompt)
        });

        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        if (response.FinishReason.Equals("error", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(response.Content))
        {
            return DreamResult.Failed(cursor, response.Content ?? "Dream provider returned no content.");
        }

        _memory.WriteMemory(response.Content.Trim() + Environment.NewLine);
        _memory.SetDreamCursor(window.NextCursor);
        return DreamResult.Completed(window.NextCursor, window.Entries.Count);
    }

    private static string BuildPrompt(IReadOnlyList<MemoryHistoryEntry> entries, string existingMemory)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Existing MEMORY.md:");
        builder.AppendLine(string.IsNullOrWhiteSpace(existingMemory) ? "(empty)" : existingMemory);
        builder.AppendLine();
        builder.AppendLine("New chat history entries:");
        foreach (var entry in entries)
        {
            builder.Append('[')
                .Append(entry.Timestamp.ToString("O"))
                .Append("] ")
                .Append(entry.SessionId)
                .Append(' ')
                .Append(entry.Role)
                .Append(": ")
                .AppendLine(entry.Content);
        }

        builder.AppendLine();
        builder.AppendLine("Rewrite MEMORY.md as concise Markdown bullets. Keep stable user preferences, durable facts, and project decisions. Drop transient chatter.");
        return builder.ToString();
    }
}

public record DreamResult
{
    public required string Status { get; init; }

    public long Cursor { get; init; }

    public int ProcessedEntries { get; init; }

    public string? Error { get; init; }

    public static DreamResult Completed(long cursor, int processedEntries)
    {
        return new DreamResult
        {
            Status = "completed",
            Cursor = cursor,
            ProcessedEntries = processedEntries
        };
    }

    public static DreamResult Skipped(long cursor)
    {
        return new DreamResult
        {
            Status = "skipped",
            Cursor = cursor
        };
    }

    public static DreamResult Failed(long cursor, string error)
    {
        return new DreamResult
        {
            Status = "failed",
            Cursor = cursor,
            Error = error
        };
    }
}
