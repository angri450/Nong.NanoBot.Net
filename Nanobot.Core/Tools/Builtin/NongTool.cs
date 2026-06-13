using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Nanobot.Core.Config;

namespace Nanobot.Core.Tools.Builtin;

public class NongTool : ITool
{
    private static readonly IReadOnlyList<string> DefaultAllowedRoots = new[]
    {
        "commands",
        "word",
        "inspect",
        "chart",
        "excel",
        "diagram",
        "genre",
        "icons",
        "skill",
        "pptx",
        "ocr",
        "pdf",
        "lit",
        "slice",
        "progress"
    };

    private const int MinTimeoutMs = 1000;
    private const int MaxTimeoutMs = 600000;
    private const int MinOutputChars = 1000;

    private readonly string _workspaceRoot;
    private readonly NongToolSettings _settings;
    private readonly INongCommandRunner _runner;

    public NongTool(
        string workspaceRoot,
        NongToolSettings? settings = null,
        INongCommandRunner? runner = null)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
        Directory.CreateDirectory(_workspaceRoot);
        _settings = settings ?? new NongToolSettings();
        _runner = runner ?? new ProcessNongCommandRunner();
    }

    public string Name => "run_nong";

    public string Description =>
        "Run any Nong CLI command. Pass args as an array. " +
        "Available command groups: " +
        "word (39 commands: check/convert/create/read/preview/fill/rebuild/extract/dissect/stats/fonts/styles/validate/merge/outline/compare/images/crop/fit-images/compact-tables/regroup-images/estimate/page-setup/indent/paragraph-control/image-wrap/cell-format/run-format/comments/revisions/infer-format/academic-format/format-gongwen/format-audit/repair-plan/table-reflow/protect/embed-font/fix-order + 11 add subcommands), " +
        "inspect (12: diagnose/classify/structure/refs/evidence/data-req/gap/varplan/semantics/write-paper/write-official/official-check), " +
        "chart (11: bar/line/scatter/pie/boxplot/histogram/heatmap/radar/analyze/anova/duncan), " +
        "excel (8: sheets/read/to-groups/create/dissect/style/formula/pivot), " +
        "pdf (8: check/dissect/render/images/merge/split/ocr/compress), " +
        "ocr (11: local/cloud/to-word/models/install-model/check-env/analyze-image/batch/video/screen/camera), " +
        "diagram (3: flowchart/network/tree), " +
        "pptx (4: read/slides/dissect/create), " +
        "lit (5: parse/validate/plan/search/export), " +
        "slice (4: inspect/blocks/block/assets), " +
        "genre/icons/skill/progress/commands. " +
        "All commands support --json. The first arg is the command group (word/pdf/chart etc). Use the --help flag on any command for details.";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "args": {
                "type": "array",
                "items": { "type": "string" },
                "description": "Nong CLI arguments without the executable name. Use an array, not a shell command string."
            },
            "workingDirectory": {
                "type": "string",
                "description": "Optional working directory. Relative paths are resolved inside the NanoBot workspace."
            },
            "timeoutMs": {
                "type": "integer",
                "minimum": 1000,
                "maximum": 600000,
                "description": "Command timeout in milliseconds."
            },
            "maxOutputChars": {
                "type": "integer",
                "minimum": 1000,
                "description": "Maximum characters to return for stdout and stderr."
            },
            "appendJson": {
                "type": "boolean",
                "description": "Append --json when it is not already present."
            }
        },
        "required": ["args"]
    }
    """)!;

    public async Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var args = ReadArguments(arguments?["args"]);
        if (args.Count == 0)
        {
            return Error("missing_args", "args is required and must contain at least one Nong argument.");
        }

        var root = ResolveRootArgument(args);
        if (root is null)
        {
            return Error("missing_nong_root", "args must include a Nong root command.");
        }

        if (!IsAllowedRoot(root))
        {
            return Error("nong_root_not_allowed", $"Nong root command '{root}' is not allowed.");
        }

        var appendJson = ReadBool(arguments?["appendJson"], _settings.AppendJson);
        if (appendJson && !args.Any(arg => arg.Equals("--json", StringComparison.OrdinalIgnoreCase)))
        {
            args.Add("--json");
        }

        try
        {
            var workingDirectory = ResolveWorkingDirectory(arguments?["workingDirectory"]?.ToString());
            if (!Directory.Exists(workingDirectory))
            {
                return Error("working_directory_not_found", $"Working directory '{workingDirectory}' does not exist.");
            }

            var timeoutMs = ReadInt(arguments?["timeoutMs"], _settings.TimeoutMs, MinTimeoutMs, MaxTimeoutMs);
            var maxOutputChars = ReadInt(arguments?["maxOutputChars"], _settings.MaxOutputChars, MinOutputChars, Math.Max(MinOutputChars, _settings.MaxOutputChars));

            var request = new NongCommandRequest(
                string.IsNullOrWhiteSpace(_settings.Command) ? "nong" : _settings.Command,
                args,
                workingDirectory,
                timeoutMs
            );
            var result = await _runner.RunAsync(request);
            var stdout = Truncate(result.Stdout, maxOutputChars, out var stdoutTruncated);
            var stderr = Truncate(result.Stderr, maxOutputChars, out var stderrTruncated);

            return JsonSerializer.Serialize(new
            {
                tool = Name,
                command = request.Command,
                args,
                workingDirectory,
                exitCode = result.TimedOut ? (int?)null : result.ExitCode,
                timedOut = result.TimedOut,
                timeoutMs,
                truncated = stdoutTruncated || stderrTruncated,
                stdout,
                stderr
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return Error("nong_execution_error", ex.Message);
        }
    }

    private List<string> ReadArguments(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return new List<string>();
        }

        return array
            .Select(item => item?.ToString() ?? "")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static string? ResolveRootArgument(IReadOnlyList<string> args)
    {
        return args.FirstOrDefault(arg => !arg.StartsWith("-", StringComparison.Ordinal));
    }

    private bool IsAllowedRoot(string root)
    {
        IReadOnlyList<string> allowedRoots = _settings.AllowedRoots ?? DefaultAllowedRoots;
        return allowedRoots.Contains(root, StringComparer.OrdinalIgnoreCase);
    }

    private string ResolveWorkingDirectory(string? requestedDirectory)
    {
        var candidate = string.IsNullOrWhiteSpace(requestedDirectory)
            ? _workspaceRoot
            : Path.IsPathRooted(requestedDirectory)
                ? requestedDirectory
                : Path.Combine(_workspaceRoot, requestedDirectory);

        var fullPath = Path.GetFullPath(candidate);
        if (!IsWithinWorkspace(fullPath))
        {
            throw new InvalidOperationException($"Working directory '{fullPath}' is outside workspace '{_workspaceRoot}'.");
        }

        return fullPath;
    }

    private bool IsWithinWorkspace(string path)
    {
        var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var root = _workspaceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalized = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return normalized.Equals(root, comparison)
            || normalized.StartsWith(root + Path.DirectorySeparatorChar, comparison)
            || normalized.StartsWith(root + Path.AltDirectorySeparatorChar, comparison);
    }

    private static int ReadInt(JsonNode? node, int fallback, int min, int max)
    {
        if (node is null)
        {
            return Math.Clamp(fallback, min, max);
        }

        try
        {
            return Math.Clamp(node.GetValue<int>(), min, max);
        }
        catch
        {
            return Math.Clamp(fallback, min, max);
        }
    }

    private static bool ReadBool(JsonNode? node, bool fallback)
    {
        if (node is null)
        {
            return fallback;
        }

        try
        {
            return node.GetValue<bool>();
        }
        catch
        {
            return fallback;
        }
    }

    private static string Truncate(string value, int maxChars, out bool truncated)
    {
        truncated = value.Length > maxChars;
        return truncated ? value[..maxChars] + "\n... (truncated)" : value;
    }

    private static string Error(string code, string message)
    {
        return JsonSerializer.Serialize(new
        {
            error = new
            {
                tool = "run_nong",
                code,
                message
            }
        });
    }

    /// <summary>Discover Nong CLI capabilities by running nong commands --json.</summary>
    public async Task<NongCapabilityInfo?> DiscoverCapabilitiesAsync()
    {
        try
        {
            var request = new NongCommandRequest(
                string.IsNullOrWhiteSpace(_settings.Command) ? "nong" : _settings.Command,
                new List<string> { "commands", "--json" },
                _workspaceRoot,
                30000
            );
            var result = await _runner.RunAsync(request);
            if (result.ExitCode != 0) return null;

            var doc = JsonNode.Parse(result.Stdout);
            if (doc?["status"]?.ToString() != "ok") return null;

            var versionNode = doc["meta"]?["version"];
            var version = versionNode?.GetValue<string>() ?? "unknown";

            var data = doc["data"]?.AsArray();
            if (data == null) return null;

            var commands = data.Select(c => new NongCommandInfo(
                c["name"]?.ToString() ?? "",
                c["description"]?.ToString() ?? "",
                c["group"]?.ToString() ?? "",
                c["status"]?.ToString() ?? "unknown"
            )).ToList();

            return new NongCapabilityInfo(
                Version: version,
                CommandCount: commands.Count,
                Commands: commands
            );
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Discover Nong CLI commands as OpenAI tool schemas by calling nong commands --format openai-tools (4.1.0+).
    /// Falls back to DiscoverCapabilitiesAsync + manual schema rendering for older CLI versions.
    /// </summary>
    public static async Task<IReadOnlyList<NongDiscoveredTool>> DiscoverOpenAiToolsAsync(
        string? nongCommand = null,
        INongCommandRunner? runner = null,
        string? workspace = null)
    {
        var cmd = string.IsNullOrWhiteSpace(nongCommand) ? "nong" : nongCommand;
        runner ??= new ProcessNongCommandRunner();
        workspace ??= Environment.CurrentDirectory;

        try
        {
            var request = new NongCommandRequest(
                cmd,
                new List<string> { "commands", "--format", "openai-tools" },
                workspace,
                30000
            );
            var result = await runner.RunAsync(request);

            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Stdout))
            {
                var tools = ParseOpenAiTools(result.Stdout);
                if (tools.Count > 0)
                    return tools;
            }
        }
        catch
        {
            // Fallback below
        }

        return new List<NongDiscoveredTool>();
    }

    private static IReadOnlyList<NongDiscoveredTool> ParseOpenAiTools(string json)
    {
        var results = new List<NongDiscoveredTool>();
        try
        {
            var array = JsonNode.Parse(json)?.AsArray();
            if (array == null) return results;

            foreach (var item in array)
            {
                var func = item["function"];
                if (func == null) continue;

                var name = func["name"]?.ToString();
                var desc = func["description"]?.ToString();
                var parameters = func["parameters"]?.DeepClone();

                if (string.IsNullOrWhiteSpace(name)) continue;

                // Convert tool name like "chart_bar" → ["chart", "bar"]
                var args = name.Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (args.Length == 0) continue;

                results.Add(new NongDiscoveredTool(
                    Name: name,
                    Description: desc ?? "",
                    Args: args,
                    Parameters: parameters ?? new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject()
                    }
                ));
            }
        }
        catch { /* parse error → empty list */ }

        return results;
    }

    /// <summary>
    /// Run a single Nong command by args array. Used by the delegating tool wrapper.
    /// </summary>
    internal Task<string> ExecuteArgsAsync(IReadOnlyList<string> args, string? workingDirectory)
    {
        // Build a fake JsonNode that looks like the run_nong args parameter
        var argsArray = new JsonArray();
        foreach (var a in args)
            argsArray.Add((string)a);

        var fullArgs = new JsonObject
        {
            ["args"] = argsArray
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
            fullArgs["workingDirectory"] = workingDirectory;

        return ExecuteAsync(fullArgs);
    }
}

/// <summary>Lightweight tool wrapper that delegates a single Nong command to NongTool.</summary>
public class NongDiscoveredToolWrapper : ITool
{
    private readonly NongTool _nongTool;
    private readonly string _toolName;
    private readonly string[] _args;

    public NongDiscoveredToolWrapper(NongTool nongTool, string toolName, string[] args, string description, JsonNode parameters)
    {
        _nongTool = nongTool;
        _toolName = toolName;
        _args = args;
        Description = description;
        Parameters = parameters;
    }

    public string Name => _toolName;
    public string Description { get; }
    public JsonNode Parameters { get; }

    public Task<string> ExecuteAsync(JsonNode? arguments)
    {
        // Merge user-provided args with the fixed command args
        // Fixed args are like ["chart", "bar"], user args provide --options and positional values
        var mergedArgs = new List<string>(_args);

        if (arguments is JsonObject obj)
        {
            foreach (var prop in obj)
            {
                var key = prop.Key;
                var value = prop.Value;

                if (string.IsNullOrWhiteSpace(key)) continue;

                // Boolean flags: --flag
                if (value?.GetValueKind() == System.Text.Json.JsonValueKind.True)
                {
                    mergedArgs.Add($"--{key}");
                }
                else if (value?.GetValueKind() == System.Text.Json.JsonValueKind.False)
                {
                    // false flag → skip (don't pass --no-flag unless specified)
                }
                else if (value != null)
                {
                    var strVal = value.ToString();
                    if (!string.IsNullOrWhiteSpace(strVal))
                    {
                        // Positional args (key "o", "file", "spec", "image", "dir", "query", "file", etc.)
                        // or named args (key starts with -- or is a regular string)
                        var isFlagKey = key.StartsWith("--") || key.StartsWith("-");
                        // Short keys like "o", "file", "spec" are likely positional or flag names
                        // For simplicity: if key is "file" and it's a positional arg, pass as value
                        // Named options: --flag value
                        if (isFlagKey)
                        {
                            mergedArgs.Add(key);
                            mergedArgs.Add(strVal);
                        }
                        else if (key.Length <= 3)
                        {
                            // Short key like "o", "q" → treat as -o value
                            mergedArgs.Add(key.StartsWith("-") ? key : $"-{key}");
                            mergedArgs.Add(strVal);
                        }
                        else
                        {
                            mergedArgs.Add($"--{key}");
                            mergedArgs.Add(strVal);
                        }
                    }
                }
            }
        }

        return _nongTool.ExecuteArgsAsync(mergedArgs, null);
    }
}

/// <summary>Discovered Nong command tool descriptor from --format openai-tools.</summary>
public sealed record NongDiscoveredTool(
    string Name,
    string Description,
    IReadOnlyList<string> Args,
    JsonNode Parameters);

public sealed record NongCommandInfo(string Name, string Description, string Group, string Status);

public sealed record NongCapabilityInfo(
    string Version,
    int CommandCount,
    IReadOnlyList<NongCommandInfo> Commands);

public interface INongCommandRunner
{
    Task<NongCommandResult> RunAsync(NongCommandRequest request);
}

public sealed record NongCommandRequest(
    string Command,
    IReadOnlyList<string> Args,
    string WorkingDirectory,
    int TimeoutMs);

public sealed record NongCommandResult(
    int ExitCode,
    bool TimedOut,
    string Stdout,
    string Stderr);

public sealed class ProcessNongCommandRunner : INongCommandRunner
{
    public async Task<NongCommandResult> RunAsync(NongCommandRequest request)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = request.Command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = request.WorkingDirectory
        };

        foreach (var arg in request.Args)
        {
            processStartInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        var timedOut = false;

        using var timeout = new CancellationTokenSource(request.TimeoutMs);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }

        var output = await outputTask;
        var error = await errorTask;

        return new NongCommandResult(
            timedOut ? -1 : process.ExitCode,
            timedOut,
            output,
            error
        );
    }
}
