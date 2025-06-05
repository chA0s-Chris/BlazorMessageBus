// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Cha0s.BlazorMessageBus;

using Chaos.BlazorMessageBus;
using FluentAssertions;
using NUnit.Framework;

public class SubscriptionTests
{
    [Test]
    public void Subscription_ShouldStartAliveWithMessageTypeSet()
    {
        var messageCallback = new MessageCallback<String>(_ => { });
        var subscription = new Subscription(messageCallback, () => { });

        subscription.IsAlive.Should().BeTrue();
        subscription.MessageType.Should().Be<String>();
    }

    [Test]
    public void Dispose_ShouldMarkSubscriptionAsDeadAndCallOnDisposeCallback()
    {
        var onDisposeCallCount = 0;

        var messageCallback = new MessageCallback<String>(_ => { });
        var subscription = new Subscription(messageCallback, () => onDisposeCallCount++);

        subscription.Dispose();

        subscription.IsAlive.Should().BeFalse();
        onDisposeCallCount.Should().Be(1);
    }

    [Test]
    public void Dispose_WithFalse_ShouldMarkSubscriptionAsDeadWithoutCallingOnDisposeCallback()
    {
        var onDisposeCallCount = 0;

        var messageCallback = new MessageCallback<String>(_ => { });
        var subscription = new Subscription(messageCallback, () => onDisposeCallCount++);

        subscription.Dispose(false);

        subscription.IsAlive.Should().BeFalse();
        onDisposeCallCount.Should().Be(0);
    }

    [Test]
    public void Dispose_CalledTwice_ShouldDoNothing()
    {
        var onDisposeCallCount = 0;

        var messageCallback = new MessageCallback<String>(_ => { });
        var subscription = new Subscription(messageCallback, () => onDisposeCallCount++);

        subscription.Dispose();
        subscription.Dispose(); // Second call should not change state

        subscription.IsAlive.Should().BeFalse();
        onDisposeCallCount.Should().Be(1);
    }

    [Test]
    public async Task InvokeAsync_DisposedSubscription_ShouldThrowObjectDisposedException()
    {
        var messageCallback = new MessageCallback<String>(_ => { });
        var subscription = new Subscription(messageCallback, () => { });

        subscription.Dispose();

        await FluentActions.Awaiting(() => subscription.InvokeAsync(new()))
                           .Should().ThrowExactlyAsync<ObjectDisposedException>();
    }

    [Test]
    public async Task InvokeAsync_WithNullPayload_ShouldThrowArgumentNullException()
    {
        var messageCallback = new MessageCallback<String>(_ => { });
        var subscription = new Subscription(messageCallback, () => { });

        await FluentActions.Awaiting(() => subscription.InvokeAsync(null!))
                           .Should().ThrowExactlyAsync<ArgumentNullException>();
    }

    [Test]
    public async Task InvokeAsync_ShouldInvokeCallbackWithPayload()
    {
        var payload = "Test Payload";
        var invokedPayload = String.Empty;

        var messageCallback = new MessageCallback<String>(p => invokedPayload = p);
        var subscription = new Subscription(messageCallback, () => { });

        await subscription.InvokeAsync(payload);

        invokedPayload.Should().Be(payload);
    }
}
