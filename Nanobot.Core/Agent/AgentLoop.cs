using Nanobot.Core.Events;
using Nanobot.Core.Memory;
using Nanobot.Core.Models;
using Nanobot.Core.Providers;
using Nanobot.Core.Skills;
using Nanobot.Core.Tools;

namespace Nanobot.Core.Agent;

public class AgentLoop
{
    private const int MaxHistoryMessages = 10;
    private const int MaxStoredHistoryMessages = MaxHistoryMessages * 2;
    private const int MaxMemoryContextChars = 20000;

    private readonly IMemory _memory;
    private readonly AgentRunner _runner;
    private readonly RuntimeEventBus _eventBus;
    private readonly SkillLoader _skillLoader;
    private readonly IReadOnlyList<IAgentHook> _hooks;
    private readonly Dictionary<string, List<Message>> _historyBySession = new();
    private readonly string _defaultWorkspace;

    public AgentLoop(
        ILLMProvider provider,
        ToolRegistry tools,
        IMemory memory,
        RuntimeEventBus? eventBus = null,
        IEnumerable<IAgentHook>? hooks = null,
        SkillLoader? skillLoader = null)
    {
        _memory = memory;
        _eventBus = eventBus ?? new RuntimeEventBus();
        _hooks = hooks?.ToList() ?? new List<IAgentHook>();
        _skillLoader = skillLoader ?? new SkillLoader();
        _runner = new AgentRunner(provider, tools, _eventBus, _hooks);
        _defaultWorkspace = memory is IWorkspaceMemory workspaceMemory
            ? workspaceMemory.Workspace
            : Environment.CurrentDirectory;
    }

    public Task<string> RunAsync(string input)
    {
        return RunAsync(input, AgentExecutionContext.CreateRoot(_defaultWorkspace));
    }

    public async Task<string> RunAsync(string input, AgentExecutionContext executionContext)
    {
        var runContext = new AgentRunContext(Guid.NewGuid().ToString("N"), executionContext, input);

        try
        {
            foreach (var hook in _hooks)
            {
                await hook.BeforeRunAsync(runContext);
            }

            await PublishRunEventAsync(RuntimeEventType.RunStarted, runContext);

            var messages = BuildMessages(input, executionContext);
            runContext.Messages = messages;

            var finalContent = await _runner.RunAsync(messages, runContext);
            runContext.Result = finalContent;

            SaveHistory(executionContext, input, finalContent);

            await PublishRunEventAsync(RuntimeEventType.RunCompleted, runContext);

            foreach (var hook in _hooks)
            {
                await hook.AfterRunAsync(runContext);
            }

            return finalContent;
        }
        catch (Exception ex)
        {
            runContext.Error = ex;
            await PublishRunEventAsync(RuntimeEventType.RunFailed, runContext);

            foreach (var hook in _hooks)
            {
                await hook.OnRunErrorAsync(runContext);
            }

            throw;
        }
    }

    public Task<string> RunStreamingAsync(
        string input,
        Func<string, CancellationToken, Task> onDeltaAsync,
        CancellationToken cancellationToken = default)
    {
        return RunStreamingAsync(
            input,
            AgentExecutionContext.CreateRoot(_defaultWorkspace),
            onDeltaAsync,
            cancellationToken
        );
    }

    public async Task<string> RunStreamingAsync(
        string input,
        AgentExecutionContext executionContext,
        Func<string, CancellationToken, Task> onDeltaAsync,
        CancellationToken cancellationToken = default)
    {
        var runContext = new AgentRunContext(Guid.NewGuid().ToString("N"), executionContext, input);

        try
        {
            foreach (var hook in _hooks)
            {
                await hook.BeforeRunAsync(runContext);
            }

            await PublishRunEventAsync(RuntimeEventType.RunStarted, runContext);

            var messages = BuildMessages(input, executionContext);
            runContext.Messages = messages;

            var finalContent = await _runner.RunStreamingAsync(
                messages,
                runContext,
                onDeltaAsync,
                cancellationToken
            );
            runContext.Result = finalContent;

            SaveHistory(executionContext, input, finalContent);

            await PublishRunEventAsync(RuntimeEventType.RunCompleted, runContext);

            foreach (var hook in _hooks)
            {
                await hook.AfterRunAsync(runContext);
            }

            return finalContent;
        }
        catch (Exception ex)
        {
            runContext.Error = ex;
            await PublishRunEventAsync(RuntimeEventType.RunFailed, runContext);

            foreach (var hook in _hooks)
            {
                await hook.OnRunErrorAsync(runContext);
            }

            throw;
        }
    }

    private List<Message> BuildMessages(string input, AgentExecutionContext executionContext)
    {
        var messages = new List<Message>
        {
            new("system", BuildSystemPrompt(executionContext))
        };

        messages.AddRange(GetHistory(executionContext).TakeLast(MaxHistoryMessages));
        messages.Add(new Message("user", input));

        return messages;
    }

    private string BuildSystemPrompt(AgentExecutionContext executionContext)
    {
        var systemPrompt = "You are nanobot, a helpful AI assistant. You have access to tools to fetch real-time data like weather, stock prices, and file system operations. When a user asks for such information, use the appropriate tool.";

        var memoryContext = _memory.GetContext();
        if (memoryContext.Length > MaxMemoryContextChars)
        {
            memoryContext = memoryContext[..MaxMemoryContextChars] + "... (Memory Truncated)";
        }

        if (!string.IsNullOrWhiteSpace(memoryContext))
        {
            systemPrompt += $"\n\nMemory Context:\n{memoryContext}";
        }

        var skillContext = _skillLoader.LoadContext(executionContext);
        if (!string.IsNullOrWhiteSpace(skillContext))
        {
            systemPrompt += $"\n\nSkill Context:\n{skillContext}";
        }

        return systemPrompt;
    }

    private List<Message> GetHistory(AgentExecutionContext executionContext)
    {
        if (!_historyBySession.TryGetValue(executionContext.SessionId, out var history))
        {
            history = new List<Message>();
            _historyBySession[executionContext.SessionId] = history;
        }

        return history;
    }

    private void SaveHistory(AgentExecutionContext executionContext, string input, string finalContent)
    {
        if (executionContext.IsEphemeral)
        {
            return;
        }

        var history = GetHistory(executionContext);
        history.Add(new Message("user", input));
        history.Add(new Message("assistant", finalContent));

        if (_memory is IWritableMemory writableMemory)
        {
            writableMemory.AppendHistory(new MemoryHistoryEntry
            {
                SessionId = executionContext.SessionId,
                Role = "user",
                Content = input
            });
            writableMemory.AppendHistory(new MemoryHistoryEntry
            {
                SessionId = executionContext.SessionId,
                Role = "assistant",
                Content = finalContent
            });
        }

        if (history.Count > MaxStoredHistoryMessages)
        {
            history.RemoveRange(0, history.Count - MaxStoredHistoryMessages);
        }
    }

    private async Task PublishRunEventAsync(RuntimeEventType type, AgentRunContext context)
    {
        await _eventBus.PublishAsync(new RuntimeEvent
        {
            Type = type,
            RunId = context.RunId,
            SessionId = context.Execution.SessionId,
            Content = context.Result,
            ErrorMessage = context.Error?.Message
        });
    }
}
