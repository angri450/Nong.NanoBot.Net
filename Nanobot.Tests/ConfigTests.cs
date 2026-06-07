using Nanobot.Core.Config;
using Nanobot.Core.Providers;
using Microsoft.Extensions.Configuration;

namespace Nanobot.Tests;

public class ConfigTests
{
    [Fact]
    public void ModelReference_ParsesUniqueModelId()
    {
        var reference = ModelReference.Parse("openrouter::openai/gpt-4o");

        Assert.Equal("openrouter", reference.ProviderId);
        Assert.Equal("openai/gpt-4o", reference.ModelId);
        Assert.Equal("openrouter::openai/gpt-4o", reference.UniqueId);
    }

    [Fact]
    public void ModelReference_ParsesBareModelWithDefaultProvider()
    {
        var reference = ModelReference.Parse("gpt-4o", "openai");

        Assert.Equal("openai", reference.ProviderId);
        Assert.Equal("gpt-4o", reference.ModelId);
    }

    [Fact]
    public void ModelReference_RejectsMalformedUniqueModelId()
    {
        var error = Assert.Throws<ArgumentException>(() => ModelReference.Parse("openai::"));

        Assert.Contains("empty model", error.Message);
    }

    [Fact]
    public void ProviderConfigurationFactory_EnvironmentModelOverridesConfig()
    {
        var config = new AppConfig
        {
            Providers =
            {
                ["openai"] = new ProviderSettings
                {
                    ApiKey = "config-key",
                    DefaultModel = "config-model"
                }
            },
            Agents =
            {
                Defaults =
                {
                    Model = "openai::config-model"
                }
            }
        };

        var result = ProviderConfigurationFactory.Create(config, Env(
            ("OPENAI_API_KEY", "env-key"),
            ("OPENAI_MODEL", "openai::env-model")
        ));

        Assert.Equal("openai::env-model", result.DefaultModel.UniqueId);
        Assert.Equal("env-model", result.Provider.GetDefaultModel());
    }

    [Fact]
    public void ProviderConfigurationFactory_KeepsLegacyBareModelCompatible()
    {
        var config = new AppConfig
        {
            Providers =
            {
                ["openai"] = new ProviderSettings
                {
                    ApiKey = "test-key"
                }
            },
            Agents =
            {
                Defaults =
                {
                    Model = "gpt-legacy"
                }
            }
        };

        var result = ProviderConfigurationFactory.Create(config, Env());

        Assert.Equal("openai", result.DefaultModel.ProviderId);
        Assert.Equal("gpt-legacy", result.DefaultModel.ModelId);
        Assert.Equal("gpt-legacy", result.Provider.GetDefaultModel());
    }

    [Fact]
    public void ProviderConfigurationFactory_UsesModelApiModelIdForProviderCalls()
    {
        var config = new AppConfig
        {
            Providers =
            {
                ["openrouter"] = new ProviderSettings
                {
                    Kind = "openai-compatible",
                    ApiKey = "test-key",
                    ApiBase = "https://openrouter.example/v1",
                    Models =
                    {
                        new ModelSettings
                        {
                            Id = "gpt-4o",
                            ApiModelId = "openai/gpt-4o"
                        }
                    }
                }
            },
            Agents =
            {
                Defaults =
                {
                    Model = "openrouter::gpt-4o"
                }
            }
        };

        var result = ProviderConfigurationFactory.Create(config, Env());

        Assert.Equal("openrouter::gpt-4o", result.DefaultModel.UniqueId);
        Assert.Equal("openai/gpt-4o", result.Provider.GetDefaultModel());
    }

    [Fact]
    public void ProviderConfigurationFactory_ConfiguresFallbackChainInOrder()
    {
        var config = new AppConfig
        {
            Providers =
            {
                ["openai"] = new ProviderSettings
                {
                    ApiKey = "primary-key",
                    DefaultModel = "primary-model"
                },
                ["backup"] = new ProviderSettings
                {
                    Kind = "openai-compatible",
                    ApiKey = "backup-key",
                    ApiBase = "https://backup.example/v1",
                    DefaultModel = "backup-model"
                }
            },
            Agents =
            {
                Defaults =
                {
                    FallbackModels = { "openai::primary-model", "backup::backup-model" }
                }
            }
        };

        var result = ProviderConfigurationFactory.Create(config, Env());

        Assert.IsType<FallbackLLMProvider>(result.Provider);
        Assert.Equal(new[] { "openai::primary-model", "backup::backup-model" }, result.FallbackModels.Select(model => model.UniqueId));
        Assert.Equal("primary-model", result.Provider.GetDefaultModel());
    }

    [Fact]
    public void ProviderConfigurationFactory_RejectsMissingReferencedProvider()
    {
        var config = new AppConfig
        {
            Agents =
            {
                Defaults =
                {
                    Model = "missing::model-a"
                }
            }
        };

        var error = Assert.Throws<ProviderConfigurationException>(() =>
            ProviderConfigurationFactory.Create(config, Env()));

        Assert.Contains("missing", error.Message);
    }

    [Fact]
    public void ProviderConfigurationFactory_SkipsUnusedIncompleteProvider()
    {
        var config = new AppConfig
        {
            Providers =
            {
                ["openai"] = new ProviderSettings
                {
                    Kind = "openai-compatible",
                    ApiKey = ""
                },
                ["anthropic"] = new ProviderSettings
                {
                    Kind = "anthropic",
                    ApiKey = "anthropic-key",
                    DefaultModel = "claude-test"
                }
            },
            Agents =
            {
                Defaults =
                {
                    Model = "anthropic::claude-test"
                }
            }
        };

        var result = ProviderConfigurationFactory.Create(config, Env());

        Assert.Equal("anthropic::claude-test", result.DefaultModel.UniqueId);
        Assert.Equal("claude-test", result.Provider.GetDefaultModel());
    }

    [Fact]
    public void AppConfig_BindsCustomNongAllowedRootsWithoutDefaultAppend()
    {
        var source = new Dictionary<string, string?>
        {
            ["tools:nong:allowedRoots:0"] = "pdf",
            ["tools:nong:allowedRoots:1"] = "ocr"
        };
        var config = new AppConfig();

        new ConfigurationBuilder()
            .AddInMemoryCollection(source)
            .Build()
            .Bind(config);

        Assert.Equal(new[] { "pdf", "ocr" }, config.Tools.Nong.AllowedRoots);
    }

    private static IReadOnlyDictionary<string, string?> Env(params (string Key, string? Value)[] values)
    {
        return values.ToDictionary(
            value => value.Key,
            value => value.Value,
            StringComparer.OrdinalIgnoreCase
        );
    }
}
