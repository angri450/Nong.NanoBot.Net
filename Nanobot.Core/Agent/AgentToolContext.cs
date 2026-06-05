using Nanobot.Core.Models;

namespace Nanobot.Core.Agent;

public class AgentToolContext
{
    public AgentToolContext(AgentRunContext run, ToolCallRequest toolCall)
    {
        Run = run;
        ToolCall = toolCall;
    }

    public AgentRunContext Run { get; }

    public AgentExecutionContext Execution => Run.Execution;

    public ToolCallRequest ToolCall { get; set; }

    public string? Result { get; internal set; }

    public Exception? Error { get; internal set; }

    public string? ErrorCode { get; internal set; }

    public string? ErrorMessage { get; internal set; }

    public bool IsRejected { get; private set; }

    public void Reject(string result)
    {
        IsRejected = true;
        Result = result;
    }
}
