// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

public interface IBlazorMessagePublisher
{
    Task PublishAsync<T>(T payload) where T : notnull;
}
