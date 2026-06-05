namespace Nanobot.Core.Config;

public sealed record ModelReference(string ProviderId, string ModelId)
{
    public const string Separator = "::";

    public string UniqueId => $"{ProviderId}{Separator}{ModelId}";

    public static ModelReference Parse(string value, string defaultProviderId = "openai")
    {
        if (TryParse(value, defaultProviderId, out var reference, out var error))
        {
            return reference;
        }

        throw new ArgumentException(error, nameof(value));
    }

    public static bool TryParse(
        string? value,
        string defaultProviderId,
        out ModelReference reference,
        out string error)
    {
        reference = null!;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Model reference is required.";
            return false;
        }

        var trimmed = value.Trim();
        var separatorIndex = trimmed.IndexOf(Separator, StringComparison.Ordinal);
        string providerId;
        string modelId;

        if (separatorIndex >= 0)
        {
            providerId = trimmed[..separatorIndex].Trim();
            modelId = trimmed[(separatorIndex + Separator.Length)..].Trim();
        }
        else
        {
            providerId = defaultProviderId.Trim();
            modelId = trimmed;
        }

        if (string.IsNullOrWhiteSpace(providerId))
        {
            error = $"Model reference '{value}' has an empty provider id.";
            return false;
        }

        if (providerId.Contains(Separator, StringComparison.Ordinal))
        {
            error = $"Provider id '{providerId}' cannot contain '{Separator}'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            error = $"Model reference '{value}' has an empty model id.";
            return false;
        }

        if (modelId.Contains('?') || modelId.Contains('#'))
        {
            error = $"Model id '{modelId}' cannot contain '?' or '#'.";
            return false;
        }

        reference = new ModelReference(providerId, modelId);
        return true;
    }

    public override string ToString() => UniqueId;
}
