// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Bridging;

/// <summary>
/// Internal extension of <see cref="IBlazorMessageBridge"/> used by the message bus to forward
/// outbound messages after local delivery. Not intended for public consumption.
/// </summary>
internal interface IBlazorMessageBridgeInternal : IBlazorMessageBridge
{
    /// <summary>
    /// Forwards a locally published message to the remote side if the bridge is active and the message type
    /// is allowed by the configured filter. Exceptions during forwarding are isolated from local processing.
    /// </summary>
    /// <param name="messageType">The type of the message being forwarded.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous forwarding operation.</returns>
    Task ForwardMessageAsync(Type messageType, Object payload, CancellationToken cancellationToken = default);
}
