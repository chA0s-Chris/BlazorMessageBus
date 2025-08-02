// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus;

using System.Collections;
using System.Collections.Immutable;

/// <summary>
/// Manages a collection of message subscriptions.
/// </summary>
internal class Subscriptions : IEnumerable<Subscription>, IDisposable
{
#if NET9_0_OR_GREATER
    private Boolean _isDisposed;
#else
    private Int32 _isDisposed; // 0 = false, 1 = true
#endif
    private ImmutableList<Subscription>? _subscriptions = [];

    public Subscription CreateSubscription(MessageCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var subscription = new Subscription(callback, PurgeInactiveSubscriptions);

        ImmutableList<Subscription>? resultingList = null;
        ImmutableInterlocked.Update(ref _subscriptions,
                                    (current, sub) => resultingList = current?.Add(sub),
                                    subscription);

        if (resultingList is null)
        {
            subscription.Dispose(false);
            throw new ObjectDisposedException(nameof(Subscriptions),
                                              "Cannot create subscription on disposed Subscriptions collection.");
        }

        return subscription;
    }

    private void PurgeInactiveSubscriptions()
    {
        ImmutableInterlocked.Update(ref _subscriptions,
                                    current => current?.RemoveAll(s => !s.IsAlive));
    }

    public void Dispose()
    {
#if NET9_0_OR_GREATER
        if (Interlocked.CompareExchange(ref _isDisposed, true, false))
            return;
#else
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 1)
            return;
#endif

        // Atomically get current subscriptions and set to null (disposed state)
        var subscriptionsToDispose = Interlocked.Exchange(ref _subscriptions, null);

        if (subscriptionsToDispose is not null)
        {
            foreach (var subscription in subscriptionsToDispose)
            {
                subscription.Dispose(false);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<Subscription> GetEnumerator()
    {
        var currentSubscriptions = _subscriptions;
        return currentSubscriptions?.GetEnumerator() ?? Enumerable.Empty<Subscription>().GetEnumerator();
    }
}
