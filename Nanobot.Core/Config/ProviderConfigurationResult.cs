using Nanobot.Core.Providers;

namespace Nanobot.Core.Config;

public sealed record ProviderConfigurationResult(
    ProviderRegistry Registry,
    ILLMProvider Provider,
    ModelReference DefaultModel,
    IReadOnlyList<ModelReference> FallbackModels,
    bool StreamingEnabled
);
