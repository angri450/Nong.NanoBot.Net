using System.Text;
using Nanobot.Core.Agent;

namespace Nanobot.Core.Skills;

public class SkillLoader
{
    public const int DefaultMaxContextChars = 12000;

    public SkillLoader(int maxContextChars = DefaultMaxContextChars)
    {
        MaxContextChars = maxContextChars;
    }

    public int MaxContextChars { get; }

    public string LoadContext(AgentExecutionContext context)
    {
        return LoadContext(context.Workspace);
    }

    public string LoadContext(string workspace)
    {
        if (string.IsNullOrWhiteSpace(workspace))
        {
            return string.Empty;
        }

        var skillsDirectory = Path.Combine(workspace, "skills");
        if (!Directory.Exists(skillsDirectory))
        {
            return string.Empty;
        }

        var sections = Directory.GetDirectories(skillsDirectory)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(BuildSkillSection)
            .Where(section => !string.IsNullOrWhiteSpace(section))
            .ToList();

        if (sections.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder("# Skills");
        foreach (var section in sections)
        {
            builder.AppendLine();
            builder.AppendLine();
            builder.Append(section);
        }

        return Truncate(builder.ToString());
    }

    private static string BuildSkillSection(string skillDirectory)
    {
        var skillFile = Path.Combine(skillDirectory, "SKILL.md");
        if (!File.Exists(skillFile))
        {
            return string.Empty;
        }

        var content = File.ReadAllText(skillFile).Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var name = Path.GetFileName(skillDirectory);
        return $"## {name}\n{content}";
    }

    private string Truncate(string content)
    {
        if (MaxContextChars <= 0 || content.Length <= MaxContextChars)
        {
            return content;
        }

        return content[..MaxContextChars] + "\n... (Skills truncated)";
    }
}
