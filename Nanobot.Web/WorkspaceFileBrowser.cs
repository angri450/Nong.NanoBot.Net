using System.Text;

namespace Nanobot.Web;

public sealed class WorkspaceFileBrowser
{
    private const int MaxPreviewBytes = 200000;
    private const int MaxEntries = 500;

    private readonly string _workspace;

    public WorkspaceFileBrowser(string workspace)
    {
        _workspace = Path.GetFullPath(workspace);
    }

    public WorkspaceFileListResponse List(string? path)
    {
        var directory = ResolvePath(path);
        if (!Directory.Exists(directory))
        {
            throw new InvalidOperationException("Directory not found.");
        }

        var entries = Directory.EnumerateFileSystemEntries(directory)
            .Where(item => !IsInternalPath(item))
            .Select(CreateEntry)
            .OrderByDescending(entry => entry.Kind == "directory")
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .Take(MaxEntries)
            .ToList();

        return new WorkspaceFileListResponse(ToRelativePath(directory), entries);
    }

    public WorkspaceFileContentResponse Read(string? path)
    {
        var file = ResolvePath(path);
        if (!File.Exists(file))
        {
            throw new InvalidOperationException("File not found.");
        }

        if (IsInternalPath(file))
        {
            throw new InvalidOperationException("Internal WebUI files cannot be previewed.");
        }

        var info = new FileInfo(file);
        var bufferLength = (int)Math.Min(MaxPreviewBytes + 1L, Math.Max(0, info.Length));
        var buffer = new byte[bufferLength];
        using var stream = File.OpenRead(file);
        var bytesRead = 0;
        while (bytesRead < buffer.Length)
        {
            var read = stream.Read(buffer, bytesRead, buffer.Length - bytesRead);
            if (read == 0)
            {
                break;
            }

            bytesRead += read;
        }
        var truncated = bytesRead > MaxPreviewBytes || info.Length > MaxPreviewBytes;
        var previewBytes = truncated ? buffer[..MaxPreviewBytes] : buffer[..bytesRead];

        if (previewBytes.Contains((byte)0))
        {
            throw new InvalidOperationException("Binary files cannot be previewed.");
        }

        return new WorkspaceFileContentResponse(
            ToRelativePath(file),
            Path.GetFileName(file),
            Encoding.UTF8.GetString(previewBytes),
            info.Length,
            truncated,
            info.LastWriteTimeUtc);
    }

    private WorkspaceFileEntry CreateEntry(string path)
    {
        if (Directory.Exists(path))
        {
            var directory = new DirectoryInfo(path);
            return new WorkspaceFileEntry(
                directory.Name,
                ToRelativePath(path),
                "directory",
                null,
                directory.LastWriteTimeUtc);
        }

        var file = new FileInfo(path);
        return new WorkspaceFileEntry(
            file.Name,
            ToRelativePath(path),
            "file",
            file.Length,
            file.LastWriteTimeUtc);
    }

    private string ResolvePath(string? relativePath)
    {
        var normalized = string.IsNullOrWhiteSpace(relativePath)
            ? string.Empty
            : relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_workspace, normalized));
        if (!IsInsideWorkspace(fullPath))
        {
            throw new InvalidOperationException("Path is outside the workspace.");
        }

        return fullPath;
    }

    private bool IsInsideWorkspace(string fullPath)
    {
        return fullPath.Equals(_workspace, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(_workspace + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsInternalPath(string fullPath)
    {
        var relative = ToRelativePath(fullPath);
        return relative.Equals(".webui", StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith(".webui/", StringComparison.OrdinalIgnoreCase);
    }

    private string ToRelativePath(string fullPath)
    {
        var relative = Path.GetRelativePath(_workspace, fullPath);
        return relative == "."
            ? string.Empty
            : relative.Replace(Path.DirectorySeparatorChar, '/');
    }
}
