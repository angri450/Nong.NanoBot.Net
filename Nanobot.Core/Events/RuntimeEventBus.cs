namespace Nanobot.Core.Events;

public class RuntimeEventBus
{
    private readonly object _lock = new();
    private readonly List<Func<RuntimeEvent, Task>> _subscribers = new();

    public IDisposable Subscribe(Action<RuntimeEvent> handler)
    {
        return Subscribe(evt =>
        {
            handler(evt);
            return Task.CompletedTask;
        });
    }

    public IDisposable Subscribe(Func<RuntimeEvent, Task> handler)
    {
        lock (_lock)
        {
            _subscribers.Add(handler);
        }

        return new Subscription(this, handler);
    }

    public async Task PublishAsync(RuntimeEvent runtimeEvent)
    {
        Func<RuntimeEvent, Task>[] subscribers;
        lock (_lock)
        {
            subscribers = _subscribers.ToArray();
        }

        foreach (var subscriber in subscribers)
        {
            await subscriber(runtimeEvent);
        }
    }

    private void Unsubscribe(Func<RuntimeEvent, Task> handler)
    {
        lock (_lock)
        {
            _subscribers.Remove(handler);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly RuntimeEventBus _bus;
        private readonly Func<RuntimeEvent, Task> _handler;
        private bool _disposed;

        public Subscription(RuntimeEventBus bus, Func<RuntimeEvent, Task> handler)
        {
            _bus = bus;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _bus.Unsubscribe(_handler);
            _disposed = true;
        }
    }
}
