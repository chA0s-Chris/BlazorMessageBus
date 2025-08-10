// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Bridging;

/// <summary>
/// Defines a host capable of creating message bridges that forward outbound messages
/// and support inbound message injection.
/// </summary>
public interface IBlazorMessageBridgeHost
{
    /// <summary>
    /// Creates a new message bridge that will forward outbound messages using the provided handler.
    /// </summary>
    /// <param name="messageHandler">The asynchronous handler invoked to deliver bridged messages to the remote side.</param>
    /// <returns>A newly created <see cref="IBlazorMessageBridge"/> instance.</returns>
    IBlazorMessageBridge CreateMessageBridge(BridgeMessageHandler messageHandler);
}
