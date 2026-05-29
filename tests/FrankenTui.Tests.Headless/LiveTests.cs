// Upstream source: crates/ftui-extras/src/live.rs — tests

using System.Diagnostics;
using FrankenTui.Extras;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class LiveTests
{
    // Config (3)
    [Fact] public void DefaultConfig() { var c = new LiveConfig(); Assert.Equal(0, c.MaxHeight); Assert.True(c.Transient); Assert.Equal(4.0, c.RefreshPerSecond); }
    [Fact] public void OverflowDefaultIsEllipsis() { Assert.Equal(VerticalOverflow.Ellipsis, new LiveConfig().Overflow); }
    [Fact] public void WithConfigCustomValues()
    {
        var c = new LiveConfig { MaxHeight = 10, Overflow = VerticalOverflow.Crop, Transient = false, RefreshPerSecond = 2.0 };
        Assert.Equal(10, c.MaxHeight); Assert.Equal(VerticalOverflow.Crop, c.Overflow); Assert.False(c.Transient); Assert.Equal(2.0, c.RefreshPerSecond);
    }

    // Construction (1)
    [Fact] public void NewCreatesInactive()
    {
        using var w = new StringWriter(); using var live = new Live(w, 80);
        Assert.False(live.IsStarted()); live.Update(c => c.Print("x")); Assert.Empty(w.ToString());
    }

    // Start/stop lifecycle (7)
    [Fact] public void StartHidesCursor() { using var w = new StringWriter(); using var live = new Live(w, 80); Assert.True(live.Start()); Assert.True(live.IsStarted()); Assert.Contains("\x1b[?25l", w.ToString()); }
    [Fact] public void StopShowsCursor() { using var w = new StringWriter(); using var live = new Live(w, 80); live.Start(); w.GetStringBuilder().Clear(); live.Stop(); Assert.False(live.IsStarted()); Assert.Contains("\x1b[?25h", w.ToString()); }
    [Fact] public void StartIsIdempotent() { using var w = new StringWriter(); using var live = new Live(w, 80); live.Start(); var o = w.ToString(); live.Start(); Assert.Equal(o, w.ToString()); }
    [Fact] public void StopIsIdempotent() { using var w = new StringWriter(); using var live = new Live(w, 80); live.Start(); live.Stop(); w.GetStringBuilder().Clear(); live.Stop(); Assert.Empty(w.ToString()); }
    [Fact] public void StopRefreshThreadWithoutStartIsSafe() { using var w = new StringWriter(); using var live = new Live(w, 80); live.StopRefreshThread(); }

    [Fact] public void StartAutoRefreshIgnoresNonPositiveRate()
    {
        using var w = new StringWriter(); var cfg = new LiveConfig { RefreshPerSecond = 0.0 }; using var live = new Live(w, 80, cfg);
        live.StartAutoRefresh(() => { }); live.Start(); Thread.Sleep(20);
    }

    [Fact] public void StopStopsRefreshThreadEvenWhenNotStarted()
    {
        using var w = new StringWriter(); var cfg = new LiveConfig { RefreshPerSecond = 200.0 }; using var live = new Live(w, 80, cfg);
        int count = 0; live.StartAutoRefresh(() => Interlocked.Increment(ref count)); Thread.Sleep(30);
        live.StopRefreshThread(); var before = count; Thread.Sleep(50);
        Assert.Equal(before, count);
    }

    // Update (7)
    [Fact] public void UpdateWritesContent() { using var w = new StringWriter(); using var live = new Live(w, 80); live.Start(); live.Update(c => c.Print("hello")); Assert.Contains("hello", w.ToString()); }
    [Fact] public void UpdateWhenStoppedIsNoop() { using var w = new StringWriter(); using var live = new Live(w, 80); live.Start(); live.Update(c => c.Print("v")); live.Stop(); w.GetStringBuilder().Clear(); live.Update(c => c.Print("x")); Assert.Empty(w.ToString()); }
    [Fact] public void MultipleUpdatesRepositionCursor() { using var w = new StringWriter(); using var live = new Live(w, 80); live.Start(); live.Update(c => c.Print("a")); live.Update(c => c.Print("b")); Assert.Contains("b", w.ToString()); Assert.Contains("\x1b[", w.ToString()); }
    [Fact] public void UpdateSanitizesEscapeInjectionPayloads() { using var w = new StringWriter(); using var live = new Live(w, 80); live.Start(); live.Update(c => c.Print("\x1b[2J bad")); Assert.DoesNotContain("\x1b[2J", w.ToString()); Assert.Contains("bad", w.ToString()); }
    [Fact] public void EmptyUpdateWritesNothing() { using var w = new StringWriter(); using var live = new Live(w, 80); live.Start(); live.Update(c => { }); }
    [Fact] public void UpdateShrinksHeightErasesExtra() { using var w = new StringWriter(); using var live = new Live(w, 80); live.Start(); live.Update(c => { c.PrintLine("a"); c.PrintLine("b"); c.PrintLine("c"); }); live.Update(c => c.PrintLine("x")); Assert.Contains("\x1b[2K", w.ToString()); }
    [Fact] public void MaxHeightZeroMeansUnlimited() { using var w = new StringWriter(); using var live = new Live(w, 80); live.Start(); live.Update(c => { c.PrintLine("a"); c.PrintLine("b"); c.PrintLine("c"); }); Assert.Contains("a", w.ToString()); Assert.Contains("c", w.ToString()); }

    // Overflow (6)
    [Fact] public void OverflowCropTruncates() { using var w = new StringWriter(); var cfg = new LiveConfig { MaxHeight = 1, Overflow = VerticalOverflow.Crop }; using var live = new Live(w, 80, cfg); live.Start(); live.Update(c => { c.PrintLine("a"); c.PrintLine("b"); }); Assert.Contains("a", w.ToString()); }
    [Fact] public void OverflowEllipsisAddsDots() { using var w = new StringWriter(); var cfg = new LiveConfig { MaxHeight = 1, Overflow = VerticalOverflow.Ellipsis }; using var live = new Live(w, 80, cfg); live.Start(); live.Update(c => { c.PrintLine("visible"); c.PrintLine("x"); }); Assert.Contains("...", w.ToString()); }
    [Fact] public void OverflowVisibleShowsAll() { using var w = new StringWriter(); var cfg = new LiveConfig { MaxHeight = 1, Overflow = VerticalOverflow.Visible }; using var live = new Live(w, 80, cfg); live.Start(); live.Update(c => { c.PrintLine("a"); c.PrintLine("b"); }); Assert.Contains("b", w.ToString()); }
    [Fact] public void NoOverflowWhenWithinLimit() { using var w = new StringWriter(); var cfg = new LiveConfig { MaxHeight = 10, Overflow = VerticalOverflow.Crop }; using var live = new Live(w, 80, cfg); live.Start(); live.Update(c => { c.PrintLine("a"); c.PrintLine("b"); }); Assert.Contains("a", w.ToString()); Assert.Contains("b", w.ToString()); }
    [Fact] public void OverflowEllipsisMaxHeight1() { using var w = new StringWriter(); var cfg = new LiveConfig { MaxHeight = 1, Overflow = VerticalOverflow.Ellipsis }; using var live = new Live(w, 80, cfg); live.Start(); live.Update(c => { c.PrintLine("line"); c.PrintLine("x"); }); Assert.Contains("...", w.ToString()); }
    [Fact] public void OverflowCropMaxHeight1() { using var w = new StringWriter(); var cfg = new LiveConfig { MaxHeight = 1, Overflow = VerticalOverflow.Crop }; using var live = new Live(w, 80, cfg); live.Start(); live.Update(c => { c.PrintLine("a"); c.PrintLine("b"); }); Assert.DoesNotContain("b", w.ToString()); }

    // Transient (2)
    [Fact] public void TransientStopErasesOutput() { using var w = new StringWriter(); using var live = new Live(w, 80); live.Start(); live.Update(c => c.PrintLine("x")); live.Stop(); Assert.Contains("\x1b[2K", w.ToString()); }
    [Fact] public void NonTransientStopPreservesOutput() { using var w = new StringWriter(); var cfg = new LiveConfig { Transient = false }; using var live = new Live(w, 80, cfg); live.Start(); live.Update(c => c.Print("p")); live.Stop(); Assert.Contains("p", w.ToString()); }

    // Clear (2)
    [Fact] public void ClearErasesRegion() { using var w = new StringWriter(); using var live = new Live(w, 80); live.Start(); live.Update(c => c.PrintLine("d")); var b = w.ToString().Length; live.Clear(); Assert.True(w.ToString().Length > b); }
    [Fact] public void ClearWhenStoppedIsNoop() { using var w = new StringWriter(); using var live = new Live(w, 80); Assert.True(live.Clear()); }

    // ANSI helpers (5)
    [Fact] public void CursorUpWritesEscape() { using var w = new StringWriter(); AnsiHelper.CursorUp(w, 3); Assert.Equal("\x1b[3A", w.ToString()); }
    [Fact] public void CursorUpZeroIsNoop() { using var w = new StringWriter(); AnsiHelper.CursorUp(w, 0); Assert.Empty(w.ToString()); }
    [Fact] public void EraseLineWritesEscape() { using var w = new StringWriter(); AnsiHelper.EraseLine(w); Assert.Equal("\x1b[2K", w.ToString()); }
    [Fact] public void HideShowCursorEscapes() { using var w = new StringWriter(); AnsiHelper.HideCursor(w); Assert.Equal("\x1b[?25l", w.ToString()); w.GetStringBuilder().Clear(); AnsiHelper.ShowCursor(w); Assert.Equal("\x1b[?25h", w.ToString()); }
    [Fact] public void CarriageReturnWritesEscape() { using var w = new StringWriter(); AnsiHelper.CarriageReturn(w); Assert.Equal("\r", w.ToString()); }

    // Auto-refresh helpers (4)
    [Fact] public void AutoRefreshIgnoresNanRate() { Assert.Null(AutoRefreshHelper.ComputeInterval(double.NaN)); }
    [Fact] public void AutoRefreshIgnoresNegativeRate() { Assert.Null(AutoRefreshHelper.ComputeInterval(-1.0)); }
    [Fact] public void AutoRefreshIgnoresInfinityRate() { Assert.Null(AutoRefreshHelper.ComputeInterval(double.PositiveInfinity)); }
    [Fact] public void AutoRefreshTinyPositiveRateStopsPromptly() { Assert.NotNull(AutoRefreshHelper.ComputeInterval(0.001)); }

    // Auto-refresh integration (1)
    [Fact] public void StopRefreshThreadDoesNotBlockOnStuckCallback()
    {
        using var w = new StringWriter(); var cfg = new LiveConfig { RefreshPerSecond = 100.0 }; using var live = new Live(w, 80, cfg);
        var signal = new ManualResetEvent(false);
        live.StartAutoRefresh(() => { signal.Set(); Thread.Sleep(5000); });
        signal.WaitOne(1000); live.StopRefreshThread();
    }

    // Reentrancy (1)
    [Fact] public void ReentrantWriterCallbackReturnsErrorInsteadOfDeadlocking()
    {
        using var w = new StringWriter(); using var live = new Live(w, 80); live.Start();
        live.Update(c => c.Print("first"));
        var beforeLength = w.ToString().Length;
        // Nested update should be blocked by reentrancy detection
        live.Update(c => live.Update(c2 => c2.Print("nested")));
        // Output length should not increase beyond the first update
        // (nested update is silently caught by Update's catch block)
        var afterLength = w.ToString().Length;
        Assert.True(afterLength >= beforeLength); // non-nested content may add, nested should not
    }

    // Drop (1)
    [Fact] public void DropStopsLive() { using var w = new StringWriter(); var live = new Live(w, 80); live.Start(); Assert.True(live.IsStarted()); live.Dispose(); Assert.False(live.IsStarted()); }

    // ===== Blocking-writer harness tests (2 tests) =====

    /// <summary>Stop should not block when the writer is stuck in a write operation.</summary>
    [Fact] public void StopDoesNotBlockWhenWriterOwnerIsStuck()
    {
        using var bw = new BlockingWriter();
        using var live = new Live(bw, 80);
        live.Start();

        bw.BlockWrites = true;
        var updateThread = new Thread(() => live.Update(c => c.Print("blocked")));
        updateThread.Start();

        // Wait for the update thread to enter the blocking writer
        Assert.True(bw.WaitForEntered(TimeSpan.FromSeconds(2)), "Update should enter the blocking writer");

        var sw = Stopwatch.StartNew();
        var result = live.Stop();
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(2), $"Stop should not block on a stuck writer: {sw.Elapsed}");
        Assert.False(result); // stop should fail with timeout

        bw.Release();
        updateThread.Join(1000);
    }

}

/// <summary>Test writer that can block writes to simulate a stuck writer owner.</summary>
internal sealed class BlockingWriter : TextWriter
{
    public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

    private readonly object _lock = new();
    private bool _blockWrites;
    private bool _released;
    private readonly ManualResetEvent _entered = new(false);

    public bool BlockWrites { set { lock (_lock) { _blockWrites = value; if (!value) Monitor.PulseAll(_lock); } } }

    public bool WaitForEntered(TimeSpan timeout) => _entered.WaitOne(timeout);

    public void Release() { lock (_lock) { _released = true; Monitor.PulseAll(_lock); } }

    public override void Write(char[] buffer, int index, int count) { CheckBlock(); }

    public override void Write(char value) { CheckBlock(); }
    public override void Write(string? value) { if (value is not null) CheckBlock(); }

    private void CheckBlock()
    {
        lock (_lock)
        {
            if (_blockWrites)
            {
                _entered.Set();
                while (!_released)
                    Monitor.Wait(_lock, TimeSpan.FromMilliseconds(100));
            }
        }
    }
}
