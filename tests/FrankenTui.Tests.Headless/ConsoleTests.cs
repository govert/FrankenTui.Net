using FrankenTui.Extras;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class ConsoleTests
{
    [Fact] public void SinkCapturesOutput() { var s = new ConsoleSink(); s.Write("hello"); s.WriteLine(" world"); Assert.Equal("hello world" + Environment.NewLine, s.GetOutput()); }
    [Fact] public void SinkClear() { var s = new ConsoleSink(); s.Write("data"); s.Clear(); Assert.Equal("", s.GetOutput()); }
    [Fact] public void SinkEmpty() { Assert.Equal("", new ConsoleSink().GetOutput()); }
    [Fact] public void SinkMultipleWrites() { var s = new ConsoleSink(); s.Write("a"); s.Write("b"); s.Write("c"); Assert.Equal("abc", s.GetOutput()); }

    [Fact] public void ConsoleWidth() { Assert.Equal(80, new FxConsole(80, new ConsoleSink()).Width); }
    [Fact] public void ConsoleZeroWidthDefaults() { Assert.Equal(80, new FxConsole(0, new ConsoleSink()).Width); }
    [Fact] public void ConsoleWidthAccessor() { Assert.Equal(132, new FxConsole(132, new ConsoleSink()).Width); }
    [Fact] public void ConsolePrintsToSink() { var s = new ConsoleSink(); var c = new FxConsole(80, s); c.Print("text"); Assert.Equal("text", s.GetOutput()); }
    [Fact] public void ConsolePrintLine() { var s = new ConsoleSink(); var c = new FxConsole(80, s); c.PrintLine("line"); Assert.Equal("line" + Environment.NewLine, s.GetOutput()); }
    [Fact] public void ConsoleNewLine() { var s = new ConsoleSink(); var c = new FxConsole(80, s); c.NewLine(); Assert.Equal(Environment.NewLine, s.GetOutput()); }
    [Fact] public void PrintStyledWritesAnsi() { var s = new ConsoleSink(); var c = new FxConsole(80, s); c.PrintStyled("hello","31","40"); Assert.Contains("\x1b[31m", s.GetOutput()); Assert.Contains("hello", s.GetOutput()); Assert.Contains("\x1b[0m", s.GetOutput()); }
    [Fact] public void PrintStyledLineIncludesNewline() { var s = new ConsoleSink(); var c = new FxConsole(80, s); c.PrintStyledLine("s","32","40"); Assert.Contains(Environment.NewLine, s.GetOutput()); }
    [Fact] public void LineCountAfterPrint() { Assert.Equal(0, new FxConsole(80, new ConsoleSink()).LineCount); }
    [Fact] public void LineCountAfterPrintLine() { var c = new FxConsole(80, new ConsoleSink()); c.PrintLine("x"); Assert.Equal(1, c.LineCount); }
    [Fact] public void LineCountAfterMultiple() { var c = new FxConsole(80, new ConsoleSink()); c.PrintLine("a"); c.PrintLine("b"); c.NewLine(); Assert.Equal(3, c.LineCount); }
    [Fact] public void CaptureCaptures() { Assert.Equal("cap", FxConsole.Capture(c => c.Print("cap"))); }
    [Fact] public void ConsoleRule() { var s = new ConsoleSink(); var c = new FxConsole(5, s); c.Rule('-'); Assert.Equal("-----" + Environment.NewLine, s.GetOutput()); }

    [Fact] public void BufferNewIsEmpty() { Assert.True(new ConsoleBuffer().IsEmpty); }
    [Fact] public void BufferAppendIncreasesWidth() { var b = new ConsoleBuffer(); b.Append("h","31","40"); Assert.Equal(1, b.Width); Assert.False(b.IsEmpty); }
    [Fact] public void BufferAppendPlain() { var b = new ConsoleBuffer(); b.Append("t"); Assert.Equal(1, b.Width); }
    [Fact] public void BufferMultipleSegments() { var b = new ConsoleBuffer(); b.Append("a","31","40"); b.Append(" b","32","40"); Assert.Equal(3, b.Width); Assert.Equal(2, b.Segments.Count); }
    [Fact] public void BufferClear() { var b = new ConsoleBuffer(); b.Append("d"); b.Clear(); Assert.True(b.IsEmpty); }
    [Fact] public void BufferRenderToAnsi() { var b = new ConsoleBuffer(); b.Append("r","31","40"); b.Append("g","32","40"); var o = b.RenderToAnsi(); Assert.Contains("r", o); Assert.Contains("g", o); Assert.EndsWith("\x1b[0m", o); }
    [Fact] public void BufferEmptySegmentsNotAdded() { var b = new ConsoleBuffer(); b.Append(""); Assert.True(b.IsEmpty); }
    [Fact] public void BufferMultipleClears() { var b = new ConsoleBuffer(); b.Append("x"); b.Clear(); b.Append("y"); Assert.Equal(1, b.Width); Assert.Equal("y", b.Segments[0].Text); }
    [Fact] public void BufferZeroWidthAfterClear() { var b = new ConsoleBuffer(); b.Append("h"); b.Clear(); Assert.Equal(0, b.Width); }
    [Fact] public void BufferSegmentsOrder() { var b = new ConsoleBuffer(); b.Append("a","31","40"); b.Append("b","32","40"); b.Append("c","33","40"); Assert.Equal(3, b.Segments.Count); Assert.Equal("a", b.Segments[0].Text); Assert.Equal("c", b.Segments[2].Text); }
    [Fact] public void BufferIsEmptyInitially() { Assert.True(new ConsoleBuffer().IsEmpty); }
    [Fact] public void BufferNotEmptyAfterAppend() { var b = new ConsoleBuffer(); b.Append("x"); Assert.False(b.IsEmpty); }

    [Fact] public void StyledSegmentPlain() { var s = StyledSegment.Plain("t"); Assert.Equal("t", s.Text); Assert.Equal("37", s.Foreground); }
    [Fact] public void StyledSegmentColors() { var s = new StyledSegment("e","31","40"); Assert.Equal("31", s.Foreground); Assert.Equal("40", s.Background); }
}
