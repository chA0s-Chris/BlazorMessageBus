// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

public interface IBlazorMessageExchange : IBlazorMessagePublisher, IDisposable
{
    void Subscribe<T>(SubscriptionHandlerAsync<T> handler);

    void Subscribe<T>(SubscriptionHandler<T> handler);
}
