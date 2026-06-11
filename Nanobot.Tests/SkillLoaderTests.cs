using Nanobot.Core.Agent;
using Nanobot.Core.Skills;

namespace Nanobot.Tests;

public class SkillLoaderTests
{
    // ===== Phase 0: legacy full-load (unchanged behavior) =====

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

    // ===== Phase 1: skill catalog =====

    [Fact]
    public void GetCatalog_ReturnsEmptyWhenNoSkillsDirectory()
    {
        var loader = new SkillLoader();
        var catalog = loader.GetCatalog(CreateWorkspace());
        Assert.Equal(0, catalog.TotalCount);
        Assert.Empty(catalog.Skills);
    }

    [Fact]
    public void GetCatalog_ReturnsEntriesWithDescriptions()
    {
        var workspace = CreateWorkspace();
        WriteSkill(workspace, "word", "---\nname: word\ndescription: Word document processing\n---\n\n# Word\n\nUse nong word.");
        WriteSkill(workspace, "pdf", "---\nname: pdf\ndescription: PDF slicing\n---\n\n# PDF\n\nUse nong pdf.");
        var loader = new SkillLoader();

        var catalog = loader.GetCatalog(workspace);

        Assert.Equal(2, catalog.TotalCount);
        Assert.Contains(catalog.Skills, s => s.Name == "word" && s.Description.Contains("Word document"));
        Assert.Contains(catalog.Skills, s => s.Name == "pdf" && s.Description.Contains("PDF slicing"));
    }

    [Fact]
    public void GetCatalog_DetectsReferencesAndExamples()
    {
        var workspace = CreateWorkspace();
        var skillDir = Path.Combine(workspace, "skills", "test");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), "---\nname: test\ndescription: Test skill\n---\n# Test");

        // Create references and examples
        Directory.CreateDirectory(Path.Combine(skillDir, "references"));
        File.WriteAllText(Path.Combine(skillDir, "references", "guide.md"), "Reference content");
        Directory.CreateDirectory(Path.Combine(skillDir, "examples"));
        File.WriteAllText(Path.Combine(skillDir, "examples", "success.md"), "Example");

        var loader = new SkillLoader();
        var catalog = loader.GetCatalog(workspace);

        var entry = catalog.Skills.Single();
        Assert.True(entry.HasReferences);
        Assert.True(entry.HasExamples);
        Assert.Equal(1, entry.ReferenceCount);
    }

    [Fact]
    public void GetCatalogContext_ReturnsCompactIndex()
    {
        var workspace = CreateWorkspace();
        WriteSkill(workspace, "word", "---\nname: word\ndescription: Word processing\n---\n# Word");
        WriteSkill(workspace, "pdf", "---\nname: pdf\ndescription: PDF slicing\n---\n# PDF");
        var loader = new SkillLoader();

        var context = loader.GetCatalogContext(workspace);

        Assert.Contains("Available Skills", context);
        Assert.Contains("load_skill(name)", context);
        Assert.Contains("word", context);
        Assert.Contains("pdf", context);
        Assert.True(context.Length <= 600); // compact
    }

    [Fact]
    public void GetCatalogContext_TruncatesAtMaxChars()
    {
        var workspace = CreateWorkspace();
        WriteSkill(workspace, "huge", "---\nname: huge\ndescription: " + new string('x', 500) + "\n---\n# Huge");
        var loader = new SkillLoader();

        var context = loader.GetCatalogContext(workspace, 100);

        Assert.True(context.Length > 90);
        Assert.Contains("truncated", context);
    }

    // ===== Phase 2: load specific skill =====

    [Fact]
    public void LoadSkill_ReturnsNotFoundForMissingSkill()
    {
        var loader = new SkillLoader();
        var result = loader.LoadSkill(CreateWorkspace(), "nonexistent");
        Assert.False(result.Found);
        Assert.Equal("nonexistent", result.SkillName);
    }

    [Fact]
    public void LoadSkill_ReturnsBodyContentWithoutFrontmatter()
    {
        var workspace = CreateWorkspace();
        WriteSkill(workspace, "insp", "---\nname: insp\ndescription: Inspection\n---\n\n# Inspect\n\nUse nong inspect.");
        var loader = new SkillLoader();

        var result = loader.LoadSkill(workspace, "insp");

        Assert.True(result.Found);
        Assert.Contains("# Inspect", result.SklContent);
        Assert.Contains("nong inspect", result.SklContent);
        Assert.DoesNotContain("---", result.SklContent); // frontmatter stripped
    }

    [Fact]
    public void LoadSkill_ListsAvailableReferences()
    {
        var workspace = CreateWorkspace();
        var skillDir = Path.Combine(workspace, "skills", "test");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), "---\nname: test\ndescription: T\n---\n# Test");
        Directory.CreateDirectory(Path.Combine(skillDir, "references"));
        File.WriteAllText(Path.Combine(skillDir, "references", "a.md"), "A");
        File.WriteAllText(Path.Combine(skillDir, "references", "b.md"), "B");

        var loader = new SkillLoader();
        var result = loader.LoadSkill(workspace, "test");

        Assert.Contains("a.md", result.References);
        Assert.Contains("b.md", result.References);
        Assert.Equal(2, result.References.Count);
    }

    // ===== Phase 3: load reference =====

    [Fact]
    public void LoadReference_ReturnsContentForValidReference()
    {
        var workspace = CreateWorkspace();
        var skillDir = Path.Combine(workspace, "skills", "test");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), "---\nname: test\ndescription: T\n---\n# Test");
        Directory.CreateDirectory(Path.Combine(skillDir, "references"));
        File.WriteAllText(Path.Combine(skillDir, "references", "guide.md"), "This is the guide content.");

        var loader = new SkillLoader();
        var content = loader.LoadReference(workspace, "test", "guide.md");

        Assert.Contains("guide content", content);
    }

    [Fact]
    public void LoadReference_ReturnsEmptyForMissingReference()
    {
        var workspace = CreateWorkspace();
        var skillDir = Path.Combine(workspace, "skills", "test");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), "---\nname: test\ndescription: T\n---\n# Test");

        var loader = new SkillLoader();
        var content = loader.LoadReference(workspace, "test", "missing.md");

        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public void GetCatalog_SkillsSortedAlphabetically()
    {
        var workspace = CreateWorkspace();
        WriteSkill(workspace, "chart", "---\nname: chart\ndescription: Charts\n---\n# Chart");
        WriteSkill(workspace, "word", "---\nname: word\ndescription: Word\n---\n# Word");
        WriteSkill(workspace, "excel", "---\nname: excel\ndescription: Excel\n---\n# Excel");
        var loader = new SkillLoader();

        var catalog = loader.GetCatalog(workspace);

        Assert.Equal("chart", catalog.Skills[0].Name);
        Assert.Equal("excel", catalog.Skills[1].Name);
        Assert.Equal("word", catalog.Skills[2].Name);
    }

    // ===== helpers =====

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
