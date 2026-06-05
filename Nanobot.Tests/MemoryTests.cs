using Nanobot.Core.Memory;
using Nanobot.Core.Models;
using Nanobot.Core.Providers;
using Nanobot.Core.Tools.Builtin;
using System.Text.Json.Nodes;

namespace Nanobot.Tests;

public class MemoryTests
{
    [Fact]
    public void FileMemoryStore_CreatesMemoryDirectory()
    {
        var workspace = CreateWorkspace();

        var store = new FileMemoryStore(workspace);

        Assert.True(Directory.Exists(store.MemoryDirectory));
    }

    [Fact]
    public void FileMemoryStore_GetContext_ReturnsEmptyWhenMemoryFileMissing()
    {
        var store = new FileMemoryStore(CreateWorkspace());

        var context = store.GetContext();

        Assert.Equal(string.Empty, context);
    }

    [Fact]
    public void FileMemoryStore_GetContext_ReturnsEmptyWhenMemoryFileIsBlank()
    {
        var store = new FileMemoryStore(CreateWorkspace());
        File.WriteAllText(Path.Combine(store.MemoryDirectory, "MEMORY.md"), "  \r\n\t");

        var context = store.GetContext();

        Assert.Equal(string.Empty, context);
    }

    [Fact]
    public void FileMemoryStore_GetContext_ReturnsLongTermMemoryContext()
    {
        var store = new FileMemoryStore(CreateWorkspace());
        File.WriteAllText(Path.Combine(store.MemoryDirectory, "MEMORY.md"), "User prefers concise answers.");

        var context = store.GetContext();

        Assert.Equal("## Long-term Memory\nUser prefers concise answers.", context);
    }

    [Fact]
    public async Task MemoryTool_AppendsMemory()
    {
        var store = new FileMemoryStore(CreateWorkspace());
        var tool = new MemoryTool(store);

        var result = await tool.ExecuteAsync(JsonNode.Parse("""{"content":"User prefers direct answers."}"""));

        Assert.Equal("Memory saved.", result);
        Assert.Contains("- User prefers direct answers.", File.ReadAllText(store.MemoryFile));
    }

    [Fact]
    public void FileMemoryStore_HistoryCursorReadsOnlyNewEntries()
    {
        var store = new FileMemoryStore(CreateWorkspace());
        store.AppendHistory(new MemoryHistoryEntry { SessionId = "a", Role = "user", Content = "first" });
        store.AppendHistory(new MemoryHistoryEntry { SessionId = "a", Role = "assistant", Content = "second" });

        var firstWindow = store.ReadHistoryAfterCursor(0, maxEntries: 1);
        var secondWindow = store.ReadHistoryAfterCursor(firstWindow.NextCursor, maxEntries: 10);

        Assert.Single(firstWindow.Entries);
        Assert.Equal("first", firstWindow.Entries[0].Content);
        Assert.Single(secondWindow.Entries);
        Assert.Equal("second", secondWindow.Entries[0].Content);
    }

    [Fact]
    public async Task DreamConsolidator_RewritesMemoryAndAdvancesCursor()
    {
        var store = new FileMemoryStore(CreateWorkspace());
        store.WriteMemory("- Existing memory\n");
        store.AppendHistory(new MemoryHistoryEntry { SessionId = "default", Role = "user", Content = "Remember I use metric units." });
        var provider = new FakeProvider(new LLMResponse("- Existing memory\n- User uses metric units."));
        var dream = new DreamConsolidator(store, provider);

        var result = await dream.RunOnceAsync();

        Assert.Equal("completed", result.Status);
        Assert.Equal(1, result.ProcessedEntries);
        Assert.Equal(1, store.GetDreamCursor());
        Assert.Contains("metric units", store.GetMemoryContent());
        Assert.Contains("Existing MEMORY.md", provider.LastMessages[1].Content);
    }

    private static string CreateWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "nanobot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeProvider : ILLMProvider
    {
        private readonly LLMResponse _response;

        public FakeProvider(LLMResponse response)
        {
            _response = response;
        }

        public List<Message> LastMessages { get; private set; } = new();

        public Task<LLMResponse> ChatAsync(
            List<Message> messages,
            List<JsonNode>? tools = null,
            string? model = null,
            int maxTokens = 4096,
            double temperature = 0.7)
        {
            LastMessages = messages;
            return Task.FromResult(_response);
        }

        public string GetDefaultModel() => "fake";
    }
}
