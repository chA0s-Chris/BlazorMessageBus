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

    public Task PublishAsync<T>(T payload) where T : notnull
        => _messageBus.PublishAsync(payload);

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

    public void Dispose()
    {
        foreach (var subscription in Subscriptions)
        {
            subscription.Dispose();
        }

        Subscriptions.Clear();
    }
}
