// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

/// <summary>
/// Represents a message exchange that manages subscriptions within a Blazor component's scope.
/// </summary>
internal class MessageExchange : IBlazorMessageExchange
{
    private readonly IBlazorMessageBus _messageBus;

    public MessageExchange(IBlazorMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    internal List<IBlazorMessageSubscription> Subscriptions { get; } = [];

    /// <inheritdoc />
    public void Subscribe<T>(SubscriptionHandlerAsync<T> handler)
    {
        var subscription = _messageBus.Subscribe(handler);
        Subscriptions.Add(subscription);
    }

    /// <inheritdoc />
    public void Subscribe<T>(SubscriptionHandler<T> handler)
    {
        var subscription = _messageBus.Subscribe(handler);
        Subscriptions.Add(subscription);
    }

    /// <inheritdoc />
    public void Subscribe(Type messageType, SubscriptionHandlerAsync<Object> handler)
    {
        var subscription = _messageBus.Subscribe(messageType, handler);
        Subscriptions.Add(subscription);
    }

    /// <inheritdoc />
    public void Subscribe(Type messageType, SubscriptionHandler<Object> handler)
    {
        var subscription = _messageBus.Subscribe(messageType, handler);
        Subscriptions.Add(subscription);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(T payload, CancellationToken cancellationToken = default) where T : notnull
        => _messageBus.PublishAsync(payload, cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync(Object payload, CancellationToken cancellationToken = default)
        => _messageBus.PublishAsync(payload, cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var subscription in Subscriptions)
        {
            subscription.Dispose();
        }

        Subscriptions.Clear();
    }
}
