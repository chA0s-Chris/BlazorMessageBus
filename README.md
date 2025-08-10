# BlazorMessageBus

A lightweight, thread-safe message bus for Blazor applications.
BlazorMessageBus enables decoupled communication between components and services using a publish/subscribe pattern.

[![GitHub License](https://img.shields.io/github/license/chA0s-Chris/BlazorMessageBus?style=for-the-badge)](https://github.com/chA0s-Chris/BlazorMessageBus/blob/main/LICENSE)
[![NuGet Version](https://img.shields.io/nuget/v/Chaos.BlazorMessageBus?style=for-the-badge)](https://www.nuget.org/packages/Chaos.BlazorMessageBus)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Chaos.BlazorMessageBus?style=for-the-badge)](https://www.nuget.org/packages/Chaos.BlazorMessageBus)
[![GitHub last commit](https://img.shields.io/github/last-commit/chA0s-Chris/BlazorMessageBus?style=for-the-badge)](https://github.com/chA0s-Chris/BlazorMessageBus/commits/)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/chA0s-Chris/BlazorMessageBus/ci.yml?style=for-the-badge)]()

## Features

- **Publish/Subscribe**: Send messages of any type and subscribe with type-safe handlers.
- **Async and Sync Handlers**: Supports both synchronous and asynchronous message handlers.
- **Scoped Message Exchange**: Simplifies subscription management for Blazor components.
- **Thread-Safe**: Safe for concurrent publishing and subscribing.
- **Flexible Error Handling**: Configurable exception handling and fail-fast options.
- **Dependency Injection**: Easy integration with ASP.NET Core DI.

## Installation

Add the NuGet package:

```
dotnet add package Chaos.BlazorMessageBus
```

## Getting Started

1. **Register the services in your `Program.cs`:**

```csharp
builder.Services.AddBlazorMessageBus(options =>
{
    options.StopOnFirstError = false;
    options.OnPublishException = ex => { /* log or handle */ return Task.CompletedTask; };
});
```

2. **Inject and use the message bus in your components or services:**

```csharp
@inject IBlazorMessageBus MessageBus

// Subscribe
var subscription = MessageBus.Subscribe<string>(msg => Console.WriteLine(msg));

// Publish
await MessageBus.PublishAsync("Hello, world!");

// Dispose when done
subscription.Dispose();
```

For cleanup best practices, see [Disposal and Lifecycle Guidance](#disposal-and-lifecycle-guidance).

3. **For Blazor components, use `IBlazorMessageExchange` for automatic cleanup:**

```csharp
@inject IBlazorMessageExchange MessageExchange

protected override void OnInitialized()
{
    MessageExchange.Subscribe<string>(msg => { /* handle message */ });
}

public void Dispose()
{
    MessageExchange.Dispose();
}
```

For cleanup best practices, see [Disposal and Lifecycle Guidance](#disposal-and-lifecycle-guidance).

## Configuration Options

- `StopOnFirstError` (Boolean): If true, publishing stops on the first handler exception. If false, all handlers are invoked and exceptions are aggregated.
- `OnPublishException` (Func<Exception, Task>): Optional async handler for exceptions thrown by subscribers.

## Advanced Usage

### Handling Multiple Message Types

You can subscribe to and publish any type:

```csharp
MessageBus.Subscribe<int>(i => Console.WriteLine($"Int: {i}"));
MessageBus.Subscribe<MyCustomEvent>(evt => HandleCustomEvent(evt));

await MessageBus.PublishAsync(42);
await MessageBus.PublishAsync(new MyCustomEvent { ... });
```

### Asynchronous Handlers

```csharp
MessageBus.Subscribe<string>(async msg =>
{
    await SomeAsyncOperation(msg);
});
```

### Exception Handling and Aggregation

If multiple handlers throw exceptions during publish, all exceptions are aggregated and thrown as an `AggregateException` (unless `StopOnFirstError` is enabled):

```csharp
try
{
    await MessageBus.PublishAsync("Test");
}
catch (AggregateException ex)
{
    foreach (var inner in ex.InnerExceptions)
    {
        // Handle each exception
    }
}
```

### Thread Safety

BlazorMessageBus is thread-safe. You can safely publish and subscribe from multiple threads concurrently.

### Unsubscribing

Dispose the returned subscription to unsubscribe:

```csharp
var subscription = MessageBus.Subscribe<string>(...);
subscription.Dispose();
```

## Disposal and Lifecycle Guidance

- `IBlazorMessageExchange` must be disposed with the owning component’s lifecycle. Implement `IDisposable` or `IAsyncDisposable` on the component and dispose the exchange in `Dispose()`/`DisposeAsync()`.
- Subscriptions returned from `Subscribe(...)` implement `IDisposable`. Disposing a subscription stops further message delivery; there is no separate “Unsubscribe”.
- Scope subscriptions to the component that created them. Prefer per-component exchanges over long-lived, global subscriptions.
- Treat disposal as idempotent and safe to call multiple times from a consumer perspective. After disposal, do not expect further callbacks; in-flight callbacks may complete, but new ones should not be scheduled.
- Avoid blocking in disposal paths. Do not wait on asynchronous work during disposal; release references and let in-flight operations complete naturally.
- For Blazor components, create the exchange during initialization and keep a field reference; dispose it in the component’s `Dispose()`/`DisposeAsync()` so all managed subscriptions are also cleaned up.

## Message Bridging

BlazorMessageBus supports bridging messages across bus instances via `IBlazorMessageBus.CreateMessageBridge(...)`. A bridge can forward outbound messages to any transport (HTTP, SignalR, etc.) and inject inbound messages into the local bus.

- Forwarding model: Forwarding to bridges is fire-and-forget and does not affect `PublishAsync` latency. Remote delivery is eventual and may complete after `PublishAsync` returns.
- Loop prevention: Each bridge has a unique Id. Inbound injections carry the originating bridge Id so the local bus can skip forwarding back to that same bridge, preventing loops.
- Error isolation: Exceptions thrown during forwarding are swallowed so local delivery is never affected.
- Lifecycle and disposal: Disposing a bridge deactivates it, stops further forwarding, forbids additional injections, and deregisters it from the bus. Disposal is idempotent and never throws.
- Filtering: Configure filters to control which message types are forwarded using `BlazorMessageBridgeFilters.Include(...)`, `Exclude(...)`, or `Where(...)`. Filters can be reconfigured at runtime and apply to subsequent messages.
- Cancellation: Cancellation tokens passed to `PublishAsync` are observed only by outbound forwarding (e.g., your transport). Local delivery is not canceled. Similarly, `InjectMessageAsync(..., cancellationToken)` does not cancel local delivery.
- Topologies and duplicates: When multiple paths exist to a destination (e.g., A→C direct and A→B→C), downstream buses may observe duplicates. Apply filters or deduplication at the edges if needed.

### Bridging Example

This example shows how to forward messages from App A to App B using a custom transport (e.g., SignalR/HTTP). The outbound side sends to your transport; the inbound side injects into the local bus.

```csharp
// App A (outbound)
// IBlazorMessageBus busA is resolved from DI

// Create a bridge and wire outbound forwarding to your transport
var bridgeToB = busA.CreateMessageBridge(async (Type type, Object payload, CancellationToken ct) =>
{
    await transportAtoB.SendAsync(type, payload, ct); // implement this (SignalR/HTTP/etc.)
});

// Optionally filter which types are forwarded
bridgeToB.ConfigureFilter(BlazorMessageBridgeFilters.Include(typeof(String)));

// App B (inbound)
// IBlazorMessageBus busB is resolved from DI

// Create a local bridge instance to own the inbound injection identity (prevents loops)
var inboundBridge = busB.CreateMessageBridge((_, _, _) => Task.CompletedTask);

// When your transport receives a message from App A, inject it into App B
transportAtoB.OnReceived(async (Type type, Object payload, CancellationToken ct) =>
{
    await inboundBridge.InjectMessageAsync(type, payload, ct);
});

// Subscriptions on App B receive injected messages
busB.Subscribe<String>(msg => Console.WriteLine($"App B received: {msg}"));

// Publish on App A: delivered locally and forwarded to App B
await busA.PublishAsync("Hello from A");

// Cleanup when done
bridgeToB.Dispose();
inboundBridge.Dispose();
```

## License

MIT License - see [LICENSE](./LICENSE) for more information.
