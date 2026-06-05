using Nanobot.Core.Memory;

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

    private static string CreateWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "nanobot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
