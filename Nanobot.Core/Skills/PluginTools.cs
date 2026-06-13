using System.Text.Json;
using System.Text.Json.Nodes;
using Nanobot.Core.Tools;

namespace Nanobot.Core.Skills;

/// <summary>
/// Tool: install a plugin (download from release archive, extract to workspace/skills).
/// </summary>
public class PluginInstallTool : ITool
{
    private readonly PluginManager _manager;

    public PluginInstallTool(PluginManager manager)
    {
        _manager = manager;
    }

    public string Name => "plugin_install";
    public string Description => "Install a Nong.Toolkit.Net skill plugin from GitHub release. Downloads and extracts skills to workspace/skills/.";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "plugin": {
                "type": "string",
                "description": "Plugin name, e.g. nong-toolkit"
            },
            "version": {
                "type": "string",
                "description": "Version tag, e.g. 4.1.0. If omitted, installs latest."
            }
        },
        "required": ["plugin"]
    }
    """)!;

    public async Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var pluginName = arguments?["plugin"]?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(pluginName))
            return Error("plugin_required", "Plugin name is required.");

        var version = arguments?["version"]?.ToString();
        if (string.IsNullOrWhiteSpace(version))
        {
            var latest = await _manager.CheckLatestVersionAsync(pluginName);
            if (latest == null)
                return Error("version_unknown", $"Cannot determine latest version for {pluginName}. Provide --version explicitly.");
            version = latest;
        }

        var result = await _manager.InstallAsync(pluginName, version);
        if (result.Success)
        {
            return JsonSerializer.Serialize(new
            {
                tool = Name,
                plugin = result.PluginName,
                version = result.Version,
                installed = true,
                skills = result.Skills,
                path = $"workspace/skills/{result.PluginName}"
            });
        }

        return JsonSerializer.Serialize(new
        {
            tool = Name,
            plugin = result.PluginName,
            installed = false,
            error = result.ErrorMessage
        });
    }

    private static string Error(string code, string message) =>
        JsonSerializer.Serialize(new { tool = "plugin_install", error = new { code, message } });
}

/// <summary>
/// Tool: list installed plugins.
/// </summary>
public class PluginListTool : ITool
{
    private readonly PluginManager _manager;

    public PluginListTool(PluginManager manager)
    {
        _manager = manager;
    }

    public string Name => "plugin_list";
    public string Description => "List installed Nong.Toolkit.Net plugin skills.";

    public JsonNode Parameters => JsonNode.Parse("""{"type":"object","properties":{}}""")!;

    public Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var registry = _manager.ListInstalled();
        var plugins = registry.Select(kv => new
        {
            name = kv.Key,
            version = kv.Value.Version,
            installedAt = kv.Value.InstalledAt.ToString("o"),
            skills = kv.Value.Skills
        }).ToList();

        return Task.FromResult(JsonSerializer.Serialize(new
        {
            tool = Name,
            plugins,
            count = plugins.Count
        }));
    }
}
