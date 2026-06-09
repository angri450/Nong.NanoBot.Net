using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nanobot.Core.CodingPlan;

public class GitCodeCodingPlanClient
{
    private const string DefaultApiBase = "https://api.gitcode.com/api/v5";

    private readonly Func<string?> _tokenProvider;
    private readonly HttpClient _httpClient;
    private readonly string _apiBase;

    public GitCodeCodingPlanClient(
        Func<string?> tokenProvider,
        HttpClient? httpClient = null,
        string? apiBase = null)
    {
        _tokenProvider = tokenProvider;
        _httpClient = httpClient ?? new HttpClient();
        _apiBase = (apiBase ?? DefaultApiBase).TrimEnd('/');
    }

    public async Task<GitCodeCodingPlanClaim> ClaimPlanAsync(PlanType planType)
    {
        var request = CreateRequest(HttpMethod.Post, "/coding-plan/claim-v2", new JsonObject
        {
            ["plan_type"] = planType.ToString()
        });

        var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new GitCodeCodingPlanClaim
            {
                PlanType = planType.ToString(),
                Status = "error",
                Message = $"HTTP {(int)response.StatusCode}: {body}"
            };
        }

        return JsonSerializer.Deserialize<GitCodeCodingPlanClaim>(body, JsonOptions)
            ?? new GitCodeCodingPlanClaim { PlanType = planType.ToString(), Status = "error", Message = "Empty response" };
    }

    public async Task<List<GitCodeModelEntry>> ListModelsAsync(PlanType planType)
    {
        var url = $"/coding-plan/models-v2?plan_type={planType}";
        var request = CreateRequest(HttpMethod.Get, url);
        var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new List<GitCodeModelEntry>();
        }

        var root = JsonNode.Parse(body) as JsonObject;
        var models = root?["models"] as JsonArray;
        if (models is null)
        {
            return new List<GitCodeModelEntry>();
        }

        return models.Select(m => JsonSerializer.Deserialize<GitCodeModelEntry>(
            m!.ToJsonString(), JsonOptions)!).Where(m => m is not null).ToList();
    }

    public async Task<GitCodeCodingPlanStatus> GetStatusAsync()
    {
        var request = CreateRequest(HttpMethod.Get, "/coding-plan/status-v2");
        var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new GitCodeCodingPlanStatus { Status = "error", Message = $"HTTP {(int)response.StatusCode}: {body}" };
        }

        return JsonSerializer.Deserialize<GitCodeCodingPlanStatus>(body, JsonOptions)
            ?? new GitCodeCodingPlanStatus { Status = "error", Message = "Empty response" };
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, JsonObject? body = null)
    {
        var request = new HttpRequestMessage(method, $"{_apiBase}{path}");

        var token = _tokenProvider();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Add("Authorization", $"Bearer {token}");
        }

        if (body is not null)
        {
            request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        }

        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
