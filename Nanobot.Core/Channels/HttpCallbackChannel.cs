using System.Net;
using System.Text;
using Nanobot.Core.Models;

namespace Nanobot.Core.Channels;

public abstract class HttpCallbackChannel : IMessageChannel
{
    private readonly ChannelMessageHandler _onMessage;
    private readonly string _listenPrefix;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    protected HttpCallbackChannel(string name, string listenPrefix, ChannelMessageHandler onMessage)
    {
        Name = name;
        _listenPrefix = listenPrefix.EndsWith('/') ? listenPrefix : listenPrefix + "/";
        _onMessage = onMessage;
    }

    public string Name { get; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(_listenPrefix);
        _listener.Start();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = Task.Run(() => AcceptLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _cts?.Cancel();
        _listener?.Close();
        return Task.CompletedTask;
    }

    public abstract Task SendAsync(OutboundMessage message, CancellationToken cancellationToken = default);

    protected abstract Task<IReadOnlyList<InboundMessage>> ParseInboundAsync(
        HttpListenerRequest request,
        string body,
        CancellationToken cancellationToken);

    protected virtual Task<bool> TryHandleControlRequestAsync(
        HttpListenerContext context,
        string body,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener?.IsListening == true)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(cancellationToken);
                _ = Task.Run(() => HandleContextAsync(context, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private async Task HandleContextAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            var body = await reader.ReadToEndAsync(cancellationToken);
            if (await TryHandleControlRequestAsync(context, body, cancellationToken))
            {
                return;
            }

            var inboundMessages = await ParseInboundAsync(context.Request, body, cancellationToken);
            foreach (var inbound in inboundMessages)
            {
                var outbound = await _onMessage(inbound, cancellationToken);
                if (outbound is not null)
                {
                    await SendAsync(outbound, cancellationToken);
                }
            }

            await RespondAsync(context, HttpStatusCode.OK, "ok", cancellationToken);
        }
        catch (Exception ex)
        {
            await RespondAsync(context, HttpStatusCode.InternalServerError, ex.Message, cancellationToken);
        }
    }

    protected static async Task RespondAsync(
        HttpListenerContext context,
        HttpStatusCode statusCode,
        string content,
        CancellationToken cancellationToken)
    {
        context.Response.StatusCode = (int)statusCode;
        var bytes = Encoding.UTF8.GetBytes(content);
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.OutputStream.WriteAsync(bytes, cancellationToken);
        context.Response.Close();
    }
}
