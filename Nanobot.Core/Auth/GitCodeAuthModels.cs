using System.Text.Json.Serialization;

namespace Nanobot.Core.Auth;

public sealed record GitCodeLoginState
{
    public string LoginId { get; init; } = "";
    public string State { get; init; } = "";
    public string LoginUrl { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public GitCodeLoginStatus Status { get; set; } = GitCodeLoginStatus.Pending;
}

public enum GitCodeLoginStatus
{
    Pending,
    Authorized,
    Expired,
    Failed
}

public sealed record GitCodeTokenSet
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = "";

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAt => CreatedAt.AddSeconds(ExpiresIn - 300); // 5 min buffer

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    public bool IsValid => !string.IsNullOrWhiteSpace(AccessToken) && !IsExpired;
}

public sealed record GitCodeUserInfo
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("login")]
    public string Login { get; init; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; init; }
}

public sealed record GitCodeAuthData
{
    public GitCodeTokenSet? Token { get; set; }

    public GitCodeUserInfo? User { get; set; }

    public DateTimeOffset? LoggedInAt { get; set; }

    public bool IsLoggedIn => Token is not null && Token.IsValid;

    public bool NeedsRefresh => Token is not null && !Token.IsExpired && Token.ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(5);
}
