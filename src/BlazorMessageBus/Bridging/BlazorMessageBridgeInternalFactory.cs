// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Bridging;

/// <summary>
/// Default internal factory for creating <see cref="IBlazorMessageBridgeInternal"/> instances.
/// </summary>
internal class BlazorMessageBridgeInternalFactory : IBlazorMessageBridgeInternalFactory
{
    /// <inheritdoc />
    public IBlazorMessageBridgeInternal CreateMessageBridge(IBlazorMessageBridgeTarget target, BridgeMessageHandler messageHandler)
    {
        var bridge = new BlazorMessageBridge(target, messageHandler);
        return bridge;
    }
}
