// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Bridging;

/// <summary>
/// Defines the internal target used by <see cref="IBlazorMessageBridge"/> implementations to inject messages
/// into the local message bus and to deregister bridges upon disposal.
/// </summary>
internal interface IBlazorMessageBridgeTarget
{
    /// <summary>
    /// Deregisters and destroys the specified message bridge instance from the target.
    /// Implementations must be idempotent and not throw.
    /// </summary>
    /// <param name="messageBridge">The bridge instance to destroy.</param>
    void DestroyMessageBridge(IBlazorMessageBridge messageBridge);

    /// <summary>
    /// Injects an inbound message into the local message bus from a remote source.
    /// </summary>
    /// <typeparam name="T">The type of the message payload.</typeparam>
    /// <param name="payload">The message payload to inject.</param>
    /// <param name="bridgeId">ID of the message bridge that injects the message.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous injection operation.</returns>
    Task InjectMessageAsync<T>(T payload, Guid bridgeId, CancellationToken cancellationToken = default) where T : notnull;

    /// <summary>
    /// Injects an inbound message into the local message bus from a remote source using runtime type information.
    /// This method is typically used after network transport where only the Type and object are available.
    /// </summary>
    /// <param name="messageType">The type of the message payload.</param>
    /// <param name="payload">The message payload to inject.</param>
    /// <param name="bridgeId">ID of the message bridge that injects the message.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous injection operation.</returns>
    Task InjectMessageAsync(Type messageType, Object payload, Guid bridgeId, CancellationToken cancellationToken = default);
}
