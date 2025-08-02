// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

public class MessageBusConcurrencyTests
{
    [Test]
    public async Task PublishAsync_FromMultipleThreads_ShouldInvokeAllSubscribers()
    {
        var messageBus = CreateMessageBus();
        const Int32 subscriberCount = 5;
        const Int32 publishCount = 100;
        var counters = new Int32[subscriberCount];

        for (var i = 0; i < subscriberCount; i++)
        {
            var index = i;
            messageBus.Subscribe<String>(_ => Interlocked.Increment(ref counters[index]));
        }

        var tasks = Enumerable.Range(0, publishCount)
                              .Select(_ => Task.Run(() => messageBus.PublishAsync("Test")))
                              .ToArray();

        await Task.WhenAll(tasks);

        foreach (var counter in counters)
        {
            counter.Should().Be(publishCount);
        }
    }

    [Test]
    public async Task PublishAsync_FromMultipleThreads_WithAsyncHandlers_ShouldInvokeAllSubscribers()
    {
        var messageBus = CreateMessageBus();
        const Int32 subscriberCount = 3;
        const Int32 publishCount = 50;
        var counters = new Int32[subscriberCount];

        for (var i = 0; i < subscriberCount; i++)
        {
            var index = i;
            messageBus.Subscribe<String>(async _ =>
            {
                await Task.Delay(1);
                Interlocked.Increment(ref counters[index]);
            });
        }

        var tasks = Enumerable.Range(0, publishCount)
                              .Select(_ => Task.Run(() => messageBus.PublishAsync("Test")))
                              .ToArray();

        await Task.WhenAll(tasks);

        foreach (var counter in counters)
        {
            counter.Should().Be(publishCount);
        }
    }

    private static MessageBus CreateMessageBus(BlazorMessageBusOptions? options = null)
    {
        if (options is not null)
            return new(new OptionsWrapper<BlazorMessageBusOptions>(options));
        return new();
    }
}
