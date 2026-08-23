// SPDX-License-Identifier: Apache-2.0
// Ported from crates/ftui-render/src/counting_writer.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source SHA-256: D553CEF2312A5B20E8A0D5421AB6A52386C00C092CA90687FC39951941004743.

using System.Diagnostics;

namespace FrankenTui.Render;

/// <summary>Wraps a stream and counts bytes successfully written through it.</summary>
public sealed class CountingWriter<T>
    where T : Stream
{
    private T? _inner;
    private ulong _bytesWritten;

    public CountingWriter(T inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public ulong BytesWritten => _bytesWritten;

    public void ResetCounter() => _bytesWritten = 0;

    public T Inner => AttachedInner;

    /// <summary>
    /// Returns the mutable stream. Writes made directly to it intentionally bypass the counter,
    /// matching Rust's <c>inner_mut</c> escape hatch.
    /// </summary>
    public T InnerMut => AttachedInner;

    /// <summary>
    /// Transfers the wrapped stream out of this object. The source consumes <c>self</c>; the
    /// managed wrapper models that ownership transition by rejecting subsequent operations.
    /// </summary>
    public T IntoInner()
    {
        var inner = AttachedInner;
        _inner = null;
        return inner;
    }

    /// <summary>Writes an entire managed stream segment and returns its byte count.</summary>
    public int Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        AttachedInner.Write(buffer, offset, count);
        _bytesWritten = unchecked(_bytesWritten + (ulong)count);
        return count;
    }

    /// <summary>Writes an entire span and returns its byte count.</summary>
    public int Write(ReadOnlySpan<byte> buffer)
    {
        AttachedInner.Write(buffer);
        _bytesWritten = unchecked(_bytesWritten + (ulong)buffer.Length);
        return buffer.Length;
    }

    public void WriteAll(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        Write(buffer.AsSpan());
    }

    public void WriteAll(ReadOnlySpan<byte> buffer) => Write(buffer);

    public void Flush() => AttachedInner.Flush();

    private T AttachedInner => _inner ?? throw new InvalidOperationException(
        "The inner stream has already been transferred by IntoInner().");
}

/// <summary>Statistics from one present operation.</summary>
public sealed record PresentStats(
    ulong BytesEmitted,
    ulong CellsChanged,
    ulong RunCount,
    TimeSpan Duration)
{
    public PresentStats(ulong bytesEmitted, int cellsChanged, int runCount, TimeSpan duration)
        : this(
            bytesEmitted,
            NonNegative(cellsChanged, nameof(cellsChanged)),
            NonNegative(runCount, nameof(runCount)),
            duration)
    {
    }

    public static PresentStats Default { get; } = new(0, 0UL, 0UL, TimeSpan.Zero);

    public double BytesPerCell => CellsChanged == 0 ? 0.0 : BytesEmitted / (double)CellsChanged;

    public double BytesPerRun => RunCount == 0 ? 0.0 : BytesEmitted / (double)RunCount;

    public bool WithinBudget => BytesEmitted <= PresentBudget.ExpectedMaxBytes(CellsChanged, RunCount);

    /// <summary>The Rust implementation conditionally traces; this is a no-op without a sink.</summary>
    public void Log()
    {
    }

    private static ulong NonNegative(int value, string parameterName)
    {
        return value >= 0
            ? (ulong)value
            : throw new ArgumentOutOfRangeException(parameterName, value, "Counts cannot be negative.");
    }
}

public static class PresentBudget
{
    public const ulong BytesPerCellMax = 40;
    public const ulong SyncOverhead = 20;
    public const ulong BytesPerCursorMove = 10;

    public static ulong ExpectedMaxBytes(ulong cellsChanged, ulong runs)
    {
        return checked(
            (runs * BytesPerCursorMove)
            + (cellsChanged * BytesPerCellMax)
            + SyncOverhead);
    }

    public static ulong ExpectedMaxBytes(int cellsChanged, int runs)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cellsChanged);
        ArgumentOutOfRangeException.ThrowIfNegative(runs);
        return ExpectedMaxBytes((ulong)cellsChanged, (ulong)runs);
    }
}

/// <summary>Monotonic timer and dimensions for one present operation.</summary>
public sealed class StatsCollector
{
    private readonly long _startedAt;
    private readonly ulong _cellsChanged;
    private readonly ulong _runCount;
    private bool _finished;

    public StatsCollector(ulong cellsChanged, ulong runCount)
    {
        _cellsChanged = cellsChanged;
        _runCount = runCount;
        _startedAt = Stopwatch.GetTimestamp();
    }

    public StatsCollector(int cellsChanged, int runCount)
        : this(
            cellsChanged >= 0
                ? (ulong)cellsChanged
                : throw new ArgumentOutOfRangeException(nameof(cellsChanged)),
            runCount >= 0
                ? (ulong)runCount
                : throw new ArgumentOutOfRangeException(nameof(runCount)))
    {
    }

    public static StatsCollector Start(ulong cellsChanged, ulong runCount) => new(cellsChanged, runCount);

    public static StatsCollector Start(int cellsChanged, int runCount) => new(cellsChanged, runCount);

    public PresentStats Finish(ulong bytesEmitted)
    {
        if (_finished)
        {
            throw new InvalidOperationException("StatsCollector.Finish may only be called once.");
        }

        _finished = true;
        return new PresentStats(
            bytesEmitted,
            _cellsChanged,
            _runCount,
            Stopwatch.GetElapsedTime(_startedAt));
    }
}
