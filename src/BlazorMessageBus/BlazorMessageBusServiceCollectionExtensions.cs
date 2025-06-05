// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using Microsoft.Extensions.DependencyInjection;
using System;

public static class BlazorMessageBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers BlazorMessageBus services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">An optional action to configure BlazorMessageBusOptions.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBlazorMessageBus(this IServiceCollection services, Action<BlazorMessageBusOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }
        services.AddSingleton<IBlazorMessageBus, MessageBus>();
        services.AddScoped<IBlazorMessageExchange, MessageExchange>();
        return services;
    }
}
