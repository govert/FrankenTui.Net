using System.Diagnostics;
using System.Threading;

namespace FrankenTui.Runtime;

public sealed record QueueTelemetry(
    long Enqueued,
    long Processed,
    long Dropped,
    long HighWater,
    long InFlight);

public sealed record RuntimeDynamics(
    long CommandEffects,
    long CommandCancellations,
    long CommandFailures,
    long SubscriptionStarts,
    long SubscriptionStops,
    long SubscriptionEffects,
    long SubscriptionCancellations,
    long SubscriptionFailures,
    long SubscriptionMessages,
    long Reconciles,
    long ReconcileDurationUs,
    long QueueEnqueued,
    long QueueProcessed,
    long QueueDropped);

public static class EffectSystem
{
    private static long _commandEffects;
    private static long _commandCancellations;
    private static long _commandFailures;
    private static long _subscriptionStarts;
    private static long _subscriptionStops;
    private static long _subscriptionEffects;
    private static long _subscriptionCancellations;
    private static long _subscriptionFailures;
    private static long _subscriptionMessages;
    private static long _queueEnqueued;
    private static long _queueProcessed;
    private static long _queueDropped;
    private static long _queueHighWater;
    private static long _reconcileCount;
    private static long _reconcileDurationUs;

    public static long CommandEffectsTotal => Interlocked.Read(ref _commandEffects);

    public static long SubscriptionEffectsTotal => Interlocked.Read(ref _subscriptionEffects);

    public static long EffectsExecutedTotal => CommandEffectsTotal + SubscriptionEffectsTotal;

    public static QueueTelemetry SnapshotQueueTelemetry()
    {
        var enqueued = Interlocked.Read(ref _queueEnqueued);
        var processed = Interlocked.Read(ref _queueProcessed);
        var dropped = Interlocked.Read(ref _queueDropped);
        return new QueueTelemetry(
            enqueued,
            processed,
            dropped,
            Interlocked.Read(ref _queueHighWater),
            Math.Max(enqueued - processed - dropped, 0));
    }

    public static RuntimeDynamics SnapshotRuntimeDynamics() =>
        new(
            CommandEffectsTotal,
            Interlocked.Read(ref _commandCancellations),
            Interlocked.Read(ref _commandFailures),
            Interlocked.Read(ref _subscriptionStarts),
            Interlocked.Read(ref _subscriptionStops),
            SubscriptionEffectsTotal,
            Interlocked.Read(ref _subscriptionCancellations),
            Interlocked.Read(ref _subscriptionFailures),
            Interlocked.Read(ref _subscriptionMessages),
            Interlocked.Read(ref _reconcileCount),
            Interlocked.Read(ref _reconcileDurationUs),
            Interlocked.Read(ref _queueEnqueued),
            Interlocked.Read(ref _queueProcessed),
            Interlocked.Read(ref _queueDropped));

    public static TResult TraceCommandEffect<TResult>(string effectKind, Func<TResult> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentException.ThrowIfNullOrWhiteSpace(effectKind);

        var started = Stopwatch.GetTimestamp();
        try
        {
            return callback();
        }
        catch (OperationCanceledException)
        {
            Interlocked.Increment(ref _commandCancellations);
            throw;
        }
        catch
        {
            Interlocked.Increment(ref _commandFailures);
            throw;
        }
        finally
        {
            Interlocked.Increment(ref _commandEffects);
            _ = ElapsedMicroseconds(started);
        }
    }

    public static IReadOnlyList<TMessage> TraceSubscriptionEffect<TMessage>(Subscription<TMessage> subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        var started = Stopwatch.GetTimestamp();
        try
        {
            var messages = subscription.Invoke().ToArray();
            Interlocked.Add(ref _subscriptionMessages, messages.Length);
            return messages;
        }
        catch (OperationCanceledException)
        {
            Interlocked.Increment(ref _subscriptionCancellations);
            throw;
        }
        catch
        {
            Interlocked.Increment(ref _subscriptionFailures);
            throw;
        }
        finally
        {
            Interlocked.Increment(ref _subscriptionEffects);
            _ = ElapsedMicroseconds(started);
        }
    }

    public static void RecordQueueEnqueue(int currentDepth)
    {
        Interlocked.Increment(ref _queueEnqueued);
        UpdateHighWater(currentDepth);
    }

    public static void RecordQueueProcessed(int currentDepth)
    {
        Interlocked.Increment(ref _queueProcessed);
        UpdateHighWater(currentDepth);
    }

    public static void RecordQueueDrop(int droppedCount)
    {
        if (droppedCount <= 0)
        {
            return;
        }

        Interlocked.Add(ref _queueDropped, droppedCount);
    }

    public static void RecordReconcile(TimeSpan duration)
    {
        Interlocked.Increment(ref _reconcileCount);
        Interlocked.Add(ref _reconcileDurationUs, (long)Math.Round(duration.TotalMilliseconds * 1000d, MidpointRounding.AwayFromZero));
    }

    public static void RecordSubscriptionStart(int count = 1)
    {
        if (count <= 0)
        {
            return;
        }

        Interlocked.Add(ref _subscriptionStarts, count);
    }

    public static void RecordSubscriptionStop(int count = 1)
    {
        if (count <= 0)
        {
            return;
        }

        Interlocked.Add(ref _subscriptionStops, count);
    }

    private static void UpdateHighWater(int currentDepth)
    {
        long observed;
        do
        {
            observed = Interlocked.Read(ref _queueHighWater);
            if (currentDepth <= observed)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref _queueHighWater, currentDepth, observed) != observed);
    }

    private static long ElapsedMicroseconds(long startedTicks)
    {
        var elapsedTicks = Stopwatch.GetTimestamp() - startedTicks;
        return (long)Math.Round(elapsedTicks * 1_000_000d / Stopwatch.Frequency, MidpointRounding.AwayFromZero);
    }
}
