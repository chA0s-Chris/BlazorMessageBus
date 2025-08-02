// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using FluentAssertions;
using NUnit.Framework;

public class MessageCallbackTests
{
    [Test]
    [TestCaseSource(nameof(MessageCallbacks))]
    public async Task InvokeAsync_WithHandlerThrowingException_ThrowsException(Object messageCallbackObject)
    {
        var messageCallback = (MessageCallback)messageCallbackObject;

        await FluentActions.Awaiting(() => messageCallback.InvokeAsync("Dummy message"))
                           .Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("Test");
    }

    private static IEnumerable<MessageCallback> MessageCallbacks()
    {
        yield return new MessageCallback<String>(_ => throw new InvalidOperationException("Test"));
        yield return new MessageCallbackAsync<String>(_ => throw new InvalidOperationException("Test"));
    }
}
