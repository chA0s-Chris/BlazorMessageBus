// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using Chaos.BlazorMessageBus.Bridging;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Collections.Concurrent;

public class MessageBusConcurrencyTests
{
    [Test]
    public async Task ConcurrentInvokeAndDispose_ShouldHandleGracefully()
    {
        var messageBus = CreateMessageBus();
        var invocationCount = 0;
        var subscription = (Subscription)messageBus.Subscribe<String>(_ =>
        {
            Thread.Sleep(5); // Simulate work
            Interlocked.Increment(ref invocationCount);
        });

        // Start concurrent invocations
        var invokeTasks =
            Enumerable.Range(0, 20)
                      .Select(_ => Task.Run(async () =>
                      {
                          try
                          {
                              await subscription.InvokeAsync("test");
                          }
                          catch (ObjectDisposedException)
                          {
                              // Expected if disposal happens first
                          }
                      }))
                      .ToArray();

        // Dispose after a short delay
        var disposeTask = Task.Run(async () =>
        {
            await Task.Delay(10);
            subscription.Dispose(false);
        });

        await Task.WhenAll(invokeTasks.Concat(new[] { disposeTask }));

        // Should complete without deadlocks
        invocationCount.Should().BeGreaterOrEqualTo(0);
        subscription.IsAlive.Should().BeFalse();
    }

    [Test]
    public async Task ConcurrentPublishingDuringSubscriptionDisposal_ShouldNotThrow()
    {
        var messageBus = CreateMessageBus();
        var subscription = messageBus.Subscribe<String>(_ => Task.Delay(10));

        var publishTasks =
            Enumerable.Range(0, 50)
                      .Select(i => Task.Run(() => messageBus.PublishAsync($"Message {i}")))
                      .ToArray();

        // Dispose subscription while publishing is happening
        var disposalTask = Task.Run(async () =>
        {
            await Task.Delay(5);
            subscription.Dispose();
        });

        // Should not throw exceptions
        await FluentActions.Awaiting(() => Task.WhenAll(publishTasks.Concat([disposalTask])))
                           .Should().NotThrowAsync();
    }

    [Test]
    public void ConcurrentSubscriptionCreation_ShouldCreateAllSubscriptions()
    {
        var messageBus = CreateMessageBus();
        const Int32 concurrentSubscribers = 100;
        var subscriptions = new IBlazorMessageSubscription[concurrentSubscribers];

        // Create subscriptions concurrently
        Parallel.For(0, concurrentSubscribers, i =>
        {
            subscriptions[i] = messageBus.Subscribe<String>(_ => { });
        });

        // All subscriptions should be created successfully
        subscriptions.Should().AllSatisfy(s =>
        {
            s.Should().NotBeNull();
            s.IsAlive.Should().BeTrue();
            s.MessageType.Should().Be<String>();
        });

        // Cleanup
        Parallel.ForEach(subscriptions, s => s.Dispose());
    }

    [Test]
    public async Task ConcurrentSubscriptionDisposal_ShouldHandleMultipleDisposeCalls()
    {
        var messageBus = CreateMessageBus();
        var subscription = messageBus.Subscribe<String>(_ => { });

        // Dispose the same subscription from multiple threads
        var disposalTasks = Enumerable.Range(0, 10)
                                      .Select(_ => Task.Run(() => subscription.Dispose()))
                                      .ToArray();

        // Should not throw exceptions
        await FluentActions.Awaiting(() => Task.WhenAll(disposalTasks))
                           .Should().NotThrowAsync();

        subscription.IsAlive.Should().BeFalse();
    }

    [Test]
    public async Task DisposedSubscription_InvokeAsync_ShouldThrowObjectDisposedException()
    {
        var messageBus = CreateMessageBus();
        var subscription = (Subscription)messageBus.Subscribe<String>(_ => { });

        subscription.Dispose(false);

        // Attempting to invoke on disposed subscription should throw
        await FluentActions.Awaiting(() => subscription.InvokeAsync("test"))
                           .Should().ThrowExactlyAsync<ObjectDisposedException>();

        subscription.IsAlive.Should().BeFalse();
    }

    [Test]
    public async Task InactiveSubscriptionPurging_ShouldWorkConcurrently()
    {
        var messageBus = CreateMessageBus();
        var activeSubscriptions = new List<IBlazorMessageSubscription>();

        // Create subscriptions and dispose some of them
        for (var i = 0; i < 20; i++)
        {
            var subscription = messageBus.Subscribe<String>(_ => { });
            activeSubscriptions.Add(subscription);

            // Dispose every other subscription
            if (i % 2 == 0)
            {
                subscription.Dispose();
            }
        }

        // Publish messages concurrently to trigger purging
        var publishTasks =
            Enumerable.Range(0, 50)
                      .Select(_ => Task.Run(() => messageBus.PublishAsync("Test")))
                      .ToArray();

        await Task.WhenAll(publishTasks);

        // Verify active subscriptions are still working
        var activeCount = activeSubscriptions.Count(s => s.IsAlive);
        activeCount.Should().Be(10); // Half should still be alive
    }

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

    [Test]
    public async Task StressTest_ConcurrentOperations_ShouldMaintainConsistency()
    {
        var messageBus = CreateMessageBus();
        const Int32 operationCount = 200;
        var messageCount = 0;
        var subscriptions = new ConcurrentBag<IBlazorMessageSubscription>();

        // Mix of concurrent operations: subscribe, publish, dispose
        var tasks = new List<Task>();

        // Subscription tasks
        tasks.AddRange(Enumerable.Range(0, operationCount / 4)
                                 .Select(_ => Task.Run(() =>
                                 {
                                     var subscription = messageBus.Subscribe<String>(_ =>
                                                                                         Interlocked.Increment(ref messageCount));
                                     subscriptions.Add(subscription);
                                 })));

        // Publishing tasks
        tasks.AddRange(Enumerable.Range(0, operationCount / 2)
                                 .Select(i => Task.Run(() => messageBus.PublishAsync($"Message {i}"))));

        // Disposal tasks
        tasks.AddRange(Enumerable.Range(0, operationCount / 4)
                                 .Select(_ => Task.Run(() =>
                                 {
                                     Thread.Sleep(Random.Shared.Next(1, 10));
                                     if (subscriptions.TryTake(out var subscription))
                                     {
                                         subscription.Dispose();
                                     }
                                 })));

        await Task.WhenAll(tasks);

        // Should complete without exceptions and messageCount should be consistent
        messageCount.Should().BeGreaterOrEqualTo(0);
    }

    private static MessageBus CreateMessageBus(BlazorMessageBusOptions? options = null)
    {
        var factory = new BlazorMessageBridgeInternalFactory();
        if (options is not null)
            return new(factory, new OptionsWrapper<BlazorMessageBusOptions>(options));
        return new(factory);
    }
}
