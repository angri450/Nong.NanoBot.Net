namespace Nanobot.Core.Providers;

public class ProviderRegistry
{
    private readonly Dictionary<string, ProviderRegistration> _providers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, ILLMProvider provider, ProviderDescriptor? descriptor = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Provider name is required.", nameof(name));
        }

        descriptor ??= new ProviderDescriptor(
            name,
            provider.GetType().Name,
            provider.GetDefaultModel(),
            ProviderCapabilities.Chat
        );

        _providers[name] = new ProviderRegistration(descriptor, provider);
    }

    public bool TryResolve(string name, out ILLMProvider provider)
    {
        if (_providers.TryGetValue(name, out var registration))
        {
            provider = registration.Provider;
            return true;
        }

        provider = null!;
        return false;
    }

    public ILLMProvider Resolve(string name)
    {
        if (TryResolve(name, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException($"Provider '{name}' is not registered.");
    }

    public bool TryGetRegistration(string name, out ProviderRegistration registration)
    {
        return _providers.TryGetValue(name, out registration!);
    }

    public IReadOnlyList<ProviderRegistration> GetRegistrations()
    {
        return _providers.Values.OrderBy(provider => provider.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
