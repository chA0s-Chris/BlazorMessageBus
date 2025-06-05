// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

public delegate Task SubscriptionHandlerAsync<in T>(T payload);

public delegate void SubscriptionHandler<in T>(T payload);
