// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Bridging;

using Chaos.BlazorMessageBus.Filtering;

/// <summary>
/// Represents a message bridge that can relay messages between different message bus instances across process boundaries.
/// </summary>
public interface IBlazorMessageBridge : IDisposable
{
    /// <summary>
    /// Gets the unique identifier of the message bridge instance.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets a value indicating whether the bridge is currently active and forwarding messages.
    /// </summary>
    Boolean IsActive { get; }

    /// <summary>
    /// Configures message type filtering for the bridge.
    /// </summary>
    /// <param name="filter">The filter configuration to apply.</param>
    void ConfigureFilter(IBlazorMessageBridgeFilter filter);

    /// <summary>
    /// Injects an inbound message into the local message bus from a remote source.
    /// </summary>
    /// <typeparam name="T">The type of the message payload.</typeparam>
    /// <param name="payload">The message payload to inject.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous injection operation.</returns>
    Task InjectMessageAsync<T>(T payload, CancellationToken cancellationToken = default) where T : notnull;

    /// <summary>
    /// Injects an inbound message into the local message bus from a remote source using runtime type information.
    /// This method is typically used after network transport where only the Type and object are available.
    /// </summary>
    /// <param name="messageType">The type of the message payload.</param>
    /// <param name="payload">The message payload to inject.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous injection operation.</returns>
    Task InjectMessageAsync(Type messageType, Object payload, CancellationToken cancellationToken = default);
}
