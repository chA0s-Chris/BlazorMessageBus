// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Cha0s.BlazorMessageBus;

using Chaos.BlazorMessageBus;
using FluentAssertions;
using NUnit.Framework;

public class SubscriptionsTests
{
    [Test]
    public void CreateSubscription_WithHandler_ShouldCreateSubscription()
    {
        var subscriptions = new Subscriptions();
        var messageCallback = new MessageCallback<String>(_ => { });

        var subscription = subscriptions.CreateSubscription(messageCallback);
        subscription.Should().NotBeNull();

        var subscriptionList = subscriptions.ToList();
        subscriptionList.Should().HaveCount(1);
        subscriptionList.First().Should().BeSameAs(subscription);
    }

    [Test]
    public void DisposingASubscription_ShouldRemoveItFromList()
    {
        var subscriptions = new Subscriptions();
        var messageCallback = new MessageCallback<String>(_ => { });

        var subscription = subscriptions.CreateSubscription(messageCallback);
        subscription.Should().NotBeNull();

        subscriptions.ToList().Should().HaveCount(1);
        subscriptions.First().Should().BeSameAs(subscription);

        subscription.Dispose();

        subscriptions.ToList().Should().BeEmpty();
    }

    [Test]
    public void Dispose_WithActiveSubscriptions_ShouldDisposeAll()
    {
        var subscriptions = new Subscriptions();
        var messageCallback1 = new MessageCallback<String>(_ => { });
        var messageCallback2 = new MessageCallback<Int32>(_ => { });

        var subscription1 = subscriptions.CreateSubscription(messageCallback1);
        var subscription2 = subscriptions.CreateSubscription(messageCallback2);

        subscriptions.ToList().Should().HaveCount(2);

        subscription1.IsAlive.Should().BeTrue();
        subscription2.IsAlive.Should().BeTrue();

        subscriptions.Dispose();

        subscription1.IsAlive.Should().BeFalse();
        subscription2.IsAlive.Should().BeFalse();

        subscriptions.ToList().Should().BeEmpty();
    }

    [Test]
    public void Dispose_CalledTwice_ShouldNotThrow()
    {
        var subscriptions = new Subscriptions();
        var messageCallback = new MessageCallback<String>(_ => { });

        var subscription = subscriptions.CreateSubscription(messageCallback);
        subscription.Should().NotBeNull();

        subscriptions.ToList().Should().HaveCount(1);

        subscriptions.Dispose();
        subscription.IsAlive.Should().BeFalse();

        subscriptions.Dispose();
        subscriptions.ToList().Should().BeEmpty();
    }
}
