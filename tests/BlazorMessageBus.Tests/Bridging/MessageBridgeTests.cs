// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Bridging;

using Chaos.BlazorMessageBus.Filtering;
using FluentAssertions;
using NUnit.Framework;

public class MessageBridgeTests
{
    [Test]
    public async Task BidirectionalBridge_ShouldWorkInBothDirections()
    {
        // Arrange
        var localBus = new BridgeableMessageBus(new MessageBus());
        var remoteBus = new BridgeableMessageBus(new MessageBus());

        var localBridge = localBus.CreateBridge();
        var remoteBridge = remoteBus.CreateBridge();

        var messagesOnLocal = new List<TestMessage>();
        var messagesOnRemote = new List<TestMessage>();

        localBus.Subscribe<TestMessage>(msg => messagesOnLocal.Add(msg));
        remoteBus.Subscribe<TestMessage>(msg => messagesOnRemote.Add(msg));

        // Set up bidirectional bridging using the non-generic injection method
        await localBridge.StartAsync(async (messageType, payload, cancellationToken) =>
        {
            if (messageType == typeof(TestMessage))
            {
                await remoteBridge.InjectMessageAsync(messageType, payload, cancellationToken);
            }
        });

        await remoteBridge.StartAsync(async (messageType, payload, cancellationToken) =>
        {
            if (messageType == typeof(TestMessage))
            {
                await localBridge.InjectMessageAsync(messageType, payload, cancellationToken);
            }
        });

        // Act
        await localBus.PublishAsync(new TestMessage("From Local"));
        await remoteBus.PublishAsync(new TestMessage("From Remote"));

        // Wait a bit for async operations
        await Task.Delay(100);

        // Assert
        messagesOnLocal.Should().HaveCount(2); // Original + bridged from remote
        messagesOnRemote.Should().HaveCount(2); // Original + bridged from local

        messagesOnLocal.Should().Contain(m => m.Content == "From Local");
        messagesOnLocal.Should().Contain(m => m.Content == "From Remote");
        messagesOnRemote.Should().Contain(m => m.Content == "From Local");
        messagesOnRemote.Should().Contain(m => m.Content == "From Remote");

        // Cleanup
        await localBridge.StopAsync();
        await remoteBridge.StopAsync();
        localBridge.Dispose();
        remoteBridge.Dispose();
    }

    [Test]
    public async Task Bridge_ShouldAllowRestartAfterStop()
    {
        // Arrange
        var messageBus = new BridgeableMessageBus(new MessageBus());
        var bridge = messageBus.CreateBridge();

        var receivedCount = 0;

        // Act
        await bridge.StartAsync((_, _, _) =>
        {
            receivedCount++;
            return Task.CompletedTask;
        });
        await messageBus.PublishAsync(new TestMessage("Message 1"));

        await bridge.StopAsync();

        await bridge.StartAsync((_, _, _) =>
        {
            receivedCount++;
            return Task.CompletedTask;
        });
        await messageBus.PublishAsync(new TestMessage("Message 2"));

        // Assert
        receivedCount.Should().Be(2);

        await bridge.StopAsync();
        bridge.Dispose();
    }

    [Test]
    public async Task Bridge_ShouldForwardMessagesToHandler_WhenActive()
    {
        // Arrange
        var messageBus = new BridgeableMessageBus(new MessageBus());
        var bridge = messageBus.CreateBridge();

        var receivedMessages = new List<(Type messageType, Object payload)>();

        // Act
        await bridge.StartAsync((messageType, payload, cancellationToken) =>
        {
            receivedMessages.Add((messageType, payload));
            return Task.CompletedTask;
        });

        await messageBus.PublishAsync(new TestMessage("Hello Bridge"));

        // Assert
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].messageType.Should().Be<TestMessage>();
        ((TestMessage)receivedMessages[0].payload).Content.Should().Be("Hello Bridge");

        // Cleanup
        await bridge.StopAsync();
        bridge.Dispose();
    }

    [Test]
    public async Task Bridge_ShouldHandleExceptionsInHandlerGracefully()
    {
        // Arrange
        var messageBus = new BridgeableMessageBus(new MessageBus());
        var bridge = messageBus.CreateBridge();

        var localSubscriberReceived = false;
        messageBus.Subscribe<TestMessage>(_ => localSubscriberReceived = true);

        await bridge.StartAsync((_, _, _) =>
        {
            // Simulate handler exception
            throw new InvalidOperationException("Bridge handler failed");
        });

        // Act & Assert - should not throw, local processing should continue
        await FluentActions.Invoking(() => messageBus.PublishAsync(new TestMessage("Test")))
                           .Should().NotThrowAsync();

        // Local subscriber should still receive the message
        localSubscriberReceived.Should().BeTrue();

        await bridge.StopAsync();
        bridge.Dispose();
    }

    [Test]
    public async Task Bridge_ShouldInjectMessagesIntoLocalBus()
    {
        // Arrange
        var messageBus = new BridgeableMessageBus(new MessageBus());
        var bridge = messageBus.CreateBridge();

        var receivedOnBus = new List<TestMessage>();
        messageBus.Subscribe<TestMessage>(msg => receivedOnBus.Add(msg));

        // Act
        await bridge.InjectMessageAsync(new TestMessage("Injected Message"));

        // Assert
        receivedOnBus.Should().HaveCount(1);
        receivedOnBus[0].Content.Should().Be("Injected Message");

        bridge.Dispose();
    }

    [Test]
    public async Task Bridge_ShouldInjectMessagesUsingNonGenericMethod()
    {
        // Arrange
        var messageBus = new BridgeableMessageBus(new MessageBus());
        var bridge = messageBus.CreateBridge();

        var receivedOnBus = new List<TestMessage>();
        messageBus.Subscribe<TestMessage>(msg => receivedOnBus.Add(msg));

        // Act - use the non-generic method (simulates post-transport scenario)
        var messageType = typeof(TestMessage);
        Object payload = new TestMessage("Non-Generic Injected");
        await bridge.InjectMessageAsync(messageType, payload);

        // Assert
        receivedOnBus.Should().HaveCount(1);
        receivedOnBus[0].Content.Should().Be("Non-Generic Injected");

        bridge.Dispose();
    }

    [Test]
    public async Task Bridge_ShouldNotForwardMessages_WhenInactive()
    {
        // Arrange
        var messageBus = new BridgeableMessageBus(new MessageBus());
        var bridge = messageBus.CreateBridge();

        var receivedMessages = new List<(Type messageType, Object payload)>();
        await bridge.StartAsync((type, payload, _) =>
        {
            receivedMessages.Add((type, payload));
            return Task.CompletedTask;
        });
        await bridge.StopAsync();

        // Act - publish without starting bridge
        await messageBus.PublishAsync(new TestMessage("Should Not Bridge"));

        // Assert
        receivedMessages.Should().BeEmpty();

        bridge.Dispose();
    }

    [Test]
    public async Task Bridge_ShouldRespectExcludeFilter()
    {
        // Arrange
        var messageBus = new BridgeableMessageBus(new MessageBus());
        var bridge = messageBus.CreateBridge();

        var receivedMessages = new List<(Type messageType, Object payload)>();

        bridge.ConfigureFilter(BlazorMessageBridgeFilters.Exclude(typeof(ExcludedMessage)));

        await bridge.StartAsync((messageType, payload, _) =>
        {
            receivedMessages.Add((messageType, payload));
            return Task.CompletedTask;
        });

        // Act
        await messageBus.PublishAsync(new TestMessage("Should Bridge"));
        await messageBus.PublishAsync(new ExcludedMessage("Should Not Bridge"));

        // Assert
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].messageType.Should().Be<TestMessage>();

        await bridge.StopAsync();
        bridge.Dispose();
    }

    [Test]
    public async Task Bridge_ShouldRespectIncludeFilter()
    {
        // Arrange
        var messageBus = new BridgeableMessageBus(new MessageBus());
        var bridge = messageBus.CreateBridge();

        var receivedMessages = new List<(Type messageType, Object payload)>();

        bridge.ConfigureFilter(BlazorMessageBridgeFilters.Include(typeof(TestMessage)));

        await bridge.StartAsync((messageType, payload, cancellationToken) =>
        {
            receivedMessages.Add((messageType, payload));
            return Task.CompletedTask;
        });

        // Act
        await messageBus.PublishAsync(new TestMessage("Should Bridge"));
        await messageBus.PublishAsync(new AnotherMessage(42)); // Should not bridge

        // Assert
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].messageType.Should().Be<TestMessage>();

        await bridge.StopAsync();
        bridge.Dispose();
    }

    [Test]
    public async Task Bridge_ShouldRespectPredicateFilter()
    {
        // Arrange
        var messageBus = new BridgeableMessageBus(new MessageBus());
        var bridge = messageBus.CreateBridge();

        var receivedMessages = new List<(Type messageType, Object payload)>();

        // Only bridge messages that start with "Test" in their name
        bridge.ConfigureFilter(BlazorMessageBridgeFilters.Where(type =>
                                                                    type.Name.StartsWith("Test")));

        await bridge.StartAsync((messageType, payload, cancellationToken) =>
        {
            receivedMessages.Add((messageType, payload));
            return Task.CompletedTask;
        });

        // Act
        await messageBus.PublishAsync(new TestMessage("Should Bridge")); // Starts with "Test"
        await messageBus.PublishAsync(new AnotherMessage(42)); // Doesn't start with "Test"

        // Assert
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].messageType.Should().Be<TestMessage>();

        await bridge.StopAsync();
        bridge.Dispose();
    }

    [Test]
    public async Task Bridge_ShouldStopForwarding_WhenStopped()
    {
        // Arrange
        var messageBus = new BridgeableMessageBus(new MessageBus());
        var bridge = messageBus.CreateBridge();

        var receivedMessages = new List<(Type messageType, Object payload)>();

        await bridge.StartAsync((messageType, payload, _) =>
        {
            receivedMessages.Add((messageType, payload));
            return Task.CompletedTask;
        });

        // Act
        await messageBus.PublishAsync(new TestMessage("Message 1"));
        await bridge.StopAsync();
        await messageBus.PublishAsync(new TestMessage("Message 2"));

        // Assert - only first message should be received
        receivedMessages.Should().HaveCount(1);
        ((TestMessage)receivedMessages[0].payload).Content.Should().Be("Message 1");

        bridge.Dispose();
    }

    [Test]
    public async Task Bridge_ShouldThrowWhenStartingAlreadyActiveBridge()
    {
        // Arrange
        var messageBus = new BridgeableMessageBus(new MessageBus());
        var bridge = messageBus.CreateBridge();

        // Act & Assert
        await bridge.StartAsync((_, _, _) => Task.CompletedTask);

        await FluentActions.Awaiting(() => bridge.StartAsync((_, _, _) => Task.CompletedTask))
                           .Should().ThrowAsync<InvalidOperationException>();

        bridge.Dispose();
    }

    private record AnotherMessage(Int32 Value);

    private record ExcludedMessage(String Data);

    private record TestMessage(String Content);
}
