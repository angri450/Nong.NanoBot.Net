using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nanobot.Core.Agent;

namespace Nanobot.Core.Skills;

public class SkillLoader
{
    public const int DefaultMaxContextChars = 12000;
    public const int DefaultIndexMaxChars = 500;

    public SkillLoader(int maxContextChars = DefaultMaxContextChars)
    {
        MaxContextChars = maxContextChars;
    }

    public int MaxContextChars { get; }

    // ===== Phase 0 (legacy): full load (kept for backward compatibility) =====

    public string LoadContext(AgentExecutionContext context)
    {
        return LoadContext(context.Workspace);
    }

    public string LoadContext(string workspace)
    {
        if (string.IsNullOrWhiteSpace(workspace))
            return string.Empty;

        var skillsDirectory = Path.Combine(workspace, "skills");
        if (!Directory.Exists(skillsDirectory))
            return string.Empty;

        var sections = Directory.GetDirectories(skillsDirectory)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(BuildSkillSection)
            .Where(section => !string.IsNullOrWhiteSpace(section))
            .ToList();

        if (sections.Count == 0)
            return string.Empty;

        var builder = new StringBuilder("# Skills");
        foreach (var section in sections)
        {
            builder.AppendLine();
            builder.AppendLine();
            builder.Append(section);
        }

        return Truncate(builder.ToString());
    }

    // ===== Phase 1: skill index (compact catalog) =====

    public SkillCatalog GetCatalog(string workspace)
    {
        var skillsDirectory = Path.Combine(workspace, "skills");
        if (!Directory.Exists(skillsDirectory))
            return new SkillCatalog();

        var entries = Directory.GetDirectories(skillsDirectory)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(dir =>
            {
                var name = Path.GetFileName(dir);
                var skillFile = Path.Combine(dir, "SKILL.md");
                if (!File.Exists(skillFile))
                    return null;

                var (frontmatter, _) = ParseFrontmatter(skillFile);
                var description = frontmatter?.ContainsKey("description") == true
                    ? frontmatter["description"]
                    : "";
                var hasReferences = Directory.Exists(Path.Combine(dir, "references"))
                    && Directory.GetFiles(Path.Combine(dir, "references"), "*.md").Length > 0;
                var hasExamples = Directory.Exists(Path.Combine(dir, "examples"))
                    && Directory.GetFiles(Path.Combine(dir, "examples"), "*.md").Length > 0;
                var refCount = hasReferences
                    ? Directory.GetFiles(Path.Combine(dir, "references"), "*.md").Length
                    : 0;

                return new SkillEntry(name, description, hasReferences, hasExamples, refCount);
            })
            .Where(e => e != null)
            .Select(e => e!)
            .ToList();

        return new SkillCatalog
        {
            Skills = entries,
            TotalCount = entries.Count
        };
    }

    public string GetCatalogContext(string workspace, int maxChars = DefaultIndexMaxChars)
    {
        var catalog = GetCatalog(workspace);
        if (catalog.TotalCount == 0) return string.Empty;

        var sb = new StringBuilder("# Available Skills (use load_skill(name) to read one)");
        sb.AppendLine();
        foreach (var s in catalog.Skills)
        {
            var line = $"- **{s.Name}**: {s.Description}";
            if (line.Length > 120) line = line[..117] + "...";
            sb.AppendLine(line);
        }

        var result = sb.ToString();
        if (maxChars > 0 && result.Length > maxChars)
            result = result[..maxChars] + "\n... (Skill index truncated)";
        return result;
    }

    // ===== Phase 2: load specific skill =====

    public SkillLoadResult LoadSkill(string workspace, string skillName)
    {
        var skillsDirectory = Path.Combine(workspace, "skills");
        if (!Directory.Exists(skillsDirectory))
            return SkillLoadResult.NotFound(skillName);

        var skillDir = Directory.GetDirectories(skillsDirectory)
            .FirstOrDefault(d => string.Equals(Path.GetFileName(d), skillName, StringComparison.OrdinalIgnoreCase));

        if (skillDir == null)
            return SkillLoadResult.NotFound(skillName);

        var skillFile = Path.Combine(skillDir, "SKILL.md");
        if (!File.Exists(skillFile))
            return SkillLoadResult.NotFound(skillName);

        var content = File.ReadAllText(skillFile).Trim();
        var (frontmatter, body) = ParseFrontmatter(skillFile);

        var references = new List<string>();
        var refsDir = Path.Combine(skillDir, "references");
        if (Directory.Exists(refsDir))
            references = Directory.GetFiles(refsDir, "*.md")
                .Select(Path.GetFileName)
                .Select(f => f!)
                .ToList();

        foreach (var linkedReference in FindLinkedMarkdownReferences(body ?? content, skillDir, skillsDirectory))
        {
            if (!references.Contains(linkedReference, StringComparer.OrdinalIgnoreCase))
            {
                references.Add(linkedReference);
            }
        }

        var examples = new List<string>();
        var exDir = Path.Combine(skillDir, "examples");
        if (Directory.Exists(exDir))
            examples = Directory.GetFiles(exDir, "*.md")
                .Select(Path.GetFileName)
                .Select(f => f!)
                .ToList();

        return SkillLoadResult.Ok(skillName, body ?? content, references, examples);
    }

    // ===== Phase 3: load reference (progressive disclosure) =====

    public string LoadReference(string workspace, string skillName, string referenceFile)
    {
        var skillsDirectory = Path.Combine(workspace, "skills");
        if (!Directory.Exists(skillsDirectory))
            return string.Empty;

        var skillDir = Directory.GetDirectories(skillsDirectory)
            .FirstOrDefault(d => string.Equals(Path.GetFileName(d), skillName, StringComparison.OrdinalIgnoreCase));

        if (skillDir == null) return string.Empty;

        var refPath = ResolveReferencePath(skillDir, skillsDirectory, referenceFile);
        if (!File.Exists(refPath)) return string.Empty;

        var content = File.ReadAllText(refPath).Trim();
        return Truncate(content);
    }

    // ===== helpers =====

    private static string BuildSkillSection(string skillDirectory)
    {
        var skillFile = Path.Combine(skillDirectory, "SKILL.md");
        if (!File.Exists(skillFile))
            return string.Empty;

        var content = File.ReadAllText(skillFile).Trim();
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var name = Path.GetFileName(skillDirectory);
        return $"## {name}\n{content}";
    }

    private static IEnumerable<string> FindLinkedMarkdownReferences(string content, string skillDir, string skillsDirectory)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            yield break;
        }

        var matches = System.Text.RegularExpressions.Regex.Matches(content, @"\[[^\]]+\]\(([^)]+\.md)\)");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Groups.Count < 2)
            {
                continue;
            }

            var relativePath = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                continue;
            }

            var resolved = ResolveReferencePath(skillDir, skillsDirectory, relativePath);
            if (!File.Exists(resolved))
            {
                continue;
            }

            yield return relativePath.Replace('\\', '/');
        }
    }

    private static string ResolveReferencePath(string skillDir, string skillsDirectory, string referenceFile)
    {
        if (string.IsNullOrWhiteSpace(referenceFile))
        {
            return string.Empty;
        }

        var looksRelative = referenceFile.Contains('/')
            || referenceFile.Contains('\\')
            || referenceFile.StartsWith(".", StringComparison.Ordinal);

        var candidate = looksRelative
            ? Path.GetFullPath(Path.Combine(skillDir, referenceFile))
            : Path.GetFullPath(Path.Combine(skillDir, "references", referenceFile));

        var skillsRoot = Path.GetFullPath(skillsDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (candidate.Equals(skillsRoot, comparison)
            || candidate.StartsWith(skillsRoot + Path.DirectorySeparatorChar, comparison)
            || candidate.StartsWith(skillsRoot + Path.AltDirectorySeparatorChar, comparison))
        {
            return candidate;
        }

        return string.Empty;
    }

    private string Truncate(string content)
    {
        if (MaxContextChars <= 0 || content.Length <= MaxContextChars)
            return content;

        return content[..MaxContextChars] + "\n... (Skills truncated)";
    }

    private static (Dictionary<string, string>? frontmatter, string? body) ParseFrontmatter(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            if (!content.StartsWith("---")) return (null, content);

            var endIdx = content.IndexOf("---", 3);
            if (endIdx < 0) return (null, content);

            var fmText = content[3..endIdx].Trim();
            var body = content[(endIdx + 3)..].Trim();

            var fm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in fmText.Split('\n'))
            {
                var colonIdx = line.IndexOf(':');
                if (colonIdx < 0) continue;
                var key = line[..colonIdx].Trim();
                var value = line[(colonIdx + 1)..].Trim();
                fm[key] = value;
            }
            return (fm, body);
        }
        catch
        {
            return (null, null);
        }
    }
}

// ===== Models =====

public sealed class SkillCatalog
{
    public List<SkillEntry> Skills { get; set; } = new();
    public int TotalCount { get; set; }
}

public sealed record SkillEntry(
    string Name,
    string Description,
    bool HasReferences,
    bool HasExamples,
    int ReferenceCount);

public sealed record SkillLoadResult(
    string SkillName,
    bool Found,
    string SklContent,
    IReadOnlyList<string> References,
    IReadOnlyList<string> Examples)
{
    public static SkillLoadResult NotFound(string name) =>
        new(name, false, "", Array.Empty<string>(), Array.Empty<string>());

    public static SkillLoadResult Ok(string name, string content, IReadOnlyList<string> refs, IReadOnlyList<string> exs) =>
        new(name, true, content, refs, exs);
}
