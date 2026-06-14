using System.Text.Json;
using System.Text.Json.Nodes;
using Nanobot.Core.Config;
using Nanobot.Core.Providers;

namespace Nanobot.Web;

public sealed class ModelSettingsStore
{
    private readonly string _configFile;
    private readonly string _modelsFile;
    private readonly string _secretsFile;

    public ModelSettingsStore(string configFile, string modelsFile, string secretsFile)
    {
        _configFile = configFile;
        _modelsFile = modelsFile;
        _secretsFile = secretsFile;
    }

    public ModelSettingsResponse Get(AppConfig config, IReadOnlyDictionary<string, string?>? environment = null)
    {
        environment ??= ReadProcessEnvironment();

        var providers = BuildAvailableProviders(config);
        var selectedProviderId = ResolveSelectedProviderId(config, providers);
        var selectedProvider = providers.FirstOrDefault(provider =>
                                 provider.ProviderId.Equals(selectedProviderId, StringComparison.OrdinalIgnoreCase))
                             ?? providers.First();
        var selectedModel = ResolveSelectedModel(config, selectedProvider);
        var environmentKeyName = ResolveApiKeyEnvironmentVariable(selectedProvider.ProviderId);
        var environmentKey = environmentKeyName is null ? null : GetEnvironmentValue(environment, environmentKeyName);
        var configuredProvider = config.Providers.TryGetValue(selectedProvider.ProviderId, out var providerSettings)
            ? providerSettings
            : null;
        var configuredKey = configuredProvider?.ApiKey;
        var effectiveKey = !string.IsNullOrWhiteSpace(environmentKey) ? environmentKey : configuredKey;
        var keySource = !string.IsNullOrWhiteSpace(environmentKey)
            ? "environment"
            : !string.IsNullOrWhiteSpace(configuredKey)
                ? "config"
                : "none";

        return new ModelSettingsResponse(
            ProviderId: selectedProvider.ProviderId,
            ProviderName: selectedProvider.DisplayName,
            ApiBase: selectedProvider.ApiBase,
            Model: selectedModel,
            HasApiKey: !string.IsNullOrWhiteSpace(effectiveKey),
            ApiKeyPreview: MaskSecret(effectiveKey),
            KeySource: keySource,
            ConfigPath: _configFile,
            AvailableProviders: providers,
            EnvironmentKeyName: environmentKeyName);
    }

    public void Save(SaveModelSettingsRequest request, AppConfig currentConfig)
    {
        var providers = BuildAvailableProviders(currentConfig);
        var fallbackProviderId = ResolveSelectedProviderId(currentConfig, providers);
        var providerId = string.IsNullOrWhiteSpace(request.ProviderId)
            ? fallbackProviderId
            : request.ProviderId.Trim();
        if (!providerId.Equals(DefaultProviderCatalog.SiliconFlowProviderId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("当前分发版模型设置只支持 SiliconFlow。");
        }

        var provider = providers.FirstOrDefault(item => item.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase))
                       ?? throw new InvalidOperationException("SiliconFlow provider catalog is not available.");

        var apiBase = string.IsNullOrWhiteSpace(request.ApiBase)
            ? provider.ApiBase
            : request.ApiBase.Trim();

        if (string.IsNullOrWhiteSpace(apiBase)
            || !Uri.TryCreate(apiBase, UriKind.Absolute, out var apiBaseUri)
            || (apiBaseUri.Scheme != Uri.UriSchemeHttp && apiBaseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("API 地址必须是 http 或 https URL。");
        }

        var model = string.IsNullOrWhiteSpace(request.Model)
            ? provider.Models.FirstOrDefault()?.Id ?? DefaultProviderCatalog.GetDefaultModel(provider.ProviderId)
            : request.Model.Trim();

        Directory.CreateDirectory(Path.GetDirectoryName(_configFile)!);

        var modelsJson = LoadOrCreateJson(_modelsFile);
        var providersJson = EnsureObject(modelsJson, "providers");
        var providerJson = EnsureObject(providersJson, provider.ProviderId);
        SetProperty(providerJson, "name", provider.DisplayName);
        SetProperty(providerJson, "apiBase", apiBase);
        SetProperty(providerJson, "defaultModel", model);
        SetProperty(providerJson, "models", BuildModelsArray(provider, model));
        File.WriteAllText(_modelsFile, modelsJson.ToJsonString(IndentedJsonOptions));

        var secretsJson = LoadOrCreateJson(_secretsFile);
        var providerSecrets = EnsureObject(secretsJson, provider.ProviderId);
        SetProperty(providerSecrets, "apiKey", request.ClearApiKey
            ? string.Empty
            : string.IsNullOrWhiteSpace(request.ApiKey)
                ? providerSecrets["apiKey"]?.ToString() ?? string.Empty
                : request.ApiKey.Trim());
        File.WriteAllText(_secretsFile, secretsJson.ToJsonString(IndentedJsonOptions));

        var configJson = LoadOrCreateJson(_configFile);
        var agents = EnsureObject(configJson, "agents");
        var defaults = EnsureObject(agents, "defaults");
        SetProperty(defaults, "provider", provider.ProviderId);
        SetProperty(defaults, "model", $"{provider.ProviderId}::{model}");
        SetProperty(defaults, "fallbackModels", BuildFallbackModels(GetProperty(defaults, "fallbackModels"), provider.ProviderId, model));

        var streaming = EnsureObject(configJson, "streaming");
        SetProperty(streaming, "enabled", true);

        File.WriteAllText(_configFile, configJson.ToJsonString(IndentedJsonOptions));
    }

    public IReadOnlyList<ModelSettingsProviderOption> BuildAvailableProviders(AppConfig config)
    {
        var catalog = DefaultProviderCatalog.CreateModelCatalog();
        var providerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            DefaultProviderCatalog.SiliconFlowProviderId
        };

        return providerIds
            .Select(providerId => BuildProviderOption(providerId, config, catalog))
            .Where(option => !string.IsNullOrWhiteSpace(option.ApiBase))
            .OrderBy(option => option.ProviderId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ModelSettingsProviderOption BuildProviderOption(
        string providerId,
        AppConfig config,
        ModelCatalogFile catalog)
    {
        config.Providers.TryGetValue(providerId, out var configured);
        catalog.Providers.TryGetValue(providerId, out var preset);

        var apiBase = configured?.ApiBase
            ?? configured?.BaseUrl
            ?? preset?.ApiBase
            ?? DefaultProviderCatalog.GetDefaultApiBase(providerId);

        var displayName = configured?.Name
            ?? preset?.Name
            ?? DefaultProviderCatalog.GetDisplayName(providerId);

        var models = (configured?.Models?.Count ?? 0) > 0
            ? configured!.Models
                .Where(model => model.Enabled && !string.IsNullOrWhiteSpace(model.Id))
                .Select(model => new ModelSettingsModelOption(
                    model.Id!,
                    model.DisplayName ?? model.Id!))
                .ToList()
            : preset?.Models
                .Where(model => !string.IsNullOrWhiteSpace(model.Id))
                .Select(model => new ModelSettingsModelOption(
                    model.Id!,
                    model.DisplayName ?? model.Id!))
                .ToList()
              ?? new List<ModelSettingsModelOption>();

        var defaultModel = configured?.DefaultModel
            ?? preset?.DefaultModel
            ?? DefaultProviderCatalog.GetDefaultModel(providerId);

        if (!models.Any(model => model.Id.Equals(defaultModel, StringComparison.OrdinalIgnoreCase)))
        {
            models.Insert(0, new ModelSettingsModelOption(defaultModel, defaultModel));
        }

        return new ModelSettingsProviderOption(providerId, displayName, apiBase, models);
    }

    private static JsonArray BuildModelsArray(ModelSettingsProviderOption provider, string selectedModel)
    {
        var models = provider.Models.ToList();
        if (!models.Any(model => model.Id.Equals(selectedModel, StringComparison.OrdinalIgnoreCase)))
        {
            models.Insert(0, new ModelSettingsModelOption(selectedModel, selectedModel));
        }

        var array = new JsonArray();
        foreach (var model in models)
        {
            array.Add(new JsonObject
            {
                ["id"] = model.Id,
                ["apiModelId"] = model.Id,
                ["displayName"] = model.DisplayName,
                ["supportsStreaming"] = true,
                ["supportsTools"] = true,
                ["providerModelFamily"] = DeepSeekV4Models.IsDeepSeekV4(model.Id) ? "deepseek-v4" : null,
                ["supportsReasoning"] = true,
                ["supportsPromptCacheMetrics"] = DeepSeekV4Models.IsDeepSeekV4(model.Id),
                ["contextWindow"] = DeepSeekV4Models.IsDeepSeekV4(model.Id) ? 1_000_000 : null,
                ["maxOutputTokens"] = DeepSeekV4Models.IsDeepSeekV4(model.Id) ? 384_000 : null
            });
        }

        return array;
    }

    private static JsonArray BuildFallbackModels(JsonNode? existingNode, string providerId, string model)
    {
        var selected = $"{providerId}::{model}";
        return new JsonArray(selected);
    }

    private static string ResolveSelectedProviderId(AppConfig config, IReadOnlyList<ModelSettingsProviderOption> providers)
    {
        if (!string.IsNullOrWhiteSpace(config.Agents.Defaults.Model)
            && ModelReference.TryParse(
                config.Agents.Defaults.Model,
                config.Agents.Defaults.Provider ?? DefaultProviderCatalog.SiliconFlowProviderId,
                out var reference,
                out _))
        {
            return reference.ProviderId;
        }

        if (!string.IsNullOrWhiteSpace(config.Agents.Defaults.Provider))
        {
            return config.Agents.Defaults.Provider!;
        }

        if (providers.Count > 0)
        {
            return providers[0].ProviderId;
        }

        return DefaultProviderCatalog.SiliconFlowProviderId;
    }

    private static string ResolveSelectedModel(AppConfig config, ModelSettingsProviderOption provider)
    {
        if (!string.IsNullOrWhiteSpace(config.Agents.Defaults.Model)
            && ModelReference.TryParse(
                config.Agents.Defaults.Model,
                provider.ProviderId,
                out var reference,
                out _)
            && reference.ProviderId.Equals(provider.ProviderId, StringComparison.OrdinalIgnoreCase))
        {
            return reference.ModelId;
        }

        return provider.Models.FirstOrDefault()?.Id ?? DefaultProviderCatalog.GetDefaultModel(provider.ProviderId);
    }

    private static ModelSettingsProviderOption CreateAdHocProvider(string providerId, string? apiBase, string? model)
    {
        var normalizedApiBase = string.IsNullOrWhiteSpace(apiBase)
            ? DefaultProviderCatalog.GetDefaultApiBase(providerId)
            : apiBase.Trim();
        var normalizedModel = string.IsNullOrWhiteSpace(model)
            ? DefaultProviderCatalog.GetDefaultModel(providerId)
            : model.Trim();

        return new ModelSettingsProviderOption(
            providerId,
            DefaultProviderCatalog.GetDisplayName(providerId),
            normalizedApiBase,
            new[] { new ModelSettingsModelOption(normalizedModel, normalizedModel) });
    }

    public static string? ResolveApiKeyEnvironmentVariable(string providerId)
    {
        return providerId.Equals(DefaultProviderCatalog.SiliconFlowProviderId, StringComparison.OrdinalIgnoreCase)
            ? "SILICONFLOW_API_KEY"
            : null;
    }

    private static string? GetEnvironmentValue(IReadOnlyDictionary<string, string?> environment, string key)
    {
        return environment.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private static IReadOnlyDictionary<string, string?> ReadProcessEnvironment()
    {
        return Environment.GetEnvironmentVariables()
            .Keys
            .OfType<string>()
            .ToDictionary(
                key => key,
                key => Environment.GetEnvironmentVariable(key),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string MaskSecret(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return string.Empty;
        }

        var value = secret.Trim();
        if (value.Length <= 10)
        {
            return "已配置";
        }

        return $"{value[..6]}...{value[^4..]}";
    }

    private static JsonObject LoadOrCreateJson(string path)
    {
        if (!File.Exists(path))
        {
            return new JsonObject();
        }

        var text = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidOperationException($"{Path.GetFileName(path)} root must be a JSON object.");
    }

    private static JsonObject EnsureObject(JsonObject parent, string propertyName)
    {
        var existingKey = parent
            .Select(item => item.Key)
            .FirstOrDefault(key => key.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

        if (existingKey is not null && parent[existingKey] is JsonObject existing)
        {
            return existing;
        }

        var created = new JsonObject();
        SetProperty(parent, propertyName, created);
        return created;
    }

    private static JsonNode? GetProperty(JsonObject parent, string propertyName)
    {
        var existingKey = parent
            .Select(item => item.Key)
            .FirstOrDefault(key => key.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        return existingKey is null ? null : parent[existingKey];
    }

    private static void SetProperty(JsonObject parent, string propertyName, JsonNode? value)
    {
        var duplicateKeys = parent
            .Select(item => item.Key)
            .Where(key => key.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in duplicateKeys)
        {
            parent.Remove(key);
        }

        parent[propertyName] = value;
    }

    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };
}
