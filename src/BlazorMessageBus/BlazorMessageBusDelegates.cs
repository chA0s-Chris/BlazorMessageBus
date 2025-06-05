// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

/// <summary>
/// An asynchronous delegate for handling messages of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Message type.</typeparam>
public delegate Task SubscriptionHandlerAsync<in T>(T payload);

/// <summary>
/// A synchronous delegate for handling messages of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Message type.</typeparam>
public delegate void SubscriptionHandler<in T>(T payload);
