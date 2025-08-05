// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Filtering;

/// <summary>
/// A message bridge filter that only allows specific message types to be bridged (inclusive filtering).
/// Only message types that are explicitly included in the allowed types collection will be bridged.
/// All other message types will be blocked from bridging.
/// </summary>
public sealed class IncludeMessagesBridgeFilter : IBlazorMessageBridgeFilter
{
    private readonly HashSet<Type> _allowedTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncludeMessagesBridgeFilter"/> class.
    /// </summary>
    /// <param name="allowedTypes">An array of message types that should be allowed to be bridged.
    /// If empty, no message types will be bridged (blocks all messages).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="allowedTypes"/> is null.</exception>
    public IncludeMessagesBridgeFilter(Type[] allowedTypes)
    {
        ArgumentNullException.ThrowIfNull(allowedTypes);
        _allowedTypes = new(allowedTypes);
    }

    /// <inheritdoc />
    public Boolean ShouldBridge(Type messageType) => _allowedTypes.Contains(messageType);
}
