// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Filtering;

/// <summary>
/// Provides factory methods for creating common message bridge filters.
/// </summary>
public static class BlazorMessageBridgeFilters
{
    /// <summary>
    /// Creates a filter that bridges all message types (no filtering).
    /// </summary>
    /// <returns>A filter that allows all messages to be bridged.</returns>
    public static IBlazorMessageBridgeFilter All()
        => new AllowAllMessagesBridgeFilter();

    /// <summary>
    /// Creates a filter that bridges all message types except the specified ones.
    /// </summary>
    /// <param name="excludedTypes">The message types to exclude from bridging.</param>
    /// <returns>A filter that excludes the specified types from being bridged.</returns>
    public static IBlazorMessageBridgeFilter Exclude(params Type[] excludedTypes)
        => new ExcludeMessagesBridgeFilter(excludedTypes);

    /// <summary>
    /// Creates a filter that only bridges the specified message types.
    /// </summary>
    /// <param name="allowedTypes">The message types to allow for bridging.</param>
    /// <returns>A filter that only allows the specified types to be bridged.</returns>
    public static IBlazorMessageBridgeFilter Include(params Type[] allowedTypes)
        => new IncludeMessagesBridgeFilter(allowedTypes);

    /// <summary>
    /// Creates a filter that only bridges message types matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to determine which message types should be bridged.</param>
    /// <returns>A filter based on the provided predicate.</returns>
    public static IBlazorMessageBridgeFilter Where(Func<Type, Boolean> predicate)
        => new PredicateBridgeFilter(predicate);
}
