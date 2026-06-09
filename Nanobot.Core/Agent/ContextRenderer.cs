using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nanobot.Core.Models;
using Nanobot.Core.Tools;

namespace Nanobot.Core.Agent;

public class ContextRenderer
{
    private readonly Tools.ToolRegistry _toolRegistry;
    private ContextFingerprint? _lastFingerprint;
    private string? _lastCacheChange;

    public ContextRenderer(Tools.ToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
    }

    public RenderedContext Render(
        string systemInstruction,
        string? projectGuidance,
        List<Message> history,
        string userInput,
        string? dynamicStatus = null,
        int contextWindow = 1_000_000,
        bool isDeepSeekV4 = false)
    {
        var staticPrefix = BuildStaticPrefix(systemInstruction, projectGuidance, isDeepSeekV4);
        var frozenHistory = FreezeHistory(history, isDeepSeekV4);
        var dynamicTail = BuildDynamicTail(userInput, dynamicStatus);
        var fingerprint = ComputeFingerprint(staticPrefix, frozenHistory, dynamicTail);

        var previousFingerprint = _lastFingerprint;
        _lastFingerprint = fingerprint;

        if (previousFingerprint is not null)
        {
            _lastCacheChange = fingerprint.DescribeChange(previousFingerprint);
        }

        var estimatedTokens = EstimateTokens(staticPrefix) + EstimateTokens(frozenHistory) + EstimateTokens(dynamicTail);

        return new RenderedContext
        {
            StaticPrefix = staticPrefix,
            StableHistory = frozenHistory,
            DynamicTail = dynamicTail,
            Fingerprint = fingerprint,
            EstimatedTokenCount = estimatedTokens,
            ContextFillRatio = contextWindow > 0 ? (double)estimatedTokens / contextWindow : 0,
            ContextWindow = contextWindow
        };
    }

    public string? LastCacheChange => _lastCacheChange;

    private List<Message> BuildStaticPrefix(string systemInstruction, string? projectGuidance, bool isDeepSeekV4)
    {
        var prefix = new List<Message>
        {
            new("system", systemInstruction)
        };

        if (!string.IsNullOrWhiteSpace(projectGuidance))
        {
            prefix.Add(new("user", projectGuidance) { Metadata = new() { ["type"] = "project_guidance" } });
        }

        // Tool catalog as a system message for stable prefix positioning
        var toolSchema = GetStableToolSchema(isDeepSeekV4);
        if (!string.IsNullOrWhiteSpace(toolSchema))
        {
            prefix.Add(new("system", toolSchema) { Metadata = new() { ["type"] = "tool_catalog" } });
        }

        return prefix;
    }

    private static List<Message> FreezeHistory(List<Message> history, bool isDeepSeekV4)
    {
        var frozen = new List<Message>(history.Count);
        foreach (var msg in history)
        {
            var frozenMsg = msg with { };
            if (!isDeepSeekV4 || msg.ToolCalls is null || msg.ToolCalls.Count == 0)
            {
                // For non-DeepSeek or non-tool-call responses, strip reasoning from history
                frozenMsg = frozenMsg with { ReasoningContent = null };
            }

            frozen.Add(frozenMsg);
        }

        return frozen;
    }

    private static List<Message> BuildDynamicTail(string userInput, string? dynamicStatus)
    {
        var tail = new List<Message> { new("user", userInput) };

        if (!string.IsNullOrWhiteSpace(dynamicStatus))
        {
            tail.Add(new("user", dynamicStatus)
            {
                Metadata = new() { ["type"] = "dynamic_status" }
            });
        }

        return tail;
    }

    private string GetStableToolSchema(bool isDeepSeekV4)
    {
        var definitions = _toolRegistry.GetDefinitions();
        if (definitions is null || definitions.Count == 0)
        {
            return "";
        }

        // Sort by name for stability
        var sorted = definitions
            .Select(def => new
            {
                Name = def["function"]?["name"]?.ToString() ?? "",
                Schema = def
            })
            .Where(def => !string.IsNullOrEmpty(def.Name))
            .OrderBy(def => def.Name, StringComparer.Ordinal)
            .Select(def => def.Schema)
            .ToList();

        if (sorted.Count == 0)
        {
            return "";
        }

        var catalog = new JsonObject { ["tools"] = new JsonArray(sorted.ToArray()) };
        return catalog.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static ContextFingerprint ComputeFingerprint(
        List<Message> staticPrefix,
        List<Message> frozenHistory,
        List<Message> dynamicTail)
    {
        return new ContextFingerprint
        {
            SystemHash = HashContent(staticPrefix.FirstOrDefault()?.Content ?? ""),
            ProjectGuidanceHash = HashContent(staticPrefix
                .Where(m => m.Metadata?.GetValueOrDefault("type") == "project_guidance")
                .Select(m => m.Content ?? "")
                .FirstOrDefault() ?? ""),
            ToolSchemaHash = HashContent(staticPrefix
                .Where(m => m.Metadata?.GetValueOrDefault("type") == "tool_catalog")
                .Select(m => m.Content ?? "")
                .FirstOrDefault() ?? ""),
            FrozenHistoryPrefixHash = HashMessages(frozenHistory),
            DynamicTailHash = HashMessages(dynamicTail),
            SentTokensEstimate = EstimateTokens(staticPrefix) + EstimateTokens(frozenHistory) + EstimateTokens(dynamicTail)
        };
    }

    private static string HashContent(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return "";
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..16];
    }

    private static string HashMessages(List<Message> messages)
    {
        if (messages.Count == 0)
        {
            return "";
        }

        var sb = new StringBuilder();
        foreach (var msg in messages)
        {
            sb.Append(msg.Role);
            sb.Append('|');
            sb.Append(msg.Content ?? "");
            sb.Append('|');
            if (msg.ToolCalls is not null)
            {
                foreach (var tc in msg.ToolCalls)
                {
                    sb.Append(tc.Name);
                    sb.Append(':');
                    sb.Append(tc.Id);
                    sb.Append(';');
                }
            }

            sb.Append('\n');
        }

        return HashContent(sb.ToString());
    }

    private static int EstimateTokens(List<Message> messages)
    {
        var charCount = 0;
        foreach (var msg in messages)
        {
            charCount += msg.Content?.Length ?? 0;
            charCount += msg.ReasoningContent?.Length ?? 0;
        }

        return charCount / 3; // rough estimate: ~3 chars per token
    }
}
