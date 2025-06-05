// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

/// <summary>
/// Represents a subscription to a message type.
/// </summary>
public interface IBlazorMessageSubscription : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the subscription is still active.
    /// </summary>
    Boolean IsAlive { get; }

    /// <summary>
    /// Gets the type of message this subscription is for.
    /// </summary>
    Type MessageType { get; }
}
