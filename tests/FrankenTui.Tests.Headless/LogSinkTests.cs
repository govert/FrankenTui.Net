// Tests for .external/frankentui/crates/ftui-runtime/src/log_sink.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class LogSinkTests
{
    [Fact]
    public void LogSinkBuffersLines()
    {
        var sw = new StringWriter();
        var writer = new TerminalWriter(sw, new ScreenMode.Inline(5), UiAnchor.Bottom, TerminalCapabilities.Basic());
        using (var sink = new LogSink(writer))
        {
            sink.Write("Hello");
        }
        Assert.Contains("Hello", sw.ToString());
    }

    [Fact]
    public void LogSinkSanitizesOutput()
    {
        var sw = new StringWriter();
        var writer = new TerminalWriter(sw, new ScreenMode.Inline(5), UiAnchor.Bottom, TerminalCapabilities.Basic());
        using (var sink = new LogSink(writer))
        {
            sink.Write("Unsafe \x1b[31mred\x1b[0m text\n");
        }
        var output = sw.ToString();
        Assert.Contains("Unsafe", output);
        Assert.Contains("red", output);
        Assert.Contains("text", output);
    }

    [Fact]
    public void LogSinkFlushesPartialLine()
    {
        var sw = new StringWriter();
        var writer = new TerminalWriter(sw, new ScreenMode.Inline(5), UiAnchor.Bottom, TerminalCapabilities.Basic());
        using (var sink = new LogSink(writer))
        {
            sink.Write("Partial");
            sink.Flush();
        }
        Assert.Contains("Partial", sw.ToString());
    }

    [Fact]
    public void LogSinkMultipleLines()
    {
        var sw = new StringWriter();
        var writer = new TerminalWriter(sw, new ScreenMode.Inline(5), UiAnchor.Bottom, TerminalCapabilities.Basic());
        using (var sink = new LogSink(writer))
        {
            sink.Write("Line1\n");
            sink.Write("Line2\n");
            sink.Write("Line3\n");
        }
        var output = sw.ToString();
        Assert.Contains("Line1", output);
        Assert.Contains("Line2", output);
        Assert.Contains("Line3", output);
    }

    [Fact]
    public void LogSinkEmptyWrite()
    {
        var sw = new StringWriter();
        var writer = new TerminalWriter(sw, new ScreenMode.Inline(5), UiAnchor.Bottom, TerminalCapabilities.Basic());
        using (var sink = new LogSink(writer))
        {
            var n = sink.Write(Array.Empty<byte>(), 0, 0);
            Assert.Equal(0, n);
        }
    }

    [Fact]
    public void LogSinkNewlineOnly()
    {
        var sw = new StringWriter();
        var writer = new TerminalWriter(sw, new ScreenMode.Inline(5), UiAnchor.Bottom, TerminalCapabilities.Basic());
        using (var sink = new LogSink(writer))
        {
            sink.Write("\n");
        }
        Assert.Contains('\n', sw.ToString());
    }

    [Fact]
    public void LogSinkMultipleNewlinesInOneWrite()
    {
        var sw = new StringWriter();
        var writer = new TerminalWriter(sw, new ScreenMode.Inline(5), UiAnchor.Bottom, TerminalCapabilities.Basic());
        using (var sink = new LogSink(writer))
        {
            sink.Write("A\nB\nC\n");
        }
        var output = sw.ToString();
        Assert.Contains('A', output);
        Assert.Contains('B', output);
        Assert.Contains('C', output);
    }

    [Fact]
    public void LogSinkSanitizesMultipleEscapes()
    {
        var sw = new StringWriter();
        var writer = new TerminalWriter(sw, new ScreenMode.Inline(5), UiAnchor.Bottom, TerminalCapabilities.Basic());
        using (var sink = new LogSink(writer))
        {
            sink.Write("\x1b[31mRed\x1b[0m \x1b[1mBold\x1b[0m\n");
        }
        var output = sw.ToString();
        Assert.Contains("Red", output);
        Assert.Contains("Bold", output);
    }

    [Fact]
    public void LogSinkDropWithoutFlushWritesPartial()
    {
        var sw = new StringWriter();
        var writer = new TerminalWriter(sw, new ScreenMode.Inline(5), UiAnchor.Bottom, TerminalCapabilities.Basic());
        using (var sink = new LogSink(writer))
        {
            sink.Write("NoNewline");
        }
        Assert.Contains("NoNewline", sw.ToString());
    }

    [Fact]
    public void LogSinkWriteReturnsFullLength()
    {
        var sw = new StringWriter();
        var writer = new TerminalWriter(sw, new ScreenMode.Inline(5), UiAnchor.Bottom, TerminalCapabilities.Basic());
        using (var sink = new LogSink(writer))
        {
            var data = "Hello World\n"u8.ToArray();
            var n = sink.Write(data, 0, data.Length);
            Assert.Equal(data.Length, n);
        }
    }

    [Fact]
    public void WriteLogInAltScreenIsNoop()
    {
        var sw = new StringWriter();
        var writer = new TerminalWriter(sw, new ScreenMode.AltScreen(), UiAnchor.Bottom, TerminalCapabilities.Basic());
        writer.WriteLog("should not appear");
        Assert.Empty(sw.ToString());
    }
}
