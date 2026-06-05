using Nanobot.Core.Heartbeat;

namespace Nanobot.Tests;

public class HeartbeatTests
{
    [Fact]
    public void HasActiveTasks_OnlyMatchesUncheckedActiveTasks()
    {
        Assert.False(HeartbeatService.HasActiveTasks("""
            # HEARTBEAT
            ## Notes
            - [ ] not active
            """));

        Assert.False(HeartbeatService.HasActiveTasks("""
            ## Active Tasks
            - [x] done
            """));

        Assert.True(HeartbeatService.HasActiveTasks("""
            ## Active Tasks
            - [ ] follow up
            """));
    }

    [Fact]
    public async Task TickAsync_InvokesHandlerWhenHeartbeatHasActiveTask()
    {
        var workspace = CreateWorkspace();
        File.WriteAllText(Path.Combine(workspace, "HEARTBEAT.md"), """
            ## Active Tasks
            - [ ] check the queue
            """);
        var called = false;
        var service = new HeartbeatService(workspace, prompt =>
        {
            called = true;
            Assert.Contains("HEARTBEAT.md", prompt);
            return Task.FromResult("done");
        });

        await service.TickAsync();

        Assert.True(called);
    }

    private static string CreateWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "nanobot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
