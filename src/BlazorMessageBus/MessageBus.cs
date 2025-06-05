// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

/// <summary>
/// Represents the main implementation of the message bus for publishing and subscribing to messages.
/// </summary>
internal class MessageBus : IBlazorMessageBus
{
    private readonly ConcurrentDictionary<Type, Message> _messages = [];
    private readonly Func<Exception, Task>? _onPublishException;
    private readonly BlazorMessageBusOptions _options;

    public MessageBus(IOptions<BlazorMessageBusOptions>? options = null)
    {
        _options = options?.Value ?? new BlazorMessageBusOptions();
        _onPublishException = _options.OnPublishException;
    }

    public async Task PublishAsync<T>(T payload) where T : notnull
    {
        if (payload == null) throw new ArgumentNullException(nameof(payload));

        var message = GetOrCreateMessage(typeof(T));

        if (_options.StopOnFirstError)
        {
            await InvokeSubscriptionsAndStopOnFirstErrorAsync(message.Subscriptions, payload).ConfigureAwait(false);
        }
        else
        {
            await InvokeSubscriptionsAsync(message.Subscriptions, payload).ConfigureAwait(false);
        }
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

    private async Task InvokeSubscriptionsAndStopOnFirstErrorAsync<T>(Subscriptions subscriptions, T payload) where T : notnull
    {
        foreach (var subscription in subscriptions)
        {
            try
            {
                await subscription.InvokeAsync(payload);
            }
            catch (Exception exception)
            {
                _onPublishException?.Invoke(exception);
                throw;
            }
        }
    }

    private Task InvokeSubscriptionsAsync<T>(Subscriptions subscriptions, T payload) where T : notnull
    {
        var tasks =
            subscriptions.Select(subscription
                                     => subscription
                                        .InvokeAsync(payload)
                                        .ContinueWith(task =>
                                        {
                                            if (!task.IsFaulted || _onPublishException is null || task.Exception is null)
                                                return;

                                            foreach (var innerException in task.Exception.Flatten().InnerExceptions)
                                            {
                                                _onPublishException.Invoke(innerException);
                                            }
                                        }));

        return Task.WhenAll(tasks);
    }

    private Subscription CreateSubscription<T>(MessageCallback callback)
    {
        var type = typeof(T);
        var message = GetOrCreateMessage(type);
        return message.Subscriptions.CreateSubscription(callback);
    }

    private Message GetOrCreateMessage(Type type)
        => _messages.GetOrAdd(type, t => new(t));
}
