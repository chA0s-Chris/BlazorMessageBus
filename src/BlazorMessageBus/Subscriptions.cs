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
    private Boolean _isDisposed;
    private ImmutableList<Subscription> _subscriptions = [];

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        foreach (var subscription in _subscriptions.ToList())
        {
            subscription.Dispose(false);
        }

        _subscriptions = [];
    }

    public IEnumerator<Subscription> GetEnumerator() => _subscriptions.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void PurgeInactiveSubscriptions()
        => ImmutableInterlocked.Update(ref _subscriptions,
                                       x => x.RemoveAll(s => !s.IsAlive));

    public Subscription CreateSubscription(MessageCallback callback)
    {
        var subscription = new Subscription(callback, PurgeInactiveSubscriptions);

        ImmutableInterlocked.Update(ref _subscriptions,
                                    (list, sub) => list.Add(sub), subscription);

        return subscription;
    }
}
