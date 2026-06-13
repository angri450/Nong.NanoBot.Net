using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Nanobot.Core.Config;

public static class ConfigLoader
{
    public static AppConfig Load(string configPath)
    {
        if (!Path.IsPathRooted(configPath))
            configPath = Path.Combine(Directory.GetCurrentDirectory(), configPath);

        var configDir = Path.GetDirectoryName(configPath)!;
        var modelsPath = Path.Combine(configDir, "models.json");
        var secretsPath = Path.Combine(configDir, "secrets.json");

        // 1. Load models catalog
        var modelCatalog = LoadModelCatalog(modelsPath);

        // 2. Load secrets
        var secrets = LoadSecrets(secretsPath);

        // 3. Load runtime config (agent defaults, gateway, tools — no keys)
        var builder = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: true, reloadOnChange: true);

        var configuration = builder.Build();
        var config = new AppConfig();
        configuration.Bind(config);

        // 4. Merge: inject model definitions and API keys into ProviderSettings
        foreach (var (providerKey, providerDef) in modelCatalog.Providers)
        {
            if (!config.Providers.TryGetValue(providerKey, out var ps))
            {
                ps = new ProviderSettings { Id = providerKey };
                config.Providers[providerKey] = ps;
            }

            ps.Id ??= providerKey;
            ps.Name ??= providerDef.Name;
            ps.Kind ??= "openai-compatible";
            ps.ApiBase ??= providerDef.ApiBase;
            ps.DefaultModel ??= providerDef.DefaultModel;

            // Inject API key from secrets
            if (secrets.TryGetValue(providerKey, out var secret) && !string.IsNullOrWhiteSpace(secret.ApiKey))
                ps.ApiKey = secret.ApiKey;

            // Inject model list from model catalog if not configured in config.json
            if (ps.Models.Count == 0 && providerDef.Models.Count > 0)
            {
                ps.Models = providerDef.Models.Select(m => new ModelSettings
                {
                    Id = m.Id,
                    ApiModelId = m.ApiModelId ?? m.Id,
                    DisplayName = m.DisplayName,
                    ContextWindow = m.ContextWindow,
                    MaxOutputTokens = m.MaxOutputTokens,
                    SupportsStreaming = m.SupportsStreaming,
                    SupportsTools = m.SupportsTools,
                    SupportsReasoning = m.SupportsReasoning,
                    SupportsInterleavedThinking = m.SupportsInterleavedThinking,
                    SupportsPromptCacheMetrics = m.SupportsPromptCacheMetrics,
                    ReasoningEffort = m.ReasoningEffort,
                    PlanAvailable = m.PlanAvailable,
                    ProviderModelFamily = m.ProviderModelFamily
                }).ToList();
            }
        }

        // Set default model from agents.defaults if not specified at provider level
        if (config.Agents.Defaults.Model != null)
        {
            var parts = config.Agents.Defaults.Model.Split("::", 2);
            if (parts.Length == 2)
            {
                var providerKey = parts[0];
                var modelId = parts[1];
                if (config.Providers.TryGetValue(providerKey, out var ps))
                    ps.DefaultModel ??= modelId;
            }
        }

        return config;
    }

    private static ModelCatalogFile LoadModelCatalog(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var catalog = JsonSerializer.Deserialize<ModelCatalogFile>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return catalog ?? new ModelCatalogFile();
            }
        }
        catch { /* missing or corrupt models.json → empty catalog */ }
        return new ModelCatalogFile();
    }

    private static Dictionary<string, SecretEntry> LoadSecrets(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var secrets = JsonSerializer.Deserialize<Dictionary<string, SecretEntry>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return secrets ?? new();
            }
        }
        catch { /* missing or corrupt secrets.json → no keys */ }
        return new();
    }
}

// === JSON file models for models.json ===

public class ModelCatalogFile
{
    public Dictionary<string, ProviderModelDef> Providers { get; set; } = new();
}

public class ProviderModelDef
{
    public string? Name { get; set; }
    public string? ApiBase { get; set; }
    public string? DefaultModel { get; set; }
    public List<ModelDef> Models { get; set; } = new();
}

public class ModelDef
{
    public string? Id { get; set; }
    public string? ApiModelId { get; set; }
    public string? DisplayName { get; set; }
    public int? ContextWindow { get; set; }
    public int? MaxOutputTokens { get; set; }
    public bool? SupportsStreaming { get; set; }
    public bool? SupportsTools { get; set; }
    public bool? SupportsReasoning { get; set; }
    public bool? SupportsInterleavedThinking { get; set; }
    public bool? SupportsPromptCacheMetrics { get; set; }
    public string? ReasoningEffort { get; set; }
    public bool? PlanAvailable { get; set; }
    public string? ProviderModelFamily { get; set; }
}

// === JSON file model for secrets.json ===

public class SecretEntry
{
    public string? ApiKey { get; set; }
}
