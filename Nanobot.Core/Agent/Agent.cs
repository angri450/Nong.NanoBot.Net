using Nanobot.Core.Events;
using Nanobot.Core.Memory;
using Nanobot.Core.Providers;
using Nanobot.Core.Skills;
using Nanobot.Core.Tools;

namespace Nanobot.Core.Agent;

public class Agent
{
    private readonly AgentLoop _loop;

    public Agent(ILLMProvider provider, ToolRegistry tools, IMemory memory)
    {
        _loop = new AgentLoop(provider, tools, memory);
    }

    public Agent(
        ILLMProvider provider,
        ToolRegistry tools,
        IMemory memory,
        RuntimeEventBus eventBus,
        IEnumerable<IAgentHook>? hooks = null,
        SkillLoader? skillLoader = null)
    {
        _loop = new AgentLoop(provider, tools, memory, eventBus, hooks, skillLoader);
    }

    public Task<string> RunAsync(string input) => _loop.RunAsync(input);

    public Task<string> RunAsync(string input, AgentExecutionContext executionContext) =>
        _loop.RunAsync(input, executionContext);

    public Task<string> RunStreamingAsync(
        string input,
        Func<string, CancellationToken, Task> onDeltaAsync,
        CancellationToken cancellationToken = default) =>
        _loop.RunStreamingAsync(input, onDeltaAsync, cancellationToken);

    public Task<string> RunStreamingAsync(
        string input,
        AgentExecutionContext executionContext,
        Func<string, CancellationToken, Task> onDeltaAsync,
        CancellationToken cancellationToken = default) =>
        _loop.RunStreamingAsync(input, executionContext, onDeltaAsync, cancellationToken);
}
