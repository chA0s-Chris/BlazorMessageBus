// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Cha0s.BlazorMessageBus;

using Chaos.BlazorMessageBus;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

public class MessageBusTests
{
    [Test]
    public async Task PublishAsync_WithNullPayload_ShouldThrowArgumentNullException()
    {
        var messageBus = CreateMessageBus();
        await FluentActions.Awaiting(() => messageBus.PublishAsync<String>(null!))
                           .Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task PublishAsync_WithNoSubscriptions_ShouldNotThrow()
    {
        var messageBus = CreateMessageBus();
        await FluentActions.Invoking(() => messageBus.PublishAsync("Test"))
                           .Should().NotThrowAsync();
    }

    [Test]
    public async Task PublishAsync_WithThrowingSubscriptionHandler_ShouldInvokeOnPublishExceptionAndThrow()
    {
        var onPublishExceptionCalled = false;
        var exception = new InvalidOperationException("Test exception");
        var options = new BlazorMessageBusOptions
        {
            StopOnFirstError = true,
            OnPublishException = e =>
            {
                e.Should().BeSameAs(exception);
                onPublishExceptionCalled = true;
                return Task.CompletedTask;
            }
        };

        var messageBus = CreateMessageBus(options);
        messageBus.Subscribe<String>(_ => throw exception);

        await FluentActions.Awaiting(() => messageBus.PublishAsync("Test"))
                           .Should().ThrowAsync<InvalidOperationException>();

        onPublishExceptionCalled.Should().BeTrue();
    }

    [Test]
    public async Task PublishAsync_WithMultipleSubscriptionsAndOneThrowingHandler_ShouldInvokeOnPublishExceptionAndRethrowTheException()
    {
        var onPublishExceptionCalled = false;
        var exception = new InvalidOperationException("Test exception");
        var options = new BlazorMessageBusOptions
        {
            StopOnFirstError = true,
            OnPublishException = e =>
            {
                e.Should().BeSameAs(exception);
                onPublishExceptionCalled = true;
                return Task.CompletedTask;
            }
        };

        var messageBus = CreateMessageBus(options);
        messageBus.Subscribe<String>(_ => { });
        messageBus.Subscribe<String>(_ => throw exception);

        await FluentActions.Awaiting(() => messageBus.PublishAsync("Test"))
                           .Should().ThrowAsync<InvalidOperationException>();

        onPublishExceptionCalled.Should().BeTrue();
    }

    [Test]
    public async Task PublishAsync_WithMultipleThrowingSubscriptionHandlers_ShouldInvokeOnPublishExceptionForEachExceptionAndThrowAggregateException()
    {
        var onPublishExceptionCalled = 0;
        var exception1 = new InvalidOperationException("Test exception 1");
        var exception2 = new FileNotFoundException("Test exception 2");
        var options = new BlazorMessageBusOptions
        {
            StopOnFirstError = false,
            OnPublishException = e =>
            {
                if (e == exception1 || e == exception2)
                {
                    onPublishExceptionCalled++;
                }

                return Task.CompletedTask;
            }
        };

        var messageBus = CreateMessageBus(options);
        messageBus.Subscribe<String>(_ => throw exception1);
        messageBus.Subscribe<String>(_ => throw exception2);

        await FluentActions.Awaiting(() => messageBus.PublishAsync("Test"))
                           .Should().ThrowExactlyAsync<AggregateException>()
                           .Where(e => e.InnerExceptions.Count == 2 &&
                                       e.InnerExceptions.Any(inner => inner is InvalidOperationException) &&
                                       e.InnerExceptions.Any(inner => inner is FileNotFoundException));

        onPublishExceptionCalled.Should().Be(2);
    }

    [Test]
    public async Task PublishAsync_WithMultipleThrowingSubscriptionHandlersAndStopOnFirstErrorSetToTrue_ShouldInvokeOnPublishExceptionForFirstExceptionAndThrow()
    {
        var onPublishExceptionCalled = 0;
        var exception1 = new InvalidOperationException("Test exception 1");
        var exception2 = new FileNotFoundException("Test exception 2");
        var options = new BlazorMessageBusOptions
        {
            StopOnFirstError = true,
            OnPublishException = e =>
            {
                if (e == exception1 || e == exception2)
                {
                    onPublishExceptionCalled++;
                }

                return Task.CompletedTask;
            }
        };

        var messageBus = CreateMessageBus(options);
        messageBus.Subscribe<String>(_ => throw exception1);
        messageBus.Subscribe<String>(_ => throw exception2);

        await FluentActions.Awaiting(() => messageBus.PublishAsync("Test"))
                           .Should().ThrowExactlyAsync<InvalidOperationException>();

        onPublishExceptionCalled.Should().Be(1);
    }

    [Test]
    public void Subscribe_WithNullAsynchronousHandler_ShouldThrowArgumentNullException()
    {
        var messageBus = CreateMessageBus();
        FluentActions.Invoking(() => messageBus.Subscribe((SubscriptionHandlerAsync<String>)null!))
                     .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Subscribe_WithAsynchronousHandler_ShouldReturnSubscription()
    {
        var messageBus = CreateMessageBus();
        var subscription = messageBus.Subscribe<String>(async _ => await Task.CompletedTask);

        subscription.Should().NotBeNull();
        subscription.MessageType.Should().Be<String>();
        subscription.IsAlive.Should().BeTrue();
    }

    [Test]
    public void Subscribe_WithNullSynchronousHandler_ShouldThrowArgumentNullException()
    {
        var messageBus = CreateMessageBus();
        FluentActions.Invoking(() => messageBus.Subscribe((SubscriptionHandler<String>)null!))
                     .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Subscribe_WithSynchronousHandler_ShouldReturnSubscription()
    {
        var messageBus = CreateMessageBus();
        var subscription = messageBus.Subscribe<String>(_ => { });

        subscription.Should().NotBeNull();
        subscription.MessageType.Should().Be<String>();
        subscription.IsAlive.Should().BeTrue();
    }

    private static MessageBus CreateMessageBus(BlazorMessageBusOptions? options = null)
    {
        if (options is not null)
            return new(new OptionsWrapper<BlazorMessageBusOptions>(options));
        return new();
    }
}
