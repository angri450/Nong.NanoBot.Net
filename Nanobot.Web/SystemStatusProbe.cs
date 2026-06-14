using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Nanobot.Web;

public static class SystemStatusProbe
{
    private static readonly ExternalToolDefinition[] ExternalToolDefinitions =
    {
        new("nong-chart", "Angri450.Nong.Tool.Chart"),
        new("nong-diagram", "Angri450.Nong.Tool.Diagram"),
        new("nong-pdf", "Angri450.Nong.Tool.Pdf"),
        new("nong-pptx", "Angri450.Nong.Tool.Pptx"),
        new("nong-ocr", "Angri450.Nong.Tool.Ocr"),
        new("nong-imaging", "Angri450.Nong.Tool.Imaging")
    };

    public static NongStatusResponse? ProbeNongStatus()
    {
        var result = TryRunProcess("nong", new[] { "commands", "--json" }, 5000);
        if (result is null || result.TimedOut || result.ExitCode != 0)
        {
            return null;
        }

        var externalTools = ProbeExternalTools();
        var ocrModels = ProbeOcrModels();
        return ParseNongStatus(result.Stdout, externalTools, ocrModels);
    }

    public static IReadOnlyList<ExternalToolStatus> ProbeExternalTools()
    {
        var result = TryRunProcess("dotnet", new[] { "tool", "list", "--global" }, 3000);
        if (result is null || result.TimedOut || result.ExitCode != 0)
        {
            return CreateUnavailableExternalToolStatuses();
        }

        return ParseExternalToolStatuses(result.Stdout);
    }

    public static OcrModelStatus? ProbeOcrModels()
    {
        var result = TryRunProcess("nong", new[] { "ocr", "models", "--json" }, 5000);
        if (result is null || result.TimedOut || result.ExitCode != 0)
        {
            return null;
        }

        return ParseOcrModelStatus(result.Stdout);
    }

    public static NongStatusResponse? ParseNongStatus(
        string json,
        IReadOnlyList<ExternalToolStatus>? externalTools = null,
        OcrModelStatus? ocrModels = null)
    {
        var doc = JsonNode.Parse(json);
        if (doc?["status"]?.ToString() != "ok")
        {
            return null;
        }

        var version = doc["meta"]?["version"]?.ToString();
        var commands = doc["data"] is JsonArray data
            ? data.OfType<JsonObject>().ToList()
            : new List<JsonObject>();

        var roots = commands
            .Select(command => command["group"]?.ToString())
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Select(group => group!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new NongStatusResponse(
            Installed: true,
            Version: version,
            CommandCount: commands.Count,
            AvailableRoots: roots,
            ExternalTools: externalTools ?? CreateUnavailableExternalToolStatuses(),
            OcrModels: ocrModels);
    }

    public static IReadOnlyList<ExternalToolStatus> ParseExternalToolStatuses(string output)
    {
        var versionsByPackageId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rawLine.StartsWith("Package Id", StringComparison.OrdinalIgnoreCase)
                || rawLine.StartsWith("---", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = rawLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            versionsByPackageId[parts[0]] = parts[1];
        }

        return ExternalToolDefinitions
            .Select(definition => versionsByPackageId.TryGetValue(definition.PackageId, out var version)
                ? new ExternalToolStatus(definition.Name, definition.PackageId, true, version)
                : new ExternalToolStatus(definition.Name, definition.PackageId, false, null))
            .ToList();
    }

    public static OcrModelStatus? ParseOcrModelStatus(string json)
    {
        var doc = JsonNode.Parse(json);
        if (doc?["status"]?.ToString() != "ok")
        {
            return null;
        }

        var models = doc["data"]?["models"] is JsonArray array
            ? array.OfType<JsonObject>()
            : Enumerable.Empty<JsonObject>();

        var v6Available = false;
        var v6Size = default(string);
        var v6Path = default(string);
        var v5Available = false;

        foreach (var model in models)
        {
            var id = model["id"]?.ToString() ?? string.Empty;
            var available = IsTrue(model["available"]);

            if (id.StartsWith("pp-ocrv6-", StringComparison.OrdinalIgnoreCase) && available)
            {
                v6Available = true;
                v6Size ??= model["modelSize"]?.ToString();
                v6Path ??= model["modelCachePath"]?.ToString();
            }

            if (id.Equals("pp-ocrv5-mobile", StringComparison.OrdinalIgnoreCase) && available)
            {
                v5Available = true;
            }
        }

        return new OcrModelStatus(v6Available, v6Size, v6Path, v5Available);
    }

    private static IReadOnlyList<ExternalToolStatus> CreateUnavailableExternalToolStatuses()
    {
        return ExternalToolDefinitions
            .Select(definition => new ExternalToolStatus(definition.Name, definition.PackageId, false, null))
            .ToList();
    }

    private static bool IsTrue(JsonNode? node)
    {
        try
        {
            return node?.GetValue<bool>() == true;
        }
        catch
        {
            return false;
        }
    }

    private static ProcessProbeResult? TryRunProcess(string fileName, IReadOnlyList<string> arguments, int timeoutMs)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            foreach (var argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (!process.Start())
            {
                return null;
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(timeoutMs))
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignore cleanup failures for status probes.
                }

                process.WaitForExit();
                Task.WaitAll(stdoutTask, stderrTask);
                return new ProcessProbeResult(-1, true, stdoutTask.Result, stderrTask.Result);
            }

            Task.WaitAll(stdoutTask, stderrTask);
            return new ProcessProbeResult(process.ExitCode, false, stdoutTask.Result, stderrTask.Result);
        }
        catch
        {
            return null;
        }
    }

    private sealed record ExternalToolDefinition(string Name, string PackageId);

    private sealed record ProcessProbeResult(int ExitCode, bool TimedOut, string Stdout, string Stderr);
}
