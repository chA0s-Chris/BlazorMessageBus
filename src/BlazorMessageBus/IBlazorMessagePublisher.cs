// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

/// <summary>
/// Defines a publisher for messages in BlazorMessageBus.
/// </summary>
public interface IBlazorMessagePublisher
{
    /// <summary>
    /// Publishes a message to all subscribers of the specified type.
    /// </summary>
    /// <remarks>
    /// If there are no subscribers for the specified message type, this method does nothing.
    /// </remarks>
    /// <typeparam name="T">The type of the message payload.</typeparam>
    /// <param name="payload">The message payload to publish.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    Task PublishAsync<T>(T payload) where T : notnull;
}
