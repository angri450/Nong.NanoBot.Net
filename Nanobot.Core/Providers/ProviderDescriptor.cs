namespace Nanobot.Core.Providers;

public record ProviderDescriptor(
    string Name,
    string Kind,
    string DefaultModel,
    ProviderCapabilities Capabilities
);
