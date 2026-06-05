namespace Nanobot.Core.Providers;

[Flags]
public enum ProviderCapabilities
{
    None = 0,
    Chat = 1,
    Tools = 2,
    Streaming = 4,
    Images = 8
}
