// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

/// <summary>
/// Provides a simplified interface for managing message subscriptions within a Blazor component's scope.
/// </summary>
public interface IBlazorMessageExchange : IBlazorMessagePublisher, IDisposable
{
    /// <summary>
    /// Subscribes an asynchronous handler to messages of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of message to subscribe to.</typeparam>
    /// <param name="handler">The asynchronous handler to invoke when a message is published.</param>
    void Subscribe<T>(SubscriptionHandlerAsync<T> handler);

    /// <summary>
    /// Subscribes a synchronous handler to messages of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of message to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke when a message is published.</param>
    void Subscribe<T>(SubscriptionHandler<T> handler);
}
