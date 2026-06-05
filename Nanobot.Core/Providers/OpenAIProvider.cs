namespace Nanobot.Core.Providers;

public class OpenAIProvider : OpenAICompatibleProvider
{
    public OpenAIProvider(string apiKey, string? baseUrl = null, string defaultModel = "gpt-4o")
        : base(apiKey, baseUrl, defaultModel)
    {
    }
}
