using System.Text;

namespace Nanobot.Web;

public sealed class WebChatTurn
{
    private const string CancelledMarker = "[已停止]";

    private readonly WebSessionStore _sessions;
    private readonly StringBuilder _assistantContent = new();
    private readonly StringBuilder _assistantReasoning = new();
    private string? _assistantMessageId;
    private bool _completed;

    private WebChatTurn(WebSessionStore sessions, string sessionId, string userMessage)
    {
        _sessions = sessions;
        SessionId = sessionId;
        UserMessage = userMessage;
    }

    public string SessionId { get; }

    public string UserMessage { get; }

    public static WebChatTurn Begin(WebSessionStore sessions, string? requestedSessionId, string message)
    {
        var sessionId = sessions.GetOrCreate(requestedSessionId).Id;
        sessions.AppendMessage(sessionId, "user", message);
        return new WebChatTurn(sessions, sessionId, message);
    }

    public void AppendAssistantDelta(string delta)
    {
        if (string.IsNullOrEmpty(delta))
        {
            return;
        }

        EnsureNotCompleted();
        EnsureAssistantMessage();
        _assistantContent.Append(delta);
        _sessions.AppendToMessage(SessionId, _assistantMessageId!, contentDelta: delta);
    }

    public void AppendAssistantReasoning(string reasoning)
    {
        if (string.IsNullOrEmpty(reasoning))
        {
            return;
        }

        EnsureNotCompleted();
        EnsureAssistantMessage();
        _assistantReasoning.Append(reasoning);
        _sessions.AppendToMessage(SessionId, _assistantMessageId!, reasoningDelta: reasoning);
    }

    public AgentMessageResponse Complete(string answer)
    {
        EnsureNotCompleted();

        var reasoning = GetReasoningOrNull();
        if (_assistantMessageId is null)
        {
            _sessions.AppendMessage(SessionId, "assistant", answer, reasoning);
        }
        else
        {
            _sessions.UpdateMessage(SessionId, _assistantMessageId, "assistant", answer, reasoning);
        }

        _completed = true;
        return new AgentMessageResponse(SessionId, answer);
    }

    public void Fail(string error)
    {
        if (_completed)
        {
            return;
        }

        var content = BuildTerminalContent($"[请求失败] {error}");
        var reasoning = GetReasoningOrNull();
        if (_assistantMessageId is null)
        {
            _sessions.AppendMessage(SessionId, "assistant error", content, reasoning);
        }
        else
        {
            _sessions.UpdateMessage(SessionId, _assistantMessageId, "assistant error", content, reasoning);
        }

        _completed = true;
    }

    public void Cancel()
    {
        if (_completed)
        {
            return;
        }

        var content = BuildTerminalContent(CancelledMarker);
        var reasoning = GetReasoningOrNull();
        if (_assistantMessageId is null)
        {
            _sessions.AppendMessage(SessionId, "assistant", content, reasoning);
        }
        else
        {
            _sessions.UpdateMessage(SessionId, _assistantMessageId, "assistant", content, reasoning);
        }

        _completed = true;
    }

    private string BuildTerminalContent(string marker)
    {
        var content = _assistantContent.ToString().TrimEnd();
        return string.IsNullOrWhiteSpace(content)
            ? marker
            : $"{content}\n\n{marker}";
    }

    private string? GetReasoningOrNull()
    {
        return _assistantReasoning.Length == 0 ? null : _assistantReasoning.ToString();
    }

    private void EnsureAssistantMessage()
    {
        if (_assistantMessageId is not null)
        {
            return;
        }

        _assistantMessageId = _sessions.CreateMessage(SessionId, "assistant").Id;
    }

    private void EnsureNotCompleted()
    {
        if (_completed)
        {
            throw new InvalidOperationException("The chat turn has already been completed.");
        }
    }
}
