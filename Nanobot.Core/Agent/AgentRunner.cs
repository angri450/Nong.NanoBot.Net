using Nanobot.Core.Events;
using Nanobot.Core.Models;
using Nanobot.Core.Providers;
using Nanobot.Core.Tools;

namespace Nanobot.Core.Agent;

public class AgentRunner
{
    private const int MaxIterations = 20;
    private const int MaxToolOutputChars = 15000;

    private readonly ILLMProvider _provider;
    private readonly ToolRegistry _tools;
    private readonly ToolRuntime? _toolRuntime;
    private readonly RuntimeEventBus _eventBus;
    private readonly IReadOnlyList<IAgentHook> _hooks;

    public AgentRunner(
        ILLMProvider provider,
        ToolRegistry tools,
        RuntimeEventBus? eventBus = null,
        IEnumerable<IAgentHook>? hooks = null,
        ToolRuntime? toolRuntime = null)
    {
        _provider = provider;
        _tools = tools;
        _eventBus = eventBus ?? new RuntimeEventBus();
        _hooks = hooks?.ToList() ?? new List<IAgentHook>();
        _toolRuntime = toolRuntime;
    }

    public async Task<string> RunAsync(List<Message> messages, AgentRunContext runContext)
    {
        var currentLoopMessages = new List<Message>(messages);

        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            var response = await _provider.ChatAsync(
                currentLoopMessages,
                _tools.GetDefinitions(runContext.Execution.AllowedTools)
            );

            if (!response.HasToolCalls)
            {
                return response.Content ?? "No response.";
            }

            currentLoopMessages.Add(new Message("assistant", response.Content)
            {
                ToolCalls = response.ToolCalls
            });

            foreach (var toolCall in response.ToolCalls)
            {
                var result = await ExecuteToolAsync(runContext, toolCall);
                currentLoopMessages.Add(new Message("tool", result)
                {
                    ToolCallId = toolCall.Id
                });
            }
        }

        return "No response.";
    }

    public async Task<string> RunStreamingAsync(
        List<Message> messages,
        AgentRunContext runContext,
        Func<string, CancellationToken, Task> onDeltaAsync,
        CancellationToken cancellationToken = default,
        Func<string, CancellationToken, Task>? onReasoningDeltaAsync = null)
    {
        var currentLoopMessages = new List<Message>(messages);

        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            var response = await CallProviderStreamingAsync(
                currentLoopMessages,
                runContext,
                onDeltaAsync,
                cancellationToken,
                onReasoningDeltaAsync
            );

            if (!response.HasToolCalls)
            {
                return response.Content ?? "No response.";
            }

            currentLoopMessages.Add(new Message("assistant", response.Content)
            {
                ToolCalls = response.ToolCalls
            });

            foreach (var toolCall in response.ToolCalls)
            {
                var result = await ExecuteToolAsync(runContext, toolCall);
                currentLoopMessages.Add(new Message("tool", result)
                {
                    ToolCallId = toolCall.Id
                });
            }
        }

        return "No response.";
    }

    private async Task<LLMResponse> CallProviderStreamingAsync(
        List<Message> messages,
        AgentRunContext runContext,
        Func<string, CancellationToken, Task> onDeltaAsync,
        CancellationToken cancellationToken,
        Func<string, CancellationToken, Task>? onReasoningDeltaAsync = null)
    {
        var tools = _tools.GetDefinitions(runContext.Execution.AllowedTools);
        if (_provider is not IStreamingLLMProvider streamingProvider)
        {
            var nonStreamingResponse = await _provider.ChatAsync(messages, tools);
            if (!nonStreamingResponse.HasToolCalls && !string.IsNullOrEmpty(nonStreamingResponse.Content))
            {
                await onDeltaAsync(nonStreamingResponse.Content, cancellationToken);
            }

            return nonStreamingResponse;
        }

        LLMResponse? finalResponse = null;
        var streamedContent = false;
        await foreach (var chunk in streamingProvider.ChatStreamAsync(
            messages,
            tools,
            cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.ContentDelta))
            {
                streamedContent = true;
                await onDeltaAsync(chunk.ContentDelta, cancellationToken);
            }

            if (!string.IsNullOrEmpty(chunk.ReasoningDelta) && onReasoningDeltaAsync is not null)
            {
                await onReasoningDeltaAsync(chunk.ReasoningDelta, cancellationToken);
            }

            if (chunk.FinalResponse is not null)
            {
                finalResponse = chunk.FinalResponse;
            }
        }

        finalResponse ??= new LLMResponse("No response.");
        if (!finalResponse.HasToolCalls && !streamedContent && !string.IsNullOrEmpty(finalResponse.Content))
        {
            await onDeltaAsync(finalResponse.Content, cancellationToken);
        }

        return finalResponse;
    }

    private async Task<string> ExecuteToolAsync(AgentRunContext runContext, ToolCallRequest toolCall)
    {
        var toolContext = new AgentToolContext(runContext, toolCall);

        foreach (var hook in _hooks)
        {
            await hook.BeforeToolAsync(toolContext);
        }

        await PublishToolEventAsync(RuntimeEventType.ToolStarted, toolContext);

        if (toolContext.IsRejected)
        {
            var rejectedResult = toolContext.Result ?? "Tool call rejected.";
            toolContext.Result = TruncateToolResult(rejectedResult);
            await PublishToolEventAsync(RuntimeEventType.ToolCompleted, toolContext);
            await RunAfterToolHooksAsync(toolContext);
            return toolContext.Result;
        }

        if (!toolContext.Execution.IsToolAllowed(toolContext.ToolCall.Name))
        {
            ApplyToolResult(toolContext, ToolExecutionResult.Error(
                toolContext.ToolCall.Name,
                "tool_not_allowed",
                $"Tool '{toolContext.ToolCall.Name}' is not allowed in this context."
            ));
            await PublishToolEventAsync(RuntimeEventType.ToolFailed, toolContext);
            await RunToolErrorHooksAsync(toolContext);
            return toolContext.Result ?? string.Empty;
        }

        ToolExecutionResult result;
        if (_toolRuntime is not null)
        {
            result = await _toolRuntime.ExecuteAsync(
                toolContext.ToolCall.Name,
                toolContext.ToolCall.Arguments,
                runContext.Execution.SessionId,
                runContext.RunId);
        }
        else
        {
            result = await _tools.ExecuteWithResultAsync(toolContext.ToolCall.Name, toolContext.ToolCall.Arguments);
        }

        ApplyToolResult(toolContext, result);

        if (!result.Success)
        {
            await PublishToolEventAsync(RuntimeEventType.ToolFailed, toolContext);
            await RunToolErrorHooksAsync(toolContext);
            return toolContext.Result ?? string.Empty;
        }

        await PublishToolEventAsync(RuntimeEventType.ToolCompleted, toolContext);
        await RunAfterToolHooksAsync(toolContext);
        return toolContext.Result ?? string.Empty;
    }

    private async Task RunAfterToolHooksAsync(AgentToolContext toolContext)
    {
        foreach (var hook in _hooks)
        {
            await hook.AfterToolAsync(toolContext);
        }
    }

    private async Task RunToolErrorHooksAsync(AgentToolContext toolContext)
    {
        foreach (var hook in _hooks)
        {
            await hook.OnToolErrorAsync(toolContext);
        }
    }

    private static void ApplyToolResult(AgentToolContext toolContext, ToolExecutionResult result)
    {
        toolContext.Result = TruncateToolResult(result.Content ?? "Tool execution returned no result.");
        toolContext.Error = result.Exception;
        toolContext.ErrorCode = result.ErrorCode;
        toolContext.ErrorMessage = result.ErrorMessage;
    }

    private async Task PublishToolEventAsync(RuntimeEventType type, AgentToolContext context)
    {
        await _eventBus.PublishAsync(new RuntimeEvent
        {
            Type = type,
            RunId = context.Run.RunId,
            SessionId = context.Execution.SessionId,
            ToolName = context.ToolCall.Name,
            ToolCallId = context.ToolCall.Id,
            Content = context.Result,
            ErrorMessage = context.Error?.Message ?? context.ErrorMessage
        });
    }

    private static string TruncateToolResult(string result)
    {
        if (result.Length <= MaxToolOutputChars)
        {
            return result;
        }

        return result[..MaxToolOutputChars] + $"\n... (Result truncated from {result.Length} chars)";
    }
}
