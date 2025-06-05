// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

public record BlazorMessageBusOptions
{
    /// <summary>
    /// If true, publishing will stop on the first handler exception. If false, all handlers are invoked.
    /// Default: false.
    /// </summary>
    public Boolean StopOnFirstError { get; init; }

    /// <summary>
    /// An optional default exception handler for publish errors.
    /// </summary>
    public Func<Exception, Task>? OnPublishException { get; init; }
}
