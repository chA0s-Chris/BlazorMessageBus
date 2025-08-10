// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

/// <summary>
/// Provides options to configure the behavior of the BlazorMessageBus.
/// </summary>
public record BlazorMessageBusOptions
{
    /// <summary>
    /// If <see langword="true"/>, publishing will stop on the first handler exception. If <see langword="false"/>, all handlers are invoked.
    /// Default: <see langword="false"/>.
    /// </summary>
    public Boolean StopOnFirstError { get; set; }

    /// <summary>
    /// An optional default exception handler for publish errors.
    /// </summary>
    public Func<Exception, Task>? OnPublishException { get; set; }

    /// <summary>
    /// An optional exception handler for errors occurring during message bridging.
    /// This is invoked when the bridge filter predicate or the outbound transport handler throws.
    /// Exceptions thrown by this callback are swallowed.
    /// </summary>
    public Func<Exception, Task>? OnBridgeException { get; set; }
}
