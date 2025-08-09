// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Bridging;

using Chaos.BlazorMessageBus.Filtering;

/// <summary>
/// Provides message bridging functionality to relay messages between different message bus instances across process boundaries.
/// </summary>
internal class BlazorMessageBridge : IBlazorMessageBridge
{
    private readonly BridgeableMessageBus _messageBus;
    private Int32 _disposed;
    private volatile BridgeState _state;

    public BlazorMessageBridge(BridgeableMessageBus messageBus)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _state = new(false, null, BlazorMessageBridgeFilters.All());
    }

    public Boolean IsActive => _state.IsActive && _disposed == 0;

    /// <summary>
    /// Internal method called by the message bus to forward outbound messages to the bridge.
    /// </summary>
    /// <param name="messageType">The type of the message being published.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous bridge operation.</returns>
    internal async Task ForwardMessageAsync(Type messageType, Object payload, CancellationToken cancellationToken = default)
    {
        if (_disposed != 0)
        {
            return;
        }

        var (isActive, handler, filter) = _state;
        if (!isActive)
        {
            return;
        }

        if (handler != null && filter.ShouldBridge(messageType))
        {
            try
            {
                await handler(messageType, payload, cancellationToken);
            }
            catch
            {
                // Bridge errors should not affect local message processing
            }
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed != 0, this);

    public void ConfigureFilter(IBlazorMessageBridgeFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ThrowIfDisposed();

        BridgeState currentState, newState;
        do
        {
            currentState = _state;
            newState = currentState with
            {
                Filter = filter
            };
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _state, newState, currentState), currentState));
    }

    public Task InjectMessageAsync<T>(T payload, CancellationToken cancellationToken = default) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(payload);
        ThrowIfDisposed();

        // Use the internal method to prevent infinite bridge forwarding loops
        return _messageBus.PublishWithoutBridgeForwardingAsync(payload);
    }

    public Task InjectMessageAsync(Type messageType, Object payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(payload);
        ThrowIfDisposed();

        // Use reflection to call the generic PublishWithoutBridgeForwardingAsync method at runtime
        var publishMethod = typeof(BridgeableMessageBus).GetMethod(nameof(BridgeableMessageBus.PublishWithoutBridgeForwardingAsync),
                                                                   System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (publishMethod is null)
        {
            throw new InvalidOperationException("PublishWithoutBridgeForwardingAsync method not found on BridgeableMessageBus");
        }

        var genericPublishMethod = publishMethod.MakeGenericMethod(messageType);
        var task = (Task)genericPublishMethod.Invoke(_messageBus, [payload])!;
        return task;
    }

    public Task StartAsync(BridgeMessageHandler outboundHandler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outboundHandler);
        ThrowIfDisposed();

        BridgeState currentState, newState;
        do
        {
            currentState = _state;
            if (currentState.IsActive)
            {
                throw new InvalidOperationException("Message bridge is already active.");
            }

            newState = currentState with
            {
                IsActive = true,
                OutboundHandler = outboundHandler
            };
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _state, newState, currentState), currentState));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        BridgeState currentState, newState;
        do
        {
            currentState = _state;
            if (!currentState.IsActive)
            {
                return Task.CompletedTask;
            }

            newState = currentState with
            {
                IsActive = false,
                OutboundHandler = null
            };
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _state, newState, currentState), currentState));

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        BridgeState currentState, newState;
        do
        {
            currentState = _state;
            newState = new(false, null, currentState.Filter);
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _state, newState, currentState), currentState));
    }

    // Immutable state record for atomic updates
    private sealed record BridgeState(Boolean IsActive, BridgeMessageHandler? OutboundHandler, IBlazorMessageBridgeFilter Filter);
}
