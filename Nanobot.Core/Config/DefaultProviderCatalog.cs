namespace Nanobot.Core.Config;

public static class DefaultProviderCatalog
{
    public const string DmxProviderId = "dmx";
    public const string DmxDisplayName = "DMX API";
    public const string DmxDefaultApiBase = "https://www.dmxapi.cn/v1/";
    public const string DmxDefaultModel = "deepseek-v4-pro-guan";

    public const string SiliconFlowProviderId = "siliconflow";
    public const string SiliconFlowDisplayName = "SiliconFlow";
    public const string SiliconFlowDefaultApiBase = "https://api.siliconflow.cn/v1/";
    public const string SiliconFlowDefaultModel = "nex-agi/Nex-N2-Pro";

    public static ModelCatalogFile CreateModelCatalog()
    {
        return new ModelCatalogFile
        {
            Providers = new Dictionary<string, ProviderModelDef>(StringComparer.OrdinalIgnoreCase)
            {
                [DmxProviderId] = new ProviderModelDef
                {
                    Name = DmxDisplayName,
                    ApiBase = DmxDefaultApiBase,
                    DefaultModel = DmxDefaultModel,
                    Models = new List<ModelDef>
                    {
                        new()
                        {
                            Id = DmxDefaultModel,
                            ApiModelId = DmxDefaultModel,
                            DisplayName = "DeepSeek V4 Pro",
                            ContextWindow = 1_000_000,
                            MaxOutputTokens = 32000,
                            SupportsStreaming = true,
                            SupportsTools = true,
                            SupportsReasoning = true,
                            SupportsInterleavedThinking = true,
                            ReasoningEffort = "high"
                        }
                    }
                },
                [SiliconFlowProviderId] = new ProviderModelDef
                {
                    Name = SiliconFlowDisplayName,
                    ApiBase = SiliconFlowDefaultApiBase,
                    DefaultModel = SiliconFlowDefaultModel,
                    Models = new List<ModelDef>
                    {
                        new()
                        {
                            Id = "nex-agi/Nex-N2-Pro",
                            ApiModelId = "nex-agi/Nex-N2-Pro",
                            DisplayName = "Nex-N2 Pro",
                            ContextWindow = 131072,
                            MaxOutputTokens = 32768,
                            SupportsStreaming = true,
                            SupportsTools = true,
                            SupportsReasoning = true,
                            ReasoningEffort = "high"
                        },
                        new()
                        {
                            Id = "deepseek-ai/DeepSeek-V3.2",
                            ApiModelId = "deepseek-ai/DeepSeek-V3.2",
                            DisplayName = "DeepSeek V3.2",
                            ContextWindow = 131072,
                            MaxOutputTokens = 8192,
                            SupportsStreaming = true,
                            SupportsTools = true,
                            SupportsReasoning = true
                        },
                        new()
                        {
                            Id = "Pro/deepseek-ai/DeepSeek-V3.2",
                            ApiModelId = "Pro/deepseek-ai/DeepSeek-V3.2",
                            DisplayName = "DeepSeek V3.2 (Pro)",
                            ContextWindow = 131072,
                            MaxOutputTokens = 8192,
                            SupportsStreaming = true,
                            SupportsTools = true,
                            SupportsReasoning = true
                        },
                        new()
                        {
                            Id = "deepseek-ai/DeepSeek-V4-Flash",
                            ApiModelId = "deepseek-ai/DeepSeek-V4-Flash",
                            DisplayName = "DeepSeek V4 Flash",
                            ContextWindow = 131072,
                            MaxOutputTokens = 16384,
                            SupportsStreaming = true,
                            SupportsTools = true,
                            SupportsReasoning = true,
                            ReasoningEffort = "high"
                        },
                        new()
                        {
                            Id = "Pro/zai-org/GLM-5",
                            ApiModelId = "Pro/zai-org/GLM-5",
                            DisplayName = "GLM-5",
                            ContextWindow = 131072,
                            MaxOutputTokens = 8192,
                            SupportsStreaming = true,
                            SupportsTools = true,
                            SupportsReasoning = true
                        },
                        new()
                        {
                            Id = "Qwen/Qwen3.5-397B-A17B",
                            ApiModelId = "Qwen/Qwen3.5-397B-A17B",
                            DisplayName = "Qwen3.5 397B MoE",
                            ContextWindow = 131072,
                            MaxOutputTokens = 8192,
                            SupportsStreaming = true,
                            SupportsTools = true,
                            SupportsReasoning = true
                        },
                        new()
                        {
                            Id = "Qwen/Qwen3.5-35B-A3B",
                            ApiModelId = "Qwen/Qwen3.5-35B-A3B",
                            DisplayName = "Qwen3.5 35B MoE",
                            ContextWindow = 131072,
                            MaxOutputTokens = 8192,
                            SupportsStreaming = true,
                            SupportsTools = true,
                            SupportsReasoning = true
                        },
                        new()
                        {
                            Id = "tencent/Hunyuan-A13B-Instruct",
                            ApiModelId = "tencent/Hunyuan-A13B-Instruct",
                            DisplayName = "Hunyuan A13B",
                            ContextWindow = 131072,
                            MaxOutputTokens = 8192,
                            SupportsStreaming = true,
                            SupportsTools = true,
                            SupportsReasoning = true
                        }
                    }
                }
            }
        };
    }

    public static Dictionary<string, SecretEntry> CreateSecretsTemplate()
    {
        return new Dictionary<string, SecretEntry>(StringComparer.OrdinalIgnoreCase)
        {
            [DmxProviderId] = new SecretEntry { ApiKey = string.Empty },
            [SiliconFlowProviderId] = new SecretEntry { ApiKey = string.Empty }
        };
    }

    public static AppConfig CreateRuntimeConfigTemplate()
    {
        return new AppConfig
        {
            Agents = new AgentSettings
            {
                Defaults = new DefaultAgentSettings
                {
                    Provider = SiliconFlowProviderId,
                    Model = $"{SiliconFlowProviderId}::{SiliconFlowDefaultModel}",
                    FallbackModels = new List<string> { $"{SiliconFlowProviderId}::{SiliconFlowDefaultModel}" }
                }
            },
            Streaming = new StreamingSettings { Enabled = true },
            Gateway = new GatewaySettings
            {
                WebSocket = new WebSocketGatewaySettings
                {
                    Prefix = "http://localhost:8765/ws/",
                    Token = string.Empty
                }
            },
            Tools = new ToolSettings
            {
                Nong = new NongToolSettings
                {
                    Enabled = true,
                    Command = "nong",
                    AppendJson = true,
                    TimeoutMs = 120000,
                    MaxOutputChars = 20000,
                    AllowedRoots = new List<string>
                    {
                        "commands",
                        "word",
                        "inspect",
                        "chart",
                        "excel",
                        "diagram",
                        "genre",
                        "icons",
                        "skill",
                        "pptx",
                        "ocr",
                        "pdf",
                        "lit",
                        "slice",
                        "progress"
                    }
                }
            }
        };
    }

    public static bool TryGetProvider(string providerId, out ProviderModelDef provider)
    {
        var catalog = CreateModelCatalog();
        return catalog.Providers.TryGetValue(providerId, out provider!);
    }

    public static string GetDefaultApiBase(string providerId)
    {
        return TryGetProvider(providerId, out var provider) && !string.IsNullOrWhiteSpace(provider.ApiBase)
            ? provider.ApiBase!
            : providerId.Equals(DmxProviderId, StringComparison.OrdinalIgnoreCase)
                ? DmxDefaultApiBase
                : SiliconFlowDefaultApiBase;
    }

    public static string GetDefaultModel(string providerId)
    {
        return TryGetProvider(providerId, out var provider) && !string.IsNullOrWhiteSpace(provider.DefaultModel)
            ? provider.DefaultModel!
            : providerId.Equals(DmxProviderId, StringComparison.OrdinalIgnoreCase)
                ? DmxDefaultModel
                : SiliconFlowDefaultModel;
    }

    public static string GetDisplayName(string providerId)
    {
        return TryGetProvider(providerId, out var provider) && !string.IsNullOrWhiteSpace(provider.Name)
            ? provider.Name!
            : providerId;
    }
}
