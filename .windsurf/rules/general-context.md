---
trigger: always_on
description: General context for BlazorMessageBus
globs: 
---

`BlazorMessageBus` is a pub/sub implementation designed to be used primarily in Blazor applications. Originally it was exclusively used with Blazor WebAssembly applications.

The message bus is designed to be thread-safe.

`IBlazorMessageBus` is the main interface that exposes all the functionality of `BlazorMessageBus`. `IBlazorMessageBus` explicitly defines the subscription functionality while `IBlazorMessagePublisher` defines the publishing functionality.

Additional to the message bus, `BlazorMessageBus` also uses the concept of a per component message exchange, defined by `IBlazorMessageExchange`. The message exchange is designed to be used by a Blazor component that requires one or more subscriptions and is a wrapper for the message bus that manages subscriptions. It should have the same lifecycle as the Blazor component and must be disposed when the component's lifecycle ends. So components using a message exchange must always implement `IDisposable`.

Publishing messages is always `async`  while subscriptions can have either synchronous or asynchronous callbacks.

To stop a subscription, the instance has to be disposed. There is no explicit way to "unsubscribe".

