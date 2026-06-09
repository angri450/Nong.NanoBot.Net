namespace Nanobot.Core.Tools;

public record ToolResultHandle
{
    public string HandleId { get; init; } = Guid.NewGuid().ToString("N");
    public string ToolName { get; init; } = "";
    public string FilePath { get; init; } = "";
    public int TotalLength { get; init; }
    public int TruncatedLength { get; init; }
    public bool IsTruncated => TotalLength > TruncatedLength;

    public string? RetrieveFull()
    {
        if (!File.Exists(FilePath))
        {
            return null;
        }

        return File.ReadAllText(FilePath);
    }

    public static ToolResultHandle Create(string toolName, string fullOutput, int maxLength, string storeDir)
    {
        if (fullOutput.Length <= maxLength)
        {
            return new ToolResultHandle
            {
                ToolName = toolName,
                TotalLength = fullOutput.Length,
                TruncatedLength = fullOutput.Length
            };
        }

        Directory.CreateDirectory(storeDir);
        var handleId = Guid.NewGuid().ToString("N");
        var filePath = Path.Combine(storeDir, $"{toolName}_{handleId}.txt");
        File.WriteAllText(filePath, fullOutput);

        return new ToolResultHandle
        {
            HandleId = handleId,
            ToolName = toolName,
            FilePath = filePath,
            TotalLength = fullOutput.Length,
            TruncatedLength = maxLength
        };
    }
}
