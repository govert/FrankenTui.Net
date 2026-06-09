// SPDX-License-Identifier: Apache-2.0
// Minimal port of ftui_text::Text / Line / Span for widget rendering.
// Text wraps styled content for display in Paragraph/List/Table widgets.

using System.Globalization;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

// ── TextLine ──────────────────────────────────────────────────────────────

public readonly record struct TextLine
{
    public TextSpan[] Spans { get; }
    TextLine(TextSpan[] spans) => Spans = spans;
    public static TextLine Raw(string s) => new(new[]{TextSpan.Raw(s)});
    public static TextLine FromSpans(TextSpan[] spans) => new(spans);
    public int Width => Spans.Sum(s => s.Width);
    public bool IsEmpty => Width == 0;
}

// ── TextSpan ──────────────────────────────────────────────────────────────

public readonly record struct TextSpan
{
    public string Content { get; }
    public WidgetStyle Style { get; }

    TextSpan(string content, WidgetStyle style) => (Content, Style) = (content, style);
    public static TextSpan Raw(string s) => new(s, WidgetStyle.Default);
    public static TextSpan Styled(string s, WidgetStyle style) => new(s, style);

    public int Width
    {
        get {
            int w = 0;
            var te = StringInfo.GetTextElementEnumerator(Content);
            while (te.MoveNext()) w += WidgetDrawing.GraphemeWidth(te.GetTextElement());
            return w;
        }
    }

    public string[] Graphemes()
    {
        var list = new List<string>();
        var te = StringInfo.GetTextElementEnumerator(Content);
        while (te.MoveNext()) list.Add(te.GetTextElement());
        return list.ToArray();
    }
}

// ── TextContent ───────────────────────────────────────────────────────────

public sealed class TextContent
{
    public TextLine[] Lines { get; }
    TextContent(TextLine[] lines) => Lines = lines;

    public static TextContent Raw(string s) =>
        new(string.IsNullOrEmpty(s)
            ? Array.Empty<TextLine>()
            : s.Split('\n').Select(l => TextLine.Raw(l)).ToArray());

    public static TextContent FromLines(TextLine[] lines) => new(lines);
    public int DisplayWidth => Lines.Length > 0 ? Lines.Max(l => l.Width) : 0;
    public int LineCount => Lines.Length;

    public TextSpan[] AllSpans() => Lines.SelectMany(l => l.Spans).ToArray();
}
