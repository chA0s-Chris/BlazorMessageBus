// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using Chaos.BlazorMessageBus.Bridging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

/// <summary>
/// Represents the main implementation of the message bus for publishing and subscribing to messages.
/// </summary>
internal class MessageBus : IBlazorMessageBus, IBlazorMessageBridgeTarget
{
    private readonly ConcurrentDictionary<Guid, IBlazorMessageBridgeInternal> _bridges = [];
    private readonly IBlazorMessageBridgeInternalFactory _messageBridgeInternalFactory;
    private readonly ConcurrentDictionary<Type, Message> _messages = [];
    private readonly Func<Exception, Task>? _onPublishException;
    private readonly Func<Exception, Task>? _onBridgeException;
    private readonly Boolean _stopOnFirstError;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBus"/> class.
    /// </summary>
    /// <param name="messageBridgeInternalFactory">The internal bridge factory used to create message bridges.</param>
    /// <param name="options">Optional message bus options.</param>
    public MessageBus(IBlazorMessageBridgeInternalFactory messageBridgeInternalFactory, IOptions<BlazorMessageBusOptions>? options = null)
    {
        _messageBridgeInternalFactory = messageBridgeInternalFactory;
        var messageBusOptions = options?.Value ?? new BlazorMessageBusOptions();

        _stopOnFirstError = messageBusOptions.StopOnFirstError;
        _onPublishException = messageBusOptions.OnPublishException;
        _onBridgeException = messageBusOptions.OnBridgeException;
    }

    private Subscription CreateSubscription(Type type, MessageCallback callback)
    {
        var message = GetOrCreateMessage(type);
        return message.Subscriptions.CreateSubscription(callback);
    }

    private async Task ForwardMessageToBridges(Type messageType, Object payload, Guid? skipBridgeId = null, CancellationToken cancellationToken = default)
    {
        var forwardTasks = _bridges.Values
                                   .Where(bridge => bridge.IsActive && (skipBridgeId is null || bridge.Id != skipBridgeId))
                                   .Select(bridge => bridge.ForwardMessageAsync(messageType, payload, cancellationToken));

        await Task.WhenAll(forwardTasks).ConfigureAwait(false);
    }

    private Message GetOrCreateMessage(Type type)
        => _messages.GetOrAdd(type, t => new(t));

    private async Task InternalPublishAsync(Type payloadType, Object payload, Guid? skipBridgeId = null, CancellationToken cancellationToken = default)
    {
        var message = GetOrCreateMessage(payloadType);

        if (_stopOnFirstError)
        {
            await InvokeSubscriptionsAndStopOnFirstErrorAsync(message.Subscriptions, payload).ConfigureAwait(false);
        }
        else
        {
            await InvokeSubscriptionsAsync(message.Subscriptions, payload).ConfigureAwait(false);
        }

        if (!_bridges.IsEmpty)
        {
            // Do not await bridge forwarding (fire-and-forget).
            // Any exceptions thrown during bridge forwarding are handled by OnBridgeException (if provided)
            // and will not affect the calling code.
            _ = ForwardMessageToBridges(payloadType, payload, skipBridgeId, cancellationToken);
        }
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

    /// <inheritdoc />
    public IBlazorMessageBridge CreateMessageBridge(BridgeMessageHandler messageHandler)
    {
        ArgumentNullException.ThrowIfNull(messageHandler);

        var bridge = _messageBridgeInternalFactory.CreateMessageBridge(this, messageHandler, _onBridgeException);
        if (!_bridges.TryAdd(bridge.Id, bridge))
        {
            throw new InvalidOperationException($"Message bridge with id '{bridge.Id}' already exists.");
        }

        return bridge;
    }

    /// <inheritdoc />
    public void DestroyMessageBridge(IBlazorMessageBridge messageBridge)
    {
        ArgumentNullException.ThrowIfNull(messageBridge);

        _bridges.TryRemove(messageBridge.Id, out _);
    }

    /// <inheritdoc />
    public Task InjectMessageAsync<T>(T payload, Guid bridgeId, CancellationToken cancellationToken = default) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(payload);
        return InjectMessageAsync(typeof(T), payload, bridgeId, cancellationToken);
    }

    /// <inheritdoc />
    public Task InjectMessageAsync(Type messageType, Object payload, Guid bridgeId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(payload);
        return InternalPublishAsync(messageType, payload, bridgeId, cancellationToken);
    }

    /// <inheritdoc />
    public IBlazorMessageSubscription Subscribe<T>(SubscriptionHandlerAsync<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return CreateSubscription(typeof(T), new MessageCallbackAsync<T>(handler));
    }

    /// <inheritdoc />
    public IBlazorMessageSubscription Subscribe<T>(SubscriptionHandler<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return CreateSubscription(typeof(T), new MessageCallback<T>(handler));
    }

    /// <inheritdoc />
    public IBlazorMessageSubscription Subscribe(Type messageType, SubscriptionHandlerAsync<Object> handler)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(handler);
        return CreateSubscription(messageType, new MessageCallbackAsyncObject(handler, messageType));
    }

    /// <inheritdoc />
    public IBlazorMessageSubscription Subscribe(Type messageType, SubscriptionHandler<Object> handler)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(handler);
        return CreateSubscription(messageType, new MessageCallbackObject(handler, messageType));
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(T payload, CancellationToken cancellationToken = default) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(payload);
        return InternalPublishAsync(typeof(T), payload, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(Object payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return InternalPublishAsync(payload.GetType(), payload, cancellationToken: cancellationToken);
    }
}
