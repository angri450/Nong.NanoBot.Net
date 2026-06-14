using Nanobot.Core.Events;
using Nanobot.Core.Sessions;

namespace Nanobot.Tests;

public class JsonlSessionStoreTests
{
    [Fact]
    public async Task ReadItemsAsync_FiltersBySequenceWithinThread()
    {
        var basePath = CreateBasePath();
        var bus = new RuntimeEventBus();
        var store = new JsonlSessionStore(basePath, bus);

        await bus.PublishAsync(new RuntimeEvent
        {
            Type = RuntimeEventType.RunStarted,
            RunId = "run-1",
            SessionId = "session-a",
            ThreadId = "thread-a",
            Content = "first"
        });

        await bus.PublishAsync(new RuntimeEvent
        {
            Type = RuntimeEventType.ContentCompleted,
            RunId = "run-1",
            SessionId = "session-a",
            ThreadId = "thread-a",
            Content = "second"
        });

        var items = await ReadAllAsync(store.ReadItemsAsync("session-a", "thread-a", sinceSequence: 1));

        Assert.Single(items);
        Assert.Equal(2, items[0].Sequence);
        Assert.Equal("second", items[0].Content);
    }

    [Fact]
    public async Task ReadItemsSinceSequenceAsync_ReplaysAcrossSessionsInSequenceOrder()
    {
        var basePath = CreateBasePath();
        var bus = new RuntimeEventBus();
        var store = new JsonlSessionStore(basePath, bus);

        await bus.PublishAsync(new RuntimeEvent
        {
            Type = RuntimeEventType.RunStarted,
            RunId = "run-1",
            SessionId = "session-a",
            Content = "a1"
        });

        await bus.PublishAsync(new RuntimeEvent
        {
            Type = RuntimeEventType.RunStarted,
            RunId = "run-2",
            SessionId = "session-b",
            Content = "b1"
        });

        await bus.PublishAsync(new RuntimeEvent
        {
            Type = RuntimeEventType.ContentCompleted,
            RunId = "run-1",
            SessionId = "session-a",
            Content = "a2"
        });

        var items = await ReadAllAsync(store.ReadItemsSinceSequenceAsync(1));

        Assert.Equal(2, items.Count);
        Assert.Collection(items,
            item =>
            {
                Assert.Equal(2, item.Sequence);
                Assert.Equal("session-b", item.SessionId);
                Assert.Equal("b1", item.Content);
            },
            item =>
            {
                Assert.Equal(3, item.Sequence);
                Assert.Equal("session-a", item.SessionId);
                Assert.Equal("a2", item.Content);
            });
    }

    [Fact]
    public async Task ReadItemsSinceSequenceAsync_PreservesToolFailureMetadata()
    {
        var basePath = CreateBasePath();
        var bus = new RuntimeEventBus();
        var store = new JsonlSessionStore(basePath, bus);

        await bus.PublishAsync(new RuntimeEvent
        {
            Type = RuntimeEventType.ToolFailed,
            RunId = "run-9",
            SessionId = "session-tools",
            ThreadId = "thread-tools",
            ToolName = "nong.word",
            ToolCallId = "tool-call-1",
            ErrorMessage = "tool crashed"
        });

        var items = await ReadAllAsync(store.ReadItemsSinceSequenceAsync(0));

        Assert.Single(items);
        Assert.Equal(RuntimeEventType.ToolFailed, items[0].EventType);
        Assert.Equal("tool crashed", items[0].ErrorMessage);
        Assert.Equal("nong.word", items[0].ToolName);
        Assert.Equal("tool-call-1", items[0].ToolCallId);
        Assert.Equal("run-9", items[0].RunId);
    }

    [Fact]
    public async Task ReadMaxSequence_ReturnsHighestPersistedSequence()
    {
        var basePath = CreateBasePath();
        var bus = new RuntimeEventBus();
        _ = new JsonlSessionStore(basePath, bus);

        await bus.PublishAsync(new RuntimeEvent
        {
            Type = RuntimeEventType.RunStarted,
            RunId = "run-1",
            SessionId = "session-a",
            Content = "a1"
        });

        await bus.PublishAsync(new RuntimeEvent
        {
            Type = RuntimeEventType.RunCompleted,
            RunId = "run-1",
            SessionId = "session-a",
            Content = "a2"
        });

        var maxSequence = JsonlSessionStore.ReadMaxSequence(basePath);

        Assert.Equal(2, maxSequence);
    }

    private static async Task<List<SessionItem>> ReadAllAsync(IAsyncEnumerable<SessionItem> items)
    {
        var result = new List<SessionItem>();
        await foreach (var item in items)
        {
            result.Add(item);
        }

        return result;
    }

    private static string CreateBasePath()
    {
        var path = Path.Combine(Path.GetTempPath(), "nanobot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
