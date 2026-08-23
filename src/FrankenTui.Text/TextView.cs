// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-text/src/view.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using System.Collections;

namespace FrankenTui.Text;

/// <summary>A terminal viewport measured in display cells and rows.</summary>
public readonly record struct Viewport
{
    public Viewport(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        Width = width;
        Height = height;
    }

    /// <summary>Width in terminal display cells.</summary>
    public int Width { get; }

    /// <summary>Height in terminal rows.</summary>
    public int Height { get; }
}

/// <summary>A single wrapped virtual line.</summary>
public sealed record ViewLine
{
    public ViewLine(string text, int sourceLine, bool isWrap, int width)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(sourceLine);
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        Text = text;
        SourceLine = sourceLine;
        IsWrap = isWrap;
        Width = width;
    }

    /// <summary>Rendered text for this virtual line.</summary>
    public string Text { get; }

    /// <summary>Zero-based logical line index in the source snapshot.</summary>
    public int SourceLine { get; }

    /// <summary>Whether this line continues a wrapped source line.</summary>
    public bool IsWrap { get; }

    /// <summary>Line width in terminal display cells.</summary>
    public int Width { get; }
}

/// <summary>A scrollable, wrapped view over an immutable text snapshot.</summary>
/// <remarks>
/// The source Rust type owns a Rope. The managed text assembly cannot depend on
/// the widget assembly that owns the compatibility Rope, so this type snapshots
/// its string value. Newline-delimited source-line and virtual-line indexes are
/// integers; line widths and viewport widths are terminal display cells. This
/// API does not expose UTF-16, Unicode-scalar, grapheme, or UTF-8 byte offsets.
/// Wrapping itself delegates to <see cref="TextWrapper"/> and therefore retains
/// the established grapheme-boundary and display-cell behavior.
/// </remarks>
public sealed class TextView : ICloneable
{
    private string text;
    private TextWrapMode wrapMode;
    private int width;
    private ViewLine[] lines = [];
    private IReadOnlyList<ViewLine> linesView = Array.Empty<ViewLine>();
    private int maxWidth;
    private int sourceLineCount;

    /// <summary>Create a view from an immutable snapshot of <paramref name="text"/>.</summary>
    public TextView(string text, int width, TextWrapMode wrapMode)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ValidateWrapMode(wrapMode);
        Bidi.ValidateText(text);

        this.text = text;
        this.width = width;
        this.wrapMode = wrapMode;
        Rebuild();
    }

    /// <summary>Current wrapping mode.</summary>
    public TextWrapMode WrapMode => wrapMode;

    /// <summary>Viewport width used for wrapping, in terminal display cells.</summary>
    public int Width => width;

    /// <summary>Number of logical newline-delimited source lines.</summary>
    public int SourceLineCount => sourceLineCount;

    /// <summary>Number of virtual wrapped lines.</summary>
    public int VirtualLineCount => lines.Length;

    /// <summary>Maximum terminal display-cell width across all virtual lines.</summary>
    public int MaxWidth => maxWidth;

    /// <summary>All virtual lines in display order.</summary>
    public IReadOnlyList<ViewLine> Lines => linesView;

    /// <summary>Replace the immutable source snapshot and rebuild the view.</summary>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Bidi.ValidateText(text);
        this.text = text;
        Rebuild();
    }

    /// <summary>Update wrapping mode, rebuilding only when it changes.</summary>
    public void SetWrap(TextWrapMode wrapMode)
    {
        ValidateWrapMode(wrapMode);
        if (this.wrapMode == wrapMode) return;
        this.wrapMode = wrapMode;
        Rebuild();
    }

    /// <summary>Update viewport width, rebuilding only when it changes.</summary>
    public void SetWidth(int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        if (this.width == width) return;
        this.width = width;
        Rebuild();
    }

    /// <summary>Map a source-line index to its first virtual-line index.</summary>
    public int? SourceToVirtual(int sourceLine)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sourceLine);
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].SourceLine == sourceLine) return i;
        }
        return null;
    }

    /// <summary>Map a virtual-line index to its source-line index.</summary>
    public int? VirtualToSource(int virtualLine)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(virtualLine);
        return virtualLine < lines.Length ? lines[virtualLine].SourceLine : null;
    }

    /// <summary>Clamp a virtual-line scroll offset for a viewport height.</summary>
    public int ClampScroll(int scrollY, int viewportHeight)
    {
        ValidateScrollArguments(scrollY, viewportHeight);
        int total = lines.Length;
        if (total == 0) return 0;
        if (viewportHeight == 0) return Math.Min(scrollY, total);
        return Math.Min(scrollY, Math.Max(0, total - viewportHeight));
    }

    /// <summary>Maximum virtual-line scroll offset for a viewport height.</summary>
    public int MaxScroll(int viewportHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(viewportHeight);
        int total = lines.Length;
        if (total == 0) return 0;
        if (viewportHeight == 0) return total;
        return Math.Max(0, total - viewportHeight);
    }

    /// <summary>Visible half-open virtual-line range for a scroll offset and height.</summary>
    public Range VisibleRange(int scrollY, int viewportHeight)
    {
        ValidateScrollArguments(scrollY, viewportHeight);
        int total = lines.Length;
        if (total == 0 || viewportHeight == 0) return new Range(0, 0);
        int scroll = ClampScroll(scrollY, viewportHeight);
        int end = (int)Math.Min((long)scroll + viewportHeight, total);
        return new Range(scroll, end);
    }

    /// <summary>Zero-copy read-only view of the visible virtual lines.</summary>
    public IReadOnlyList<ViewLine> VisibleLines(int scrollY, int viewportHeight)
    {
        Range range = VisibleRange(scrollY, viewportHeight);
        (int offset, int length) = range.GetOffsetAndLength(lines.Length);
        return length == 0 ? Array.Empty<ViewLine>() : new ReadOnlyLineSlice(lines, offset, length);
    }

    /// <summary>Scroll so a source line begins at the top, or null when absent.</summary>
    public int? ScrollToLine(int sourceLine, int viewportHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sourceLine);
        ArgumentOutOfRangeException.ThrowIfNegative(viewportHeight);
        int? virtualLine = SourceToVirtual(sourceLine);
        return virtualLine is null ? null : ClampScroll(virtualLine.Value, viewportHeight);
    }

    public int ScrollToTop() => 0;

    public int ScrollToBottom(int viewportHeight) => MaxScroll(viewportHeight);

    /// <summary>Scroll by a signed number of virtual lines.</summary>
    public int ScrollByLines(int scrollY, int delta, int viewportHeight)
    {
        ValidateScrollArguments(scrollY, viewportHeight);
        long next = (long)scrollY + delta;
        int candidate = next <= 0 ? 0 : next >= int.MaxValue ? int.MaxValue : (int)next;
        return ClampScroll(candidate, viewportHeight);
    }

    /// <summary>Scroll by a signed number of viewport-height pages.</summary>
    public int ScrollByPages(int scrollY, int pages, int viewportHeight)
    {
        ValidateScrollArguments(scrollY, viewportHeight);
        if (viewportHeight == 0) return ClampScroll(scrollY, viewportHeight);
        long next = scrollY + ((long)viewportHeight * pages);
        int candidate = next <= 0 ? 0 : next >= int.MaxValue ? int.MaxValue : (int)next;
        return ClampScroll(candidate, viewportHeight);
    }

    /// <summary>Clone the owned snapshot and its derived layout.</summary>
    public TextView Clone() => new(text, width, wrapMode);

    object ICloneable.Clone() => Clone();

    private void Rebuild()
    {
        var rebuilt = new List<ViewLine>();
        int widest = 0;
        string[] sourceLines = text.Split('\n', StringSplitOptions.None);
        var options = new WrapOptions(width)
            .WithMode(wrapMode)
            .WithPreserveIndent(wrapMode == TextWrapMode.Char);

        for (int sourceLine = 0; sourceLine < sourceLines.Length; sourceLine++)
        {
            string lineText = sourceLines[sourceLine];
            if (lineText.EndsWith('\r')) lineText = lineText[..^1];

            IReadOnlyList<string> wrapped = TextWrapper.WrapWithOptions(lineText, options);
            if (wrapped.Count == 0)
            {
                rebuilt.Add(new ViewLine(string.Empty, sourceLine, false, 0));
                continue;
            }

            for (int index = 0; index < wrapped.Count; index++)
            {
                string part = wrapped[index];
                int lineWidth = TextWrapper.DisplayWidth(part);
                widest = Math.Max(widest, lineWidth);
                rebuilt.Add(new ViewLine(part, sourceLine, index > 0, lineWidth));
            }
        }

        lines = rebuilt.ToArray();
        linesView = Array.AsReadOnly(lines);
        maxWidth = widest;
        sourceLineCount = sourceLines.Length;
    }

    private static void ValidateWrapMode(TextWrapMode wrapMode)
    {
        if (wrapMode is TextWrapMode.None or TextWrapMode.Word or TextWrapMode.Char or
            TextWrapMode.Optimal or TextWrapMode.WordChar)
        {
            return;
        }
        throw new ArgumentOutOfRangeException(nameof(wrapMode), wrapMode, "Unknown text wrapping mode.");
    }

    private static void ValidateScrollArguments(int scrollY, int viewportHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(scrollY);
        ArgumentOutOfRangeException.ThrowIfNegative(viewportHeight);
    }

    private sealed class ReadOnlyLineSlice : IReadOnlyList<ViewLine>
    {
        private readonly ViewLine[] source;
        private readonly int offset;

        public ReadOnlyLineSlice(ViewLine[] source, int offset, int count)
        {
            this.source = source;
            this.offset = offset;
            Count = count;
        }

        public int Count { get; }

        public ViewLine this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
                return source[offset + index];
            }
        }

        public IEnumerator<ViewLine> GetEnumerator()
        {
            for (int index = 0; index < Count; index++) yield return source[offset + index];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}