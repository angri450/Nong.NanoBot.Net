using System.IO.Compression;
using System.Net;
using System.Text.Json;
using Nanobot.Core.Skills;

namespace Nanobot.Tests;

public class PluginManagerTests
{
    [Fact]
    public async Task InstallAsync_InstallsIndividualSkillAndSharedReferencesFromMarketplaceArchive()
    {
        var workspace = CreateWorkspace();
        using var archive = CreateToolkitArchive(
            rootPluginSkills: new[] { "./word", "./pdf" },
            pluginSources: new Dictionary<string, string>
            {
                ["nong-toolkit"] = "./",
                ["word"] = "./word",
                ["pdf"] = "./pdf"
            });

        var manager = new PluginManager(workspace, CreateHttpClient(archive.ToArray()));

        var result = await manager.InstallAsync("word@nong-toolkit", "v1.0.0");

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(new[] { "word" }, result.Skills);
        Assert.True(File.Exists(Path.Combine(workspace, "skills", "word", "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(workspace, "skills", "references", "shared", "nong-cli-preflight.md")));

        var registry = manager.ListInstalled();
        Assert.True(registry.ContainsKey("word"));
        Assert.Equal("1.0.0", registry["word"].Version);
    }

    [Fact]
    public async Task InstallAsync_FullBundleInstallsAllManifestSkills()
    {
        var workspace = CreateWorkspace();
        using var archive = CreateToolkitArchive(
            rootPluginSkills: new[] { "./word", "./pdf" },
            pluginSources: new Dictionary<string, string>
            {
                ["nong-toolkit"] = "./",
                ["word"] = "./word",
                ["pdf"] = "./pdf"
            });

        var manager = new PluginManager(workspace, CreateHttpClient(archive.ToArray()));

        var result = await manager.InstallAsync("nong-toolkit", "1.0.0");

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(new[] { "pdf", "word" }, result.Skills.OrderBy(x => x).ToArray());
        Assert.True(File.Exists(Path.Combine(workspace, "skills", "word", "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(workspace, "skills", "pdf", "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(workspace, "skills", "references", "shared", "nong-cli-preflight.md")));
    }

    [Fact]
    public async Task Uninstall_RemovesAllInstalledSkillsForBundleEntry()
    {
        var workspace = CreateWorkspace();
        using var archive = CreateToolkitArchive(
            rootPluginSkills: new[] { "./word", "./pdf" },
            pluginSources: new Dictionary<string, string>
            {
                ["nong-toolkit"] = "./",
                ["word"] = "./word",
                ["pdf"] = "./pdf"
            });

        var manager = new PluginManager(workspace, CreateHttpClient(archive.ToArray()));
        await manager.InstallAsync("nong-toolkit", "1.0.0");

        var removed = manager.Uninstall("nong-toolkit");

        Assert.True(removed);
        Assert.False(Directory.Exists(Path.Combine(workspace, "skills", "word")));
        Assert.False(Directory.Exists(Path.Combine(workspace, "skills", "pdf")));
        Assert.Empty(manager.ListInstalled());
    }

    [Fact]
    public async Task InstallAsync_ReturnsErrorWhenPluginIsMissingFromMarketplace()
    {
        var workspace = CreateWorkspace();
        using var archive = CreateToolkitArchive(
            rootPluginSkills: new[] { "./word" },
            pluginSources: new Dictionary<string, string>
            {
                ["nong-toolkit"] = "./",
                ["word"] = "./word"
            });

        var manager = new PluginManager(workspace, CreateHttpClient(archive.ToArray()));

        var result = await manager.InstallAsync("missing", "1.0.0");

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClient CreateHttpClient(byte[] payload)
    {
        return new HttpClient(new StaticResponseHandler(payload))
        {
            BaseAddress = new Uri("https://example.test/")
        };
    }

    private static MemoryStream CreateToolkitArchive(
        IReadOnlyList<string> rootPluginSkills,
        IReadOnlyDictionary<string, string> pluginSources)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            const string root = "Nong.Toolkit.Net-1.0.0/";

            AddEntry(archive, root + ".claude-plugin/marketplace.json", CreateMarketplaceJson(pluginSources));
            AddEntry(archive, root + ".claude-plugin/plugin.json", CreatePluginJson("nong-toolkit", rootPluginSkills));
            AddEntry(archive, root + "references/shared/nong-cli-preflight.md", "Confirm nong is installed.");

            foreach (var plugin in pluginSources.Where(item => item.Key != "nong-toolkit"))
            {
                var dir = plugin.Value.TrimStart('.', '/', '\\').Replace('/', '\\');
                var normalizedDir = dir.Replace('\\', '/');
                AddEntry(archive, $"{root}{normalizedDir}/SKILL.md", $$"""
                    ---
                    name: {{plugin.Key}}
                    description: {{plugin.Key}} skill
                    ---

                    # {{plugin.Key}}

                    Read [../references/shared/nong-cli-preflight.md](../references/shared/nong-cli-preflight.md).
                    """);
                AddEntry(archive, $"{root}{normalizedDir}/.claude-plugin/plugin.json", CreatePluginJson(plugin.Key, new[] { "./" }));
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static string CreateMarketplaceJson(IReadOnlyDictionary<string, string> pluginSources)
    {
        return JsonSerializer.Serialize(new
        {
            plugins = pluginSources.Select(source => new
            {
                name = source.Key,
                source = source.Value.Replace("\\", "/")
            })
        });
    }

    private static string CreatePluginJson(string name, IReadOnlyList<string> skills)
    {
        return JsonSerializer.Serialize(new
        {
            name,
            skills = skills.Select(skill => skill.Replace("\\", "/")).ToArray()
        });
    }

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    private static string CreateWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "nanobot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class StaticResponseHandler : HttpMessageHandler
    {
        private readonly byte[] _payload;

        public StaticResponseHandler(byte[] payload)
        {
            _payload = payload;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_payload)
            };
            return Task.FromResult(response);
        }
    }
}
