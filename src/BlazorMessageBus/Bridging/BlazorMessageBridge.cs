// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Bridging;

using Chaos.BlazorMessageBus.Filtering;

/// <summary>
/// Core implementation of a message bridge that can forward outbound messages to a custom transport
/// and inject inbound messages into the local <see cref="IBlazorMessageBus"/>.
/// The bridge is thread-safe, supports message type filtering, and isolates forwarding errors from local delivery.
/// </summary>
internal class BlazorMessageBridge : IBlazorMessageBridgeInternal
{
    private readonly IBlazorMessageBridgeTarget _target;
    private Int32 _disposed;
    private volatile BridgeState _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorMessageBridge"/> class.
    /// </summary>
    /// <param name="target">The message bus target used for inbound injection.</param>
    /// <param name="outboundHandler">The asynchronous handler used to forward outbound messages to a remote transport.</param>
    public BlazorMessageBridge(IBlazorMessageBridgeTarget target, BridgeMessageHandler outboundHandler)
    {
        _target = target;
        _state = new(true, outboundHandler, BlazorMessageBridgeFilters.All());
    }

#if NET9_0_OR_GREATER
    /// <summary>
    /// Gets the unique identifier for this bridge instance.
    /// </summary>
    public Guid Id { get; } = Guid.CreateVersion7();
#else
    /// <summary>
    /// Gets the unique identifier for this bridge instance.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();
#endif

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed != 0, this);

    /// <summary>
    /// Gets a value indicating whether the bridge is active and not disposed.
    /// When inactive or disposed, messages will not be forwarded.
    /// </summary>
    public Boolean IsActive => _state.IsActive && _disposed == 0;

    /// <summary>
    /// Configures the message type filter that determines which messages are forwarded.
    /// </summary>
    /// <param name="filter">The filter to apply. Must not be null.</param>
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

    /// <summary>
    /// Injects an inbound message of type <typeparamref name="T"/> into the local bus.
    /// </summary>
    /// <typeparam name="T">The message payload type.</typeparam>
    /// <param name="payload">The message payload to inject.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous injection operation.</returns>
    public Task InjectMessageAsync<T>(T payload, CancellationToken cancellationToken = default) where T : notnull
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(payload);
        return _target.InjectMessageAsync(payload, Id, cancellationToken);
    }

    /// <summary>
    /// Injects an inbound message using the runtime <see cref="Type"/> of the payload.
    /// </summary>
    /// <param name="messageType">The runtime type of the payload.</param>
    /// <param name="payload">The message payload to inject.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous injection operation.</returns>
    public Task InjectMessageAsync(Type messageType, Object payload, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(payload);
        return _target.InjectMessageAsync(messageType, payload, Id, cancellationToken);
    }

    /// <summary>
    /// Forwards a locally published message to the remote side if the bridge is active and the filter allows it.
    /// Forwarding exceptions are swallowed to ensure local message processing is unaffected.
    /// </summary>
    /// <param name="messageType">The type of the message being forwarded.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous forwarding operation.</returns>
    public async Task ForwardMessageAsync(Type messageType, Object payload, CancellationToken cancellationToken = default)
    {
        if (_disposed != 0)
        {
            return;
        }

        var (isActive, outboundHandler, filter) = _state;
        if (!isActive)
        {
            return;
        }

        if (outboundHandler is not null &&
            filter.ShouldBridge(messageType))
        {
            try
            {
                await outboundHandler.Invoke(messageType, payload, cancellationToken);
            }
            catch
            {
                // Bridge errors should not affect local message processing
            }
        }
    }

    private sealed record BridgeState(Boolean IsActive, BridgeMessageHandler? OutboundHandler, IBlazorMessageBridgeFilter Filter);

    /// <summary>
    /// Disposes the bridge, making it inactive and deregistering it from the message bus target.
    /// Subsequent calls are no-ops. Disposal never throws.
    /// </summary>
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
            newState = currentState with
            {
                IsActive = false,
                OutboundHandler = null
            };
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _state, newState, currentState), currentState));

        try
        {
            _target.DestroyMessageBridge(this);
        }
        catch
        {
            // Bridge disposal must not throw
        }
    }
}
