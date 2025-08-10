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

    [Test]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        var subscriptionMock = new Mock<IBlazorMessageSubscription>();
        _messageExchange.Subscriptions.Add(subscriptionMock.Object);

        FluentActions.Invoking(() =>
        {
            _messageExchange.Dispose();
            _messageExchange.Dispose();
        }).Should().NotThrow();
    }

    [Test]
    public void Dispose_ShouldDisposeAllSubscriptions()
    {
        var subscriptionMock1 = new Mock<IBlazorMessageSubscription>();
        var subscriptionMock2 = new Mock<IBlazorMessageSubscription>();

        _messageExchange.Subscriptions.Add(subscriptionMock1.Object);
        _messageExchange.Subscriptions.Add(subscriptionMock2.Object);

        _messageExchange.Dispose();

        subscriptionMock1.Verify(s => s.Dispose(), Times.Once);
        subscriptionMock2.Verify(s => s.Dispose(), Times.Once);

        _messageExchange.Subscriptions.Should().BeEmpty();
    }

    [Test]
    public void Dispose_WithNoSubscriptions_ShouldNotThrow()
    {
        FluentActions.Invoking(() => _messageExchange.Dispose()).Should().NotThrow();
    }

    [Test]
    public async Task PublishAsync_WithNonNullPayload_ShouldCallMessageBusPublishAsync()
    {
        var payload = "TestPayload";
        _messageBusMock.Setup(m => m.PublishAsync(payload, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _messageExchange.PublishAsync(payload);
        _messageBusMock.VerifyAll();
    }

    [SetUp]
    public void Setup()
    {
        _messageBusMock = new(MockBehavior.Strict);
        _messageExchange = new(_messageBusMock.Object);
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

    [TearDown]
    public void Teardown() => _messageExchange.Dispose();
}
