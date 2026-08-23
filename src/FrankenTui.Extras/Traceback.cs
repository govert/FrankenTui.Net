// Upstream source: .external/frankentui/crates/ftui-extras/src/traceback.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Faithful 1-1 port of TracebackFrame, TracebackStyle, Traceback, and render().

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Style;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

/// <summary>A single traceback frame. Port of TracebackFrame.</summary>
public sealed class TracebackFrame
{
    public string? Filename { get; set; }
    public string Name { get; set; } = "";
    public int Line { get; set; }
    public string? SourceContext { get; set; }
    public int SourceFirstLine { get; set; } = 1;

    public TracebackFrame() { }

    /// <summary>Create a new frame with a function name and line number.</summary>
    public TracebackFrame(string name, int line)
    {
        Name = name; Line = line;
    }

    /// <summary>Set the source filename.</summary>
    public TracebackFrame WithFilename(string filename)
    {
        Filename = filename; return this;
    }

    /// <summary>Provide source context lines directly.</summary>
    public TracebackFrame WithSourceContext(string source, int firstLine)
    {
        SourceContext = source; SourceFirstLine = Math.Max(1, firstLine); return this;
    }
}

/// <summary>Style configuration for traceback rendering. Port of TracebackStyle.</summary>
public sealed class TracebackStyle
{
    public WidgetStyle Title { get; set; }
    public WidgetStyle Border { get; set; }
    public WidgetStyle Filename { get; set; }
    public WidgetStyle Function { get; set; }
    public WidgetStyle Lineno { get; set; }
    public WidgetStyle Indicator { get; set; }
    public WidgetStyle Source { get; set; }
    public WidgetStyle ErrorLine { get; set; }
    public WidgetStyle ExceptionType { get; set; }
    public WidgetStyle ExceptionMessage { get; set; }

    public static TracebackStyle Default => new()
    {
        Title = default,
        Border = default,
        Filename = default,
        Function = default,
        Lineno = default,
        Indicator = default,
        Source = default,
        ErrorLine = default,
        ExceptionType = default,
        ExceptionMessage = default,
    };
}

/// <summary>Error traceback renderer. Port of Traceback struct and render().</summary>
public sealed class Traceback
{
    List<TracebackFrame> _frames = [];
    string _exceptionType = "";
    string _exceptionMessage = "";
    string _title = "Traceback (most recent call last)";
    TracebackStyle _style = TracebackStyle.Default;

    /// <summary>Create a new traceback with frames, exception type, and message.</summary>
    public Traceback(IEnumerable<TracebackFrame> frames, string exceptionType, string exceptionMessage)
    {
        _frames = new List<TracebackFrame>(frames);
        _exceptionType = exceptionType;
        _exceptionMessage = exceptionMessage;
    }

    /// <summary>Create a simple traceback with just frames (used by dashboard).</summary>
    public Traceback(IEnumerable<TracebackFrame> frames)
    {
        _frames = new List<TracebackFrame>(frames);
        _exceptionType = "Application Error";
        _exceptionMessage = "";
    }

    public Traceback WithTitle(string title) { _title = title; return this; }
    public Traceback WithStyle(TracebackStyle style) { _style = style; return this; }
    public void PushFrame(TracebackFrame frame) => _frames.Add(frame);
    public IReadOnlyList<TracebackFrame> Frames => _frames;
    public string ExceptionType => _exceptionType;
    public string ExceptionMessage => _exceptionMessage;

    /// <summary>Compute the number of lines needed to render this traceback.</summary>
    public int LineCount()
    {
        int count = 1; // title
        foreach (var f in _frames)
        {
            count++; // location line
            if (f.SourceContext is { } ctx)
                count += ctx.Split('\n').Length;
        }
        count++; // exception line
        return count;
    }

    /// <summary>Render the traceback into a frame. Port of render().</summary>
    public void Render(Rect area, Frame frame)
    {
        if (area.Height == 0 || area.Width == 0) return;
        int width = area.Width;
        ushort y = area.Y;
        ushort maxY = (ushort)(area.Y + area.Height);

        // Title line
        if (y < maxY)
        {
            DrawLine(frame, area.X, y, $"\u2500\u2500 {_title} \u2500\u2500", _style.Title, width);
            y++;
        }

        // Frames
        foreach (var f in _frames)
        {
            if (y >= maxY) break;
            // Location line: "  File "filename", line N, in name"
            var filename = f.Filename ?? "<?>";
            DrawLine(frame, area.X, y, $"  File \"{filename}\", line {f.Line}, in {f.Name}", _style.Filename, width);
            y++;
            if (y >= maxY) break;

            // Source context
            if (f.SourceContext is { } ctx)
            {
                var lines = ctx.Split('\n');
                for (int li = 0; li < lines.Length && y < maxY; li++, y++)
                {
                    bool isError = f.SourceFirstLine + li == f.Line;
                    var indent = isError ? " \u2771 " : "   ";
                    var style = isError ? _style.ErrorLine : _style.Source;
                    var num = (f.SourceFirstLine + li).ToString().PadLeft(4);
                    DrawLine(frame, area.X, y, $"{indent}{num} \u2502 {lines[li]}", style, width);
                }
            }
        }

        // Exception line
        if (y < maxY)
        {
            var exLine = string.IsNullOrEmpty(_exceptionMessage)
                ? _exceptionType : $"{_exceptionType}: {_exceptionMessage}";
            DrawLine(frame, area.X, y, exLine, _style.ExceptionType, width);
        }
    }

    static void DrawLine(Frame frame, ushort x, ushort y, string text, WidgetStyle style, int maxWidth)
    {
        for (int i = 0; i < text.Length && x + i < x + maxWidth; i++)
        {
            ushort cx = (ushort)(x + i);
            if (cx >= frame.Buffer.Width) break;
            var cell = Cell.FromChar(text[i]);
            WidgetDrawing.ApplyStyle(ref cell, style);
            frame.Buffer.SetFast(cx, y, cell);
        }
    }
}
