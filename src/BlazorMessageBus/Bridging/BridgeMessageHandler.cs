// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Bridging;

/// <summary>
/// Delegate for handling outbound messages from the message bridge.
/// </summary>
/// <param name="messageType">The type of the message being bridged.</param>
/// <param name="payload">The message payload.</param>
/// <param name="cancellationToken">Cancellation token for the operation.</param>
/// <returns>A task representing the asynchronous bridge operation.</returns>
public delegate Task BridgeMessageHandler(Type messageType, Object payload, CancellationToken cancellationToken = default);
