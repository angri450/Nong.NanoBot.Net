using System.IO.Compression;
using System.Text.Json;

namespace Nanobot.Core.Skills;

/// <summary>
/// Manages plugin installation: download, extract, register skills from Nong.Toolkit.Net releases.
/// </summary>
public class PluginManager
{
    private readonly string _skillsDir;
    private readonly string _registryPath;
    private readonly HttpClient _http;

    public PluginManager(string workspace, HttpClient? http = null)
    {
        _skillsDir = Path.Combine(workspace, "skills");
        _registryPath = Path.Combine(workspace, "plugin-registry.json");
        _http = http ?? new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("NanoBot-PluginManager/1.0");
        Directory.CreateDirectory(_skillsDir);
    }

    /// <summary>
    /// Install Nong.Toolkit.Net plugin from a GitHub/Gitee release zip.
    /// </summary>
    public async Task<PluginInstallResult> InstallAsync(string pluginName, string version)
    {
        // Derive download URL
        var url = GetReleaseUrl(pluginName, version);
        var tmpDir = Path.Combine(Path.GetTempPath(), $"nanobot-plugin-{pluginName}-{Guid.NewGuid():N}");
        var tmpZip = Path.Combine(tmpDir, "plugin.zip");

        try
        {
            Directory.CreateDirectory(tmpDir);

            // Download
            await DownloadAsync(url, tmpZip);

            // Validate zip
            if (!File.Exists(tmpZip) || new FileInfo(tmpZip).Length == 0)
                return PluginInstallResult.Fail(pluginName, "Downloaded file is empty or missing.");

            // Extract to skills directory
            var extractDir = Path.Combine(_skillsDir, pluginName);
            if (Directory.Exists(extractDir))
            {
                // Backup existing
                var backupDir = extractDir + $".bak.{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
                Directory.Move(extractDir, backupDir);
            }

            ZipFile.ExtractToDirectory(tmpZip, extractDir);

            // Find all SKILL.md files to discover sub-skills
            var skillDirs = Directory.GetDirectories(extractDir)
                .Where(d => File.Exists(Path.Combine(d, "SKILL.md")))
                .Select(d => Path.GetFileName(d))
                .ToList();

            // If the extract dir itself has SKILL.md, it's a single skill
            if (File.Exists(Path.Combine(extractDir, "SKILL.md")) && skillDirs.Count == 0)
            {
                skillDirs.Add(pluginName);
            }

            // Update registry
            var registry = LoadRegistry();
            registry[pluginName] = new PluginEntry
            {
                Name = pluginName,
                Version = version,
                InstalledAt = DateTimeOffset.UtcNow,
                SourceUrl = url,
                Skills = skillDirs
            };
            SaveRegistry(registry);

            return PluginInstallResult.Ok(pluginName, version, skillDirs);
        }
        catch (HttpRequestException ex)
        {
            return PluginInstallResult.Fail(pluginName, $"Download failed: {ex.Message}");
        }
        catch (InvalidDataException ex)
        {
            return PluginInstallResult.Fail(pluginName, $"Invalid zip: {ex.Message}");
        }
        catch (Exception ex)
        {
            return PluginInstallResult.Fail(pluginName, ex.Message);
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// List installed plugins from the registry.
    /// </summary>
    public Dictionary<string, PluginEntry> ListInstalled()
    {
        return LoadRegistry();
    }

    /// <summary>
    /// Uninstall a plugin (remove directory + registry entry).
    /// </summary>
    public bool Uninstall(string pluginName)
    {
        var registry = LoadRegistry();
        registry.Remove(pluginName);
        SaveRegistry(registry);

        var dir = Path.Combine(_skillsDir, pluginName);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check latest available version from GitHub releases API.
    /// </summary>
    public async Task<string?> CheckLatestVersionAsync(string pluginName)
    {
        try
        {
            var apiUrl = $"https://api.github.com/repos/angri450/Nong.Toolkit.Net/releases/latest";
            var json = await _http.GetStringAsync(apiUrl);
            using var doc = JsonDocument.Parse(json);
            var tag = doc.RootElement.GetProperty("tag_name").GetString();
            return tag?.TrimStart('v');
        }
        catch
        {
            return null;
        }
    }

    private async Task DownloadAsync(string url, string destPath)
    {
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(destPath);
        await stream.CopyToAsync(fileStream);
    }

    private static string GetReleaseUrl(string pluginName, string version)
    {
        // GitHub release archive URL
        return $"https://github.com/angri450/Nong.Toolkit.Net/archive/refs/tags/v{version}.zip";
    }

    private Dictionary<string, PluginEntry> LoadRegistry()
    {
        try
        {
            if (File.Exists(_registryPath))
            {
                var json = File.ReadAllText(_registryPath);
                return JsonSerializer.Deserialize<Dictionary<string, PluginEntry>>(json)
                    ?? new();
            }
        }
        catch { }
        return new();
    }

    private void SaveRegistry(Dictionary<string, PluginEntry> registry)
    {
        File.WriteAllText(_registryPath,
            JsonSerializer.Serialize(registry, new JsonSerializerOptions { WriteIndented = true }));
    }
}

public sealed class PluginEntry
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public DateTimeOffset InstalledAt { get; set; }
    public string? SourceUrl { get; set; }
    public List<string> Skills { get; set; } = new();
}

public sealed record PluginInstallResult(string PluginName, bool Success, string? Version, List<string> Skills, string? ErrorMessage)
{
    public static PluginInstallResult Ok(string name, string version, List<string> skills) =>
        new(name, true, version, skills, null);
    public static PluginInstallResult Fail(string name, string error) =>
        new(name, false, null, new(), error);
}
