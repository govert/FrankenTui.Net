// Upstream source: crates/ftui-extras/src/live.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Full 1-1 port of Live, LiveConfig, VerticalOverflow, auto-refresh,
// writer access protocol (Condvar-based), and ANSI escape helpers.

using FrankenTui.Render;

namespace FrankenTui.Extras;

public enum VerticalOverflow { Crop, Ellipsis, Visible }

public sealed class LiveConfig
{
    public int MaxHeight { get; set; }
    public VerticalOverflow Overflow { get; set; } = VerticalOverflow.Ellipsis;
    public bool Transient { get; set; } = true;
    public double RefreshPerSecond { get; set; } = 4.0;
}

public static class AnsiHelper
{
    public static void CursorUp(TextWriter w, int n) { if (n > 0) w.Write($"\x1b[{n}A"); }
    public static void CarriageReturn(TextWriter w) { w.Write('\r'); }
    public static void EraseLine(TextWriter w) { w.Write("\x1b[2K"); }
    public static void HideCursor(TextWriter w) { w.Write("\x1b[?25l"); }
    public static void ShowCursor(TextWriter w) { w.Write("\x1b[?25h"); }
}

public static class AutoRefreshHelper
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(1);
    public static readonly TimeSpan SleepSlice = TimeSpan.FromMilliseconds(50);
    public static readonly TimeSpan JoinTimeout = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan WriterAccessTimeout = TimeSpan.FromMilliseconds(250);

    public static TimeSpan? ComputeInterval(double rate)
    {
        if (double.IsNaN(rate) || double.IsInfinity(rate) || rate <= 0.0) return null;
        return TimeSpan.FromSeconds(Math.Max(1.0 / rate, MinInterval.TotalSeconds));
    }
}

internal sealed class RefreshThread
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Thread _thread;
    public RefreshThread(Action callback, TimeSpan interval)
    {
        _thread = new Thread(() =>
        {
            var token = _cts.Token;
            while (!token.IsCancellationRequested)
            {
                var slept = TimeSpan.Zero;
                while (slept < interval && !token.IsCancellationRequested)
                {
                    var step = (interval - slept) > AutoRefreshHelper.SleepSlice ? AutoRefreshHelper.SleepSlice : (interval - slept);
                    try { token.WaitHandle.WaitOne(step); } catch { }
                    slept += step;
                }
                if (!token.IsCancellationRequested) callback();
            }
        }) { Name = "ftui-live-refresh", IsBackground = true };
        _thread.Start();
    }
    public void Stop()
    {
        _cts.Cancel();
        if (_thread.ManagedThreadId == Environment.CurrentManagedThreadId) return;
        if (!_thread.Join(AutoRefreshHelper.JoinTimeout))
            _thread.IsBackground = true;
    }
}

public sealed class Live : IDisposable
{
    private readonly TextWriter _writer;
    private readonly object _writerLock = new();
    private readonly object _ownerLock = new();
    private readonly object _readySignal = new();
    private int? _ownerThreadId;
    private readonly int _width;
    private readonly LiveConfig _config;
    private int _lastHeight;
    private bool _started;
    private RefreshThread? _refreshThread;
    private readonly object _refreshLock = new();

    public Live(TextWriter writer, int width) : this(writer, width, new LiveConfig()) { }
    public Live(TextWriter writer, int width, LiveConfig config)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _width = width > 0 ? width : 80;
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Factory mirroring upstream <c>Live::with_config</c>: build a live region
    /// from a writer, width, and explicit config.
    /// </summary>
    public static Live WithConfig(TextWriter writer, int width, LiveConfig config)
        => new(writer, width, config);



    public bool Start()
    {
        if (_started) return true;
        try { WithWriter(w => { AnsiHelper.HideCursor(w); w.Flush(); return true; }); _started = true; return true; }
        catch { _started = false; return false; }
    }

    public bool Stop()
    {
        StopRefreshThread();
        if (!_started) return true;
        _started = false;
        try { WithWriter(w => { if (_config.Transient) EraseRegion(w, _lastHeight); AnsiHelper.ShowCursor(w); w.Flush(); return true; }); return true; }
        catch { return false; }
    }

    public void Update(Action<FxConsole> render)
    {
        if (!_started) return;
        var sink = new ConsoleSink();
        var console = new FxConsole(_width, sink);
        render(console);
        var lines = sink.GetOutput().Split('\n', StringSplitOptions.None).Select(l => l.TrimEnd('\r')).ToArray();
        var processed = ApplyOverflow(lines);
        var newHeight = processed.Length;
        try
        {
            WithWriter(w =>
            {
                if (_lastHeight > 0) { AnsiHelper.CarriageReturn(w); AnsiHelper.CursorUp(w, _lastHeight - 1); }
                for (int i = 0; i < processed.Length; i++)
                {
                    AnsiHelper.EraseLine(w);
                    w.Write(OutputSanitizer.Sanitize(processed[i]));
                    if (i < processed.Length - 1) w.WriteLine();
                }
                if (_lastHeight > newHeight)
                {
                    for (int i = 0; i < _lastHeight - newHeight; i++) { w.WriteLine(); AnsiHelper.EraseLine(w); }
                    AnsiHelper.CursorUp(w, _lastHeight - newHeight);
                }
                w.Flush();
                _lastHeight = newHeight;
                return true;
            });
        }
        catch { }
    }

    public bool Clear()
    {
        if (!_started) return true;
        try { WithWriter(w => { EraseRegion(w, _lastHeight); _lastHeight = 0; w.Flush(); return true; }); return true; }
        catch { return false; }
    }

    public bool IsStarted() => _started;

    public void StartAutoRefresh(Action callback)
    {
        StopRefreshThread();
        var interval = AutoRefreshHelper.ComputeInterval(_config.RefreshPerSecond);
        if (interval is null) return;
        lock (_refreshLock) _refreshThread = new RefreshThread(callback, interval.Value);
    }

    public void StopRefreshThread()
    {
        RefreshThread? t;
        lock (_refreshLock) { t = _refreshThread; _refreshThread = null; }
        t?.Stop();
    }

    private void EnterWriterAccess()
    {
        var current = Environment.CurrentManagedThreadId;
        lock (_ownerLock)
        {
            if (_ownerThreadId == current) throw new InvalidOperationException("Reentrant live writer operation");
            var deadline = DateTime.UtcNow + AutoRefreshHelper.WriterAccessTimeout;
            while (_ownerThreadId.HasValue)
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero) throw new TimeoutException("Timed out waiting for live writer access");
                if (!Monitor.Wait(_ownerLock, remaining > TimeSpan.Zero ? remaining : TimeSpan.FromMilliseconds(1)))
                    throw new TimeoutException("Timed out waiting for live writer access");
                if (_ownerThreadId == current) throw new InvalidOperationException("Reentrant live writer operation");
            }
            _ownerThreadId = current;
        }
    }

    private void ExitWriterAccess()
    {
        lock (_ownerLock) { _ownerThreadId = null; Monitor.PulseAll(_ownerLock); }
    }

    private T WithWriter<T>(Func<TextWriter, T> f)
    {
        EnterWriterAccess();
        try { lock (_writerLock) return f(_writer); }
        finally { ExitWriterAccess(); }
    }

    private void EraseRegion(TextWriter w, int height)
    {
        if (height == 0) return;
        if (height > 0) { AnsiHelper.CarriageReturn(w); AnsiHelper.CursorUp(w, height - 1); }
        for (int i = 0; i < height; i++) { AnsiHelper.EraseLine(w); if (i < height - 1) w.WriteLine(); }
        AnsiHelper.CarriageReturn(w);
    }

    private string[] ApplyOverflow(string[] lines)
    {
        int max = _config.MaxHeight;
        if (max == 0 || lines.Length <= max) return lines;
        return _config.Overflow switch
        {
            VerticalOverflow.Visible => lines,
            VerticalOverflow.Crop => lines.Take(max).ToArray(),
            VerticalOverflow.Ellipsis => max == 0 ? [] : max == 1 ? [lines[0][..Math.Min(lines[0].Length, _width)].TrimEnd() + "..."] : [.. lines.Take(max - 1), "..."],
            _ => lines,
        };
    }

    public void Dispose() { Stop(); }
}
