using System.Text.Json;
using System.Text.Json.Nodes;
using Nanobot.Core.Skills;

namespace Nanobot.Core.Tools.Builtin;

public class LoadSkillTool : ITool
{
    private readonly SkillLoader _skillLoader;
    private readonly string _workspace;

    public LoadSkillTool(SkillLoader skillLoader, string workspace)
    {
        _skillLoader = skillLoader;
        _workspace = workspace;
    }

    public string Name => "load_skill";
    public string Description => "Load a specific skill's SKILL.md body content. Use this after reviewing the skill catalog to get detailed instructions for a skill.";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "name": {
                "type": "string",
                "description": "The skill name to load (e.g. 'word', 'pdf', 'ocr'). Use get_skill_catalog first to see available names."
            }
        },
        "required": ["name"]
    }
    """)!;

    public Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var name = arguments?["name"]?.ToString();
        if (string.IsNullOrEmpty(name))
            return Task.FromResult(Error("missing_name", "Skill name is required."));

        var result = _skillLoader.LoadSkill(_workspace, name);
        if (!result.Found)
            return Task.FromResult(Error("skill_not_found", $"Skill '{name}' not found. Use get_skill_catalog to see available skills."));

        var summary = new
        {
            skill = result.SkillName,
            content = result.SklContent,
            availableReferences = result.References,
            availableExamples = result.Examples,
            hint = result.References.Count > 0
                ? $"This skill has {result.References.Count} reference file(s). Use load_skill_reference to read one."
                : "No additional references available."
        };

        return Task.FromResult(JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string Error(string code, string message)
    {
        return JsonSerializer.Serialize(new
        {
            error = new { tool = "load_skill", code, message }
        });
    }
}

public class LoadSkillReferenceTool : ITool
{
    private readonly SkillLoader _skillLoader;
    private readonly string _workspace;

    public LoadSkillReferenceTool(SkillLoader skillLoader, string workspace)
    {
        _skillLoader = skillLoader;
        _workspace = workspace;
    }

    public string Name => "load_skill_reference";
    public string Description => "Load a reference file from a loaded skill for deeper guidance. Use the file names returned by load_skill.";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "skill": {
                "type": "string",
                "description": "The skill name (e.g. 'word', 'ocr')."
            },
            "reference": {
                "type": "string",
                "description": "The reference file name (e.g. 'guide.md', 'ocr-local.md')."
            }
        },
        "required": ["skill", "reference"]
    }
    """)!;

    public Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var skill = arguments?["skill"]?.ToString();
        var reference = arguments?["reference"]?.ToString();
        if (string.IsNullOrEmpty(skill) || string.IsNullOrEmpty(reference))
            return Task.FromResult(Error("missing_args", "Both skill and reference are required."));

        var content = _skillLoader.LoadReference(_workspace, skill, reference);
        if (string.IsNullOrEmpty(content))
            return Task.FromResult(Error("reference_not_found", $"Reference '{reference}' not found for skill '{skill}'."));

        var summary = new
        {
            skill,
            reference,
            content
        };

        return Task.FromResult(JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string Error(string code, string message)
    {
        return JsonSerializer.Serialize(new
        {
            error = new { tool = "load_skill_reference", code, message }
        });
    }
}

public class GetSkillCatalogTool : ITool
{
    private readonly SkillLoader _skillLoader;
    private readonly string _workspace;

    public GetSkillCatalogTool(SkillLoader skillLoader, string workspace)
    {
        _skillLoader = skillLoader;
        _workspace = workspace;
    }

    public string Name => "get_skill_catalog";
    public string Description => "List all available skills with their descriptions, reference counts, and example counts. Use this first to discover what skills are available, then use load_skill(name) to read a specific skill.";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {},
        "required": []
    }
    """)!;

    public Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var catalog = _skillLoader.GetCatalog(_workspace);
        if (catalog.TotalCount == 0)
            return Task.FromResult(JsonSerializer.Serialize(new
            {
                message = "No skills installed. Place skill directories under ~/.nanobot/workspace/skills/.",
                skillCount = 0,
                skills = Array.Empty<object>()
            }, new JsonSerializerOptions { WriteIndented = true }));

        var result = new
        {
            skillCount = catalog.TotalCount,
            hint = "Use load_skill(name) to read a specific skill. Use load_skill_reference(skill, ref) to read deeper guidance.",
            skills = catalog.Skills.Select(s => new
            {
                name = s.Name,
                description = s.Description,
                hasReferences = s.HasReferences,
                referenceCount = s.ReferenceCount,
                hasExamples = s.HasExamples
            })
        };

        return Task.FromResult(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }
}
