// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Cha0s.BlazorMessageBus;

using Chaos.BlazorMessageBus;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

public class BlazorMessageBusServiceCollectionExtensionsTests
{
    private IServiceCollection _services;

    [SetUp]
    public void Setup() => _services = new ServiceCollection();

    [Test]
    public void AddBlazorMessageBus_ShouldRegisterServices()
    {
        _services.AddBlazorMessageBus();

        var serviceProvider = _services.BuildServiceProvider();

        serviceProvider.GetService<IBlazorMessageBus>().Should().NotBeNull();
        serviceProvider.GetService<IBlazorMessageExchange>().Should().NotBeNull();
    }

    [Test]
    public void AddBlazorMessageBus_WithConfigureAction_ShouldInvokeConfigureActionAndRegisterOptions()
    {
        _services.AddBlazorMessageBus(options =>
        {
            options.StopOnFirstError = true;
            options.OnPublishException = _ => Task.CompletedTask;
        });

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<BlazorMessageBusOptions>>();
        options.Should().NotBeNull();
        options.Value.StopOnFirstError.Should().BeTrue();
        options.Value.OnPublishException.Should().NotBeNull();
    }
}
