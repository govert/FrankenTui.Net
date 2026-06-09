// Tests for .external/frankentui/crates/ftui-runtime/src/debug_trace.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c

using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class DebugTraceTests
{
    [Fact]
    public void IsEnabledReturnsBool()
    {
        // Just verify it returns a bool without panicking
        var _ = DebugTrace.IsEnabled;
    }

    [Fact]
    public void ElapsedMsIncreases()
    {
        var t1 = DebugTrace.ElapsedMs;
        Thread.Sleep(10);
        var t2 = DebugTrace.ElapsedMs;
        Assert.True(t2 >= t1);
    }

    [Fact]
    public void TraceDoesNotThrowWhenDisabled()
    {
        // By default, FTUI_DEBUG_TRACE is not set, so Trace is a no-op.
        // Just verify it doesn't throw.
        DebugTrace.Trace("test message");
    }
}
