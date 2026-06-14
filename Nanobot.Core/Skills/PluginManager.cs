using System.IO.Compression;
using System.Text.Json;

namespace Nanobot.Core.Skills;

/// <summary>
/// Manages plugin installation: download, extract, register skills from Nong.Toolkit.Net releases.
/// </summary>
public class PluginManager
{
    private const string ToolkitOwner = "angri450";
    private const string ToolkitRepository = "Nong.Toolkit.Net";
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
        pluginName = NormalizePluginName(pluginName);
        version = NormalizeVersion(version);

        // Derive download URL
        var url = GetReleaseUrl(pluginName, version);
        var tmpDir = Path.Combine(Path.GetTempPath(), $"nanobot-plugin-{pluginName}-{Guid.NewGuid():N}");
        var tmpZip = Path.Combine(tmpDir, "plugin.zip");
        var extractRoot = Path.Combine(tmpDir, "extract");

        try
        {
            Directory.CreateDirectory(tmpDir);
            Directory.CreateDirectory(extractRoot);

            // Download
            await DownloadAsync(url, tmpZip);

            // Validate zip
            if (!File.Exists(tmpZip) || new FileInfo(tmpZip).Length == 0)
                return PluginInstallResult.Fail(pluginName, "Downloaded file is empty or missing.");

            ZipFile.ExtractToDirectory(tmpZip, extractRoot);

            var packageRoot = FindPackageRoot(extractRoot);
            var installPlan = BuildInstallPlan(packageRoot, pluginName);
            ApplyInstallPlan(installPlan, tmpDir);

            // Update registry
            var registry = LoadRegistry();
            registry[pluginName] = new PluginEntry
            {
                Name = pluginName,
                Version = version,
                InstalledAt = DateTimeOffset.UtcNow,
                SourceUrl = url,
                Skills = installPlan.Skills.Select(skill => skill.InstallName).ToList()
            };
            SaveRegistry(registry);

            return PluginInstallResult.Ok(pluginName, version, installPlan.Skills.Select(skill => skill.InstallName).ToList());
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
        pluginName = NormalizePluginName(pluginName);
        var registry = LoadRegistry();
        registry.TryGetValue(pluginName, out var entry);
        registry.Remove(pluginName);
        SaveRegistry(registry);

        var removed = false;
        foreach (var skillName in entry?.Skills ?? new List<string> { pluginName })
        {
            var dir = Path.Combine(_skillsDir, skillName);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
                removed = true;
            }
        }

        return removed;
    }

    /// <summary>
    /// Check latest available version from GitHub releases API.
    /// </summary>
    public async Task<string?> CheckLatestVersionAsync(string pluginName)
    {
        try
        {
            var apiUrl = $"https://api.github.com/repos/{ToolkitOwner}/{ToolkitRepository}/releases/latest";
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
        return $"https://github.com/{ToolkitOwner}/{ToolkitRepository}/archive/refs/tags/v{version}.zip";
    }

    private ToolkitInstallPlan BuildInstallPlan(string packageRoot, string pluginName)
    {
        var marketplace = LoadMarketplace(packageRoot);
        var plugin = marketplace.Plugins
            .FirstOrDefault(item => string.Equals(item.Name, pluginName, StringComparison.OrdinalIgnoreCase));

        if (plugin is null)
        {
            throw new InvalidOperationException($"Plugin '{pluginName}' was not found in Nong.Toolkit.Net marketplace.");
        }

        var sourceRoot = ResolvePackagePath(packageRoot, plugin.Source, packageRoot);
        var manifest = LoadPluginManifest(sourceRoot);
        var skills = ResolveSkillInstallSources(packageRoot, sourceRoot, manifest);
        if (skills.Count == 0)
        {
            throw new InvalidOperationException($"Plugin '{pluginName}' does not expose any installable skills.");
        }

        var sharedReferences = Directory.Exists(Path.Combine(packageRoot, "references", "shared"))
            ? Path.Combine(packageRoot, "references", "shared")
            : null;

        return new ToolkitInstallPlan(plugin.Name, skills, sharedReferences);
    }

    private void ApplyInstallPlan(ToolkitInstallPlan plan, string tmpDir)
    {
        var backupRoot = Path.Combine(tmpDir, "backup");
        Directory.CreateDirectory(backupRoot);

        var backups = new List<BackupEntry>();
        var installedTargets = new List<string>();

        try
        {
            foreach (var skill in plan.Skills)
            {
                var targetDir = Path.Combine(_skillsDir, skill.InstallName);
                BackupDirectory(targetDir, backupRoot, backups);
                installedTargets.Add(targetDir);
                CopyDirectory(skill.SourcePath, targetDir);
            }

            if (!string.IsNullOrWhiteSpace(plan.SharedReferencesPath))
            {
                var targetSharedDir = Path.Combine(_skillsDir, "references", "shared");
                BackupDirectory(targetSharedDir, backupRoot, backups);
                installedTargets.Add(targetSharedDir);
                CopyDirectory(plan.SharedReferencesPath!, targetSharedDir);
            }
        }
        catch
        {
            foreach (var target in installedTargets.Where(Directory.Exists))
            {
                Directory.Delete(target, recursive: true);
            }

            RestoreBackups(backups);
            throw;
        }
    }

    private static void BackupDirectory(string targetDir, string backupRoot, List<BackupEntry> backups)
    {
        if (!Directory.Exists(targetDir))
        {
            return;
        }

        var backupDir = Path.Combine(backupRoot, backups.Count.ToString("D4"));
        var parent = Path.GetDirectoryName(backupDir);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        Directory.Move(targetDir, backupDir);
        backups.Add(new BackupEntry(targetDir, backupDir));
    }

    private static void RestoreBackups(IEnumerable<BackupEntry> backups)
    {
        foreach (var backup in backups.Reverse())
        {
            var parent = Path.GetDirectoryName(backup.TargetPath);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }

            if (Directory.Exists(backup.TargetPath))
            {
                Directory.Delete(backup.TargetPath, recursive: true);
            }

            Directory.Move(backup.BackupPath, backup.TargetPath);
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(sourceDir))
        {
            throw new DirectoryNotFoundException($"Source directory '{sourceDir}' does not exist.");
        }

        if (Directory.Exists(destinationDir))
        {
            Directory.Delete(destinationDir, recursive: true);
        }

        Directory.CreateDirectory(destinationDir);

        foreach (var directory in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, directory);
            Directory.CreateDirectory(Path.Combine(destinationDir, relative));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var target = Path.Combine(destinationDir, relative);
            var parent = Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.Copy(file, target, overwrite: true);
        }
    }

    private static string FindPackageRoot(string extractRoot)
    {
        if (LooksLikeMarketplaceRoot(extractRoot))
        {
            return extractRoot;
        }

        foreach (var directory in Directory.GetDirectories(extractRoot))
        {
            if (LooksLikeMarketplaceRoot(directory))
            {
                return directory;
            }
        }

        throw new InvalidOperationException("Downloaded package does not contain a Nong.Toolkit.Net marketplace root.");
    }

    private static bool LooksLikeMarketplaceRoot(string path)
    {
        return File.Exists(Path.Combine(path, ".claude-plugin", "marketplace.json"))
            || File.Exists(Path.Combine(path, ".claude-plugin", "plugin.json"));
    }

    private static ToolkitMarketplaceManifest LoadMarketplace(string packageRoot)
    {
        var path = Path.Combine(packageRoot, ".claude-plugin", "marketplace.json");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException("Nong.Toolkit.Net marketplace manifest '.claude-plugin/marketplace.json' was not found.");
        }

        var manifest = JsonSerializer.Deserialize<ToolkitMarketplaceManifest>(File.ReadAllText(path), JsonOptions);
        if (manifest is null || manifest.Plugins.Count == 0)
        {
            throw new InvalidOperationException("Nong.Toolkit.Net marketplace manifest is empty or invalid.");
        }

        return manifest;
    }

    private static ToolkitPluginManifest LoadPluginManifest(string sourceRoot)
    {
        var path = Path.Combine(sourceRoot, ".claude-plugin", "plugin.json");
        if (!File.Exists(path))
        {
            if (File.Exists(Path.Combine(sourceRoot, "SKILL.md")))
            {
                return new ToolkitPluginManifest
                {
                    Name = Path.GetFileName(sourceRoot),
                    Skills = new List<string> { "./" }
                };
            }

            throw new InvalidOperationException($"Plugin manifest was not found under '{sourceRoot}'.");
        }

        var manifest = JsonSerializer.Deserialize<ToolkitPluginManifest>(File.ReadAllText(path), JsonOptions);
        if (manifest is null)
        {
            throw new InvalidOperationException($"Plugin manifest under '{sourceRoot}' is invalid.");
        }

        return manifest;
    }

    private static List<SkillInstallSource> ResolveSkillInstallSources(
        string packageRoot,
        string sourceRoot,
        ToolkitPluginManifest manifest)
    {
        var skills = new List<SkillInstallSource>();

        foreach (var skillPath in manifest.Skills.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var resolved = ResolvePackagePath(sourceRoot, skillPath, packageRoot);
            if (!Directory.Exists(resolved) || !File.Exists(Path.Combine(resolved, "SKILL.md")))
            {
                throw new InvalidOperationException($"Skill source '{skillPath}' in plugin '{manifest.Name}' is missing SKILL.md.");
            }

            var installName = Path.GetFileName(resolved.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            skills.Add(new SkillInstallSource(installName, resolved));
        }

        if (skills.Count == 0 && File.Exists(Path.Combine(sourceRoot, "SKILL.md")))
        {
            skills.Add(new SkillInstallSource(Path.GetFileName(sourceRoot), sourceRoot));
        }

        return skills
            .GroupBy(skill => skill.InstallName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static string ResolvePackagePath(string baseRoot, string relativePath, string packageRoot)
    {
        var fullPath = Path.GetFullPath(Path.Combine(baseRoot, relativePath));
        var normalizedRoot = Path.GetFullPath(packageRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!fullPath.Equals(normalizedRoot, comparison)
            && !fullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison)
            && !fullPath.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, comparison))
        {
            throw new InvalidOperationException($"Manifest path '{relativePath}' resolves outside package root.");
        }

        return fullPath;
    }

    private static string NormalizePluginName(string pluginName)
    {
        if (string.IsNullOrWhiteSpace(pluginName))
        {
            return string.Empty;
        }

        var normalized = pluginName.Trim();
        var atIndex = normalized.IndexOf('@');
        return atIndex > 0 ? normalized[..atIndex] : normalized;
    }

    private static string NormalizeVersion(string version)
    {
        return version.Trim().TrimStart('v', 'V');
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private Dictionary<string, PluginEntry> LoadRegistry()
    {
        try
        {
            if (File.Exists(_registryPath))
            {
                var json = File.ReadAllText(_registryPath);
                return JsonSerializer.Deserialize<Dictionary<string, PluginEntry>>(json, JsonOptions)
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

internal sealed record ToolkitInstallPlan(
    string PluginName,
    IReadOnlyList<SkillInstallSource> Skills,
    string? SharedReferencesPath);

internal sealed record SkillInstallSource(string InstallName, string SourcePath);

internal sealed record BackupEntry(string TargetPath, string BackupPath);

internal sealed class ToolkitMarketplaceManifest
{
    public List<ToolkitMarketplacePlugin> Plugins { get; set; } = new();
}

internal sealed class ToolkitMarketplacePlugin
{
    public string Name { get; set; } = "";
    public string Source { get; set; } = "";
}

internal sealed class ToolkitPluginManifest
{
    public string Name { get; set; } = "";
    public List<string> Skills { get; set; } = new();
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
