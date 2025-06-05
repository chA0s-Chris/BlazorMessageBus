// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using Microsoft.Extensions.DependencyInjection;

public static class BlazorMessageBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers BlazorMessageBus services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBlazorMessageBus(this IServiceCollection services)
    {
        services.AddSingleton<IBlazorMessageBus, MessageBus>();
        services.AddScoped<IBlazorMessageExchange, MessageExchange>();
        return services;
    }
}
