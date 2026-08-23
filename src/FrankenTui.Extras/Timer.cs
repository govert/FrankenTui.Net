// Upstream source: .external/frankentui/crates/ftui-extras/src/timer.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Faithful 1-1 port.

using System;

namespace FrankenTui.Extras;

/// <summary>Display format for the timer. Port of DisplayFormat enum.</summary>
public enum TimerDisplayFormat { Compact, Clock }

/// <summary>Countdown timer. Port of Timer struct.</summary>
public sealed class Timer
{
    private TimeSpan _initial;
    private TimeSpan _remaining;
    private TimeSpan _interval;
    private bool _running;
    private TimerDisplayFormat _format;

    /// <summary>Create a new timer with default 1-second tick interval.</summary>
    public Timer(TimeSpan duration) : this(duration, TimeSpan.FromSeconds(1)) { }

    /// <summary>Create a new timer with the given duration and tick interval.</summary>
    public Timer(TimeSpan duration, TimeSpan interval)
    {
        _initial = duration;
        _remaining = duration;
        _interval = interval;
        _running = false;
        _format = TimerDisplayFormat.Compact;
    }

    /// <summary>Set the display format.</summary>
    public Timer WithFormat(TimerDisplayFormat format) { _format = format; return this; }

    /// <summary>Whether the timer is currently running.</summary>
    public bool IsRunning => _running && !IsFinished;

    /// <summary>Whether the countdown has reached zero.</summary>
    public bool IsFinished => _remaining <= TimeSpan.Zero;

    /// <summary>The remaining time.</summary>
    public TimeSpan Remaining => _remaining;

    /// <summary>The initial duration.</summary>
    public TimeSpan Initial => _initial;

    /// <summary>The tick interval.</summary>
    public TimeSpan Interval => _interval;

    /// <summary>Progress as a fraction from 0.0 to 1.0.</summary>
    public double Progress => _initial <= TimeSpan.Zero ? 1.0
        : (_initial - _remaining).TotalSeconds / _initial.TotalSeconds;

    /// <summary>Start the timer.</summary>
    public void Start() { _running = true; }

    /// <summary>Stop (pause) the timer.</summary>
    public void Stop() { _running = false; }

    /// <summary>Toggle between running and stopped.</summary>
    public void Toggle() { if (!IsFinished) _running = !_running; }

    /// <summary>Reset to initial duration. Does not change running state.</summary>
    public void Reset() { _remaining = _initial; }

    /// <summary>Advance by one tick interval. Returns true if just finished.</summary>
    public bool TickOnce()
    {
        if (!_running || IsFinished) return false;
        bool wasNonzero = _remaining > TimeSpan.Zero;
        _remaining -= _interval;
        if (_remaining < TimeSpan.Zero) _remaining = TimeSpan.Zero;
        return wasNonzero && _remaining == TimeSpan.Zero;
    }

    /// <summary>Advance by an arbitrary duration. Returns true if just finished.</summary>
    public bool Tick(TimeSpan delta)
    {
        if (!_running || IsFinished) return false;
        bool wasNonzero = _remaining > TimeSpan.Zero;
        _remaining -= delta;
        if (_remaining < TimeSpan.Zero) _remaining = TimeSpan.Zero;
        return wasNonzero && _remaining == TimeSpan.Zero;
    }

    /// <summary>Render the remaining time as a string.</summary>
    public string View() => _format switch
    {
        TimerDisplayFormat.Compact => FormatCompact(_remaining),
        TimerDisplayFormat.Clock => FormatClock(_remaining),
        _ => FormatCompact(_remaining),
    };

    // ── format_compact ────────────────────────────────────────────────
    static string FormatCompact(TimeSpan d)
    {
        long totalNanos = (long)d.TotalNanoseconds;
        if (totalNanos == 0) return "0s";
        long totalSecs = (long)d.TotalSeconds;
        long subsecNanos = totalNanos % 1_000_000_000;

        if (totalSecs == 0)
        {
            long micros = totalNanos / 1000;
            if (micros >= 1000)
            {
                long millis = totalNanos / 1_000_000;
                long remMicros = micros % 1000;
                if (remMicros == 0) return $"{millis}ms";
                string dec = (totalNanos % 1_000_000).ToString("D6").TrimEnd('0');
                return dec.Length == 0 ? $"{millis}ms" : $"{millis}.{dec}ms";
            }
            else if (micros >= 1)
            {
                long nanos = totalNanos % 1000;
                if (nanos == 0) return $"{micros}\u00B5s";
                string dec = nanos.ToString("D3").TrimEnd('0');
                return $"{micros}.{dec}\u00B5s";
            }
            return $"{totalNanos}ns";
        }

        long hours = totalSecs / 3600, minutes = (totalSecs % 3600) / 60, seconds = totalSecs % 60;
        string sub = subsecNanos > 0 ? subsecNanos.ToString("D9").TrimEnd('0') : "";
        if (sub.Length > 0) sub = "." + sub;
        if (hours > 0) return $"{hours}h{minutes}m{seconds}{sub}s";
        if (minutes > 0) return $"{minutes}m{seconds}{sub}s";
        return $"{seconds}{sub}s";
    }

    // ── format_clock ──────────────────────────────────────────────────
    static string FormatClock(TimeSpan d)
    {
        long totalSecs = (long)d.TotalSeconds;
        long hours = totalSecs / 3600, minutes = (totalSecs % 3600) / 60, seconds = totalSecs % 60;
        return hours > 0 ? $"{hours}:{minutes:D2}:{seconds:D2}" : $"{minutes:D2}:{seconds:D2}";
    }
}
