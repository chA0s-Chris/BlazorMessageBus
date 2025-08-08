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

/// <summary>
/// Represents a callback for a message subscription that can handle synchronous operations.
/// </summary>
/// <param name="handler">Synchronous handler.</param>
/// <typeparam name="T">Message type.</typeparam>
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

/// <summary>
/// Represents a callback for a message subscription using a non-generic asynchronous handler.
/// </summary>
/// <param name="handler">Asynchronous object-typed handler.</param>
/// <param name="type">Runtime message type this callback is associated with.</param>
internal sealed class MessageCallbackAsyncObject(SubscriptionHandlerAsync<Object> handler, Type type) : MessageCallback(type)
{
    public override Task InvokeAsync(Object payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (payload.GetType() != Type)
        {
            throw new ArgumentException($"Payload type '{payload.GetType()}' does not match expected subscription type '{Type}'.", nameof(payload));
        }

        return handler.Invoke(payload);
    }
}

/// <summary>
/// Represents a callback for a message subscription using a non-generic synchronous handler.
/// </summary>
/// <param name="handler">Synchronous object-typed handler.</param>
/// <param name="type">Runtime message type this callback is associated with.</param>
internal sealed class MessageCallbackObject(SubscriptionHandler<Object> handler, Type type) : MessageCallback(type)
{
    public override Task InvokeAsync(Object payload)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(payload);
            if (payload.GetType() != Type)
            {
                throw new ArgumentException($"Payload type '{payload.GetType()}' does not match expected subscription type '{Type}'.", nameof(payload));
            }

            handler.Invoke(payload);
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            return Task.FromException(e);
        }
    }
}
