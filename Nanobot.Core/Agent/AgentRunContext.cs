using Nanobot.Core.Models;

namespace Nanobot.Core.Agent;

public class AgentRunContext
{
    public AgentRunContext(string runId, AgentExecutionContext execution, string input)
    {
        RunId = runId;
        Execution = execution;
        Input = input;
    }

    public string RunId { get; }

    public AgentExecutionContext Execution { get; }

    public string Input { get; }

    public IReadOnlyList<Message> Messages { get; internal set; } = Array.Empty<Message>();

    public string? Result { get; internal set; }

    public Exception? Error { get; internal set; }
}
