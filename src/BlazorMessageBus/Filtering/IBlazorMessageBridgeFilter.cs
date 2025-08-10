// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Filtering;

/// <summary>
/// Defines filtering capabilities for message bridging operations.
/// </summary>
public interface IBlazorMessageBridgeFilter
{
    /// <summary>
    /// Determines whether the specified message type should be bridged.
    /// </summary>
    /// <param name="messageType">The type of the message to check.</param>
    /// <returns>True if the message should be bridged; otherwise, false.</returns>
    Boolean ShouldBridge(Type messageType);
}
