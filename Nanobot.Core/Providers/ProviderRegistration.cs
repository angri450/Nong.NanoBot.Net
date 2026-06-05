namespace Nanobot.Core.Providers;

public record ProviderRegistration(
    ProviderDescriptor Descriptor,
    ILLMProvider Provider
)
{
    public string Name => Descriptor.Name;
}
