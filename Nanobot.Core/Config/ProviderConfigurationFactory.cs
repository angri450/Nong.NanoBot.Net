using Nanobot.Core.Providers;

namespace Nanobot.Core.Config;

public static class ProviderConfigurationFactory
{
    private const string OpenAIProviderId = "openai";
    private const string DmxProviderId = DefaultProviderCatalog.DmxProviderId;
    private const string DmxDefaultApiBase = DefaultProviderCatalog.DmxDefaultApiBase;
    private const string DmxDefaultModel = DefaultProviderCatalog.DmxDefaultModel;
    private const string SiliconFlowProviderId = DefaultProviderCatalog.SiliconFlowProviderId;
    private const string SiliconFlowDefaultApiBase = DefaultProviderCatalog.SiliconFlowDefaultApiBase;
    private const string SiliconFlowDefaultModel = DefaultProviderCatalog.SiliconFlowDefaultModel;

    public static ProviderConfigurationResult Create(
        AppConfig config,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        environment ??= ReadProcessEnvironment();

        var providers = CloneProviders(config.Providers);
        ApplyEnvironmentOverrides(providers, config, environment);

        var defaultProviderId = config.Agents.Defaults.Provider ?? OpenAIProviderId;
        var defaultModel = ResolveDefaultModel(config, providers, defaultProviderId);
        var fallbackModels = ResolveFallbackModels(config, defaultModel, defaultProviderId);

        var registry = new ProviderRegistry();
        var requiredProviderIds = fallbackModels
            .Select(model => model.ProviderId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var createdProviders = CreateEnabledProviders(providers, registry, requiredProviderIds);

        foreach (var modelReference in fallbackModels)
        {
            if (!createdProviders.ContainsKey(modelReference.ProviderId))
            {
                throw new ProviderConfigurationException(
                    $"Model '{modelReference}' references provider '{modelReference.ProviderId}', but that provider is not configured or is disabled."
                );
            }
        }

        var fallbackRegistrations = fallbackModels
            .Select(reference => CreateModelBoundRegistration(reference, providers, createdProviders))
            .ToList();

        var selectedProvider = fallbackRegistrations.Count == 1
            ? fallbackRegistrations[0].Provider
            : new FallbackLLMProvider(fallbackRegistrations);

        var streamingEnabled = ResolveStreamingEnabled(config, environment);

        return new ProviderConfigurationResult(
            registry,
            selectedProvider,
            defaultModel,
            fallbackModels,
            streamingEnabled
        );
    }

    private static Dictionary<string, ProviderSettings> CloneProviders(Dictionary<string, ProviderSettings> providers)
    {
        return providers.ToDictionary(
            pair => pair.Key,
            pair => CloneProvider(pair.Value),
            StringComparer.OrdinalIgnoreCase
        );
    }

    private static ProviderSettings CloneProvider(ProviderSettings source)
    {
        return new ProviderSettings
        {
            Id = source.Id,
            Name = source.Name,
            Kind = source.Kind,
            Type = source.Type,
            Enabled = source.Enabled,
            ApiKey = source.ApiKey,
            ApiBase = source.ApiBase,
            BaseUrl = source.BaseUrl,
            Endpoint = source.Endpoint,
            Deployment = source.Deployment,
            ApiVersion = source.ApiVersion,
            DefaultModel = source.DefaultModel,
            Models = source.Models.Select(model => new ModelSettings
            {
                Id = model.Id,
                ApiModelId = model.ApiModelId,
                Enabled = model.Enabled,
                SupportsStreaming = model.SupportsStreaming,
                SupportsTools = model.SupportsTools,
                DisplayName = model.DisplayName,
                ContextWindow = model.ContextWindow,
                MaxOutputTokens = model.MaxOutputTokens,
                SupportsReasoning = model.SupportsReasoning,
                SupportsInterleavedThinking = model.SupportsInterleavedThinking,
                SupportsPromptCacheMetrics = model.SupportsPromptCacheMetrics,
                ReasoningEffort = model.ReasoningEffort,
                PlanAvailable = model.PlanAvailable,
                ProviderModelFamily = model.ProviderModelFamily
            }).ToList(),
            Capabilities = new ProviderCapabilitySettings
            {
                Chat = source.Capabilities.Chat,
                Tools = source.Capabilities.Tools,
                Streaming = source.Capabilities.Streaming,
                Images = source.Capabilities.Images
            },
            Settings = new Dictionary<string, string>(source.Settings, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static void ApplyEnvironmentOverrides(
        Dictionary<string, ProviderSettings> providers,
        AppConfig config,
        IReadOnlyDictionary<string, string?> environment)
    {
        var hasDmxApiKey = TryGetEnvironmentValue(environment, "DMX_API_KEY", out _);
        var hasDmxApiBase = TryGetEnvironmentValue(environment, "DMX_API_BASE", out _);
        var hasDmxModelOverride = TryGetEnvironmentValue(environment, "DMX_MODEL", out var dmxModelOverride);
        var hasDmxEnvironment = hasDmxApiKey || hasDmxApiBase || hasDmxModelOverride;
        var hasSiliconFlowApiKey = TryGetEnvironmentValue(environment, "SILICONFLOW_API_KEY", out _);
        var hasSiliconFlowApiBase = TryGetEnvironmentValue(environment, "SILICONFLOW_API_BASE", out _);
        var hasSiliconFlowModelOverride = TryGetEnvironmentValue(environment, "SILICONFLOW_MODEL", out var siliconFlowModelOverride);
        var hasSiliconFlowEnvironment = hasSiliconFlowApiKey || hasSiliconFlowApiBase || hasSiliconFlowModelOverride;

        var hasOpenAiModelOverride = TryGetEnvironmentValue(environment, "OPENAI_MODEL", out var openAiModelOverride);
        var shouldCreateOpenAi = providers.ContainsKey(OpenAIProviderId)
            || TryGetEnvironmentValue(environment, "OPENAI_API_KEY", out _)
            || TryGetEnvironmentValue(environment, "OPENAI_API_BASE", out _)
            || (!hasDmxEnvironment && !hasSiliconFlowEnvironment && RequiresOpenAIDefault(config, openAiModelOverride));

        ProviderSettings? openAi = null;
        if (shouldCreateOpenAi)
        {
            openAi = GetOrCreateProvider(providers, OpenAIProviderId);
            openAi.Kind ??= "openai-compatible";

            ApplyIfPresent(environment, "OPENAI_API_KEY", value => openAi.ApiKey = value);
            ApplyIfPresent(environment, "OPENAI_API_BASE", value => openAi.ApiBase = value);
        }

        if (hasOpenAiModelOverride)
        {
            if (ModelReference.TryParse(openAiModelOverride, OpenAIProviderId, out var reference, out _))
            {
                config.Agents.Defaults.Provider = reference.ProviderId;
                config.Agents.Defaults.Model = reference.UniqueId;
            }
            else if (openAi is not null)
            {
                openAi.DefaultModel = openAiModelOverride;
                config.Agents.Defaults.Model = openAiModelOverride;
            }
        }

        var shouldCreateDmx = providers.ContainsKey(DmxProviderId)
            || hasDmxApiKey
            || hasDmxApiBase
            || RequiresDmxDefault(config, dmxModelOverride);

        ProviderSettings? dmx = null;
        if (shouldCreateDmx)
        {
            dmx = GetOrCreateProvider(providers, DmxProviderId);
            dmx.Kind ??= "openai-compatible";
            dmx.ApiBase ??= DmxDefaultApiBase;
            dmx.DefaultModel ??= DmxDefaultModel;

            ApplyIfPresent(environment, "DMX_API_KEY", value => dmx.ApiKey = value);
            ApplyIfPresent(environment, "DMX_API_BASE", value => dmx.ApiBase = value);
        }

        if (hasDmxModelOverride)
        {
            if (ModelReference.TryParse(dmxModelOverride, DmxProviderId, out var reference, out _))
            {
                config.Agents.Defaults.Provider = reference.ProviderId;
                config.Agents.Defaults.Model = reference.UniqueId;
            }
            else if (dmx is not null)
            {
                dmx.DefaultModel = dmxModelOverride;
                config.Agents.Defaults.Model = dmxModelOverride;
            }
        }
        else if (hasDmxEnvironment && dmx is not null)
        {
            config.Agents.Defaults.Provider = DmxProviderId;
            config.Agents.Defaults.Model = new ModelReference(DmxProviderId, dmx.DefaultModel ?? DmxDefaultModel).UniqueId;
        }

        var shouldCreateSiliconFlow = providers.ContainsKey(SiliconFlowProviderId)
            || hasSiliconFlowApiKey
            || hasSiliconFlowApiBase
            || RequiresSpecificDefault(config, SiliconFlowProviderId, siliconFlowModelOverride);

        ProviderSettings? siliconFlow = null;
        if (shouldCreateSiliconFlow)
        {
            siliconFlow = GetOrCreateProvider(providers, SiliconFlowProviderId);
            siliconFlow.Kind ??= "openai-compatible";
            siliconFlow.ApiBase ??= SiliconFlowDefaultApiBase;
            siliconFlow.DefaultModel ??= SiliconFlowDefaultModel;

            ApplyIfPresent(environment, "SILICONFLOW_API_KEY", value => siliconFlow.ApiKey = value);
            ApplyIfPresent(environment, "SILICONFLOW_API_BASE", value => siliconFlow.ApiBase = value);
        }

        if (hasSiliconFlowModelOverride)
        {
            if (ModelReference.TryParse(siliconFlowModelOverride, SiliconFlowProviderId, out var reference, out _))
            {
                config.Agents.Defaults.Provider = reference.ProviderId;
                config.Agents.Defaults.Model = reference.UniqueId;
            }
            else if (siliconFlow is not null)
            {
                siliconFlow.DefaultModel = siliconFlowModelOverride;
                config.Agents.Defaults.Model = siliconFlowModelOverride;
            }
        }
        else if (hasSiliconFlowEnvironment && siliconFlow is not null)
        {
            config.Agents.Defaults.Provider = SiliconFlowProviderId;
            config.Agents.Defaults.Model = new ModelReference(SiliconFlowProviderId, siliconFlow.DefaultModel ?? SiliconFlowDefaultModel).UniqueId;
        }

        if (TryGetEnvironmentValue(environment, "ANTHROPIC_API_KEY", out var anthropicKey))
        {
            var anthropic = GetOrCreateProvider(providers, "anthropic");
            anthropic.Kind ??= "anthropic";
            anthropic.ApiKey = anthropicKey;
            ApplyIfPresent(environment, "ANTHROPIC_API_BASE", value => anthropic.ApiBase = value);
            ApplyIfPresent(environment, "ANTHROPIC_MODEL", value => anthropic.DefaultModel = value);
        }

        if (TryGetEnvironmentValue(environment, "AZURE_OPENAI_API_KEY", out var azureKey))
        {
            var azure = GetOrCreateProvider(providers, "azure-openai");
            azure.Kind ??= "azure-openai";
            azure.ApiKey = azureKey;
            ApplyIfPresent(environment, "AZURE_OPENAI_ENDPOINT", value => azure.Endpoint = value);
            ApplyIfPresent(environment, "AZURE_OPENAI_DEPLOYMENT", value => azure.Deployment = value);
            ApplyIfPresent(environment, "AZURE_OPENAI_API_VERSION", value => azure.ApiVersion = value);
        }
    }

    private static bool RequiresOpenAIDefault(AppConfig config, string? openAiModelOverride)
    {
        if (config.Providers.Count == 0)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(openAiModelOverride)
            && ModelReference.TryParse(openAiModelOverride, OpenAIProviderId, out var envModel, out _)
            && envModel.ProviderId.Equals(OpenAIProviderId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(config.Agents.Defaults.Provider)
            && config.Agents.Defaults.Provider.Equals(OpenAIProviderId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(config.Agents.Defaults.Model))
        {
            return config.Providers.ContainsKey(OpenAIProviderId);
        }

        if (ModelReference.TryParse(config.Agents.Defaults.Model, OpenAIProviderId, out var configuredModel, out _))
        {
            return configuredModel.ProviderId.Equals(OpenAIProviderId, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool RequiresDmxDefault(AppConfig config, string? dmxModelOverride)
    {
        return RequiresSpecificDefault(config, DmxProviderId, dmxModelOverride);
    }

    private static bool RequiresSpecificDefault(AppConfig config, string providerId, string? modelOverride)
    {
        if (!string.IsNullOrWhiteSpace(modelOverride)
            && ModelReference.TryParse(modelOverride, providerId, out var envModel, out _)
            && envModel.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(config.Agents.Defaults.Provider)
            && config.Agents.Defaults.Provider.Equals(providerId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(config.Agents.Defaults.Model))
        {
            return config.Providers.ContainsKey(providerId);
        }

        if (ModelReference.TryParse(config.Agents.Defaults.Model, providerId, out var configuredModel, out _))
        {
            return configuredModel.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static ProviderSettings GetOrCreateProvider(
        Dictionary<string, ProviderSettings> providers,
        string providerId)
    {
        if (providers.TryGetValue(providerId, out var provider))
        {
            return provider;
        }

        provider = new ProviderSettings { Id = providerId };
        providers[providerId] = provider;
        return provider;
    }

    private static ModelReference ResolveDefaultModel(
        AppConfig config,
        Dictionary<string, ProviderSettings> providers,
        string defaultProviderId)
    {
        var configuredModel = config.Agents.Defaults.Model;
        if (!string.IsNullOrWhiteSpace(configuredModel))
        {
            return ParseModelReference(configuredModel, defaultProviderId);
        }

        if (providers.TryGetValue(defaultProviderId, out var defaultProvider)
            && !string.IsNullOrWhiteSpace(defaultProvider.DefaultModel))
        {
            return new ModelReference(defaultProviderId, defaultProvider.DefaultModel!);
        }

        var firstProviderWithModel = providers
            .Where(pair => pair.Value.Enabled && IsLLMProvider(pair.Key, pair.Value))
            .Select(pair => new
            {
                ProviderId = pair.Key,
                Model = ResolveProviderDefaultModel(pair.Key, pair.Value)
            })
            .FirstOrDefault(pair => !string.IsNullOrWhiteSpace(pair.Model));

        if (firstProviderWithModel is not null)
        {
            return new ModelReference(firstProviderWithModel.ProviderId, firstProviderWithModel.Model!);
        }

        return new ModelReference(OpenAIProviderId, "gpt-4o");
    }

    private static IReadOnlyList<ModelReference> ResolveFallbackModels(
        AppConfig config,
        ModelReference defaultModel,
        string defaultProviderId)
    {
        var configuredFallbacks = config.Agents.Defaults.FallbackModels;
        if (configuredFallbacks.Count == 0)
        {
            return new[] { defaultModel };
        }

        return configuredFallbacks
            .Select(model => ParseModelReference(model, defaultProviderId))
            .ToList();
    }

    private static ModelReference ParseModelReference(string value, string defaultProviderId)
    {
        try
        {
            return ModelReference.Parse(value, defaultProviderId);
        }
        catch (ArgumentException ex)
        {
            throw new ProviderConfigurationException(ex.Message);
        }
    }

    private static Dictionary<string, ILLMProvider> CreateEnabledProviders(
        Dictionary<string, ProviderSettings> providers,
        ProviderRegistry registry,
        IReadOnlySet<string> requiredProviderIds)
    {
        var created = new Dictionary<string, ILLMProvider>(StringComparer.OrdinalIgnoreCase);

        foreach (var (providerId, settings) in providers)
        {
            if (!settings.Enabled || !IsLLMProvider(providerId, settings))
            {
                continue;
            }

            ILLMProvider provider;
            try
            {
                provider = CreateProvider(providerId, settings);
            }
            catch (ProviderConfigurationException) when (!requiredProviderIds.Contains(providerId))
            {
                continue;
            }

            created[providerId] = provider;
            registry.Register(providerId, provider, new ProviderDescriptor(
                providerId,
                ResolveKind(providerId, settings),
                provider.GetDefaultModel(),
                ResolveCapabilities(settings)
            ));
        }

        return created;
    }

    private static ILLMProvider CreateProvider(string providerId, ProviderSettings settings)
    {
        var kind = ResolveKind(providerId, settings);
        var defaultModel = ResolveProviderDefaultModel(providerId, settings);
        var apiKey = RequireApiKey(providerId, settings);
        var apiBase = settings.ApiBase ?? settings.BaseUrl;

        // Detect DeepSeek V4 models: if any enabled model is DeepSeek V4 family,
        // or the default model itself is a DeepSeek V4 model, use DeepSeekV4Provider
        var isDeepSeekV4 = IsDeepSeekV4Provider(settings, defaultModel);

        return kind switch
        {
            "openai" or "openai-compatible" when isDeepSeekV4 && !string.IsNullOrWhiteSpace(apiKey) =>
                new DeepSeekV4Provider(apiKey, apiBase, new DeepSeekV4Options
                {
                    Model = defaultModel,
                    ReasoningEffort = ResolveReasoningEffort(settings, defaultModel) ?? "high"
                }),
            "openai" or "openai-compatible" => new OpenAIProvider(
                apiKey,
                apiBase,
                defaultModel ?? "gpt-4o"
            ),
            "anthropic" => new AnthropicProvider(
                apiKey,
                defaultModel ?? "claude-sonnet-4-5",
                baseUrl: apiBase
            ),
            "azure-openai" or "azure" => new AzureOpenAIProvider(
                RequireField(providerId, settings.Endpoint ?? apiBase, "endpoint"),
                apiKey,
                RequireField(providerId, settings.Deployment ?? defaultModel, "deployment"),
                settings.ApiVersion ?? AzureOpenAIProvider.DefaultApiVersion
            ),
            _ => throw new ProviderConfigurationException(
                $"Provider '{providerId}' has unsupported kind '{kind}'. Supported kinds: openai-compatible, anthropic, azure-openai."
            )
        };
    }

    private static bool IsDeepSeekV4Provider(ProviderSettings settings, string? defaultModel)
    {
        if (!string.IsNullOrWhiteSpace(defaultModel) && DeepSeekV4Models.IsDeepSeekV4(defaultModel))
        {
            return true;
        }

        return settings.Models.Any(model =>
            model.Enabled
            && !string.IsNullOrWhiteSpace(model.ProviderModelFamily)
            && model.ProviderModelFamily.Equals("deepseek-v4", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveReasoningEffort(ProviderSettings settings, string? modelId)
    {
        var model = settings.Models.FirstOrDefault(item =>
            item.Enabled && string.Equals(item.Id, modelId, StringComparison.OrdinalIgnoreCase));
        return model?.ReasoningEffort;
    }

    private static ProviderRegistration CreateModelBoundRegistration(
        ModelReference reference,
        Dictionary<string, ProviderSettings> providerSettings,
        Dictionary<string, ILLMProvider> createdProviders)
    {
        var settings = providerSettings[reference.ProviderId];
        var provider = createdProviders[reference.ProviderId];
        var apiModelId = ResolveApiModelId(settings, reference.ModelId);
        var boundProvider = new ModelBoundLLMProvider(provider, apiModelId);
        var descriptor = new ProviderDescriptor(
            reference.UniqueId,
            ResolveKind(reference.ProviderId, settings),
            apiModelId,
            ResolveCapabilities(settings, reference.ModelId)
        );

        return new ProviderRegistration(descriptor, boundProvider);
    }

    private static string? ResolveProviderDefaultModel(string providerId, ProviderSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.DefaultModel))
        {
            return settings.DefaultModel;
        }

        var enabledModel = settings.Models.FirstOrDefault(model => model.Enabled && !string.IsNullOrWhiteSpace(model.Id));
        if (enabledModel is not null)
        {
            return enabledModel.ApiModelId ?? enabledModel.Id;
        }

        return ResolveKind(providerId, settings) switch
        {
            "openai" or "openai-compatible" => "gpt-4o",
            "anthropic" => "claude-sonnet-4-5",
            _ => null
        };
    }

    private static string ResolveApiModelId(ProviderSettings settings, string modelId)
    {
        var modelSettings = settings.Models.FirstOrDefault(model =>
            model.Enabled
            && string.Equals(model.Id, modelId, StringComparison.OrdinalIgnoreCase)
        );

        return string.IsNullOrWhiteSpace(modelSettings?.ApiModelId)
            ? modelId
            : modelSettings.ApiModelId!;
    }

    private static string ResolveKind(string providerId, ProviderSettings settings)
    {
        var kind = settings.Kind ?? settings.Type;
        if (!string.IsNullOrWhiteSpace(kind))
        {
            return kind.Trim().ToLowerInvariant();
        }

        if (providerId.Equals(OpenAIProviderId, StringComparison.OrdinalIgnoreCase))
        {
            return "openai-compatible";
        }

        if (providerId.Equals("anthropic", StringComparison.OrdinalIgnoreCase))
        {
            return "anthropic";
        }

        if (providerId.Equals("azure-openai", StringComparison.OrdinalIgnoreCase)
            || providerId.Equals("azure", StringComparison.OrdinalIgnoreCase))
        {
            return "azure-openai";
        }

        if (!string.IsNullOrWhiteSpace(settings.ApiBase) || !string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            return "openai-compatible";
        }

        return providerId.ToLowerInvariant();
    }

    private static bool IsLLMProvider(string providerId, ProviderSettings settings)
    {
        var kind = ResolveKind(providerId, settings);
        return kind is "openai" or "openai-compatible" or "anthropic" or "azure-openai" or "azure";
    }

    private static ProviderCapabilities ResolveCapabilities(ProviderSettings settings, string? modelId = null)
    {
        var capabilities = ProviderCapabilities.None;
        if (settings.Capabilities.Chat)
        {
            capabilities |= ProviderCapabilities.Chat;
        }

        var model = modelId is null
            ? null
            : settings.Models.FirstOrDefault(item =>
                item.Enabled && string.Equals(item.Id, modelId, StringComparison.OrdinalIgnoreCase));

        var toolsSupported = model?.SupportsTools ?? settings.Capabilities.Tools;
        if (toolsSupported)
        {
            capabilities |= ProviderCapabilities.Tools;
        }

        var streamingSupported = model?.SupportsStreaming ?? settings.Capabilities.Streaming;
        if (streamingSupported)
        {
            capabilities |= ProviderCapabilities.Streaming;
        }

        if (settings.Capabilities.Images)
        {
            capabilities |= ProviderCapabilities.Images;
        }

        var reasoningSupported = model?.SupportsReasoning ?? false;
        if (reasoningSupported)
        {
            capabilities |= ProviderCapabilities.Reasoning;
        }

        return capabilities;
    }

    private static string RequireApiKey(string providerId, ProviderSettings settings)
    {
        return RequireField(providerId, settings.ApiKey, "apiKey");
    }

    private static string RequireField(string providerId, string? value, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new ProviderConfigurationException(
            $"Provider '{providerId}' is missing required field '{fieldName}'."
        );
    }

    private static bool ResolveStreamingEnabled(
        AppConfig config,
        IReadOnlyDictionary<string, string?> environment)
    {
        if (TryGetEnvironmentValue(environment, "NANOBOT_STREAMING", out var value))
        {
            return value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        return config.Streaming.Enabled;
    }

    private static void ApplyIfPresent(
        IReadOnlyDictionary<string, string?> environment,
        string name,
        Action<string> apply)
    {
        if (TryGetEnvironmentValue(environment, name, out var value))
        {
            apply(value);
        }
    }

    private static bool TryGetEnvironmentValue(
        IReadOnlyDictionary<string, string?> environment,
        string name,
        out string value)
    {
        if (environment.TryGetValue(name, out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            value = raw;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static IReadOnlyDictionary<string, string?> ReadProcessEnvironment()
    {
        return Environment.GetEnvironmentVariables()
            .Keys
            .OfType<string>()
            .ToDictionary(
                key => key,
                key => Environment.GetEnvironmentVariable(key),
                StringComparer.OrdinalIgnoreCase
            );
    }
}
