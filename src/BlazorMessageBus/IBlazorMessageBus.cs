// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

/// <summary>
/// Represents the main message bus for publishing and subscribing to messages.
/// </summary>
public interface IBlazorMessageBus : IBlazorMessagePublisher
{
    /// <summary>
    /// Subscribes an asynchronous handler to messages of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of message to subscribe to.</typeparam>
    /// <param name="handler">The asynchronous handler to invoke when a message is published.</param>
    /// <returns>A subscription object that can be disposed to unsubscribe.</returns>
    IBlazorMessageSubscription Subscribe<T>(SubscriptionHandlerAsync<T> handler);

    /// <summary>
    /// Subscribes a synchronous handler to messages of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of message to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when a message is published.</param>
    /// <returns>A subscription object that can be disposed to unsubscribe.</returns>
    IBlazorMessageSubscription Subscribe<T>(SubscriptionHandler<T> handler);
}
