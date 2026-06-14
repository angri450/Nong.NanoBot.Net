using Nanobot.Web;

namespace Nanobot.Tests;

public class WebChatTurnTests
{
    [Fact]
    public void Complete_PersistsReasoningAndFinalAssistantContent()
    {
        var store = new WebSessionStore(CreateWorkspace());
        var turn = WebChatTurn.Begin(store, null, "Explain the result.");

        turn.AppendAssistantReasoning("First inspect the inputs.");
        turn.AppendAssistantDelta("Draft");
        var response = turn.Complete("Final answer");

        var session = store.Get(response.SessionId);

        Assert.NotNull(session);
        Assert.Single(store.List());
        Assert.Equal(2, session!.Messages.Count);
        Assert.Equal("Final answer", session.Messages[^1].Content);
        Assert.Equal("First inspect the inputs.", session.Messages[^1].Reasoning);
        Assert.Equal("assistant", session.Messages[^1].Role);
    }

    [Fact]
    public void Cancel_PreservesPartialAssistantContentWithoutCreatingOrphanSession()
    {
        var store = new WebSessionStore(CreateWorkspace());
        var turn = WebChatTurn.Begin(store, null, "Write something long.");

        turn.AppendAssistantDelta("Partial answer");
        turn.Cancel();

        var sessions = store.List();
        var session = store.Get(turn.SessionId);

        Assert.Single(sessions);
        Assert.NotNull(session);
        Assert.Equal(2, session!.Messages.Count);
        Assert.Equal("assistant", session.Messages[^1].Role);
        Assert.Equal("Partial answer\n\n[已停止]", session.Messages[^1].Content);
    }

    [Fact]
    public void Fail_PersistsAssistantErrorRoleAndReasoning()
    {
        var store = new WebSessionStore(CreateWorkspace());
        var turn = WebChatTurn.Begin(store, null, "Use a tool.");

        turn.AppendAssistantReasoning("Need to call the tool.");
        turn.AppendAssistantDelta("Started");
        turn.Fail("Tool execution failed.");

        var session = store.Get(turn.SessionId);

        Assert.NotNull(session);
        Assert.Equal(2, session!.Messages.Count);
        Assert.Equal("assistant error", session.Messages[^1].Role);
        Assert.Equal("Started\n\n[请求失败] Tool execution failed.", session.Messages[^1].Content);
        Assert.Equal("Need to call the tool.", session.Messages[^1].Reasoning);
    }

    [Fact]
    public void Fail_BeforeStreamingAddsAssistantErrorMessage()
    {
        var store = new WebSessionStore(CreateWorkspace());
        var turn = WebChatTurn.Begin(store, null, "Hello?");

        turn.Fail("Agent runtime is not ready.");

        var session = store.Get(turn.SessionId);

        Assert.NotNull(session);
        Assert.Equal(2, session!.Messages.Count);
        Assert.Equal("assistant error", session.Messages[^1].Role);
        Assert.Equal("[请求失败] Agent runtime is not ready.", session.Messages[^1].Content);
    }

    private static string CreateWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "nanobot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
