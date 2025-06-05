// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

/// <summary>
/// Represents the base of all callback for a message subscription.
/// </summary>
internal abstract class MessageCallback(Type type)
{
    public Type Type { get; } = type;

    public abstract Task InvokeAsync(Object payload);
}

/// <summary>
/// Represents a callback for a message subscription that can handle asynchronous operations.
/// </summary>
/// <param name="handler">Asynchronous handler.</param>
/// <typeparam name="T">Message type.</typeparam>
internal class MessageCallbackAsync<T>(SubscriptionHandlerAsync<T> handler) : MessageCallback(typeof(T))
{
    public override Task InvokeAsync(Object payload)
        => handler.Invoke((T)payload);
}

// <summary>
// Represents a callback for a message subscription that can handle synchronous operations.
// </summary>
// <param name="handler">Synchronous handler.</param>
// <typeparam name="T">Message type.</typeparam>
internal class MessageCallback<T>(SubscriptionHandler<T> handler) : MessageCallback(typeof(T))
{
    public override Task InvokeAsync(Object payload)
    {
        try
        {
            handler.Invoke((T)payload);
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            return Task.FromException(e);
        }
    }
}
