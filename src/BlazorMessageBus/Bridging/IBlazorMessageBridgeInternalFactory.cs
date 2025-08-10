// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Bridging;

/// <summary>
/// Internal factory abstraction for creating <see cref="IBlazorMessageBridgeInternal"/> instances.
/// Used by the message bus to construct bridges with the appropriate target and outbound handler.
/// </summary>
internal interface IBlazorMessageBridgeInternalFactory
{
    /// <summary>
    /// Creates a new internal message bridge bound to the specified target and outbound handler.
    /// </summary>
    /// <param name="target">The bridge target used for inbound message injection and lifecycle management.</param>
    /// <param name="messageHandler">The asynchronous outbound handler used to forward bridged messages.</param>
    /// <returns>A new <see cref="IBlazorMessageBridgeInternal"/> implementation.</returns>
    IBlazorMessageBridgeInternal CreateMessageBridge(IBlazorMessageBridgeTarget target, BridgeMessageHandler messageHandler);
}
