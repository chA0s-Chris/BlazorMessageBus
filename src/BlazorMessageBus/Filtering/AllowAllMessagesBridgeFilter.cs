// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Filtering;

/// <summary>
/// A message bridge filter that allows all message types to be bridged without any restrictions.
/// This is the default filter when no specific filtering requirements are needed.
/// </summary>
public sealed class AllowAllMessagesBridgeFilter : IBlazorMessageBridgeFilter
{
    /// <inheritdoc />
    public Boolean ShouldBridge(Type messageType) => true;
}
