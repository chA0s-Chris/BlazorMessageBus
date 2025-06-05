// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using System.Collections.Concurrent;

internal class MessageBus : IBlazorMessageBus
{
    private readonly ConcurrentDictionary<Type, Message> _messages = [];

    public async Task PublishAsync<T>(T payload) where T : notnull
    {
        if (payload == null) throw new ArgumentNullException(nameof(payload));

        var message = GetOrCreateMessage(typeof(T));

        var tasks = message.Subscriptions
                           .Select(x => x.InvokeAsync(payload));

        await Task.WhenAll(tasks);
    }

    public IBlazorMessageSubscription Subscribe<T>(SubscriptionHandlerAsync<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        return CreateSubscription<T>(new MessageCallbackAsync<T>(handler));
    }

    public IBlazorMessageSubscription Subscribe<T>(SubscriptionHandler<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        return CreateSubscription<T>(new MessageCallback<T>(handler));
    }

    private Subscription CreateSubscription<T>(MessageCallback callback)
    {
        var type = typeof(T);
        var message = GetOrCreateMessage(type);
        return message.Subscriptions.CreateSubscription(callback);
    }

    internal Message GetOrCreateMessage(Type type)
        => _messages.GetOrAdd(type, t => new(t));
}
