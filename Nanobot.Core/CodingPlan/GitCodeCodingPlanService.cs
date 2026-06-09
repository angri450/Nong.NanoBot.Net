using System.Text.Json;
using System.Text.Json.Nodes;
using Nanobot.Core.Auth;
using Nanobot.Core.Providers;

namespace Nanobot.Core.CodingPlan;

public class GitCodeCodingPlanService
{
    private readonly GitCodeCodingPlanClient _client;
    private readonly GitCodeAuthStore _authStore;
    private readonly string _configFile;

    public GitCodeCodingPlanService(
        GitCodeCodingPlanClient client,
        GitCodeAuthStore authStore,
        string configFile)
    {
        _client = client;
        _authStore = authStore;
        _configFile = configFile;
    }

    public async Task<CodingPlanSetupResult> SetupAsync()
    {
        var result = new CodingPlanSetupResult();

        // Step 1: Check auth
        var authData = _authStore.Load();
        if (!authData.IsLoggedIn)
        {
            result.Steps["login"] = new CodingPlanStep { Status = "failed", Message = "Not logged in. Please login first." };
            return result;
        }

        result.Steps["login"] = new CodingPlanStep { Status = "skipped", Message = "Already logged in." };

        // Step 2: Claim cascade (Max -> Pro -> Lite)
        var planTypes = new[] { PlanType.Max, PlanType.Pro, PlanType.Lite };
        GitCodeCodingPlanClaim? lastClaim = null;

        foreach (var planType in planTypes)
        {
            lastClaim = await _client.ClaimPlanAsync(planType);
            if (lastClaim.Status is "success" or "duplicate" or "already_claimed")
            {
                result.Steps["claim"] = new CodingPlanStep
                {
                    Status = "ok",
                    Message = $"CodingPlan {planType} {lastClaim.Status}"
                };
                break;
            }
        }

        if (result.Steps["claim"].Status != "ok")
        {
            result.Steps["claim"] = new CodingPlanStep
            {
                Status = "failed",
                Message = lastClaim?.Message ?? "All claim attempts failed."
            };
        }

        // Step 3: Sync models from all plan types
        var allModels = new List<GitCodeModelEntry>();
        foreach (var planType in planTypes)
        {
            try
            {
                var models = await _client.ListModelsAsync(planType);
                allModels.AddRange(models);
            }
            catch
            {
                // Continue with whatever we got
            }
        }

        var uniqueModels = allModels
            .GroupBy(m => m.DisplayModelName)
            .Select(g => g.First())
            .ToList();

        if (uniqueModels.Count > 0)
        {
            SyncModelsToConfig(uniqueModels);
            result.Models = uniqueModels;
            result.Steps["models"] = new CodingPlanStep
            {
                Status = "ok",
                Message = $"{uniqueModels.Count} models synced"
            };
        }
        else
        {
            result.Steps["models"] = new CodingPlanStep
            {
                Status = "failed",
                Message = "No models returned from CodingPlan."
            };
        }

        // Step 4: Get status
        try
        {
            var status = await _client.GetStatusAsync();
            result.Steps["status"] = new CodingPlanStep
            {
                Status = "ok",
                Message = status.Message ?? $"Plan: {status.PlanType}, Used: {status.UsedTokens}/{status.TotalTokens}"
            };
        }
        catch
        {
            result.Steps["status"] = new CodingPlanStep
            {
                Status = "failed",
                Message = "Could not retrieve usage status."
            };
        }

        result.DefaultProvider = "gitcode";
        result.Success = result.Steps["models"].Status == "ok";
        return result;
    }

    private void SyncModelsToConfig(List<GitCodeModelEntry> models)
    {
        var configJson = ReadConfigJson();
        var providers = EnsureObject(configJson, "providers");
        var gitcode = EnsureObject(providers, "gitcode");

        gitcode["kind"] = "gitcode-codingplan";
        gitcode["enabled"] = true;

        var settings = EnsureObject(gitcode, "settings");
        settings["apiBase"] = "https://api.gitcode.com/api/v5";

        var modelsArray = new JsonArray();
        foreach (var model in models)
        {
            var modelId = model.ModelName ?? NormalizeId(model.DisplayModelName);
            var isDeepSeek = DeepSeekV4Models.IsDeepSeekV4(modelId);

            var entry = new JsonObject
            {
                ["id"] = modelId,
                ["apiModelId"] = modelId,
                ["displayName"] = model.DisplayModelName,
                ["enabled"] = model.PlanAvailable,
                ["planAvailable"] = model.PlanAvailable,
                ["supportsStreaming"] = true,
                ["supportsTools"] = true,
                ["contextWindow"] = model.ContextWindow > 0 ? model.ContextWindow : 64000,
                ["providerModelFamily"] = isDeepSeek ? "deepseek-v4" : null,
                ["supportsReasoning"] = isDeepSeek,
                ["supportsPromptCacheMetrics"] = isDeepSeek,
                ["baseUrl"] = model.BaseUrl
            };

            modelsArray.Add(entry);
        }

        gitcode["models"] = modelsArray;

        // Set first available model as default
        var defaultModel = models.FirstOrDefault(m => m.PlanAvailable);
        if (defaultModel is not null)
        {
            var defaultModelId = defaultModel.ModelName ?? NormalizeId(defaultModel.DisplayModelName);
            gitcode["defaultModel"] = defaultModelId;

            var agents = EnsureObject(configJson, "agents");
            var defaults = EnsureObject(agents, "defaults");
            defaults["provider"] = "gitcode";
            defaults["model"] = $"gitcode::{defaultModelId}";
        }

        WriteConfigJson(configJson);
    }

    private static string NormalizeId(string name)
    {
        return name.ToLowerInvariant().Replace(" ", "-").Replace("/", "-");
    }

    private JsonObject ReadConfigJson()
    {
        if (!File.Exists(_configFile))
        {
            return new JsonObject();
        }

        var text = File.ReadAllText(_configFile);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(text) as JsonObject ?? new JsonObject();
    }

    private void WriteConfigJson(JsonObject configJson)
    {
        var dir = Path.GetDirectoryName(_configFile);
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(
            _configFile,
            configJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static JsonObject EnsureObject(JsonObject parent, string propertyName)
    {
        if (parent[propertyName] is JsonObject existing)
        {
            return existing;
        }

        var created = new JsonObject();
        parent[propertyName] = created;
        return created;
    }
}
