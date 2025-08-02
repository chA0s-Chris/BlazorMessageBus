// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

/// <summary>
/// Represents a subscription to a message type.
/// </summary>
internal class Subscription : IBlazorMessageSubscription
{
    private readonly Action _onDispose;
    private volatile MessageCallback? _callback;

    public Subscription(MessageCallback callback, Action onDispose)
    {
        _callback = callback;
        _onDispose = onDispose;
        MessageType = callback.Type;
    }

    public Boolean IsAlive => _callback is not null;

    public Type MessageType { get; }

    public Task InvokeAsync(Object payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        // Atomically capture callback reference
        var callback = _callback;
        if (callback is null)
            throw new ObjectDisposedException(nameof(Subscription), "Subscription is already disposed.");

        return callback.InvokeAsync(payload);
    }

    internal void Dispose(Boolean invokeCallback)
    {
        var originalCallback = Interlocked.Exchange(ref _callback, null);
        if (originalCallback is null)
            return;

        if (invokeCallback)
        {
            _onDispose.Invoke();
        }
    }

    public void Dispose() => Dispose(true);
}
