// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

public class MessageBusTests
{
    [Test]
    public async Task PublishAsync_AllSubscriptionsDisposedBeforePublish_ShouldNotThrowOrAggregate()
    {
        var messageBus = new MessageBus();
        var subscription1 = messageBus.Subscribe<String>(_ => throw new InvalidOperationException());
        var subscription2 = messageBus.Subscribe<String>(_ => throw new FileNotFoundException());
        subscription1.Dispose();
        subscription2.Dispose();

        await FluentActions.Awaiting(() => messageBus.PublishAsync("Test"))
                           .Should().NotThrowAsync();
    }

    [Test]
    public async Task PublishAsync_Object_WithGenericSubscription_ShouldInvokeHandler()
    {
        var messageBus = CreateMessageBus();
        var called = false;
        messageBus.Subscribe<String>(_ => called = true);

        Object payload = "Test";
        await messageBus.PublishAsync(payload);

        called.Should().BeTrue();
    }

    [Test]
    public async Task PublishAsync_Object_WithNoSubscriptions_ShouldNotThrow()
    {
        var messageBus = CreateMessageBus();
        Object payload = "Test";
        await FluentActions.Invoking(() => messageBus.PublishAsync(payload))
                           .Should().NotThrowAsync();
    }

    [Test]
    public async Task PublishAsync_Object_WithNullPayload_ShouldThrowArgumentNullException()
    {
        var messageBus = CreateMessageBus();
        await FluentActions.Awaiting(() => messageBus.PublishAsync((Object)null!))
                           .Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task PublishAsync_Object_WithTypeBasedSubscription_ShouldInvokeHandler()
    {
        var messageBus = CreateMessageBus();
        var called = false;
        messageBus.Subscribe(typeof(String), _ => called = true);

        Object payload = "Test";
        await messageBus.PublishAsync(payload);

        called.Should().BeTrue();
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
    public async Task PublishAsync_WithNoSubscriptions_ShouldNotThrow()
    {
        var messageBus = CreateMessageBus();
        await FluentActions.Invoking(() => messageBus.PublishAsync("Test"))
                           .Should().NotThrowAsync();
    }

    [Test]
    public async Task PublishAsync_WithNullPayload_ShouldThrowArgumentNullException()
    {
        var messageBus = CreateMessageBus();
        await FluentActions.Awaiting(() => messageBus.PublishAsync<String>(null!))
                           .Should().ThrowAsync<ArgumentNullException>();
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
    public void Subscribe_WithAsynchronousHandler_ShouldReturnSubscription()
    {
        var messageBus = CreateMessageBus();
        var subscription = messageBus.Subscribe<String>(async _ => await Task.CompletedTask);

        subscription.Should().NotBeNull();
        subscription.MessageType.Should().Be<String>();
        subscription.IsAlive.Should().BeTrue();
    }

    [Test]
    public void Subscribe_WithNullAsynchronousHandler_ShouldThrowArgumentNullException()
    {
        var messageBus = CreateMessageBus();
        FluentActions.Invoking(() => messageBus.Subscribe((SubscriptionHandlerAsync<String>)null!))
                     .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Subscribe_WithNullSynchronousHandler_ShouldThrowArgumentNullException()
    {
        var messageBus = CreateMessageBus();
        FluentActions.Invoking(() => messageBus.Subscribe((SubscriptionHandler<String>)null!))
                     .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Subscribe_WithNullTypeAndAsynchronousHandler_ShouldThrowArgumentNullException()
    {
        var messageBus = CreateMessageBus();
        FluentActions.Invoking(() => messageBus.Subscribe((Type)null!, async _ => await Task.CompletedTask))
                     .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Subscribe_WithNullTypeAndSynchronousHandler_ShouldThrowArgumentNullException()
    {
        var messageBus = CreateMessageBus();
        FluentActions.Invoking(() => messageBus.Subscribe((Type)null!, _ => { }))
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

    [Test]
    public void Subscribe_WithTypeAndAsynchronousHandler_ShouldReturnSubscription()
    {
        var messageBus = CreateMessageBus();
        var subscription = messageBus.Subscribe(typeof(String), async _ => await Task.CompletedTask);

        subscription.Should().NotBeNull();
        subscription.MessageType.Should().Be<String>();
        subscription.IsAlive.Should().BeTrue();
    }

    [Test]
    public void Subscribe_WithTypeAndNullAsynchronousHandler_ShouldThrowArgumentNullException()
    {
        var messageBus = CreateMessageBus();
        FluentActions.Invoking(() => messageBus.Subscribe(typeof(String), (SubscriptionHandlerAsync<Object>)null!))
                     .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Subscribe_WithTypeAndNullSynchronousHandler_ShouldThrowArgumentNullException()
    {
        var messageBus = CreateMessageBus();
        FluentActions.Invoking(() => messageBus.Subscribe(typeof(String), (SubscriptionHandler<Object>)null!))
                     .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Subscribe_WithTypeAndSynchronousHandler_ShouldReturnSubscription()
    {
        var messageBus = CreateMessageBus();
        var subscription = messageBus.Subscribe(typeof(String), _ => { });

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
