using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nanobot.Core.Tools.Builtin;

public class ShellTool : ITool
{
    private const int DefaultTimeoutMs = 30000;
    private const int MaxTimeoutMs = 120000;
    private const int DefaultMaxOutputChars = 20000;

    private readonly string _workspaceRoot;
    private readonly int _defaultTimeoutMs;
    private readonly int _defaultMaxOutputChars;

    public ShellTool(
        string? workspaceRoot = null,
        int defaultTimeoutMs = DefaultTimeoutMs,
        int defaultMaxOutputChars = DefaultMaxOutputChars)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot ?? Environment.CurrentDirectory);
        Directory.CreateDirectory(_workspaceRoot);
        _defaultTimeoutMs = Math.Clamp(defaultTimeoutMs, 1, MaxTimeoutMs);
        _defaultMaxOutputChars = Math.Max(100, defaultMaxOutputChars);
    }

    public string Name => "run_shell";
    public string Description => "Execute a shell command.";
    
    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "command": {
                "type": "string",
                "description": "The command to execute."
            },
            "workingDirectory": {
                "type": "string",
                "description": "Optional working directory. Relative paths are resolved inside the workspace."
            },
            "timeoutMs": {
                "type": "integer",
                "minimum": 1,
                "maximum": 120000,
                "description": "Command timeout in milliseconds."
            },
            "maxOutputChars": {
                "type": "integer",
                "minimum": 100,
                "description": "Maximum characters to return for stdout and stderr."
            }
        },
        "required": ["command"]
    }
    """)!;

    public async Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var command = arguments?["command"]?.ToString();
        if (string.IsNullOrEmpty(command))
        {
            return Error("missing_command", "command is required");
        }

        try
        {
            var workingDirectory = ResolveWorkingDirectory(arguments?["workingDirectory"]?.ToString());
            if (!Directory.Exists(workingDirectory))
            {
                return Error("working_directory_not_found", $"Working directory '{workingDirectory}' does not exist.");
            }

            var timeoutMs = GetInt(arguments?["timeoutMs"], _defaultTimeoutMs, 1, MaxTimeoutMs);
            var maxOutputChars = GetInt(arguments?["maxOutputChars"], _defaultMaxOutputChars, 100, _defaultMaxOutputChars);

            var processStartInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                processStartInfo.FileName = "cmd.exe";
                processStartInfo.Arguments = $"/c {command}";
            }
            else
            {
                processStartInfo.FileName = "/bin/sh";
                processStartInfo.Arguments = $"-c \"{command}\"";
            }

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var timedOut = false;
            using var timeout = new CancellationTokenSource(timeoutMs);
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

            var stdout = Truncate(output, maxOutputChars, out var stdoutTruncated);
            var stderr = Truncate(error, maxOutputChars, out var stderrTruncated);

            return JsonSerializer.Serialize(new
            {
                command,
                workingDirectory,
                exitCode = timedOut ? (int?)null : process.ExitCode,
                timedOut,
                timeoutMs,
                truncated = stdoutTruncated || stderrTruncated,
                stdout,
                stderr
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return Error("shell_execution_error", ex.Message);
        }
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

    private static int GetInt(JsonNode? node, int fallback, int min, int max)
    {
        if (node is null)
        {
            return fallback;
        }

        try
        {
            return Math.Clamp(node.GetValue<int>(), min, max);
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
                code,
                message
            }
        });
    }
}
