// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/retry.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Retry policies and timeout-enforced task helpers.
//
// Provides RetryPolicy for configurable retry-with-backoff and
// task_with_timeout / task_with_retry constructors that wrap
// Cmd.BackgroundTask with deterministic lifecycle guarantees.
//
// Determinism:
// Backoff delays use fixed formulas (no jitter/randomness) so that
// replay-based determinism tests can reproduce exact timing sequences.

using System.Diagnostics;

namespace FrankenTui.Runtime;

// ── Constants ────────────────────────────────────────────────────────────

static class RetryConstants
{
    public static readonly TimeSpan TaskThreadJoinTimeout = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan TaskThreadJoinPoll = TimeSpan.FromMilliseconds(5);
}

// ── Saturating duration helpers ──────────────────────────────────────────

static class RetryDuration
{
    public static TimeSpan FromMillisSaturating(ulong millis)
    {
        if (millis >= (ulong)TimeSpan.MaxValue.TotalMilliseconds)
            return TimeSpan.MaxValue;
        return TimeSpan.FromMilliseconds(millis);
    }
}

// ── Thread join helpers ──────────────────────────────────────────────────

static class RetryJoin
{
    public static void JoinTaskThread(Thread handle)
    {
        handle.Join();
    }

    public static void JoinTaskThreadBounded(Thread handle, string taskName)
    {
        var sw = Stopwatch.StartNew();
        while (handle.IsAlive)
        {
            if (sw.Elapsed >= RetryConstants.TaskThreadJoinTimeout)
            {
                // DIVERGENCE: Upstream uses tracing::warn! for this.
                // We emit to stderr since the tracing layer may not be active.
                Console.Error.WriteLine(
                    $"Timed-out worker thread '{taskName}' did not exit within " +
                    $"{RetryConstants.TaskThreadJoinTimeout.TotalMilliseconds:F0}ms; detaching");
                return;
            }
            Thread.Sleep(RetryConstants.TaskThreadJoinPoll);
        }
        JoinTaskThread(handle);
    }
}

// ── BackoffStrategy ──────────────────────────────────────────────────────

/// <summary>
/// Backoff strategy for retry delays.
/// Mirrors upstream BackoffStrategy enum with associated data.
/// </summary>
public abstract record BackoffStrategy
{
    /// <summary>Fixed delay between retries.</summary>
    public sealed record Fixed(ulong DelayMs) : BackoffStrategy;

    /// <summary>Exponential backoff: base_ms * 2^attempt, capped at max_ms.</summary>
    public sealed record Exponential(ulong BaseMs, ulong MaxMs) : BackoffStrategy;

    /// <summary>Linear backoff: base_ms * (attempt + 1), capped at max_ms.</summary>
    public sealed record Linear(ulong BaseMs, ulong MaxMs) : BackoffStrategy;
}

// ── RetryPolicy ──────────────────────────────────────────────────────────

/// <summary>
/// A retry policy with configurable attempts and backoff.
/// </summary>
public sealed class RetryPolicy
{
    /// <summary>Maximum number of retry attempts (0 = no retries, just the initial attempt).</summary>
    public uint MaxRetries { get; }

    /// <summary>Backoff strategy between retries.</summary>
    public BackoffStrategy Backoff { get; }

    /// <summary>Create a new retry policy.</summary>
    public RetryPolicy(uint maxRetries, BackoffStrategy backoff)
    {
        MaxRetries = maxRetries;
        Backoff = backoff;
    }

    /// <summary>No retries — execute once.</summary>
    public static RetryPolicy NoRetry() => new(0, new BackoffStrategy.Fixed(0));

    /// <summary>Compute the delay before the given attempt (0-indexed).</summary>
    public TimeSpan Delay(uint attempt)
    {
        return Backoff switch
        {
            BackoffStrategy.Fixed f =>
                TimeSpan.FromMilliseconds(f.DelayMs),

            BackoffStrategy.Exponential e =>
                DelayExponential(attempt, e.BaseMs, e.MaxMs),

            BackoffStrategy.Linear l =>
                DelayLinear(attempt, l.BaseMs, l.MaxMs),

            _ => TimeSpan.Zero,
        };
    }

    private static TimeSpan DelayExponential(uint attempt, ulong baseMs, ulong maxMs)
    {
        const ulong maxTimeSpanMs = 922337203685477UL;
        ulong multiplier = attempt >= 63 ? ulong.MaxValue : (1UL << (int)attempt);
        if (multiplier == 0 && attempt < 63) multiplier = 1;
        ulong delay;
        try { delay = checked(baseMs * multiplier); }
        catch (OverflowException) { delay = ulong.MaxValue; }
        if (delay > maxMs) delay = maxMs;
        if (delay > maxTimeSpanMs) delay = maxTimeSpanMs;
        return TimeSpan.FromMilliseconds(delay);
    }

    private static TimeSpan DelayLinear(uint attempt, ulong baseMs, ulong maxMs)
    {
        const ulong maxTimeSpanMs = 922337203685477UL;
        ulong factor = (ulong)attempt + 1;
        ulong delay;
        try { delay = checked(baseMs * factor); }
        catch (OverflowException) { delay = ulong.MaxValue; }
        if (delay > maxMs) delay = maxMs;
        if (delay > maxTimeSpanMs) delay = maxTimeSpanMs;
        return TimeSpan.FromMilliseconds(delay);
    }

    /// <summary>Total maximum delay across all retries (for timeout budgeting).</summary>
    public TimeSpan TotalMaxDelay()
    {
        const ulong maxDurationMs = 922337203685477; // TimeSpan.MaxValue.TotalMilliseconds truncated to ulong

        return Backoff switch
        {
            BackoffStrategy.Fixed f =>
                RetryDuration.FromMillisSaturating(
                    SaturatedMul(f.DelayMs, MaxRetries, maxDurationMs)),

            BackoffStrategy.Linear l =>
                TotalMaxDelayLinear(l.BaseMs, l.MaxMs, maxDurationMs),

            BackoffStrategy.Exponential e =>
                TotalMaxDelayExponential(e.BaseMs, e.MaxMs, maxDurationMs),

            _ => TimeSpan.Zero,
        };
    }

    private TimeSpan TotalMaxDelayLinear(ulong baseMs, ulong maxMs, ulong maxDurationMs)
    {
        if (MaxRetries == 0 || baseMs == 0 || maxMs == 0)
            return TimeSpan.Zero;

        ulong retryCount = MaxRetries;
        ulong uncappedTerms = Math.Min(retryCount, maxMs / baseMs);
        ulong arithmeticSum = uncappedTerms * (uncappedTerms + 1) / 2;
        ulong totalMs = SaturatedMul(baseMs, arithmeticSum, maxDurationMs);

        ulong cappedTerms = retryCount - uncappedTerms;
        totalMs = SaturatedAdd(totalMs, SaturatedMul(maxMs, cappedTerms, maxDurationMs), maxDurationMs);
        return RetryDuration.FromMillisSaturating(totalMs);
    }

    private TimeSpan TotalMaxDelayExponential(ulong baseMs, ulong maxMs, ulong maxDurationMs)
    {
        if (MaxRetries == 0 || baseMs == 0 || maxMs == 0)
            return TimeSpan.Zero;

        ulong totalMs = 0;
        uint attempt = 0;
        while (attempt < MaxRetries)
        {
            ulong multiplier = attempt >= 63 ? ulong.MaxValue : (1UL << (int)attempt);
            if (multiplier == 0 && attempt < 63) multiplier = 1;
            ulong delayMs;
            try { delayMs = checked(baseMs * multiplier); }
            catch (OverflowException) { delayMs = ulong.MaxValue; }
            if (delayMs > maxMs) delayMs = maxMs;

            totalMs = SaturatedAdd(totalMs, delayMs, maxDurationMs);
            attempt = attempt == uint.MaxValue ? attempt : attempt + 1;

            if (delayMs == maxMs)
            {
                uint remaining = MaxRetries - attempt; // wraps to ~0 if attempt > MaxRetries
                totalMs = SaturatedAdd(totalMs, SaturatedMul(maxMs, remaining, maxDurationMs), maxDurationMs);
                break;
            }
        }
        return RetryDuration.FromMillisSaturating(totalMs);
    }

    private static ulong SaturatedMul(ulong a, ulong b, ulong cap)
    {
        if (a == 0 || b == 0) return 0;
        ulong result;
        try { result = checked(a * b); }
        catch (OverflowException) { return cap; }
        return Math.Min(result, cap);
    }

    private static ulong SaturatedAdd(ulong a, ulong b, ulong cap)
    {
        ulong result;
        try { result = checked(a + b); }
        catch (OverflowException) { return cap; }
        return Math.Min(result, cap);
    }
}

// ── Task constructors ────────────────────────────────────────────────────

/// <summary>
/// Create a Cmd.BackgroundTask that enforces a cooperative timeout.
///
/// The worker closure receives an FtuiCancellationToken and must honor it for
/// timely timeout teardown. On timeout, the runtime requests cancellation and
/// returns on_timeout; any late worker result is discarded.
/// </summary>
public static class RetryTasks
{
    /// <summary>
    /// Create a Cmd that enforces a cooperative timeout.
    /// </summary>
    public static Cmd<T> TaskWithTimeout<T>(
        TimeSpan timeout,
        Func<FtuiCancellationToken, T> f,
        T onTimeout)
    {
        return Cmd<T>.TaskCmd(() =>
        {
            var source = new FtuiCancellationSource();
            var token = source.Token;
            var result = default(T);
            var done = false;

            var thread = new Thread(() =>
            {
                result = f(token);
                done = true;
            });
            thread.Start();

            if (!thread.Join(timeout))
            {
                source.Cancel();
                RetryJoin.JoinTaskThreadBounded(thread, "task_with_timeout");
                return onTimeout;
            }
            return result;
        });
    }

    /// <summary>
    /// Create a Cmd.BackgroundTask with a named spec and cooperative timeout.
    /// </summary>
    public static Cmd<T> TaskWithTimeoutNamed<T>(
        string name,
        TimeSpan timeout,
        Func<FtuiCancellationToken, T> f,
        T onTimeout)
    {
        var spec = new TaskSpec().WithName(name);
        return Cmd<T>.TaskWithSpec(spec, () =>
        {
            var source = new FtuiCancellationSource();
            var token = source.Token;
            var result = default(T);
            var done = false;

            var thread = new Thread(() =>
            {
                result = f(token);
                done = true;
            });
            thread.Start();

            if (!thread.Join(timeout))
            {
                source.Cancel();
                RetryJoin.JoinTaskThreadBounded(thread, "task_with_timeout_named");
                return onTimeout;
            }
            return result;
        });
    }

    /// <summary>
    /// Create a Cmd.BackgroundTask that retries on failure with the given policy.
    ///
    /// The f closure returns (success, errorMessage). On success, the message is
    /// returned immediately. On failure, the task retries according to the policy,
    /// sleeping between attempts. After all retries are exhausted, onExhaust
    /// is called with the last error to produce a fallback message.
    /// </summary>
    public static Cmd<T> TaskWithRetry<T>(
        RetryPolicy policy,
        Func<(bool Success, string Error)> f,
        Func<string, T> onExhaust)
    {
        return Cmd<T>.TaskCmd(() =>
        {
            string lastErr = "";
            for (uint attempt = 0; attempt <= policy.MaxRetries; attempt++)
            {
                var (success, err) = f();
                if (success) return default!; // DIVERGENCE: result type from closure pattern differs from upstream
                lastErr = err;
                if (attempt < policy.MaxRetries)
                    Thread.Sleep(policy.Delay(attempt));
            }
            return onExhaust(lastErr);
        });
    }

    /// <summary>
    /// Create a Cmd.BackgroundTask with both retry and timeout.
    ///
    /// Each individual attempt is bounded by per_attempt_timeout. The worker
    /// receives an FtuiCancellationToken and must honor it for timely timeout
    /// teardown. The total number of attempts is governed by the retry policy.
    /// </summary>
    public static Cmd<T> TaskWithRetryAndTimeout<T>(
        RetryPolicy policy,
        TimeSpan perAttemptTimeout,
        Func<FtuiCancellationToken, (bool Success, string Error)> f,
        Func<string, T> onExhaust)
    {
        return Cmd<T>.TaskCmd(() =>
        {
            string lastErr = "";
            for (uint attempt = 0; attempt <= policy.MaxRetries; attempt++)
            {
                var source = new FtuiCancellationSource();
                var token = source.Token;
                var result = (Success: false, Error: "");
                var done = false;

                var thread = new Thread(() =>
                {
                    result = f(token);
                    done = true;
                });
                thread.Start();

                if (!thread.Join(perAttemptTimeout))
                {
                    source.Cancel();
                    RetryJoin.JoinTaskThreadBounded(thread, "task_with_retry_and_timeout");
                    lastErr = "timeout";
                }
                else
                {
                    RetryJoin.JoinTaskThread(thread);
                    if (result.Success)
                        return default!; // DIVERGENCE: result type differs from upstream
                    lastErr = result.Error;
                }

                if (attempt < policy.MaxRetries)
                    Thread.Sleep(policy.Delay(attempt));
            }
            return onExhaust(lastErr);
        });
    }
}
