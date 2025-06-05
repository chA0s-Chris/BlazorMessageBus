// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents a subscription to a message type.
/// </summary>
internal class Subscription : IBlazorMessageSubscription
{
    private readonly Action _onDispose;
    private MessageCallback? _callback;

    public Subscription(MessageCallback callback, Action onDispose)
    {
        _callback = callback;
        _onDispose = onDispose;
        MessageType = _callback.Type;
    }

    public Boolean IsAlive { get; private set; } = true;

    public Type MessageType { get; }

    public void Dispose() => Dispose(true);

    internal void Dispose(Boolean invokeCallback)
    {
        if (!IsAlive)
            return;

        IsAlive = false;
        _callback = null;

        if (invokeCallback)
        {
            _onDispose.Invoke();
        }
    }

    public Task InvokeAsync(Object payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        CheckIfDisposed();
        return _callback.InvokeAsync(payload);
    }

    [MemberNotNull(nameof(_callback))]
    private void CheckIfDisposed()
    {
        if (_callback is null)
            throw new ObjectDisposedException("Subscription is already disposed.");
    }
}
