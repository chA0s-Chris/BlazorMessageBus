---
trigger: glob
description: Unit test conventions for NUnit + Moq + FluentAssertions to keep tests clear, reliable, and consistent.
globs: tests/**/*.cs
---

## Test Guidelines

### Frameworks & Tooling
- Use the `NUnit` testing framework, `Moq` for mocking, and `FluentAssertions` for descriptive assertions.
- Follow Microsoft's Unit Testing Best Practices for .NET: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices

### File & Class Organization
- Mirror the source folder structure under tests (e.g., `src/BlazorMessageBus/Bridging/BlazorMessageBridge.cs` -> `tests/BlazorMessageBus.Tests/Bridging/BlazorMessageBridgeTests.cs`).
- Place tests for a specific class in a test file named like the original code file with a `Tests` suffix. Example: tests for `MessageBus.cs` go in `MessageBusTests.cs`.
- Do not add `[TestFixture]` to the test class (not required with modern NUnit).

### Test Naming
- Use the pattern `MethodName_Condition_ExpectedResult`. Examples:
  - `Subscribe_WithTypeAndAsynchronousHandler_ShouldReturnSubscription`
  - `Publish_WithNoSubscribers_ShouldNotThrow`

### Structure (AAA)
- Use the Arrange/Act/Assert structure with blank line separators.
- Keep Arrange focused and explicit; avoid over-mocking. Keep Act to a single, clear action.
- Assertions should be expressive via FluentAssertions and verify behavior that matters.

### Async Tests
- Async tests must return `Task` (avoid `async void` except for event handlers).
- Do not block on async (`.Result`, `.Wait()`); always `await`.
- For exceptions, prefer: `await act.Should().ThrowAsync<TException>();`

### Flakiness & Timing
- Avoid `Thread.Sleep`. Prefer synchronization primitives or eventual assertions:
  - `TaskCompletionSource`, `ManualResetEventSlim`, or polling with timeouts.
  - Apply conservative timeouts and avoid brittle, timing-dependent checks.
- Avoid reliance on system time or randomness; inject abstractions or use fixed seeds.

### Mocking (Moq)
- Default to `MockBehavior.Loose` for readability; verify only meaningful interactions.
- Use strict mocks selectively when interaction order/shape is critical to the behavior under test.
- Prefer `ReturnsAsync(...)` and `Verifiable()` sparingly; verify the minimal necessary interactions.

### Parallelization
- Tests should be safe to run in parallel; avoid shared mutable state (especially statics/singletons).
- Mark non-parallel-safe tests with `[NonParallelizable]`. Prefer fixture-level parallelization where safe.
