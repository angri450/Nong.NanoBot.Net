using System.Text.Json.Nodes;
using Nanobot.Core.Models;

namespace Nanobot.Core.Providers;

public interface IStreamingLLMProvider : ILLMProvider
{
    IAsyncEnumerable<LLMStreamChunk> ChatStreamAsync(
        List<Message> messages,
        List<JsonNode>? tools = null,
        string? model = null,
        int maxTokens = 4096,
        double temperature = 0.7,
        CancellationToken cancellationToken = default
    );
}
