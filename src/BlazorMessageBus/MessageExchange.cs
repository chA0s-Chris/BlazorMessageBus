// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using Chaos.BlazorMessageBus.Bridging;

/// <summary>
/// Represents a message exchange that manages subscriptions within a Blazor component's scope.
/// </summary>
internal class MessageExchange : IBlazorMessageExchange
{
    private readonly IBridgeableMessageBus _messageBus;

    public MessageExchange(IBridgeableMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    internal List<IBlazorMessageSubscription> Subscriptions { get; } = [];

    public void Subscribe<T>(SubscriptionHandlerAsync<T> handler)
    {
        var subscription = _messageBus.Subscribe(handler);
        Subscriptions.Add(subscription);
    }

    public void Subscribe<T>(SubscriptionHandler<T> handler)
    {
        var subscription = _messageBus.Subscribe(handler);
        Subscriptions.Add(subscription);
    }

    public void Subscribe(Type messageType, SubscriptionHandlerAsync<Object> handler)
    {
        var subscription = _messageBus.Subscribe(messageType, handler);
        Subscriptions.Add(subscription);
    }

    public void Subscribe(Type messageType, SubscriptionHandler<Object> handler)
    {
        var subscription = _messageBus.Subscribe(messageType, handler);
        Subscriptions.Add(subscription);
    }

    public Task PublishAsync<T>(T payload) where T : notnull
        => _messageBus.PublishAsync(payload);

    public Task PublishAsync(Object payload)
        => _messageBus.PublishAsync(payload);

    public void Dispose()
    {
        foreach (var subscription in Subscriptions)
        {
            subscription.Dispose();
        }

        Subscriptions.Clear();
    }
}
