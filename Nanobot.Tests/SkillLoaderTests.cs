using Nanobot.Core.Agent;
using Nanobot.Core.Skills;

namespace Nanobot.Tests;

public class SkillLoaderTests
{
    [Fact]
    public void SkillLoader_ReturnsEmptyWhenSkillsDirectoryIsMissing()
    {
        var loader = new SkillLoader();

        var context = loader.LoadContext(AgentExecutionContext.CreateRoot(CreateWorkspace()));

        Assert.Equal(string.Empty, context);
    }

    [Fact]
    public void SkillLoader_LoadsSkillFilesSortedByDirectoryName()
    {
        var workspace = CreateWorkspace();
        WriteSkill(workspace, "zeta", "Zeta instructions");
        WriteSkill(workspace, "alpha", "Alpha instructions");
        var loader = new SkillLoader();

        var context = loader.LoadContext(AgentExecutionContext.CreateRoot(workspace));

        Assert.Contains("# Skills", context);
        Assert.True(context.IndexOf("## alpha", StringComparison.Ordinal) < context.IndexOf("## zeta", StringComparison.Ordinal));
        Assert.Contains("Alpha instructions", context);
        Assert.Contains("Zeta instructions", context);
    }

    [Fact]
    public void SkillLoader_TruncatesLongSkillContext()
    {
        var workspace = CreateWorkspace();
        WriteSkill(workspace, "large", new string('x', 200));
        var loader = new SkillLoader(maxContextChars: 40);

        var context = loader.LoadContext(AgentExecutionContext.CreateRoot(workspace));

        Assert.Contains("Skills truncated", context);
        Assert.True(context.Length > 40);
    }

    private static void WriteSkill(string workspace, string name, string content)
    {
        var directory = Path.Combine(workspace, "skills", name);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "SKILL.md"), content);
    }

    private static string CreateWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "nanobot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
