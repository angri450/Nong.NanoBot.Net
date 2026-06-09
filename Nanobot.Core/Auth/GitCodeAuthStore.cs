using System.Text.Json;

namespace Nanobot.Core.Auth;

public class GitCodeAuthStore
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GitCodeAuthStore(string nanoDir)
    {
        var authDir = Path.Combine(nanoDir, "auth");
        Directory.CreateDirectory(authDir);
        _filePath = Path.Combine(authDir, "gitcode.json");
    }

    public GitCodeAuthData Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_filePath))
            {
                return new GitCodeAuthData();
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<GitCodeAuthData>(json, JsonOptions) ?? new GitCodeAuthData();
            }
            catch
            {
                return new GitCodeAuthData();
            }
        }
    }

    public void Save(GitCodeAuthData data)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
    }

    public string? GetValidAccessToken()
    {
        var data = Load();
        return data.IsLoggedIn ? data.Token!.AccessToken : null;
    }
}
