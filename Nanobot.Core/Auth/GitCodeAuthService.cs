using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nanobot.Core.Auth;

public class GitCodeAuthService
{
    private const string DefaultPlatformBase = "https://acs.atomgit.com";

    private readonly GitCodeAuthStore _store;
    private readonly HttpClient _httpClient;
    private readonly string _platformBase;

    public GitCodeAuthService(GitCodeAuthStore store, HttpClient? httpClient = null, string? platformBase = null)
    {
        _store = store;
        _httpClient = httpClient ?? new HttpClient();
        _platformBase = (platformBase ?? DefaultPlatformBase).TrimEnd('/');
    }

    public async Task<GitCodeLoginState> StartLoginAsync()
    {
        var url = $"{_platformBase}/auth/login?provider=atomgit";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var root = JsonNode.Parse(body) as JsonObject;
        var loginUrl = root?["login_url"]?.ToString();
        var state = root?["state"]?.ToString();

        if (string.IsNullOrWhiteSpace(loginUrl) || string.IsNullOrWhiteSpace(state))
        {
            throw new InvalidOperationException("OAuth login start failed: missing login_url or state in response.");
        }

        return new GitCodeLoginState
        {
            LoginId = Guid.NewGuid().ToString("N"),
            State = state,
            LoginUrl = loginUrl,
            Status = GitCodeLoginStatus.Pending
        };
    }

    public async Task<GitCodeLoginState> PollLoginAsync(GitCodeLoginState loginState)
    {
        var url = $"{_platformBase}/auth/check?state={Uri.EscapeDataString(loginState.State)}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var root = JsonNode.Parse(body) as JsonObject;
        var status = root?["status"]?.ToString();

        loginState.Status = status switch
        {
            "authorized" => GitCodeLoginStatus.Authorized,
            "pending" => GitCodeLoginStatus.Pending,
            "expired" => GitCodeLoginStatus.Expired,
            _ => GitCodeLoginStatus.Failed
        };

        return loginState;
    }

    public async Task FinishLoginAsync(GitCodeLoginState loginState)
    {
        if (loginState.Status != GitCodeLoginStatus.Authorized)
        {
            throw new InvalidOperationException($"Cannot finish login in state: {loginState.Status}");
        }

        var url = $"{_platformBase}/auth/token?state={Uri.EscapeDataString(loginState.State)}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var tokenSet = JsonSerializer.Deserialize<GitCodeTokenSet>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (tokenSet is null || string.IsNullOrWhiteSpace(tokenSet.AccessToken))
        {
            throw new InvalidOperationException("Failed to parse OAuth token response.");
        }

        tokenSet = tokenSet with { CreatedAt = DateTimeOffset.UtcNow };

        // Fetch user info
        GitCodeUserInfo? userInfo = null;
        try
        {
            userInfo = await FetchUserInfoAsync(tokenSet.AccessToken);
        }
        catch
        {
            // User info is optional; login still succeeds
        }

        var authData = new GitCodeAuthData
        {
            Token = tokenSet,
            User = userInfo,
            LoggedInAt = DateTimeOffset.UtcNow
        };

        _store.Save(authData);
    }

    public async Task<bool> RefreshTokenIfNeededAsync()
    {
        var data = _store.Load();
        if (data.Token is null || string.IsNullOrWhiteSpace(data.Token.RefreshToken))
        {
            return false;
        }

        if (!data.NeedsRefresh)
        {
            return data.IsLoggedIn;
        }

        return await RefreshTokenAsync(data);
    }

    private async Task<bool> RefreshTokenAsync(GitCodeAuthData data)
    {
        try
        {
            var requestBody = new JsonObject
            {
                ["refresh_token"] = data.Token!.RefreshToken
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_platformBase}/oauth/refresh")
            {
                Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var body = await response.Content.ReadAsStringAsync();
            var tokenSet = JsonSerializer.Deserialize<GitCodeTokenSet>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenSet is null || string.IsNullOrWhiteSpace(tokenSet.AccessToken))
            {
                return false;
            }

            // Preserve refresh token if new one not provided
            if (string.IsNullOrWhiteSpace(tokenSet.RefreshToken))
            {
                tokenSet = tokenSet with { RefreshToken = data.Token.RefreshToken };
            }

            data.Token = tokenSet with { CreatedAt = DateTimeOffset.UtcNow };
            _store.Save(data);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Logout()
    {
        _store.Clear();
    }

    public GitCodeAuthData GetStatus()
    {
        return _store.Load();
    }

    public string? GetValidAccessToken()
    {
        var data = _store.Load();
        if (data.NeedsRefresh)
        {
            var refreshed = RefreshTokenAsync(data).GetAwaiter().GetResult();
            if (refreshed)
            {
                data = _store.Load();
            }
        }

        return data.IsLoggedIn ? data.Token!.AccessToken : null;
    }

    private async Task<GitCodeUserInfo?> FetchUserInfoAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_platformBase}/user");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitCodeUserInfo>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
