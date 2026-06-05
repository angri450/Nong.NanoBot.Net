using Nanobot.Core.Models;

namespace Nanobot.Core.Channels;

public interface IMessageChannel
{
    string Name { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task SendAsync(OutboundMessage message, CancellationToken cancellationToken = default);
}

public delegate Task<OutboundMessage?> ChannelMessageHandler(InboundMessage message, CancellationToken cancellationToken);
