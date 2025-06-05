// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

/// <summary>
/// Represents a message and its associated subscriptions.
/// </summary>
internal record Message(Type Type)
{
    public Subscriptions Subscriptions { get; } = new();
}
