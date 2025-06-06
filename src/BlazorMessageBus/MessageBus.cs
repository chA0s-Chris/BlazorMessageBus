// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

/// <summary>
/// Represents the main implementation of the message bus for publishing and subscribing to messages.
/// </summary>
internal class MessageBus : IBlazorMessageBus
{
    private readonly ConcurrentDictionary<Type, Message> _messages = [];
    private readonly Func<Exception, Task>? _onPublishException;
    private readonly Boolean _stopOnFirstError;

    public MessageBus(IOptions<BlazorMessageBusOptions>? options = null)
    {
        var messageBusOptions = options?.Value ?? new BlazorMessageBusOptions();

        _stopOnFirstError = messageBusOptions.StopOnFirstError;
        _onPublishException = messageBusOptions.OnPublishException;
    }

    public async Task PublishAsync<T>(T payload) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(payload);

        var message = GetOrCreateMessage(typeof(T));

        if (_stopOnFirstError)
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
        ArgumentNullException.ThrowIfNull(handler);

        return CreateSubscription<T>(new MessageCallbackAsync<T>(handler));
    }

    public IBlazorMessageSubscription Subscribe<T>(SubscriptionHandler<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

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
                if (_onPublishException is not null)
                {
                    await _onPublishException.Invoke(exception);
                }

                throw;
            }
        }
    }

    private async Task InvokeSubscriptionsAsync<T>(Subscriptions subscriptions, T payload) where T : notnull
    {
        var tasks = subscriptions.Select(s => InvokeSubscriptionWrapperAsync(s, payload)).ToArray();

        var exceptions = (await Task.WhenAll(tasks)).Where(e => e is not null)
                                                    .Cast<Exception>()
                                                    .ToList();

        if (exceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(exceptions.First()).Throw();
        }
        else if (exceptions.Count > 1)
        {
            throw new AggregateException(exceptions);
        }
    }

    private async Task<Exception?> InvokeSubscriptionWrapperAsync<T>(Subscription subscription, T payload) where T : notnull
    {
        try
        {
            await subscription.InvokeAsync(payload);
            return null;
        }
        catch (Exception exception)
        {
            if (_onPublishException is not null)
            {
                await _onPublishException.Invoke(exception);
            }

            return exception;
        }
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
