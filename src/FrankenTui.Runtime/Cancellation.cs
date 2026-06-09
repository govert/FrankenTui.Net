// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/cancellation.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// DIVERGENCE: Renamed CancellationToken → FtuiCancellationToken and
// CancellationSource → FtuiCancellationSource to avoid clash with
// System.Threading.CancellationToken/CancellationTokenSource which are
// pervasive in any async .NET code.
//
// Cooperative cancellation tokens for commands and tasks.
//
// FtuiCancellationToken provides a thread-safe, cloneable signal that tasks can
// poll to detect cooperative cancellation requests. It extends the
// subscription StopSignal pattern to the command/task domain, enabling
// bounded-lifetime effects and graceful teardown.

using System.Diagnostics;

namespace FrankenTui.Runtime;

/// <summary>
/// A thread-safe, cloneable cancellation token.
///
/// Tasks and effects receive a token and poll <see cref="IsCancelled"/>
/// to detect cancellation requests. Tokens are cheap to clone and share
/// across thread boundaries.
/// </summary>
public sealed class FtuiCancellationToken
{
    internal FtuiCancellationToken(CancellationInner inner)
    {
        _inner = inner;
    }

    private readonly CancellationInner _inner;

    /// <summary>Returns true if cancellation has been requested.</summary>
    public bool IsCancelled => _inner._cancelled != 0;

    /// <summary>
    /// Block until either cancellation is requested or the timeout elapses.
    ///
    /// Returns true if cancelled, false if timed out.
    /// </summary>
    public bool WaitTimeout(TimeSpan duration)
    {
        if (IsCancelled)
            return true;

        var sw = Stopwatch.StartNew();
        var remaining = duration;

        while (true)
        {
            if (IsCancelled)
                return true;

            lock (_inner._notifyLock)
            {
                if (IsCancelled)
                    return true;

                bool signalled = Monitor.Wait(_inner._notifyLock, remaining);
                if (IsCancelled)
                    return true;

                if (!signalled)
                    return false;

                var elapsed = sw.Elapsed;
                if (elapsed >= duration)
                    return false;

                remaining = duration - elapsed;
            }
        }
    }
}

/// <summary>
/// The control handle that triggers cancellation.
///
/// Dropping the source does NOT cancel the token — call <see cref="Cancel"/>
/// explicitly. This prevents accidental cancellation on scope exit.
/// </summary>
public sealed class FtuiCancellationSource
{
    private readonly CancellationInner _inner;

    /// <summary>Create a new cancellation source with an uncancelled token.</summary>
    public FtuiCancellationSource()
    {
        _inner = new CancellationInner();
    }

    /// <summary>Obtain a cloneable token that observes this source's state.</summary>
    public FtuiCancellationToken Token => new FtuiCancellationToken(_inner);

    /// <summary>
    /// Signal cancellation. All tokens derived from this source will observe
    /// IsCancelled == true and any pending WaitTimeout calls will wake.
    /// </summary>
    public void Cancel()
    {
        Interlocked.Exchange(ref _inner._cancelled, 1);
        lock (_inner._notifyLock)
        {
            Monitor.PulseAll(_inner._notifyLock);
        }
    }

    /// <summary>Check whether cancellation has already been requested.</summary>
    public bool IsCancelled => _inner._cancelled != 0;
}

internal sealed class CancellationInner
{
    internal int _cancelled; // volatile via Interlocked
    internal readonly object _notifyLock = new();
}
