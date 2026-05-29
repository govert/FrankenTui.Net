// Upstream source: crates/ftui-extras/src/console.rs
// Port of ConsoleSink, ConsoleBuffer, FxConsole, StyledSegment.

using System.Text;

namespace FrankenTui.Extras;

public readonly record struct StyledSegment(string Text, string Foreground, string Background)
{
    public static StyledSegment Plain(string text) => new(text, "37", "40");
}

public sealed class ConsoleBuffer
{
    private readonly List<StyledSegment> _segments = [];
    private int _totalWidth;
    public int Width => _totalWidth;
    public bool IsEmpty => _segments.Count == 0 || _segments.All(s => s.Text.Length == 0);
    public IReadOnlyList<StyledSegment> Segments => _segments;

    public void Append(string text, string fg = "37", string bg = "40")
    {
        if (text.Length == 0) return;
        _segments.Add(new StyledSegment(text, fg, bg));
        _totalWidth += text.Length;
    }

    public void Clear() { _segments.Clear(); _totalWidth = 0; }

    public string RenderToAnsi()
    {
        var sb = new StringBuilder();
        foreach (var seg in _segments)
            sb.Append($"\x1b[{seg.Foreground}m\x1b[{seg.Background}m{seg.Text}");
        sb.Append("\x1b[0m");
        return sb.ToString();
    }
}

public sealed class ConsoleSink
{
    private readonly StringBuilder _sb = new();
    public void Write(string text) => _sb.Append(text);
    public void WriteLine(string line) => _sb.AppendLine(line);
    public string GetOutput() => _sb.ToString();
    public void Clear() => _sb.Clear();
}

public sealed class FxConsole
{
    private readonly int _width;
    private readonly ConsoleSink _sink;
    private int _lineCount;

    public int Width => _width;
    public ConsoleSink Sink => _sink;
    public int LineCount => _lineCount;

    public FxConsole(int width, ConsoleSink sink)
    {
        _width = width > 0 ? width : 80;
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public void Print(string text)
    {
        _sink.Write(text);
        _lineCount += text.Count(c => c == '\n');
    }

    public void PrintLine(string line) { Print(line + Environment.NewLine); }

    public void NewLine() { _sink.Write(Environment.NewLine); _lineCount++; }

    public static bool IsOutputRedirected => System.Console.IsOutputRedirected;
    public static bool IsErrorRedirected => System.Console.IsErrorRedirected;
    public static int WindowWidth => Math.Max(System.Console.WindowWidth, 1);
    public static int WindowHeight => Math.Max(System.Console.WindowHeight, 1);

    public static string ReadPassword(char mask = '*')
    {
        var sb = new StringBuilder();
        while (true)
        {
            var key = System.Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace && sb.Length > 0) sb.Length--;
            else if (key.KeyChar >= 32) sb.Append(key.KeyChar);
        }
        System.Console.WriteLine();
        return sb.ToString();
    }

    public void PrintStyled(string text, string fgColor = "37", string bgColor = "40")
    {
        Print($"\x1b[{fgColor}m\x1b[{bgColor}m{text}\x1b[0m");
    }

    public void PrintStyledLine(string text, string fgColor = "37", string bgColor = "40")
    {
        PrintStyled(text, fgColor, bgColor);
        NewLine();
    }

    public void Rule(char ch = '─')
    {
        var line = new string(ch, _width);
        PrintLine(line);
    }

    public static string Capture(Action<FxConsole> action, int width = 80)
    {
        var sink = new ConsoleSink();
        var console = new FxConsole(width, sink);
        action(console);
        return sink.GetOutput();
    }
}
