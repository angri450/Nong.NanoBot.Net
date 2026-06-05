using Nanobot.Core.Config;
using Nanobot.Core.Models;

namespace Nanobot.Core.Channels;

public static class ChannelFactory
{
    public static IMessageChannel Create(
        string name,
        ChannelSettings settings,
        ChannelMessageHandler onMessage,
        HttpClient? httpClient = null)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return normalized switch
        {
            "telegram" => new TelegramChannel(
                Require(settings.Token ?? settings.BotToken, "telegram.token"),
                inbound => onMessage(inbound, CancellationToken.None)),
            "slack" => new SlackChannel(
                Require(settings.Token ?? settings.BotToken, "slack.token"),
                Require(settings.Endpoint, "slack.endpoint"),
                onMessage,
                httpClient),
            "discord" => new DiscordChannel(
                Require(settings.Token ?? settings.BotToken, "discord.token"),
                Require(settings.Endpoint, "discord.endpoint"),
                onMessage,
                httpClient),
            "feishu" => new FeishuChannel(
                Require(settings.AppId, "feishu.appId"),
                Require(settings.AppSecret, "feishu.appSecret"),
                Require(settings.Endpoint, "feishu.endpoint"),
                onMessage,
                httpClient),
            _ => throw new InvalidOperationException($"Unsupported channel '{name}'.")
        };
    }

    private static string Require(string? value, string field)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Channel config missing required field '{field}'.")
            : value;
    }
}
