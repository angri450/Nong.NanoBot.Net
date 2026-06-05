using System.Text.Json.Nodes;
using Nanobot.Core.Agent;
using Nanobot.Core.Events;
using Nanobot.Core.Memory;
using Nanobot.Core.Models;
using Nanobot.Core.Providers;
using Nanobot.Core.Tools;

namespace Nanobot.Tests;

public class AgentArchitectureTests
{
    [Fact]
    public async Task AgentRunner_ReturnsFinalContentWithoutToolCall()
    {
        var provider = new SequenceProvider(new LLMResponse("final"));
        var runner = new AgentRunner(provider, new ToolRegistry());
        var run = CreateRunContext();

        var result = await runner.RunAsync(new List<Message> { new("user", "hello") }, run);

        Assert.Equal("final", result);
        Assert.Single(provider.Calls);
    }

    [Fact]
    public async Task AgentRunner_ExecutesSingleToolCallAndReturnsFollowup()
    {
        var provider = new SequenceProvider(ToolResponse("call-1", "lookup"), new LLMResponse("done"));
        var registry = new ToolRegistry();
        var tool = new FakeTool("lookup", "tool-result");
        registry.Register(tool);
        var runner = new AgentRunner(provider, registry);

        var result = await runner.RunAsync(new List<Message> { new("user", "lookup") }, CreateRunContext());

        Assert.Equal("done", result);
        Assert.Single(tool.Calls);
        Assert.Contains(provider.Calls[1], message => message.Role == "tool" && message.Content == "tool-result");
    }

    [Fact]
    public async Task AgentRunner_HandlesMultipleToolRounds()
    {
        var provider = new SequenceProvider(
            ToolResponse("call-1", "lookup"),
            ToolResponse("call-2", "lookup"),
            new LLMResponse("done")
        );
        var registry = new ToolRegistry();
        var tool = new FakeTool("lookup", "tool-result");
        registry.Register(tool);
        var runner = new AgentRunner(provider, registry);

        var result = await runner.RunAsync(new List<Message> { new("user", "lookup") }, CreateRunContext());

        Assert.Equal("done", result);
        Assert.Equal(2, tool.Calls.Count);
        Assert.Equal(3, provider.Calls.Count);
    }

    [Fact]
    public async Task AgentRunner_ReturnsNoResponseAfterMaxIterations()
    {
        var responses = Enumerable
            .Range(0, 20)
            .Select(i => ToolResponse($"call-{i}", "lookup"))
            .ToArray();
        var provider = new SequenceProvider(responses);
        var registry = new ToolRegistry();
        var tool = new FakeTool("lookup", "tool-result");
        registry.Register(tool);
        var runner = new AgentRunner(provider, registry);

        var result = await runner.RunAsync(new List<Message> { new("user", "loop") }, CreateRunContext());

        Assert.Equal("No response.", result);
        Assert.Equal(20, provider.Calls.Count);
        Assert.Equal(20, tool.Calls.Count);
    }

    [Fact]
    public async Task AgentLoop_PublishesRunAndToolEventsInOrder()
    {
        var provider = new SequenceProvider(ToolResponse("call-1", "lookup"), new LLMResponse("done"));
        var registry = new ToolRegistry();
        registry.Register(new FakeTool("lookup", "tool-result"));
        var eventBus = new RuntimeEventBus();
        var events = new List<RuntimeEventType>();
        using var _ = eventBus.Subscribe(evt => events.Add(evt.Type));
        var agent = new Agent(provider, registry, new StaticMemory(), eventBus);

        await agent.RunAsync("lookup", AgentExecutionContext.CreateRoot(CreateWorkspace()));

        Assert.Equal(
            new[]
            {
                RuntimeEventType.RunStarted,
                RuntimeEventType.ToolStarted,
                RuntimeEventType.ToolCompleted,
                RuntimeEventType.RunCompleted
            },
            events
        );
    }

    [Fact]
    public async Task AgentHook_CanModifyToolCall()
    {
        var provider = new SequenceProvider(ToolResponse("call-1", "lookup"), new LLMResponse("done"));
        var registry = new ToolRegistry();
        var renamedTool = new FakeTool("renamed_lookup", "renamed-result");
        registry.Register(renamedTool);
        var hook = new RenameToolHook("renamed_lookup");
        var runner = new AgentRunner(provider, registry, hooks: new[] { hook });

        var result = await runner.RunAsync(new List<Message> { new("user", "lookup") }, CreateRunContext());

        Assert.Equal("done", result);
        Assert.Single(renamedTool.Calls);
        Assert.Contains(provider.Calls[1], message => message.Role == "tool" && message.Content == "renamed-result");
    }

    [Fact]
    public async Task AgentHook_CanRejectToolCall()
    {
        var provider = new SequenceProvider(ToolResponse("call-1", "lookup"), new LLMResponse("done"));
        var registry = new ToolRegistry();
        var tool = new FakeTool("lookup", "tool-result");
        registry.Register(tool);
        var runner = new AgentRunner(provider, registry, hooks: new[] { new RejectToolHook("hook-result") });

        var result = await runner.RunAsync(new List<Message> { new("user", "lookup") }, CreateRunContext());

        Assert.Equal("done", result);
        Assert.Empty(tool.Calls);
        Assert.Contains(provider.Calls[1], message => message.Role == "tool" && message.Content == "hook-result");
    }

    [Fact]
    public async Task AgentHook_ReceivesOriginalToolException()
    {
        var provider = new SequenceProvider(ToolResponse("call-1", "lookup"), new LLMResponse("done"));
        var registry = new ToolRegistry();
        registry.Register(new FakeTool("lookup", "tool-result")
        {
            Handler = _ => throw new InvalidOperationException("boom")
        });
        var hook = new CaptureToolErrorHook();
        var eventBus = new RuntimeEventBus();
        var events = new List<RuntimeEvent>();
        using var _ = eventBus.Subscribe(evt => events.Add(evt));
        var runner = new AgentRunner(provider, registry, eventBus, new[] { hook });

        var result = await runner.RunAsync(new List<Message> { new("user", "lookup") }, CreateRunContext());

        Assert.Equal("done", result);
        Assert.NotNull(hook.Error);
        Assert.Equal("boom", hook.Error.Message);
        Assert.Contains(events, evt => evt.Type == RuntimeEventType.ToolFailed && evt.ErrorMessage == "boom");
        Assert.Contains(provider.Calls[1], message =>
        {
            if (message.Role != "tool" || message.Content is null)
            {
                return false;
            }

            var json = JsonNode.Parse(message.Content);
            return json?["error"]?["tool"]?.ToString() == "lookup"
                && json["error"]?["code"]?.ToString() == "tool_exception"
                && json["error"]?["message"]?.ToString() == "boom";
        });
    }

    [Fact]
    public async Task AgentLoop_DoesNotShareHistoryAcrossSessions()
    {
        var provider = new SequenceProvider(
            new LLMResponse("session-a-first"),
            new LLMResponse("session-b-first"),
            new LLMResponse("session-a-second")
        );
        var agent = new Agent(provider, new ToolRegistry(), new StaticMemory());
        var workspace = CreateWorkspace();
        var sessionA = AgentExecutionContext.CreateRoot(workspace) with { SessionId = "a" };
        var sessionB = AgentExecutionContext.CreateRoot(workspace) with { SessionId = "b" };

        await agent.RunAsync("first-a", sessionA);
        await agent.RunAsync("first-b", sessionB);
        await agent.RunAsync("second-a", sessionA);

        Assert.DoesNotContain(provider.Calls[1], message => message.Content == "first-a");
        Assert.Contains(provider.Calls[2], message => message.Content == "first-a");
        Assert.Contains(provider.Calls[2], message => message.Content == "session-a-first");
    }

    [Fact]
    public async Task AgentRunner_FiltersToolDefinitionsByExecutionContext()
    {
        var provider = new SequenceProvider(new LLMResponse("done"));
        var registry = new ToolRegistry();
        registry.Register(new FakeTool("allowed", "allowed-result"));
        registry.Register(new FakeTool("blocked", "blocked-result"));
        var runner = new AgentRunner(provider, registry);
        var run = CreateRunContext(new AgentExecutionContext
        {
            Workspace = CreateWorkspace(),
            AllowedTools = new[] { "allowed" }
        });

        await runner.RunAsync(new List<Message> { new("user", "hello") }, run);

        var toolNames = provider.ToolDefinitions[0]!
            .Select(node => node["function"]?["name"]?.ToString())
            .ToList();
        Assert.Equal(new[] { "allowed" }, toolNames);
    }

    [Fact]
    public async Task AgentRunner_StreamsProviderDeltasAndReturnsFinalContent()
    {
        var provider = new StreamingSequenceProvider(
            LLMStreamChunk.Delta("hel"),
            LLMStreamChunk.Delta("lo"),
            LLMStreamChunk.Final(new LLMResponse("hello"))
        );
        var runner = new AgentRunner(provider, new ToolRegistry());
        var deltas = new List<string>();

        var result = await runner.RunStreamingAsync(
            new List<Message> { new("user", "hello") },
            CreateRunContext(),
            (delta, _) =>
            {
                deltas.Add(delta);
                return Task.CompletedTask;
            }
        );

        Assert.Equal("hello", result);
        Assert.Equal(new[] { "hel", "lo" }, deltas);
    }

    private static AgentRunContext CreateRunContext(AgentExecutionContext? context = null)
    {
        return new AgentRunContext("run-test", context ?? AgentExecutionContext.CreateRoot(CreateWorkspace()), "input");
    }

    private static LLMResponse ToolResponse(string id, string name)
    {
        return new LLMResponse
        {
            ToolCalls = new List<ToolCallRequest>
            {
                new(id, name, JsonNode.Parse("{}"))
            }
        };
    }

    private static string CreateWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "nanobot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class SequenceProvider : ILLMProvider
    {
        private readonly Queue<LLMResponse> _responses;

        public SequenceProvider(params LLMResponse[] responses)
        {
            _responses = new Queue<LLMResponse>(responses);
        }

        public List<List<Message>> Calls { get; } = new();

        public List<List<JsonNode>?> ToolDefinitions { get; } = new();

        public Task<LLMResponse> ChatAsync(
            List<Message> messages,
            List<JsonNode>? tools = null,
            string? model = null,
            int maxTokens = 4096,
            double temperature = 0.7)
        {
            Calls.Add(messages.Select(CloneMessage).ToList());
            ToolDefinitions.Add(tools);
            return Task.FromResult(_responses.Count > 0 ? _responses.Dequeue() : new LLMResponse("done"));
        }

        public string GetDefaultModel() => "fake";

        private static Message CloneMessage(Message message)
        {
            return new Message(message.Role, message.Content)
            {
                ToolCalls = message.ToolCalls?.ToList(),
                ToolCallId = message.ToolCallId
            };
        }
    }

    private sealed class StreamingSequenceProvider : IStreamingLLMProvider
    {
        private readonly IReadOnlyList<LLMStreamChunk> _chunks;

        public StreamingSequenceProvider(params LLMStreamChunk[] chunks)
        {
            _chunks = chunks;
        }

        public Task<LLMResponse> ChatAsync(
            List<Message> messages,
            List<JsonNode>? tools = null,
            string? model = null,
            int maxTokens = 4096,
            double temperature = 0.7)
        {
            return Task.FromResult(_chunks.LastOrDefault(chunk => chunk.FinalResponse is not null)?.FinalResponse ?? new LLMResponse("done"));
        }

        public async IAsyncEnumerable<LLMStreamChunk> ChatStreamAsync(
            List<Message> messages,
            List<JsonNode>? tools = null,
            string? model = null,
            int maxTokens = 4096,
            double temperature = 0.7,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var chunk in _chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return chunk;
            }
        }

        public string GetDefaultModel() => "streaming-fake";
    }

    private sealed class FakeTool : ITool
    {
        public FakeTool(string name, string result)
        {
            Name = name;
            Result = result;
        }

        public string Name { get; }

        public string Description => $"Fake tool {Name}";

        public JsonNode Parameters => JsonNode.Parse("{\"type\":\"object\",\"properties\":{}}")!;

        public string Result { get; }

        public Func<JsonNode?, Task<string>>? Handler { get; init; }

        public List<JsonNode?> Calls { get; } = new();

        public Task<string> ExecuteAsync(JsonNode? arguments)
        {
            Calls.Add(arguments);
            return Handler is null ? Task.FromResult(Result) : Handler(arguments);
        }
    }

    private sealed class StaticMemory : IMemory
    {
        private readonly string _context;

        public StaticMemory(string context = "")
        {
            _context = context;
        }

        public string GetContext() => _context;
    }

    private sealed class RenameToolHook : IAgentHook
    {
        private readonly string _name;

        public RenameToolHook(string name)
        {
            _name = name;
        }

        public Task BeforeToolAsync(AgentToolContext context)
        {
            context.ToolCall = context.ToolCall with { Name = _name };
            return Task.CompletedTask;
        }
    }

    private sealed class RejectToolHook : IAgentHook
    {
        private readonly string _result;

        public RejectToolHook(string result)
        {
            _result = result;
        }

        public Task BeforeToolAsync(AgentToolContext context)
        {
            context.Reject(_result);
            return Task.CompletedTask;
        }
    }

    private sealed class CaptureToolErrorHook : IAgentHook
    {
        public Exception? Error { get; private set; }

        public Task OnToolErrorAsync(AgentToolContext context)
        {
            Error = context.Error;
            return Task.CompletedTask;
        }
    }
}
