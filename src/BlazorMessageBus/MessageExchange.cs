// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

internal class MessageExchange : IBlazorMessageExchange
{
    private readonly IBlazorMessageBus _messageBus;
    private readonly List<IBlazorMessageSubscription> _subscriptions = [];
    // Remove all event and handler registration logic

    public MessageExchange(IBlazorMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public Func<Task, Exception, Task>? OnPublishException { get; set; }

    public Task PublishAsync<T>(T payload) where T : notnull
        => _messageBus.PublishAsync<T>(payload);

    public void Subscribe<T>(SubscriptionHandlerAsync<T> handler)
    {
        var subscription = _messageBus.Subscribe(handler);
        _subscriptions.Add(subscription);
    }

    public void Subscribe<T>(SubscriptionHandler<T> handler)
    {
        var subscription = _messageBus.Subscribe(handler);
        _subscriptions.Add(subscription);
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
        // Remove all registered exception handlers
    }

    private sealed class EventHandlerRegistration
    {
        public Action<Exception> Original { get; }
        public Action<Exception> Wrapped { get; }
        public EventHandlerRegistration(Action<Exception> original, Action<Exception> wrapped)
        {
            Original = original;
            Wrapped = wrapped;
        }
    }
}
