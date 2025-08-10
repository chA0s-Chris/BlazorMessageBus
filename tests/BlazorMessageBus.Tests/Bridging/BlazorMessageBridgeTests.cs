// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Bridging;

using Chaos.BlazorMessageBus.Filtering;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

// ReSharper disable AccessToModifiedClosure
// ReSharper disable AccessToDisposedClosure
public class BlazorMessageBridgeTests
{
    [Test]
    [Explicit("Demonstrates ping-pong when configuring bi-directional bridges. Keep unidirectional bridging or implement dedup/cycle prevention.")]
    public async Task BiDirectional_Cycle_A_and_B_WillPingPong_UntilDisposed()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();

        var receivedA = 0;
        var receivedB = 0;
        busA.Subscribe<String>(_ => receivedA++);
        busB.Subscribe<String>(_ => receivedB++);

        // A -> B
        var remoteB = new BridgeRef();
        var bridgeAtoB = busA.CreateMessageBridge((type, payload, ct) => remoteB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        // B -> A
        var remoteA = new BridgeRef();
        var bridgeBtoA = busB.CreateMessageBridge((type, payload, ct) => remoteA.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteA.Bridge = busA.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        await busA.PublishAsync("cycle");

        await Task.Delay(50);

        bridgeAtoB.Dispose();
        bridgeBtoA.Dispose();

        (receivedA + receivedB).Should().BeGreaterThan(2);
    }

    [Test]
    public void Bridge_ConfigureFilter_Null_ShouldThrow()
    {
        var bus = CreateMessageBus();
        using var bridge = bus.CreateMessageBridge((_, _, _) => Task.CompletedTask);
        FluentActions.Invoking(() => bridge.ConfigureFilter(null!)).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task Bridge_Dispose_ShouldDeactivateAndStopForwarding()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();

        var receivedB = 0;
        busB.Subscribe<String>(_ => receivedB++);

        var remoteB = new BridgeRef();
        var bridgeA = busA.CreateMessageBridge((type, payload, ct) => remoteB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        bridgeA.Dispose();

        bridgeA.IsActive.Should().BeFalse();
        await FluentActions.Awaiting(() => bridgeA.InjectMessageAsync("X")).Should().ThrowAsync<ObjectDisposedException>();

        await busA.PublishAsync("Ignored");
        await Task.Delay(50);

        receivedB.Should().Be(0);
    }

    [Test]
    public async Task Bridge_Filter_Exclude_ShouldNotForwardExcludedTypes()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();

        var receivedStrings = 0;
        var receivedInts = 0;
        busB.Subscribe<String>(_ => receivedStrings++);
        busB.Subscribe<Int32>(_ => receivedInts++);

        var remoteB = new BridgeRef();
        var bridgeA = busA.CreateMessageBridge((type, payload, ct) => remoteB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        bridgeA.ConfigureFilter(BlazorMessageBridgeFilters.Exclude(typeof(Int32)));

        await busA.PublishAsync("Forwarded");
        await busA.PublishAsync(123);

        (await WaitUntilAsync(() => receivedStrings == 1, TimeSpan.FromSeconds(1))).Should().BeTrue();
        receivedInts.Should().Be(0);
    }

    [Test]
    public async Task Bridge_Filter_Include_ShouldForwardOnlyIncludedTypes()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();

        var receivedStrings = 0;
        var receivedInts = 0;
        busB.Subscribe<String>(_ => receivedStrings++);
        busB.Subscribe<Int32>(_ => receivedInts++);

        var remoteB = new BridgeRef();
        var bridgeA = busA.CreateMessageBridge((type, payload, ct) => remoteB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        bridgeA.ConfigureFilter(BlazorMessageBridgeFilters.Include(typeof(String)));

        await busA.PublishAsync("Allowed");
        await busA.PublishAsync(42);

        (await WaitUntilAsync(() => receivedStrings == 1, TimeSpan.FromSeconds(1))).Should().BeTrue();
        receivedInts.Should().Be(0);
    }

    [Test]
    public async Task Bridge_Filter_Reconfigure_Runtime_ShouldAffectSubsequentForwards()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();

        var receivedStrings = 0;
        var receivedInts = 0;
        busB.Subscribe<String>(_ => receivedStrings++);
        busB.Subscribe<Int32>(_ => receivedInts++);

        var remoteB = new BridgeRef();
        var bridgeA = busA.CreateMessageBridge((type, payload, ct) => remoteB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        bridgeA.ConfigureFilter(BlazorMessageBridgeFilters.Include(typeof(String)));

        await busA.PublishAsync("first");
        await busA.PublishAsync(1);

        (await WaitUntilAsync(() => receivedStrings == 1, TimeSpan.FromSeconds(1))).Should().BeTrue();
        receivedInts.Should().Be(0);

        bridgeA.ConfigureFilter(BlazorMessageBridgeFilters.Exclude(typeof(String)));

        await busA.PublishAsync("second");
        await busA.PublishAsync(2);

        await Task.Delay(50);
        receivedStrings.Should().Be(1);
        (await WaitUntilAsync(() => receivedInts == 1, TimeSpan.FromSeconds(1))).Should().BeTrue();
    }

    [Test]
    public async Task Bridge_Filter_Where_ShouldForwardOnlyPredicateMatches()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();

        var receivedStrings = 0;
        var receivedInts = 0;
        busB.Subscribe<String>(_ => receivedStrings++);
        busB.Subscribe<Int32>(_ => receivedInts++);

        var remoteB = new BridgeRef();
        var bridgeA = busA.CreateMessageBridge((type, payload, ct) => remoteB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);
        bridgeA.ConfigureFilter(BlazorMessageBridgeFilters.Where(t => t == typeof(String)));

        await busA.PublishAsync("Allowed");
        await busA.PublishAsync(123);

        (await WaitUntilAsync(() => receivedStrings == 1, TimeSpan.FromSeconds(1))).Should().BeTrue();
        receivedInts.Should().Be(0);
    }

    [Test]
    public async Task Bridge_ForwardingException_ShouldNotAffectLocalDelivery()
    {
        var bus = CreateMessageBus();
        var localCount = 0;
        bus.Subscribe<String>(_ => localCount++);

        using var bridge = bus.CreateMessageBridge((_, _, _) => throw new InvalidOperationException("Bridge transport failed"));

        await FluentActions.Awaiting(() => bus.PublishAsync("Hello")).Should().NotThrowAsync();
        localCount.Should().Be(1);
    }

    [Test]
    public async Task Bridge_Inject_WithCancelledToken_ShouldStillDeliverLocally()
    {
        var bus = CreateMessageBus();
        var received = 0;
        bus.Subscribe<String>(_ => received++);

        using var bridge = bus.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await FluentActions.Awaiting(() => bridge.InjectMessageAsync("Hi", cts.Token)).Should().NotThrowAsync();

        (await WaitUntilAsync(() => received == 1, TimeSpan.FromSeconds(1))).Should().BeTrue();
    }

    [Test]
    public async Task Bridge_Inject_WithNullArguments_ShouldThrow()
    {
        var bus = CreateMessageBus();
        using var bridge = bus.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        await FluentActions.Awaiting(() => bridge.InjectMessageAsync(null!, new())).Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => bridge.InjectMessageAsync(typeof(String), null!)).Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task Concurrency_PublishAndDisposeUnderLoad_ShouldBeStableAndStopForwarding()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();

        var countA = 0;
        var countB = 0;
        busA.Subscribe<String>(_ => Interlocked.Increment(ref countA));
        busB.Subscribe<String>(_ => Interlocked.Increment(ref countB));

        var remoteB = new BridgeRef();
        var bridgeA = busA.CreateMessageBridge((type, payload, ct) => remoteB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        // First batch: ensure forwarding works under load
        var batch1 = 100;
        var publish1 = Task.Run(async () =>
        {
            for (var i = 0; i < batch1; i++)
            {
                await busA.PublishAsync("load-1");
            }
        });

        await publish1;

        (await WaitUntilAsync(() => countB >= batch1, TimeSpan.FromSeconds(1))).Should().BeTrue();

        var countBBeforeDispose = countB;

        // Concurrently inject while disposing to stress the bridge; ignore expected ObjectDisposedException
        var injectTask = Task.Run(async () =>
        {
            try
            {
                for (var i = 0; i < 50; i++)
                {
                    await bridgeA.InjectMessageAsync("inbound");
                }
            }
            catch (ObjectDisposedException)
            {
                // expected if disposal races
            }
        });

        bridgeA.Dispose();

        // Second batch after disposal should NOT be forwarded
        var batch2 = 50;
        for (var i = 0; i < batch2; i++)
        {
            await busA.PublishAsync("load-2");
        }

        await injectTask;

        await Task.Delay(100);

        countB.Should().Be(countBBeforeDispose, "forwarding should stop after disposal");
        countA.Should().BeGreaterThan(0);
    }

    [Test]
    public void ConfigureFilter_AfterDispose_ShouldThrow()
    {
        var bus = CreateMessageBus();
        var bridge = bus.CreateMessageBridge((_, _, _) => Task.CompletedTask);
        bridge.Dispose();

        FluentActions.Invoking(() => bridge.ConfigureFilter(BlazorMessageBridgeFilters.All()))
                     .Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public async Task CreateMessageBridge_ShouldForwardToRemoteBus_AndNotLoopBack()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();

        var receivedA = 0;
        var receivedB = 0;
        busA.Subscribe<String>(_ => receivedA++);
        busB.Subscribe<String>(_ => receivedB++);

        var remoteB = new BridgeRef();
        busA.CreateMessageBridge((type, payload, ct) => remoteB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        await busA.PublishAsync("Hello");

        (await WaitUntilAsync(() => receivedB == 1, TimeSpan.FromSeconds(1))).Should().BeTrue("message should be forwarded to remote bus");

        receivedA.Should().Be(1, "local delivery should happen once");
        receivedB.Should().Be(1, "remote delivery should happen once");
    }

    [Test]
    public async Task Disposal_Timing_LargeBatch_ShouldEventuallyStopForwarding()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();

        var countB = 0;
        busB.Subscribe<String>(_ => Interlocked.Increment(ref countB));

        var remoteB = new BridgeRef();
        var bridgeA = busA.CreateMessageBridge(async (type, payload, ct) =>
        {
            // Simulate slow transport to create a backlog
            await Task.Delay(1, ct);
            await remoteB.Bridge!.InjectMessageAsync(type, payload, ct);
        });
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        var total = 300;
        var publisher = Task.Run(async () =>
        {
            for (var i = 0; i < total; i++)
            {
                await busA.PublishAsync("stream");
            }
        });

        // Wait until some messages have been forwarded, then dispose the bridge mid-stream
        (await WaitUntilAsync(() => Volatile.Read(ref countB) >= 50, TimeSpan.FromSeconds(2))).Should().BeTrue();
        bridgeA.Dispose();

        await publisher;

        // Wait for remote count to stabilize (backlog drains) before sending more
        var timeoutAt = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        var stableSince = DateTime.UtcNow;
        var last = Volatile.Read(ref countB);
        while (DateTime.UtcNow < timeoutAt)
        {
            await Task.Delay(50);
            var current = Volatile.Read(ref countB);
            if (current == last)
            {
                if (DateTime.UtcNow - stableSince >= TimeSpan.FromMilliseconds(200))
                {
                    break; // stable enough
                }
            }
            else
            {
                last = current;
                stableSince = DateTime.UtcNow;
            }
        }

        var baseline = Volatile.Read(ref countB);

        // Publish additional messages after disposal; they must NOT be forwarded
        for (var i = 0; i < 50; i++)
        {
            await busA.PublishAsync("extra-after-dispose");
        }

        await Task.Delay(200);
        Volatile.Read(ref countB).Should().Be(baseline, "no messages published after disposal should be forwarded");

        _ = bridgeA;
    }

    [Test]
    public async Task FilterPredicate_Throwing_ShouldNotAffectLocalDelivery()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();

        var receivedLocal = 0;
        var receivedRemote = 0;
        busA.Subscribe<String>(_ => receivedLocal++);
        busB.Subscribe<String>(_ => receivedRemote++);

        var remoteB = new BridgeRef();
        var bridgeA = busA.CreateMessageBridge((type, payload, ct) => remoteB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        bridgeA.ConfigureFilter(BlazorMessageBridgeFilters.Where(_ => throw new InvalidOperationException("boom")));

        await FluentActions.Awaiting(() => busA.PublishAsync("X")).Should().NotThrowAsync();

        receivedLocal.Should().Be(1);
        await Task.Delay(50);
        receivedRemote.Should().Be(0);

        _ = bridgeA;
    }

    [Test]
    public async Task Forwarding_FireAndForget_PublishReturnsBeforeOutboundCompletes()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();

        var receivedLocal = 0;
        var receivedRemote = 0;
        busA.Subscribe<String>(_ => receivedLocal++);
        busB.Subscribe<String>(_ => receivedRemote++);

        var tcs = new TaskCompletionSource<Boolean>(TaskCreationOptions.RunContinuationsAsynchronously);

        var remoteB = new BridgeRef();
        var bridgeA = busA.CreateMessageBridge(async (type, payload, ct) =>
        {
            await tcs.Task;
            await remoteB.Bridge!.InjectMessageAsync(type, payload, ct);
        });
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        var publishTask = busA.PublishAsync("fire-and-forget");

        // Publish must complete even though outbound forwarding is blocked
        await publishTask;

        receivedLocal.Should().Be(1);
        receivedRemote.Should().Be(0);

        tcs.SetResult(true);
        (await WaitUntilAsync(() => receivedRemote == 1, TimeSpan.FromSeconds(1))).Should().BeTrue();

        _ = bridgeA;
    }

    [Test]
    public async Task MultiBridge_Chain_A_to_B_to_C_ShouldForwardDownstreamOnce()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();
        var busC = CreateMessageBus();

        var receivedA = 0;
        var receivedB = 0;
        var receivedC = 0;
        busA.Subscribe<String>(_ => receivedA++);
        busB.Subscribe<String>(_ => receivedB++);
        busC.Subscribe<String>(_ => receivedC++);

        // Set up unidirectional A->B and B->C bridges using direct bus injection to avoid closure warnings
        var remoteB = new BridgeRef();
        var bridgeA = busA.CreateMessageBridge((type, payload, ct) => remoteB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);
        var remoteC = new BridgeRef();
        var bridgeB = busB.CreateMessageBridge((type, payload, ct) => remoteC.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteC.Bridge = busC.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        await busA.PublishAsync("Hello-Chain");

        (await WaitUntilAsync(() => receivedC == 1, TimeSpan.FromSeconds(1))).Should().BeTrue("message should reach bus C");
        receivedB.Should().Be(1, "message should reach bus B once");
        receivedA.Should().Be(1, "local delivery should be once");

        // ensure no additional forwarding happens
        await Task.Delay(50);
        receivedB.Should().Be(1);
        receivedC.Should().Be(1);

        // suppress warnings about unused variables
        _ = bridgeA;
        _ = bridgeB;
    }

    [Test]
    public async Task MultiPath_Topology_ShouldDeliverDuplicates_OnC()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();
        var busC = CreateMessageBus();

        var receivedA = 0;
        var receivedB = 0;
        var receivedC = 0;
        busA.Subscribe<String>(_ => receivedA++);
        busB.Subscribe<String>(_ => receivedB++);
        busC.Subscribe<String>(_ => receivedC++);

        var remoteB = new BridgeRef();
        busA.CreateMessageBridge((type, payload, ct) => remoteB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        var remoteCFromB = new BridgeRef();
        busB.CreateMessageBridge((type, payload, ct) => remoteCFromB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteCFromB.Bridge = busC.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        var remoteCFromA = new BridgeRef();
        busA.CreateMessageBridge((type, payload, ct) => remoteCFromA.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteCFromA.Bridge = busC.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        await busA.PublishAsync("dup-test");

        (await WaitUntilAsync(() => receivedC >= 2, TimeSpan.FromSeconds(1))).Should().BeTrue("C should receive via A->C and A->B->C");
        receivedB.Should().Be(1);
        receivedA.Should().Be(1);
    }

    [Test]
    public async Task OnBridgeException_IsInvoked_WhenFilterPredicateThrows()
    {
        var invoked = 0;
        var options = new BlazorMessageBusOptions
        {
            OnBridgeException = _ =>
            {
                Interlocked.Increment(ref invoked);
                return Task.CompletedTask;
            }
        };

        var busA = CreateMessageBus(options);
        var busB = CreateMessageBus();

        var receivedLocal = 0;
        var receivedRemote = 0;
        busA.Subscribe<String>(_ => receivedLocal++);
        busB.Subscribe<String>(_ => receivedRemote++);

        var remoteB = new BridgeRef();
        var bridgeA = busA.CreateMessageBridge((type, payload, ct) => remoteB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        bridgeA.ConfigureFilter(BlazorMessageBridgeFilters.Where(_ => throw new InvalidOperationException("boom")));

        await FluentActions.Awaiting(() => busA.PublishAsync("X")).Should().NotThrowAsync();

        receivedLocal.Should().Be(1);
        receivedRemote.Should().Be(0);
        (await WaitUntilAsync(() => Volatile.Read(ref invoked) == 1, TimeSpan.FromSeconds(1))).Should().BeTrue();

        _ = bridgeA;
    }

    [Test]
    public async Task OnBridgeException_IsInvoked_WhenOutboundHandlerThrows()
    {
        var invoked = 0;
        var options = new BlazorMessageBusOptions
        {
            OnBridgeException = _ =>
            {
                Interlocked.Increment(ref invoked);
                return Task.CompletedTask;
            }
        };

        var bus = CreateMessageBus(options);
        var localCount = 0;
        bus.Subscribe<String>(_ => localCount++);

        using var bridge = bus.CreateMessageBridge((_, _, _) => throw new InvalidOperationException("Bridge transport failed"));

        await FluentActions.Awaiting(() => bus.PublishAsync("Hello")).Should().NotThrowAsync();
        localCount.Should().Be(1);
        (await WaitUntilAsync(() => Volatile.Read(ref invoked) == 1, TimeSpan.FromSeconds(1))).Should().BeTrue();
    }

    [Test]
    public async Task OnBridgeException_ThrowingCallback_IsSwallowed()
    {
        var invoked = 0;
        var options = new BlazorMessageBusOptions
        {
            OnBridgeException = _ =>
            {
                Interlocked.Increment(ref invoked);
                throw new("observer failed");
            }
        };

        var bus = CreateMessageBus(options);
        var localCount = 0;
        bus.Subscribe<String>(_ => localCount++);

        using var bridge = bus.CreateMessageBridge((_, _, _) => throw new InvalidOperationException("Bridge transport failed"));

        await FluentActions.Awaiting(() => bus.PublishAsync("Hello")).Should().NotThrowAsync();
        localCount.Should().Be(1);
        (await WaitUntilAsync(() => Volatile.Read(ref invoked) == 1, TimeSpan.FromSeconds(1))).Should().BeTrue();
    }

    [Test]
    public async Task Publish_WithCancelledToken_ShouldDeliverLocally_ButNotForward_WhenOutboundHonorsCancellation()
    {
        var busA = CreateMessageBus();
        var busB = CreateMessageBus();

        var receivedLocal = 0;
        var receivedRemote = 0;
        busA.Subscribe<String>(_ => receivedLocal++);
        busB.Subscribe<String>(_ => receivedRemote++);

        var remoteB = new BridgeRef();
        var bridgeA = busA.CreateMessageBridge((type, payload, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return remoteB.Bridge!.InjectMessageAsync(type, payload, ct);
        });
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await FluentActions.Awaiting(() => busA.PublishAsync("Hello", cts.Token)).Should().NotThrowAsync();

        receivedLocal.Should().Be(1);

        await Task.Delay(50);
        receivedRemote.Should().Be(0);

        _ = bridgeA;
    }

    [Test]
    public async Task StopOnFirstError_True_ShouldNotForward_WhenLocalHandlerThrows()
    {
        var options = new BlazorMessageBusOptions
        {
            StopOnFirstError = true
        };
        var busA = CreateMessageBus(options);
        var busB = CreateMessageBus();

        var receivedRemote = 0;
        busB.Subscribe<String>(_ => receivedRemote++);

        var remoteB = new BridgeRef();
        var bridgeA = busA.CreateMessageBridge((type, payload, ct) => remoteB.Bridge!.InjectMessageAsync(type, payload, ct));
        remoteB.Bridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

        busA.Subscribe<String>(_ => throw new InvalidOperationException("local failure"));

        await FluentActions.Awaiting(() => busA.PublishAsync("test")).Should().ThrowAsync<InvalidOperationException>();

        await Task.Delay(50);
        receivedRemote.Should().Be(0);

        _ = bridgeA;
    }

    private static IBlazorMessageBus CreateMessageBus(BlazorMessageBusOptions? options = null)
    {
        var factory = new BlazorMessageBridgeInternalFactory();
        return new MessageBus(factory, options is null ? null : Options.Create(options));
    }

    private static async Task<Boolean> WaitUntilAsync(Func<Boolean> predicate, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            if (predicate())
            {
                return true;
            }

            await Task.Delay(10);
        }

        return predicate();
    }

    private sealed class BridgeRef
    {
        public IBlazorMessageBridge? Bridge { get; set; }
    }
}
// ReSharper restore AccessToDisposedClosure
// ReSharper restore AccessToModifiedClosure
