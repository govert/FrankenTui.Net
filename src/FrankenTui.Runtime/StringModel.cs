// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/string_model.rs
// Easy-mode adapter for string-based views.

using System.Globalization;
using FrankenTui.Render;

namespace FrankenTui.Runtime;

// ── StringModel trait ─────────────────────────────────────────────────────

public interface IStringModel<TMessage>
{
    Cmd<TMessage> Init() => Cmd<TMessage>.NoneCmd;
    Cmd<TMessage> Update(TMessage msg);
    string ViewString();
}

// ── StringModelAdapter (implements IModel<TMessage>) ──────────────────────

public sealed class StringModelAdapter<TMessage, S> : IModel<TMessage>
    where TMessage : class
    where S : IStringModel<TMessage>
{
    S _inner;

    public StringModelAdapter(S inner) => _inner = inner;
    public S Inner => _inner;

    public Cmd<TMessage> Init() => _inner.Init();
    public Cmd<TMessage> Update(TMessage msg) => _inner.Update(msg);

    public void View(Frame frame)
    {
        var s = _inner.ViewString();
        var text = StyledText.Raw(s);
        RenderTextToFrame(text, frame);
    }

    // ── render_text_to_frame ──────────────────────────────────────────────

    static void RenderTextToFrame(StyledText text, Frame frame)
    {
        int width = frame.Width, height = frame.Height;
        var lines = text.Lines;
        for (int y = 0; y < lines.Length && y < height; y++)
        {
            int x = 0;
            foreach (var span in lines[y].Spans)
            {
                if (x >= width) break;
                var spanStyle = span;
                foreach (var grapheme in span.Graphemes())
                {
                    if (x >= width) break;
                    int w = GraphemeWidth(grapheme);
                    if (w == 0) continue;
                    if (x + w > width) break;

                    CellContent content;
                    if (w > 1 || grapheme.Length > 1)
                    {
                        var id = frame.InternWithWidth(grapheme, (byte)w);
                        content = CellContent.FromGrapheme(id);
                    }
                    else if (grapheme.Length > 0)
                    {
                        content = CellContent.FromChar(grapheme[0]);
                    }
                    else continue;

                    var cell = new Cell(content, PackedRgba.White, PackedRgba.Transparent, CellAttributes.None);
                    ApplyStyle(ref cell, spanStyle);
                    frame.Buffer.Set((ushort)x, (ushort)y, cell);
                    x += w;
                }
            }
        }
    }

    // ── apply_style ───────────────────────────────────────────────────────

    static void ApplyStyle(ref Cell cell, StyledTextSpan style)
    {
        if (style.Fg is { } fg) cell = cell.WithForeground(fg);
        if (style.Bg is { } bg)
        {
            if (bg.A == 0) { }          // fully transparent: no-op
            else if (bg.A == 255) cell = cell.WithBackground(bg);
            else cell = cell.WithBackground(bg.Over(cell.Background));  // composite src-over-dst
        }
        if (style.Attrs is { } attrs)
            cell = cell.WithAttributes(cell.Attributes.MergedFlags(attrs));
    }

    // ── grapheme_width (port of ftui_core::text_width::grapheme_width) ────

    internal static int GraphemeWidth(string g)
    {
        if (string.IsNullOrEmpty(g)) return 0;
        // Sum width of all chars; zero-width joiners contribute 0
        int w = 0;
        foreach (char c in g)
        {
            if (char.IsControl(c)) { /* skip */ }
            else if (c == 0x200D || c == 0xFE0F || c == 0xFE0E) { /* ZWJ, VS16, VS15 */ }
            else if (c >= 0x1100 && c <= 0x115F) w += 2;      // Hangul Jamo
            else if (c >= 0x2E80 && c <= 0xA4CF) w += 2;      // CJK
            else if (c >= 0xAC00 && c <= 0xD7A3) w += 2;      // Hangul Syllables
            else if (c >= 0xF900 && c <= 0xFAFF) w += 2;      // CJK Compat
            else if (c >= 0xFE10 && c <= 0xFE19) w += 2;      // Vertical forms
            else if (c >= 0xFE30 && c <= 0xFE6F) w += 2;      // CJK Compat Forms
            else if (c >= 0xFF01 && c <= 0xFF60) w += 2;      // Fullwidth
            else if (c >= 0xFFE0 && c <= 0xFFE6) w += 2;      // Fullwidth
            else if (c >= 0x1F300 && c <= 0x1F9FF) w += 2;    // Emoji/Emoticons
            else w += 1;
        }
        return w > 0 ? w : 1;
    }
}

// ── Minimal StyledText / Span / Line (from ftui_text::text) ───────────────

public sealed class StyledText
{
    public StyledLine[] Lines { get; }
    StyledText(StyledLine[] lines) => Lines = lines;

    public static StyledText Raw(string s) =>
        new(string.IsNullOrEmpty(s)
            ? Array.Empty<StyledLine>()
            : s.Split('\n').Select(l => StyledLine.Raw(l)).ToArray());
}

public readonly record struct StyledLine
{
    public StyledTextSpan[] Spans { get; }
    StyledLine(StyledTextSpan[] spans) => Spans = spans;
    public static StyledLine Raw(string s) => new(new[] { StyledTextSpan.Raw(s) });
}

public readonly record struct StyledTextSpan
{
    public string Content { get; }
    public PackedRgba? Fg { get; }
    public PackedRgba? Bg { get; }
    public CellStyleFlags? Attrs { get; }

    public static StyledTextSpan Raw(string s) => new(s, null, null, null);

    StyledTextSpan(string content, PackedRgba? fg, PackedRgba? bg, CellStyleFlags? attrs)
    {
        Content = content; Fg = fg; Bg = bg; Attrs = attrs;
    }

    public IEnumerable<string> Graphemes()
    {
        var te = StringInfo.GetTextElementEnumerator(Content);
        while (te.MoveNext()) yield return te.GetTextElement();
    }
}
