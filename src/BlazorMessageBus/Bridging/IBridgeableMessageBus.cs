// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Bridging;

/// <summary>
/// Represents a message bus that supports message bridging capabilities.
/// </summary>
public interface IBridgeableMessageBus : IBlazorMessageBus
{
    /// <summary>
    /// Creates a new message bridge for this message bus instance.
    /// </summary>
    /// <returns>A new message bridge configured for this message bus.</returns>
    IBlazorMessageBridge CreateBridge();
}
