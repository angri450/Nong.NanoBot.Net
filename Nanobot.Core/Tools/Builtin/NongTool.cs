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
        "Run the Nong CLI deterministic tool layer. Pass Nong arguments as an array, for example [\"pdf\", \"check\", \"paper.pdf\"].";

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
}

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
