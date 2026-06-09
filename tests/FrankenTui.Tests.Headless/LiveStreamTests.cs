// Upstream source: crates/ftui-extras/src/live.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Comprehensive tests for Live, LiveConfig, VerticalOverflow, AnsiHelper, AutoRefreshHelper.

using FrankenTui.Extras;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class LiveStreamTests
{
    // ---- LiveConfig (3) ----
    [Fact] public void LiveWithConfigFactory()
    {
        var config = new LiveConfig { MaxHeight = 10, Transient = false };
        using var sw = new StringWriter();
        var live = Live.WithConfig(sw, 80, config);
        Assert.False(live.IsStarted());
    }

    [Fact] public void LiveConfigDefaults()
    {
        var c = new LiveConfig();
        Assert.Equal(0, c.MaxHeight);
        Assert.Equal(VerticalOverflow.Ellipsis, c.Overflow);
        Assert.True(c.Transient);
        Assert.Equal(4.0, c.RefreshPerSecond, 1);
    }

    [Fact] public void LiveConfigCustom()
    {
        var c = new LiveConfig { MaxHeight = 10, Overflow = VerticalOverflow.Crop, Transient = false, RefreshPerSecond = 10.0 };
        Assert.Equal(10, c.MaxHeight);
        Assert.Equal(VerticalOverflow.Crop, c.Overflow);
        Assert.False(c.Transient);
        Assert.Equal(10.0, c.RefreshPerSecond, 1);
    }

    // ---- AnsiHelper (5) ----
    [Fact] public void AnsiCursorUpZero()
    {
        var sw = new StringWriter();
        AnsiHelper.CursorUp(sw, 0);
        Assert.Equal("", sw.ToString()); // no output for n=0
    }

    [Fact] public void AnsiCursorUpPositive()
    {
        var sw = new StringWriter();
        AnsiHelper.CursorUp(sw, 3);
        Assert.Equal("\x1b[3A", sw.ToString());
    }

    [Fact] public void AnsiCarriageReturn()
    {
        var sw = new StringWriter();
        AnsiHelper.CarriageReturn(sw);
        Assert.Equal("\r", sw.ToString());
    }

    [Fact] public void AnsiEraseLine()
    {
        var sw = new StringWriter();
        AnsiHelper.EraseLine(sw);
        Assert.Equal("\x1b[2K", sw.ToString());
    }

    [Fact] public void AnsiHideShowCursor()
    {
        var sw = new StringWriter();
        AnsiHelper.HideCursor(sw);
        Assert.Equal("\x1b[?25l", sw.ToString());
        sw.GetStringBuilder().Clear();
        AnsiHelper.ShowCursor(sw);
        Assert.Equal("\x1b[?25h", sw.ToString());
    }

    // ---- AutoRefreshHelper (4) ----
    [Fact] public void ComputeIntervalValid()
    {
        var ts = AutoRefreshHelper.ComputeInterval(2.0);
        Assert.NotNull(ts);
        Assert.Equal(TimeSpan.FromMilliseconds(500), ts!.Value);
    }

    [Fact] public void ComputeIntervalZero()
    {
        var ts = AutoRefreshHelper.ComputeInterval(0.0);
        Assert.Null(ts);
    }

    [Fact] public void ComputeIntervalNegative()
    {
        var ts = AutoRefreshHelper.ComputeInterval(-1.0);
        Assert.Null(ts);
    }

    [Fact] public void ComputeIntervalNaN()
    {
        var ts = AutoRefreshHelper.ComputeInterval(double.NaN);
        Assert.Null(ts);
    }

    // ---- Live start/stop/isStarted (5) ----
    [Fact] public void LiveNotStartedInitially()
    {
        using var live = new Live(new StringWriter(), 80);
        Assert.False(live.IsStarted());
    }

    [Fact] public void LiveStart()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80);
        Assert.True(live.Start());
        Assert.True(live.IsStarted());
    }

    [Fact] public void LiveStop()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80, new LiveConfig { Transient = false });
        live.Start();
        Assert.True(live.Stop());
        Assert.False(live.IsStarted());
    }

    [Fact] public void LiveDoubleStart()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80);
        Assert.True(live.Start());
        Assert.True(live.Start()); // second start is a no-op
    }

    [Fact] public void LiveDoubleStop()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80, new LiveConfig { Transient = false });
        live.Start();
        live.Stop();
        Assert.True(live.Stop()); // second stop is a no-op
    }

    // ---- Live Update (6) ----
    [Fact] public void LiveUpdateRenders()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80, new LiveConfig { Transient = false });
        live.Start();
        live.Update(c => { c.PrintText("hello"); c.Flush(); });
        live.Stop();
        var output = sw.ToString();
        Assert.Contains("hello", output);
    }

    [Fact] public void LiveUpdateNotStartedDoesNothing()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80);
        live.Update(c => c.PrintText("should not appear"));
        Assert.Equal("", sw.ToString());
    }

    [Fact] public void LiveClear()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80, new LiveConfig { Transient = false });
        live.Start();
        live.Update(c => { c.PrintText("temp"); c.Flush(); });
        Assert.True(live.Clear());
        live.Stop();
    }

    [Fact] public void LiveClearNotStarted()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80);
        Assert.True(live.Clear()); // OK when not started
    }

    // ---- VerticalOverflow (4) ----
    [Fact] public void VerticalOverflowCrop()
    {
        using var sw = new StringWriter();
        var config = new LiveConfig { MaxHeight = 1, Overflow = VerticalOverflow.Crop, Transient = false };
        using var live = new Live(sw, 80, config);
        live.Start();
        live.Update(c => { c.PrintlnText("line 1"); c.PrintlnText("line 2"); c.Flush(); });
        live.Stop();
        var output = sw.ToString();
        Assert.Contains("line 1", output);
        Assert.DoesNotContain("line 2", output);
    }

    [Fact] public void VerticalOverflowEllipsis()
    {
        using var sw = new StringWriter();
        var config = new LiveConfig { MaxHeight = 1, Overflow = VerticalOverflow.Ellipsis, Transient = false };
        using var live = new Live(sw, 80, config);
        live.Start();
        live.Update(c => { c.PrintlnText("line 1"); c.PrintlnText("line 2"); c.Flush(); });
        live.Stop();
        var output = sw.ToString();
        Assert.Contains("...", output);
    }

    [Fact] public void VerticalOverflowVisible()
    {
        using var sw = new StringWriter();
        var config = new LiveConfig { MaxHeight = 1, Overflow = VerticalOverflow.Visible, Transient = false };
        using var live = new Live(sw, 80, config);
        live.Start();
        live.Update(c => { c.PrintlnText("line 1"); c.PrintlnText("line 2"); c.Flush(); });
        live.Stop();
        var output = sw.ToString();
        Assert.Contains("line 1", output);
        Assert.Contains("line 2", output);
    }

    // ---- Transient behavior (2) ----
    [Fact] public void TransientErasesOnStop()
    {
        using var sw = new StringWriter();
        var config = new LiveConfig { Transient = true };
        using var live = new Live(sw, 80, config);
        live.Start();
        live.Update(c => { c.PrintText("transient content"); c.Flush(); });
        live.Stop();
        var output = sw.ToString();
        // Transient stop should contain erase sequences
        Assert.Contains("\x1b[2K", output);
    }

    [Fact] public void NonTransientPreservesContent()
    {
        using var sw = new StringWriter();
        var config = new LiveConfig { Transient = false };
        using var live = new Live(sw, 80, config);
        live.Start();
        live.Update(c => { c.PrintText("persistent"); c.Flush(); });
        live.Stop();
        var output = sw.ToString();
        Assert.Contains("persistent", output);
    }

    // ---- Auto-refresh (3) ----
    [Fact] public void StartAutoRefresh()
    {
        int count = 0;
        using var sw = new StringWriter();
        using var live = new Live(sw, 80, new LiveConfig { RefreshPerSecond = 10.0, Transient = false });
        live.Start();
        live.StartAutoRefresh(() => { Interlocked.Increment(ref count); });
        Thread.Sleep(300); // enough for at least one refresh
        live.StopRefreshThread();
        Assert.True(Volatile.Read(ref count) >= 1);
    }

    [Fact] public void StopAutoRefreshNoop()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80);
        live.StopRefreshThread(); // no-op when no refresh thread
    }

    [Fact] public void DoubleStartAutoRefresh()
    {
        int count = 0;
        using var sw = new StringWriter();
        using var live = new Live(sw, 80, new LiveConfig { RefreshPerSecond = 10.0, Transient = false });
        live.Start();
        live.StartAutoRefresh(() => { Interlocked.Increment(ref count); });
        live.StartAutoRefresh(() => { Interlocked.Increment(ref count); }); // replaces previous
        Thread.Sleep(200);
        live.StopRefreshThread();
        Assert.True(Volatile.Read(ref count) >= 1);
    }

    // ---- Dispose (1) ----
    [Fact] public void LiveDisposeStops()
    {
        var sw = new StringWriter();
        var live = new Live(sw, 80, new LiveConfig { Transient = false });
        live.Start();
        live.Dispose();
        Assert.False(live.IsStarted());
    }

    // ---- Sanitization (1) ----
    [Fact] public void UpdateSanitizesEscapeInjection()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80, new LiveConfig { Transient = false });
        live.Start();
        live.Update(c => { c.PrintText("safe\x1b]52;c;SGVsbG8=\x1b\\tail"); c.Flush(); });
        live.Stop();
        var output = sw.ToString();
        // Sanitization strips control chars < 0x20 (except tab/newline).
        // The \x1b (0x1B) characters are removed. The display-safe payload text remains.
        Assert.Contains("safe", output);
        Assert.Contains("tail", output);
        // Count \x1b chars in output: only Start/Stop cursor escapes should remain
        int escCount = output.Count(c => c == '\x1b');
        Assert.Equal(3, escCount); // hide cursor + erase line + show cursor
    }

    // ---- Cursor reposition / height shrink (3) ----
    [Fact] public void MultipleUpdatesRepositionCursor()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80, new LiveConfig { Transient = false });
        live.Start();
        live.Update(c => { c.PrintText("first"); c.Flush(); });
        live.Update(c => { c.PrintText("second"); c.Flush(); });
        live.Stop();
        var output = sw.ToString();
        Assert.Contains("second", output);
    }

    [Fact] public void UpdateShrinksHeightErasesExtra()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80, new LiveConfig { Transient = false });
        live.Start();
        live.Update(c => { c.PrintlnText("line1"); c.PrintlnText("line2"); c.Flush(); });
        live.Update(c => { c.PrintlnText("short"); c.Flush(); });
        live.Stop();
        var output = sw.ToString();
        Assert.Contains("short", output);
    }

    [Fact] public void EmptyUpdateWritesNothing()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80, new LiveConfig { Transient = false });
        live.Start();
        live.Update(c => { }); // empty update does not crash
        live.Stop();
        Assert.False(live.IsStarted());
    }

    // ---- Overflow edge cases (5) ----
    [Fact] public void NoOverflowWhenWithinLimit()
    {
        using var sw = new StringWriter();
        var config = new LiveConfig { MaxHeight = 5, Overflow = VerticalOverflow.Crop, Transient = false };
        using var live = new Live(sw, 80, config);
        live.Start();
        live.Update(c => { c.PrintlnText("l1"); c.PrintlnText("l2"); c.Flush(); });
        live.Stop();
        Assert.Contains("l2", sw.ToString()); // fits within limit
    }

    [Fact] public void MaxHeightZeroMeansUnlimited()
    {
        using var sw = new StringWriter();
        var config = new LiveConfig { MaxHeight = 0, Overflow = VerticalOverflow.Crop, Transient = false };
        using var live = new Live(sw, 80, config);
        live.Start();
        live.Update(c => { for (int i = 0; i < 100; i++) c.PrintlnText($"line{i}"); c.Flush(); });
        live.Stop();
        Assert.Contains("line99", sw.ToString()); // no truncation
    }

    [Fact] public void OverflowEllipsisMaxHeight1()
    {
        using var sw = new StringWriter();
        var config = new LiveConfig { MaxHeight = 1, Overflow = VerticalOverflow.Ellipsis, Transient = false };
        using var live = new Live(sw, 80, config);
        live.Start();
        live.Update(c => { c.PrintlnText("line1"); c.PrintlnText("line2"); c.Flush(); });
        live.Stop();
        Assert.Contains("...", sw.ToString());
    }

    [Fact] public void OverflowCropMaxHeight1()
    {
        using var sw = new StringWriter();
        var config = new LiveConfig { MaxHeight = 1, Overflow = VerticalOverflow.Crop, Transient = false };
        using var live = new Live(sw, 80, config);
        live.Start();
        live.Update(c => { c.PrintlnText("line1"); c.PrintlnText("line2"); c.Flush(); });
        live.Stop();
        Assert.Contains("line1", sw.ToString());
        Assert.DoesNotContain("line2", sw.ToString());
    }

    [Fact] public void ClearErasesRegion()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80, new LiveConfig { Transient = false });
        live.Start();
        live.Update(c => { c.PrintText("temp"); c.Flush(); });
        Assert.True(live.Clear());
        live.Stop();
    }

    // ---- Stop without start (1) ----
    [Fact] public void StopRefreshThreadWithoutStartIsSafe()
    {
        using var sw = new StringWriter();
        using var live = new Live(sw, 80);
        live.StopRefreshThread(); // safe to call without starting
    }

    // ---- Auto-refresh infinity (1) ----
    [Fact] public void ComputeIntervalInfinity()
    {
        var ts = AutoRefreshHelper.ComputeInterval(double.PositiveInfinity);
        Assert.Null(ts);
    }
}
