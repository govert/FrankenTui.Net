// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/subscription.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
//
// Subscription system for continuous event sources.
//
// Subscriptions provide a declarative way to receive events from external
// sources like timers, file watchers, or network connections. The runtime
// manages subscription lifecycles automatically based on what the model
// declares as active.
//
// DIVERGENCE: Uses System.Threading.Channels instead of Rust mpsc channels.
// DIVERGENCE: StopSignal/CancellationToken → FtuiCancellationToken/FtuiCancellationSource.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace FrankenTui.Runtime;

/// <summary>
/// Declarative subscription record consumed by the app runtime's effect system.
/// Distinct from <see cref="ISubscription{M}"/> (the background-thread event source).
/// </summary>
public sealed record Subscription<TMessage>(string Key, Func<IEnumerable<TMessage>> CreateMessages, string EffectKind = "subscription")
{
    public IEnumerable<TMessage> Invoke() => CreateMessages();

    public Subscription<TMessage> WithEffectKind(string effectKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(effectKind);
        return this with { EffectKind = effectKind };
    }
}

/// <summary>A unique identifier for a subscription.</summary>
public readonly record struct SubId(ulong Value)
{
    public static implicit operator SubId(ulong v) => new(v);
    public static implicit operator ulong(SubId s) => s.Value;
    public override string ToString() => Value.ToString();
}

// ── ISubscription ────────────────────────────────────────────────────────

/// <summary>
/// A subscription produces messages from an external event source.
///
/// Subscriptions run on background threads and send messages through
/// the provided channel writer. The runtime manages their lifecycle.
/// </summary>
public interface ISubscription<M> where M : class
{
    /// <summary>Unique identifier for deduplication.</summary>
    SubId Id { get; }

    /// <summary>Start the subscription, sending messages through the writer.</summary>
    void Run(ChannelWriter<M> writer, StopSignal stop);
}

// ── StopSignal / StopTrigger ──────────────────────────────────────────────

/// <summary>
/// Signal for stopping a subscription.
///
/// When the runtime stops a subscription, it sets this signal. The subscription
/// should check it periodically and exit its run loop when set.
/// Backed by FtuiCancellationToken for structured cancellation.
/// </summary>
public sealed class StopSignal
{
    private readonly FtuiCancellationToken _token;

    internal StopSignal(FtuiCancellationToken token)
    {
        _token = token;
    }

    /// <summary>Create a new stop signal pair (signal, trigger).</summary>
    internal static (StopSignal Signal, StopTrigger Trigger) Create()
    {
        var source = new FtuiCancellationSource();
        var signal = new StopSignal(source.Token);
        var trigger = new StopTrigger(source);
        return (signal, trigger);
    }

    /// <summary>Check if the stop signal has been triggered.</summary>
    public bool IsStopped => _token.IsCancelled;

    /// <summary>
    /// Wait for either the stop signal or a timeout.
    /// Returns true if stopped, false if timed out.
    /// </summary>
    public bool WaitTimeout(TimeSpan duration) => _token.WaitTimeout(duration);

    /// <summary>Access the underlying cancellation token.</summary>
    public FtuiCancellationToken CancellationToken => _token;
}

/// <summary>Trigger to stop a subscription from the runtime side.</summary>
internal sealed class StopTrigger
{
    private readonly FtuiCancellationSource _source;

    internal StopTrigger(FtuiCancellationSource source)
    {
        _source = source;
    }

    /// <summary>Signal the subscription to stop.</summary>
    internal void Stop() => _source.Cancel();
}

// ── RunningSubscription ──────────────────────────────────────────────────

internal sealed record SubscriptionFailure(SubId Id, string Error);

internal sealed class SubscriptionRunState
{
    private int _panicked;

    internal bool HasPanicked => Volatile.Read(ref _panicked) != 0;

    internal void MarkPanicked() => Interlocked.Exchange(ref _panicked, 1);
}

internal sealed class RunningSubscription
{
    internal SubId Id { get; }
    private readonly StopTrigger _trigger;
    private readonly SubscriptionRunState _state;
    private Thread? _thread;

    private static readonly TimeSpan StopJoinTimeout = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan StopJoinPoll = TimeSpan.FromMilliseconds(1);

    internal RunningSubscription(
        SubId id,
        StopTrigger trigger,
        Thread thread,
        SubscriptionRunState? state = null)
    {
        Id = id;
        _trigger = trigger;
        _thread = thread;
        _state = state ?? new SubscriptionRunState();
    }

    /// <summary>Returns true if the subscription thread panicked.</summary>
    internal bool HasPanicked => _state.HasPanicked;

    internal void SignalStop()
    {
        _trigger.Stop();
    }

    /// <summary>
    /// Join the subscription thread with a bounded timeout (phase 2).
    /// Returns the thread if it did not finish within the timeout.
    /// </summary>
    internal Thread? JoinBounded()
    {
        var handle = Interlocked.Exchange(ref _thread, null!);
        if (handle == null) return null;
        var sw = Stopwatch.StartNew();

        if (!handle.IsAlive)
        {
            handle.Join();
            return null;
        }

        while (handle.IsAlive)
        {
            if (sw.Elapsed >= StopJoinTimeout)
            {
                return handle; // detached
            }
            Thread.Sleep(StopJoinPoll);
        }

        handle.Join();
        return null;
    }

    /// <summary>Stop the subscription and join its thread if it exits promptly.</summary>
    internal void Stop()
    {
        _trigger.Stop();
        var handle = Interlocked.Exchange(ref _thread, null!);
        if (handle == null) return;

        if (!handle.IsAlive)
        {
            handle.Join();
            return;
        }

        var sw = Stopwatch.StartNew();
        while (handle.IsAlive)
        {
            if (sw.Elapsed >= StopJoinTimeout)
                return; // detached
            Thread.Sleep(StopJoinPoll);
        }
        handle.Join();
    }
}

// ── SubscriptionManager ──────────────────────────────────────────────────

/// <summary>Manages the lifecycle of subscriptions for a program.</summary>
internal sealed class SubscriptionManager<M> where M : class
{
    internal List<RunningSubscription> Active = new();
    private readonly Channel<M> _channel = Channel.CreateUnbounded<M>();
    private readonly Channel<SubscriptionFailure> _failureChannel =
        Channel.CreateUnbounded<SubscriptionFailure>();
    private readonly ChannelWriter<M> _writer;
    private readonly ChannelReader<M> _reader;
    private readonly ChannelWriter<SubscriptionFailure> _failureWriter;
    private readonly ChannelReader<SubscriptionFailure> _failureReader;

    internal SubscriptionManager()
    {
        _writer = _channel.Writer;
        _reader = _channel.Reader;
        _failureWriter = _failureChannel.Writer;
        _failureReader = _failureChannel.Reader;
    }

    internal int ActiveCount => Active.Count;

    /// <summary>
    /// Update the set of active subscriptions.
    ///
    /// Compares new set against currently running subscriptions:
    /// - Starts subscriptions that are new (ID not in active set)
    /// - Stops subscriptions that are no longer declared
    /// - Leaves unchanged subscriptions running
    /// </summary>
    internal void Reconcile(List<ISubscription<M>> subscriptions)
    {
        var sw = Stopwatch.StartNew();
        var newIds = new HashSet<ulong>(subscriptions.Select(s => s.Id.Value));
        var activeCountBefore = Active.Count;

        DebugTrace.Trace($"reconcile: new_ids=[{string.Join(",", newIds)}], active_before={activeCountBefore}");

        // Stop subscriptions that are no longer active (two-phase)
        var remaining = new List<RunningSubscription>();
        var toStop = new List<RunningSubscription>();
        foreach (var running in Active)
        {
            if (newIds.Contains(running.Id.Value))
                remaining.Add(running);
            else
            {
                DebugTrace.Trace($"stopping subscription: id={running.Id}");
                EffectSystem.RecordSubscriptionStop("subscription", running.Id.Value, 0);
                EffectSystem.RecordDynamicsSubStop();
                toStop.Add(running);
            }
        }
        // Phase 1: Signal all removals
        foreach (var r in toStop) r.SignalStop();
        // Phase 2: Join with bounded timeout
        foreach (var r in toStop) r.JoinBounded();
        Active = remaining;

        // Start new subscriptions
        var activeIds = new HashSet<ulong>(Active.Select(r => r.Id.Value));
        foreach (var sub in subscriptions)
        {
            var id = sub.Id.Value;
            if (!activeIds.Add(id))
                continue;

            DebugTrace.Trace($"starting subscription: id={id}");
            EffectSystem.RecordSubscriptionStart("subscription", id);
            EffectSystem.RecordDynamicsSubStart();
            var (signal, trigger) = StopSignal.Create();
            var writer = _writer;
            var failureWriter = _failureWriter;
            var state = new SubscriptionRunState();

            var thread = new Thread(() =>
            {
                try
                {
                    sub.Run(writer, signal);
                }
                catch (Exception ex)
                {
                    state.MarkPanicked();
                    EffectSystem.RecordDynamicsSubPanic();
                    EffectSystem.ErrorEffectPanic("subscription", $"sub_id={id}: {ex.Message}");
                    failureWriter.TryWrite(new SubscriptionFailure(id, ex.Message));
                }
            })
            {
                IsBackground = true,
            };
            thread.Start();

            Active.Add(new RunningSubscription(id, trigger, thread, state));
        }

        DebugTrace.Trace($"reconcile complete: active_after={Active.Count}");
    }

    /// <summary>Drain pending messages from subscriptions.</summary>
    internal List<M> DrainMessages()
    {
        var messages = new List<M>();
        while (_reader.TryRead(out var msg))
            messages.Add(msg);
        return messages;
    }

    /// <summary>Drain subscription failures not yet reported to the model.</summary>
    internal List<SubscriptionFailure> DrainFailures()
    {
        var failures = new List<SubscriptionFailure>();
        while (_failureReader.TryRead(out var failure))
            failures.Add(failure);
        return failures;
    }

    /// <summary>Stop all running subscriptions using two-phase parallel shutdown.</summary>
    internal void StopAll()
    {
        if (Active.Count == 0) return;

        // Phase 1: Signal all
        foreach (var r in Active) r.SignalStop();

        // Phase 2: Join all with bounded timeout
        foreach (var r in Active) r.JoinBounded();

        Active.Clear();
    }
}

// ── Every<M> ──────────────────────────────────────────────────────────────

/// <summary>
/// A subscription that fires at a fixed interval.
/// </summary>
public sealed class Every<M> : ISubscription<M> where M : class
{
    private readonly ulong _id;
    private readonly TimeSpan _interval;
    private readonly Func<M> _makeMsg;

    /// <summary>
    /// Create a tick subscription with the given interval and message factory.
    /// ID is derived from interval to allow deduplication.
    /// </summary>
    public Every(TimeSpan interval, Func<M> makeMsg)
    {
        // Generate stable ID: interval nanos XOR 0x5449_434B ("TICK" magic)
        _id = (ulong)(interval.Ticks * 100) ^ 0x5449_434BUL;
        _interval = interval;
        _makeMsg = makeMsg;
    }

    /// <summary>Create a tick subscription with an explicit ID.</summary>
    public Every(SubId id, TimeSpan interval, Func<M> makeMsg)
    {
        _id = id.Value;
        _interval = interval;
        _makeMsg = makeMsg;
    }

    public SubId Id => new(_id);

    public void Run(ChannelWriter<M> writer, StopSignal stop)
    {
        ulong tickCount = 0;
        DebugTrace.Trace($"Every subscription started: id={_id}, interval={_interval.TotalMilliseconds:F0}ms");
        while (true)
        {
            if (stop.WaitTimeout(_interval))
            {
                DebugTrace.Trace($"Every subscription stopped: id={_id}, sent {tickCount} ticks");
                break;
            }
            tickCount++;
            var msg = _makeMsg();
            if (!writer.TryWrite(msg))
            {
                DebugTrace.Trace($"Every subscription channel closed: id={_id}, sent {tickCount} ticks");
                break;
            }
        }
    }
}
