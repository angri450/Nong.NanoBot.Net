using System.Text.Json;
using Nanobot.Core.Config;
using Nanobot.Web;

namespace Nanobot.Tests;

public class ModelSettingsStoreTests
{
    [Fact]
    public void Get_UsesConfiguredSiliconFlowProviderAndListsAvailableProviders()
    {
        var nanoDir = CreateNanoDir();
        WriteBootstrapFiles(nanoDir);
        var config = ConfigLoader.Load(Path.Combine(nanoDir, "config.json"));
        var store = CreateStore(nanoDir);

        var settings = store.Get(config);

        Assert.Equal("siliconflow", settings.ProviderId);
        Assert.Equal("https://api.siliconflow.cn/v1/", settings.ApiBase);
        Assert.Equal("nex-agi/Nex-N2-Pro", settings.Model);
        Assert.True(settings.HasApiKey);
        Assert.Contains(settings.AvailableProviders, provider => provider.ProviderId == "siliconflow");
        Assert.Contains(settings.AvailableProviders, provider => provider.ProviderId == "dmx");
    }

    [Fact]
    public void Save_UpdatesSiliconFlowSelectionAndPreservesProviderCatalog()
    {
        var nanoDir = CreateNanoDir();
        WriteBootstrapFiles(nanoDir);
        var configPath = Path.Combine(nanoDir, "config.json");
        var config = ConfigLoader.Load(configPath);
        var store = CreateStore(nanoDir);

        store.Save(new SaveModelSettingsRequest(
            ProviderId: "siliconflow",
            ApiKey: "new-sf-key",
            ApiBase: "https://api.siliconflow.cn/v1/",
            Model: "Qwen/Qwen3.5-35B-A3B",
            ClearApiKey: false), config);

        var reloaded = ConfigLoader.Load(configPath);
        var settings = store.Get(reloaded);
        var modelsJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(nanoDir, "models.json")));

        Assert.Equal("siliconflow::Qwen/Qwen3.5-35B-A3B", reloaded.Agents.Defaults.Model);
        Assert.Equal("Qwen/Qwen3.5-35B-A3B", settings.Model);
        Assert.Contains(reloaded.Providers["siliconflow"].Models, model => model.Id == "nex-agi/Nex-N2-Pro");
        Assert.Contains(reloaded.Providers["siliconflow"].Models, model => model.Id == "Qwen/Qwen3.5-35B-A3B");
        Assert.Equal("new-sf-key", JsonDocument.Parse(File.ReadAllText(Path.Combine(nanoDir, "secrets.json")))
            .RootElement.GetProperty("siliconflow").GetProperty("apiKey").GetString());
        Assert.True(modelsJson.RootElement.GetProperty("providers").GetProperty("siliconflow").GetProperty("models").GetArrayLength() >= 2);
    }

    private static ModelSettingsStore CreateStore(string nanoDir)
    {
        return new ModelSettingsStore(
            Path.Combine(nanoDir, "config.json"),
            Path.Combine(nanoDir, "models.json"),
            Path.Combine(nanoDir, "secrets.json"));
    }

    private static void WriteBootstrapFiles(string nanoDir)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        Directory.CreateDirectory(nanoDir);
        File.WriteAllText(
            Path.Combine(nanoDir, "models.json"),
            JsonSerializer.Serialize(DefaultProviderCatalog.CreateModelCatalog(), options));
        File.WriteAllText(
            Path.Combine(nanoDir, "secrets.json"),
            """
            {
              "siliconflow": {
                "apiKey": "sf-initial-key"
              },
              "dmx": {
                "apiKey": ""
              }
            }
            """);
        File.WriteAllText(
            Path.Combine(nanoDir, "config.json"),
            JsonSerializer.Serialize(DefaultProviderCatalog.CreateRuntimeConfigTemplate(), options));
    }

    private static string CreateNanoDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "nanobot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
