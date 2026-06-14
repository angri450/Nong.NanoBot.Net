using System.Text.Json;
using Nanobot.Core.Config;
using Nanobot.Web;

namespace Nanobot.Tests;

public class ModelSettingsStoreTests
{
    [Fact]
    public void Get_UsesConfiguredSiliconFlowProviderAndListsOnlySiliconFlow()
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
        var provider = Assert.Single(settings.AvailableProviders);
        Assert.Equal("siliconflow", provider.ProviderId);
    }

    [Fact]
    public void Get_IgnoresLegacyDmxProviderInWebUiDistributionPath()
    {
        var nanoDir = CreateNanoDir();
        WriteBootstrapFiles(nanoDir, includeLegacyDmx: true);
        var configPath = Path.Combine(nanoDir, "config.json");
        var configJson = File.ReadAllText(configPath)
            .Replace("\"provider\": \"siliconflow\"", "\"provider\": \"dmx\"")
            .Replace("\"model\": \"siliconflow::nex-agi/Nex-N2-Pro\"", "\"model\": \"dmx::deepseek-v4-pro-guan\"");
        File.WriteAllText(configPath, configJson);

        var config = ConfigLoader.Load(configPath);
        var settings = CreateStore(nanoDir).Get(config);

        Assert.Equal("siliconflow", settings.ProviderId);
        Assert.Equal("nex-agi/Nex-N2-Pro", settings.Model);
        Assert.DoesNotContain(settings.AvailableProviders, provider => provider.ProviderId == "dmx");
    }

    [Fact]
    public void Save_UpdatesSiliconFlowSelectionAndKeepsFallbackSiliconFlowOnly()
    {
        var nanoDir = CreateNanoDir();
        WriteBootstrapFiles(nanoDir);
        var configPath = Path.Combine(nanoDir, "config.json");
        File.WriteAllText(
            configPath,
            File.ReadAllText(configPath).Replace(
                "\"fallbackModels\": [\n        \"siliconflow::nex-agi/Nex-N2-Pro\"\n      ]",
                "\"fallbackModels\": [\n        \"siliconflow::nex-agi/Nex-N2-Pro\",\n        \"dmx::deepseek-v4-pro-guan\"\n      ]"));
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
        Assert.Equal(new[] { "siliconflow::Qwen/Qwen3.5-35B-A3B" }, reloaded.Agents.Defaults.FallbackModels);
        Assert.Equal("Qwen/Qwen3.5-35B-A3B", settings.Model);
        Assert.Contains(reloaded.Providers["siliconflow"].Models, model => model.Id == "nex-agi/Nex-N2-Pro");
        Assert.Contains(reloaded.Providers["siliconflow"].Models, model => model.Id == "Qwen/Qwen3.5-35B-A3B");
        Assert.Equal("new-sf-key", JsonDocument.Parse(File.ReadAllText(Path.Combine(nanoDir, "secrets.json")))
            .RootElement.GetProperty("siliconflow").GetProperty("apiKey").GetString());
        Assert.True(modelsJson.RootElement.GetProperty("providers").GetProperty("siliconflow").GetProperty("models").GetArrayLength() >= 2);
    }

    [Fact]
    public void Save_RejectsNonSiliconFlowProvider()
    {
        var nanoDir = CreateNanoDir();
        WriteBootstrapFiles(nanoDir, includeLegacyDmx: true);
        var config = ConfigLoader.Load(Path.Combine(nanoDir, "config.json"));
        var store = CreateStore(nanoDir);

        var error = Assert.Throws<InvalidOperationException>(() =>
            store.Save(new SaveModelSettingsRequest(
                ProviderId: "dmx",
                ApiKey: "dmx-key",
                ApiBase: "https://www.dmxapi.cn/v1/",
                Model: "deepseek-v4-pro-guan",
                ClearApiKey: false), config));

        Assert.Contains("SiliconFlow", error.Message);
    }

    [Fact]
    public void Save_RejectsInvalidApiBaseFromRequest()
    {
        var nanoDir = CreateNanoDir();
        WriteBootstrapFiles(nanoDir);
        var config = ConfigLoader.Load(Path.Combine(nanoDir, "config.json"));
        var store = CreateStore(nanoDir);

        var error = Assert.Throws<InvalidOperationException>(() =>
            store.Save(new SaveModelSettingsRequest(
                ProviderId: "siliconflow",
                ApiKey: "new-key",
                ApiBase: "not-a-url",
                Model: "nex-agi/Nex-N2-Pro",
                ClearApiKey: false), config));

        Assert.Contains("API 地址", error.Message);
    }

    private static ModelSettingsStore CreateStore(string nanoDir)
    {
        return new ModelSettingsStore(
            Path.Combine(nanoDir, "config.json"),
            Path.Combine(nanoDir, "models.json"),
            Path.Combine(nanoDir, "secrets.json"));
    }

    private static void WriteBootstrapFiles(string nanoDir, bool includeLegacyDmx = false)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        Directory.CreateDirectory(nanoDir);
        var catalog = DefaultProviderCatalog.CreateModelCatalog();
        if (includeLegacyDmx)
        {
            catalog.Providers["dmx"] = new ProviderModelDef
            {
                Name = "DMX API",
                ApiBase = "https://www.dmxapi.cn/v1/",
                DefaultModel = "deepseek-v4-pro-guan",
                Models = new List<ModelDef>
                {
                    new()
                    {
                        Id = "deepseek-v4-pro-guan",
                        ApiModelId = "deepseek-v4-pro-guan",
                        DisplayName = "DeepSeek V4 Pro",
                        SupportsStreaming = true,
                        SupportsTools = true
                    }
                }
            };
        }

        File.WriteAllText(Path.Combine(nanoDir, "models.json"), JsonSerializer.Serialize(catalog, options));
        File.WriteAllText(
            Path.Combine(nanoDir, "secrets.json"),
            includeLegacyDmx
                ? """
                  {
                    "siliconflow": {
                      "apiKey": "sf-initial-key"
                    },
                    "dmx": {
                      "apiKey": ""
                    }
                  }
                  """
                : """
                  {
                    "siliconflow": {
                      "apiKey": "sf-initial-key"
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
