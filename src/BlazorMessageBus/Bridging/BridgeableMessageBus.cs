// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Bridging;

using System.Collections.Concurrent;

/// <summary>
/// Represents a message bus implementation that supports message bridging capabilities.
/// This implementation uses composition to wrap an existing IBlazorMessageBus and add bridging functionality.
/// </summary>
internal class BridgeableMessageBus : IBridgeableMessageBus
{
    private readonly ConcurrentBag<BlazorMessageBridge> _bridges = [];
    private readonly IBlazorMessageBus _innerMessageBus;

    public BridgeableMessageBus(IBlazorMessageBus innerMessageBus)
    {
        _innerMessageBus = innerMessageBus ?? throw new ArgumentNullException(nameof(innerMessageBus));
    }

    /// <summary>
    /// Internal method to publish messages without forwarding to bridges.
    /// This is used when injecting messages from remote bridges to prevent infinite loops.
    /// </summary>
    internal async Task PublishWithoutBridgeForwardingAsync<T>(T payload) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(payload);

        // Only publish to local subscribers via the inner message bus, do not forward to bridges
        await _innerMessageBus.PublishAsync(payload);
    }

    private static async Task ForwardToBridgeWithErrorHandling(BlazorMessageBridge bridge, Type messageType, Object payload)
    {
        try
        {
            await bridge.ForwardMessageAsync(messageType, payload);
        }
        catch
        {
            // Bridge errors should not affect local message processing
            // Consider adding logging here in the future
        }
    }

    private async Task ForwardToBridgesAsync(Type messageType, Object payload)
    {
        var bridgeForwardingTasks = _bridges
                                    .Where(bridge => bridge.IsActive)
                                    .Select(bridge => ForwardToBridgeWithErrorHandling(bridge, messageType, payload));

        // Wait for all bridge forwarding to complete, but don't let bridge errors affect local processing
        await Task.WhenAll(bridgeForwardingTasks);
    }

    public IBlazorMessageSubscription Subscribe<T>(SubscriptionHandlerAsync<T> handler)
        => _innerMessageBus.Subscribe(handler);

    public IBlazorMessageSubscription Subscribe<T>(SubscriptionHandler<T> handler)
        => _innerMessageBus.Subscribe(handler);

    public async Task PublishAsync<T>(T payload) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(payload);

        // First, publish to local subscribers via the inner message bus
        await _innerMessageBus.PublishAsync(payload);

        // Then, forward to any active bridges
        await ForwardToBridgesAsync(typeof(T), payload);
    }

    public IBlazorMessageBridge CreateBridge()
    {
        var bridge = new BlazorMessageBridge(this);
        _bridges.Add(bridge);
        return bridge;
    }
}
