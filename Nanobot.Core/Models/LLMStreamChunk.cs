namespace Nanobot.Core.Models;

public sealed record LLMStreamChunk
{
    public string? ContentDelta { get; init; }
    public string? ReasoningDelta { get; init; }
    public LLMResponse? FinalResponse { get; init; }

    public bool IsFinal => FinalResponse is not null;

    public static LLMStreamChunk Delta(string content) => new()
    {
        ContentDelta = content
    };

    public static LLMStreamChunk Reasoning(string content) => new()
    {
        ReasoningDelta = content
    };

    public static LLMStreamChunk Final(LLMResponse response) => new()
    {
        FinalResponse = response
    };
}
