// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/debug_trace.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Zero-cost debug tracing controlled by environment variable.
//
// Enable runtime debug output by setting FTUI_DEBUG_TRACE=1 before launching
// your application. When disabled (the default), the trace checks compile
// down to a single static bool load with no other overhead.

using System.Diagnostics;

namespace FrankenTui.Runtime;

/// <summary>
/// Zero-cost debug tracing controlled by environment variable FTUI_DEBUG_TRACE.
///
/// When disabled (default), IsEnabled compiles to a single static bool check.
/// The Trace method writes timestamped messages to stderr when enabled.
/// </summary>
public static class DebugTrace
{
    private static readonly bool _enabled = InitEnabled();
    private static readonly long _startTimestamp = Stopwatch.GetTimestamp();
    private static readonly double _tickToMs = 1000.0 / Stopwatch.Frequency;

    private static bool InitEnabled()
    {
        var val = Environment.GetEnvironmentVariable("FTUI_DEBUG_TRACE");
        return val == "1" || string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if debug tracing is enabled.
    /// After initialization, this is effectively a single static bool load.
    /// </summary>
    public static bool IsEnabled => _enabled;

    /// <summary>
    /// Get elapsed time since program start in milliseconds.
    /// Useful for correlating debug output across threads.
    /// </summary>
    public static long ElapsedMs
    {
        get
        {
            long delta = Stopwatch.GetTimestamp() - _startTimestamp;
            return (long)(delta * _tickToMs);
        }
    }

    /// <summary>
    /// Conditionally print debug trace output to stderr.
    /// When FTUI_DEBUG_TRACE is not set, this is a near-zero-cost no-op
    /// (single static bool check).
    /// </summary>
    public static void Trace(string message)
    {
        if (!_enabled) return;
        Console.Error.WriteLine($"[FTUI {ElapsedMs,8}ms] {message}");
    }
}
