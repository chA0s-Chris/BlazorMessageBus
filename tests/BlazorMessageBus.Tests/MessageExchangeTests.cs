// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using FluentAssertions;
using Moq;
using NUnit.Framework;

public class MessageExchangeTests
{
    private Mock<IBlazorMessageBus> _messageBusMock;
    private MessageExchange _messageExchange;

    [SetUp]
    public void Setup()
    {
        _messageBusMock = new(MockBehavior.Strict);
        _messageExchange = new(_messageBusMock.Object);
    }

    [TearDown]
    public void Teardown() => _messageExchange.Dispose();

    [Test]
    public async Task PublishAsync_WithNonNullPayload_ShouldCallMessageBusPublishAsync()
    {
        var payload = "TestPayload";
        _messageBusMock.Setup(m => m.PublishAsync(payload)).Returns(Task.CompletedTask);

        await _messageExchange.PublishAsync(payload);
        _messageBusMock.VerifyAll();
    }

    [Test]
    public void Subscribe_WithNonNullAsynchronousHandler_ShouldCallMessageBusSubscribeAndSaveSubscription()
    {
        var handler = new SubscriptionHandlerAsync<String>(_ => Task.CompletedTask);
        var subscription = new Subscription(new MessageCallbackAsync<String>(handler), () => { });

        _messageBusMock.Setup(m => m.Subscribe(handler))
                       .Returns(subscription);

        _messageExchange.Subscribe(handler);

        _messageExchange.Subscriptions.Should().HaveCount(1);
        _messageExchange.Subscriptions.First().Should().BeSameAs(subscription);

        _messageBusMock.VerifyAll();
    }

    [Test]
    public void Subscribe_WithNonNullSynchronousHandler_ShouldCallMessageBusSubscribeAndSaveSubscription()
    {
        var handler = new SubscriptionHandler<String>(_ => { });
        var subscription = new Subscription(new MessageCallback<String>(handler), () => { });

        _messageBusMock.Setup(m => m.Subscribe(handler))
                       .Returns(subscription);

        _messageExchange.Subscribe(handler);

        _messageExchange.Subscriptions.Should().HaveCount(1);
        _messageExchange.Subscriptions.First().Should().BeSameAs(subscription);

        _messageBusMock.VerifyAll();
    }

    [Test]
    public void Dispose_ShouldDisposeAllSubscriptions()
    {
        var subscription1 = new Mock<IBlazorMessageSubscription>();
        var subscription2 = new Mock<IBlazorMessageSubscription>();

        _messageExchange.Subscriptions.Add(subscription1.Object);
        _messageExchange.Subscriptions.Add(subscription2.Object);

        subscription1.Setup(s => s.Dispose());
        subscription2.Setup(s => s.Dispose());

        _messageExchange.Dispose();

        subscription1.VerifyAll();
        subscription2.VerifyAll();
        _messageExchange.Subscriptions.Should().BeEmpty();
    }
}
