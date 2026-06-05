using System.Net;
using System.Net.WebSockets;
using System.Text;
using Nanobot.Core.Agent;
using Nanobot.Core.Events;
using AgentInstance = Nanobot.Core.Agent.Agent;

namespace Nanobot.Core.Gateway;

public class WebSocketAgentGateway
{
    private const int BufferSize = 8192;

    private readonly AgentInstance _agent;
    private readonly RuntimeEventBus _eventBus;
    private readonly string _workspace;
    private readonly string _prefix;
    private readonly string? _authToken;
    private readonly bool _streamingEnabled;

    public WebSocketAgentGateway(
        AgentInstance agent,
        RuntimeEventBus eventBus,
        string workspace,
        string prefix,
        string? authToken = null,
        bool streamingEnabled = true)
    {
        _agent = agent;
        _eventBus = eventBus;
        _workspace = workspace;
        _prefix = prefix.EndsWith('/') ? prefix : prefix + "/";
        _authToken = authToken;
        _streamingEnabled = streamingEnabled;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(_prefix);
        listener.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            var context = await listener.GetContextAsync().WaitAsync(cancellationToken);
            _ = Task.Run(() => HandleContextAsync(context, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleContextAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        if (!WebSocketGatewayAuth.IsAuthorized(
            _authToken,
            context.Request.Headers["Authorization"],
            context.Request.QueryString["token"]))
        {
            await RejectAsync(context, HttpStatusCode.Unauthorized, "Unauthorized.", cancellationToken);
            return;
        }

        if (!context.Request.IsWebSocketRequest)
        {
            await RejectAsync(context, HttpStatusCode.BadRequest, "WebSocket upgrade required.", cancellationToken);
            return;
        }

        var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
        var socket = webSocketContext.WebSocket;
        var sendLock = new SemaphoreSlim(1, 1);

        using var subscription = _eventBus.Subscribe(async runtimeEvent =>
        {
            if (socket.State != WebSocketState.Open)
            {
                return;
            }

            await SendTextAsync(socket, WebSocketGatewayProtocol.FormatEvent(runtimeEvent), sendLock, cancellationToken);
        });

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var payload = await ReceiveTextAsync(socket, cancellationToken);
                if (payload is null)
                {
                    break;
                }

                var request = WebSocketGatewayProtocol.ParseRequest(payload);
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    await SendTextAsync(socket, WebSocketGatewayProtocol.FormatError("Message is required."), sendLock, cancellationToken);
                    continue;
                }

                try
                {
                    var executionContext = AgentExecutionContext.CreateRoot(_workspace) with
                    {
                        SessionId = string.IsNullOrWhiteSpace(request.SessionId)
                            ? AgentExecutionContext.DefaultSessionId
                            : request.SessionId
                    };
                    var response = _streamingEnabled
                        ? await _agent.RunStreamingAsync(
                            request.Message,
                            executionContext,
                            async (delta, ct) =>
                                await SendTextAsync(socket, WebSocketGatewayProtocol.FormatDelta(delta), sendLock, ct),
                            cancellationToken
                        )
                        : await _agent.RunAsync(request.Message, executionContext);
                    await SendTextAsync(socket, WebSocketGatewayProtocol.FormatResponse(response), sendLock, cancellationToken);
                }
                catch (Exception ex)
                {
                    await SendTextAsync(socket, WebSocketGatewayProtocol.FormatError(ex.Message), sendLock, cancellationToken);
                }
            }
        }
        finally
        {
            sendLock.Dispose();
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }

    private static async Task RejectAsync(
        HttpListenerContext context,
        HttpStatusCode statusCode,
        string message,
        CancellationToken cancellationToken)
    {
        context.Response.StatusCode = (int)statusCode;
        await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(message), cancellationToken);
        context.Response.Close();
    }

    private static async Task<string?> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[BufferSize];
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static async Task SendTextAsync(
        WebSocket socket,
        string payload,
        SemaphoreSlim sendLock,
        CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        await sendLock.WaitAsync(cancellationToken);
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
            }
        }
        finally
        {
            sendLock.Release();
        }
    }
}
