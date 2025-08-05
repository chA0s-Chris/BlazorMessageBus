// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Filtering;

/// <summary>
/// A message bridge filter that excludes specific message types from being bridged (exclusive filtering).
/// All message types will be bridged except those that are explicitly excluded in the excluded types collection.
/// This is useful for filtering out sensitive or internal message types while allowing everything else through.
/// </summary>
public sealed class ExcludeMessagesBridgeFilter : IBlazorMessageBridgeFilter
{
    private readonly HashSet<Type> _excludedTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcludeMessagesBridgeFilter"/> class.
    /// </summary>
    /// <param name="excludedTypes">An array of message types that should be excluded from bridging.
    /// If empty, all message types will be bridged (excludes nothing).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="excludedTypes"/> is null.</exception>
    public ExcludeMessagesBridgeFilter(Type[] excludedTypes)
    {
        ArgumentNullException.ThrowIfNull(excludedTypes);
        _excludedTypes = new(excludedTypes);
    }

    /// <inheritdoc />
    public Boolean ShouldBridge(Type messageType) => !_excludedTypes.Contains(messageType);
}
