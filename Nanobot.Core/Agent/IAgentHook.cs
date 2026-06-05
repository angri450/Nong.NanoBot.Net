namespace Nanobot.Core.Agent;

public interface IAgentHook
{
    Task BeforeRunAsync(AgentRunContext context) => Task.CompletedTask;

    Task AfterRunAsync(AgentRunContext context) => Task.CompletedTask;

    Task OnRunErrorAsync(AgentRunContext context) => Task.CompletedTask;

    Task BeforeToolAsync(AgentToolContext context) => Task.CompletedTask;

    Task AfterToolAsync(AgentToolContext context) => Task.CompletedTask;

    Task OnToolErrorAsync(AgentToolContext context) => Task.CompletedTask;
}
