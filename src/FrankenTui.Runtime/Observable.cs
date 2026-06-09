// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/reactive/observable.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Observable value wrapper with change notification and version tracking.
//
// Observable<T> wraps a value in shared storage. When the value changes
// (determined by EqualityComparer<T>.Default), all live subscribers are
// notified in registration order.
//
// DIVERGENCE: No tracing spans (tracing not yet ported for this module).
// DIVERGENCE: Uses lock + List instead of Rc<RefCell<Vec<Weak>>>.
// DIVERGENCE: Batch system deferred (not needed by locale.rs caller).

namespace FrankenTui.Runtime;

/// <summary>
/// RAII guard for a subscriber callback. Dropping unsubscribes.
/// </summary>
public sealed class Subscription : IDisposable
{
    internal readonly Action _unsubscribe;

    internal Subscription(Action unsubscribe)
    {
        _unsubscribe = unsubscribe;
    }

    /// <summary>Unsubscribe the callback.</summary>
    public void Dispose() => _unsubscribe();
}

/// <summary>
/// A shared, version-tracked value with change notification.
/// </summary>
public sealed class Observable<T>
{
    private readonly object _lock = new();
    private T _value;
    private ulong _version;
    private readonly List<(Action<T> Callback, bool Alive)> _subscribers = new();
    private bool _notifying;

    /// <summary>Create a new observable with the given initial value.</summary>
    public Observable(T value)
    {
        _value = value;
        _version = 0;
    }

    /// <summary>Get a clone of the current value.</summary>
    public T Get()
    {
        lock (_lock) { return _value; }
    }

    /// <summary>Access the current value by reference without cloning.</summary>
    public R With<R>(Func<T, R> f)
    {
        lock (_lock) { return f(_value); }
    }

    /// <summary>
    /// Set a new value. If different from current, version increments and
    /// subscribers are notified.
    /// </summary>
    public void Set(T value)
    {
        List<Action<T>>? callbacks = null;
        lock (_lock)
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
                return;

            _value = value;
            _version++;
            callbacks = new List<Action<T>>();
            foreach (var (cb, alive) in _subscribers)
            {
                if (alive) callbacks.Add(cb);
            }
        }
        NotifyCallbacks(callbacks, value);
    }

    private void NotifyCallbacks(List<Action<T>>? callbacks, T value)
    {
        if (callbacks == null || callbacks.Count == 0) return;
        _notifying = true;
        foreach (var cb in callbacks)
            cb(value);
        _notifying = false;
        lock (_lock) { _subscribers.RemoveAll(s => !s.Alive); }
    }

    /// <summary>
    /// Modify the value in place. If the value changes, version increments
    /// and subscribers are notified.
    /// </summary>
    public void Update(Action<T> f)
    {
        T old;
        List<Action<T>>? callbacks = null;
        lock (_lock)
        {
            old = _value;
            f(_value);
            if (EqualityComparer<T>.Default.Equals(_value, old))
                return;
            _version++;
            callbacks = new List<Action<T>>();
            foreach (var (cb, alive) in _subscribers)
                if (alive) callbacks.Add(cb);
        }
        NotifyCallbacks(callbacks, _value);
    }

    /// <summary>
    /// Subscribe to value changes. Returns a Subscription guard.
    /// Dropping the guard unsubscribes.
    /// </summary>
    public Subscription Subscribe(Action<T> callback)
    {
        int index;
        lock (_lock)
        {
            _subscribers.Add((callback, true));
            index = _subscribers.Count - 1;
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (index < _subscribers.Count)
                    _subscribers[index] = (_subscribers[index].Callback, false);
            }
        });
    }

    /// <summary>Current version number. Increments on value changes.</summary>
    public ulong Version
    {
        get { lock (_lock) { return _version; } }
    }

    /// <summary>Number of registered subscribers (including dead ones not yet pruned).</summary>
    public int SubscriberCount
    {
        get { lock (_lock) { return _subscribers.Count; } }
    }
}
