// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Filtering;

/// <summary>
/// A message bridge filter that uses a custom predicate function to determine which message types should be bridged.
/// This provides maximum flexibility for complex filtering logic that cannot be achieved with simple include/exclude patterns.
/// </summary>
/// <example>
/// <code>
/// // Only bridge messages from specific namespaces
/// var filter = new PredicateBridgeFilter(type => type.Namespace?.StartsWith("MyApp.PublicEvents") == true);
/// 
/// // Bridge messages based on attributes
/// var filter = new PredicateBridgeFilter(type => type.GetCustomAttribute&lt;BridgeableAttribute&gt;() != null);
/// </code>
/// </example>
public sealed class PredicateBridgeFilter : IBlazorMessageBridgeFilter
{
    private readonly Func<Type, Boolean> _predicate;

    /// <summary>
    /// Initializes a new instance of the <see cref="PredicateBridgeFilter"/> class.
    /// </summary>
    /// <param name="predicate">A function that determines whether a message type should be bridged. 
    /// The function receives a message type and should return true if the type should be bridged, false otherwise.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
    public PredicateBridgeFilter(Func<Type, Boolean> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    /// <inheritdoc />
    public Boolean ShouldBridge(Type messageType) => _predicate(messageType);
}
