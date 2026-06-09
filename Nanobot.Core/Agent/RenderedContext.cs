using Nanobot.Core.Models;

namespace Nanobot.Core.Agent;

public record RenderedContext
{
    public List<Message> StaticPrefix { get; init; } = new();
    public List<Message> StableHistory { get; init; } = new();
    public List<Message> DynamicTail { get; init; } = new();
    public ContextFingerprint Fingerprint { get; init; } = new();
    public int EstimatedTokenCount { get; init; }
    public double ContextFillRatio { get; init; }
    public int ContextWindow { get; init; } = 1_000_000;

    public List<Message> ToMessages()
    {
        var all = new List<Message>(StaticPrefix.Count + StableHistory.Count + DynamicTail.Count);
        all.AddRange(StaticPrefix);
        all.AddRange(StableHistory);
        all.AddRange(DynamicTail);
        return all;
    }
}
