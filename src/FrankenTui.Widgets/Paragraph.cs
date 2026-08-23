// Port of .external/frankentui/crates/ftui-widgets/src/paragraph.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Paragraph widget renders multi-line styled text with wrapping, scrolling, alignment, and accessibility.
// Wrap helpers ported from .external/frankentui/crates/ftui-text/src/text.rs (lines 719-1049).

using System.Globalization;
using System.Text;
using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Render;
using CanonicalA11y = FrankenTui.A11y;
// Alias to avoid ambiguity between the Alignment enum type and the Paragraph.Alignment() builder method.
using AlignmentValue = FrankenTui.Widgets.Alignment;

namespace FrankenTui.Widgets;

// ── WrapMode ─────────────────────────────────────────────────────────────────

/// <summary>
/// Text wrapping modes for the Paragraph widget.
/// Port of ftui_text::WrapMode (wrap.rs).
/// </summary>
public enum WrapMode
{
    /// <summary>No wrapping – lines may exceed width.</summary>
    None,
    /// <summary>Wrap at word boundaries when possible.</summary>
    Word,
    /// <summary>Wrap at character (grapheme) boundaries.</summary>
    Char,
    /// <summary>Word wrap with character fallback for long words.</summary>
    WordChar,
    /// <summary>Knuth-Plass optimal line breaking. Maps to word-wrap (char_fallback=false) — upstream Line::wrap Optimal arm.</summary>
    Optimal,
}

// ── Cache types ───────────────────────────────────────────────────────────────

/// <summary>Cached metrics for a paragraph's unwrapped text. Port of CachedParagraphMetrics.</summary>
internal sealed class CachedParagraphMetrics
{
    public int TextWidth { get; }
    public int TextHeight { get; }
    public int MinWidth { get; }
    // DIVERGENCE: Arc<[usize]> -> int[] (value-typed, no shared ownership needed in C#).
    public int[] LineWidths { get; }

    public CachedParagraphMetrics(int textWidth, int textHeight, int minWidth, int[] lineWidths)
    {
        TextWidth = textWidth;
        TextHeight = textHeight;
        MinWidth = minWidth;
        LineWidths = lineWidths;
    }
}

/// <summary>Cached wrapped lines for a paragraph. Port of CachedWrappedParagraph.</summary>
internal sealed class CachedWrappedParagraph
{
    // DIVERGENCE: Arc<[Line<'static>]> -> TextLine[] (value-typed TextLine).
    public TextLine[] Lines { get; }
    public int[] LineWidths { get; }

    public CachedWrappedParagraph(TextLine[] lines, int[] lineWidths)
    {
        Lines = lines;
        LineWidths = lineWidths;
    }
}

/// <summary>Cache key for wrapped paragraphs. Port of ParagraphWrapCacheKey.</summary>
internal readonly struct ParagraphWrapCacheKey : IEquatable<ParagraphWrapCacheKey>
{
    public readonly ulong TextHash;
    public readonly WrapMode WrapMode;
    public readonly int Width;

    public ParagraphWrapCacheKey(ulong textHash, WrapMode wrapMode, int width)
    {
        TextHash = textHash;
        WrapMode = wrapMode;
        Width = width;
    }

    public bool Equals(ParagraphWrapCacheKey other)
        => TextHash == other.TextHash && WrapMode == other.WrapMode && Width == other.Width;

    public override bool Equals(object? obj)
        => obj is ParagraphWrapCacheKey k && Equals(k);

    public override int GetHashCode()
        => HashCode.Combine(TextHash, WrapMode, Width);
}

/// <summary>
/// Thread-local cache state for Paragraph metrics and wrapped lines.
/// Port of ParagraphCacheState (paragraph.rs:43-70).
/// </summary>
internal sealed class ParagraphCacheState
{
    private const int MetricsCacheCapacity = 256;  // PARAGRAPH_METRICS_CACHE_CAPACITY
    private const int WrappedCacheCapacity = 256;  // PARAGRAPH_WRAP_CACHE_CAPACITY

    private readonly Dictionary<ulong, CachedParagraphMetrics> _metrics = new();
    private readonly Queue<ulong> _metricsFifo = new();
    private readonly Dictionary<ParagraphWrapCacheKey, CachedWrappedParagraph> _wrapped = new();
    private readonly Queue<ParagraphWrapCacheKey> _wrappedFifo = new();

    public CachedParagraphMetrics? GetMetrics(ulong key)
        => _metrics.GetValueOrDefault(key);

    public void InsertMetrics(ulong key, CachedParagraphMetrics value)
        => CacheInsert(_metrics, _metricsFifo, MetricsCacheCapacity, key, value);

    public CachedWrappedParagraph? GetWrapped(ParagraphWrapCacheKey key)
        => _wrapped.GetValueOrDefault(key);

    public void InsertWrapped(ParagraphWrapCacheKey key, CachedWrappedParagraph value)
        => CacheInsert(_wrapped, _wrappedFifo, WrappedCacheCapacity, key, value);

    /// <summary>
    /// FIFO-evicting cache insert. Port of cache_insert (paragraph.rs:76-94).
    /// If the map is at capacity, evict the oldest entry before inserting.
    /// Always overwrites if the key already exists (matching upstream behavior).
    /// </summary>
    private static void CacheInsert<K, V>(
        Dictionary<K, V> map,
        Queue<K> fifo,
        int capacity,
        K key,
        V value)
        where K : notnull
    {
        if (!map.ContainsKey(key))
        {
            if (map.Count >= capacity && fifo.TryDequeue(out var oldest))
                map.Remove(oldest);
            fifo.Enqueue(key);
        }
        map[key] = value;
    }
}

// ── Paragraph widget ──────────────────────────────────────────────────────────

/// <summary>
/// A widget that renders multi-line styled text.
/// Port of ftui_widgets::Paragraph (paragraph.rs:104-607).
/// </summary>
public sealed class Paragraph : IWidget, IMeasurableWidget, IAccessible, CanonicalA11y.IAccessible
{
    // ── Constants ─────────────────────────────────────────────────────────

    private const int AccessibleTextLimit = 200;       // ACCESSIBLE_TEXT_LIMIT
    private const int AccessibleTextPrefixLimit = 197; // ACCESSIBLE_TEXT_PREFIX_LIMIT

    // ── Thread-local cache ────────────────────────────────────────────────

    // DIVERGENCE: Rust uses thread_local! with RefCell<ParagraphCacheState>. C# uses [ThreadStatic].
    [ThreadStatic]
    private static ParagraphCacheState? _cacheState;

    private static ParagraphCacheState Cache
        => _cacheState ??= new ParagraphCacheState();

    // ── Fields ────────────────────────────────────────────────────────────

    // DIVERGENCE: text_into_owned / Arc lifetime management omitted; TextContent is a C# reference
    // type already holding owned strings. The 'static bound from the Rust type alias is satisfied
    // by C# value semantics for TextContent.
    private readonly TextContent _text;
    private Block? _block;
    private WidgetStyle _style;
    private WrapMode? _wrap;
    private AlignmentValue _alignment;
    private (ushort vertical, ushort horizontal) _scroll;

    // ── Constructor ───────────────────────────────────────────────────────

    /// <summary>
    /// Create a new paragraph from the given text.
    /// Port of Paragraph::new (paragraph.rs:142-151).
    /// </summary>
    public Paragraph(TextContent text)
    {
        _text = text;
        _alignment = AlignmentValue.Left;
        _scroll = (0, 0);
    }

    // ── Builder methods ───────────────────────────────────────────────────

    /// <summary>Set the surrounding block. Port of Paragraph::block.</summary>
    public Paragraph Block(Block block) { _block = block; return this; }

    /// <summary>Set the base text style. Port of Paragraph::style.</summary>
    public Paragraph Style(WidgetStyle style) { _style = style; return this; }

    /// <summary>Set the text wrapping mode. Port of Paragraph::wrap.</summary>
    public Paragraph Wrap(WrapMode wrap) { _wrap = wrap; return this; }

    /// <summary>Set the text alignment. Port of Paragraph::alignment.</summary>
    public Paragraph Alignment(AlignmentValue alignment) { _alignment = alignment; return this; }

    /// <summary>Set the scroll offset as (vertical, horizontal). Port of Paragraph::scroll.</summary>
    public Paragraph Scroll((ushort vertical, ushort horizontal) offset)
    {
        _scroll = offset;
        return this;
    }

    // ── IWidget ───────────────────────────────────────────────────────────

    /// <summary>
    /// Render the paragraph into the given area and frame.
    /// Port of Widget::render for Paragraph (paragraph.rs:276-448).
    /// </summary>
    public void Render(Rect area, Frame frame)
    {
        var deg = frame.Degradation;

        // Skeleton+: clear the owned area so previously rendered content does not linger.
        if (!deg.RenderContent())
        {
            WidgetDrawing.ClearTextArea(frame, area, WidgetStyle.Default);
            return;
        }

        // Special-case: an empty Paragraph with no Block is commonly used as a screen-clear.
        // In that mode we must clear cell *content* (not just paint style), otherwise old
        // borders/characters can bleed through Flex gaps.
        var style = deg.ApplyStyling() ? _style : WidgetStyle.Default;
        if (_block == null && _text.Lines.Length == 0)
        {
            WidgetDrawing.ClearTextArea(frame, area, style);
            return;
        }

        WidgetDrawing.ClearTextArea(frame, area, style);

        var textArea = area;
        if (_block != null)
        {
            _block.Render(area, frame);
            textArea = _block.Inner(area);
        }

        if (textArea.Width == 0 || textArea.Height == 0)
            return;

        // At NoStyling, render text without per-span styles.
        // Background is already applied for the whole area via ClearTextArea. When drawing
        // text we avoid re-applying the same background, otherwise semi-transparent BG colors
        // get composited multiple times.
        var textStyle = deg.ApplyStyling()
            ? new WidgetStyle(_style.Fg, null, _style.Attrs)
            : WidgetStyle.Default;

        ushort y = textArea.Y;
        int currentVisualLine = 0;
        int scrollOffset = _scroll.vertical;

        // Local render-line closure — mirrors the Rust closure inside render.
        void RenderLine(TextLine line, int lineWidth)
        {
            ushort scrollX = _scroll.horizontal;
            ushort startX = AlignX(textArea, lineWidth, _alignment);

            // span_visual_offset: relative to line start (in visual columns).
            int spanVisualOffset = 0;

            // Alignment offset relative to text_area.x.
            int alignmentOffset = startX - textArea.X;

            foreach (var span in line.Spans)
            {
                int spanWidth = span.Width;

                // Effective position of this span relative to text_area.x.
                int lineRelStart = alignmentOffset + spanVisualOffset;

                // Check visibility: fully scrolled out to the left.
                if (lineRelStart + spanWidth <= scrollX)
                {
                    spanVisualOffset += spanWidth;
                    continue;
                }

                // Calculate actual draw position.
                ushort drawX;
                ushort localScroll;

                if (lineRelStart < scrollX)
                {
                    // Partially scrolled out left.
                    drawX = textArea.X;
                    localScroll = (ushort)(scrollX - lineRelStart);
                }
                else
                {
                    // Start is visible.
                    drawX = (ushort)(textArea.X + (lineRelStart - scrollX));
                    localScroll = 0;
                }

                if (drawX >= textArea.Right)
                    break; // Fully clipped to the right.

                // At NoStyling+, ignore span-level styles entirely.
                var spanStyle = deg.ApplyStyling()
                    ? (span.Style.IsEmpty ? textStyle : MergeStyles(span.Style, textStyle))
                    : textStyle;

                if (localScroll > 0)
                {
                    WidgetDrawing.DrawTextSpanScrolled(
                        frame, drawX, y, span.Content, spanStyle, textArea.Right, localScroll);
                }
                else
                {
                    WidgetDrawing.DrawTextSpan(
                        frame, drawX, y, span.Content, spanStyle, textArea.Right);
                }

                spanVisualOffset += spanWidth;
            }
        }

        var metrics = CachedMetrics();
        CachedWrappedParagraph? renderedLines = _wrap.HasValue
            ? CachedWrappedLines(textArea.Width, _wrap.Value)
            : null;

        if (renderedLines != null)
        {
            var lines = renderedLines.Lines;
            var lineWidths = renderedLines.LineWidths;
            for (int i = 0; i < lines.Length; i++)
            {
                if (currentVisualLine < scrollOffset) { currentVisualLine++; continue; }
                if (y >= textArea.Bottom) break;
                RenderLine(lines[i], lineWidths[i]);
                y = (ushort)(y + 1);
                currentVisualLine++;
            }
        }
        else
        {
            var lines = _text.Lines;
            var lineWidths = metrics.LineWidths;
            for (int i = 0; i < lines.Length; i++)
            {
                if (currentVisualLine < scrollOffset) { currentVisualLine++; continue; }
                if (y >= textArea.Bottom) break;
                RenderLine(lines[i], lineWidths[i]);
                y = (ushort)(y + 1);
                currentVisualLine++;
            }
        }
    }

    // ── IMeasurableWidget ─────────────────────────────────────────────────

    /// <summary>
    /// Measure preferred and minimum size for layout purposes.
    /// Port of MeasurableWidget::measure for Paragraph (paragraph.rs:449-509).
    /// </summary>
    public SizeConstraints Measure(Size available)
    {
        var metrics = CachedMetrics();
        int textWidth = metrics.TextWidth;
        int textHeight = metrics.TextHeight;
        int minWidth = metrics.MinWidth;

        // Get block chrome if present.
        var (chromeWidth, chromeHeight) = _block != null
            ? _block.ChromeSize()
            : ((ushort)0, (ushort)0);

        int preferredWidth, preferredHeight;

        if (_wrap.HasValue && _wrap.Value != WrapMode.None)
        {
            // When wrapping, preferred width is either the text width or available width.
            int wrapWidth = available.Width > chromeWidth
                ? available.Width - chromeWidth
                : 1;

            int wrappedHeight = CachedWrappedLines(wrapWidth, _wrap.Value).Lines.Length;

            // Preferred width is min(text_width, available_width - chrome).
            preferredWidth = Math.Min(textWidth, wrapWidth);
            preferredHeight = wrappedHeight;
        }
        else
        {
            // No wrapping: preferred is natural text dimensions.
            preferredWidth = textWidth;
            preferredHeight = textHeight;
        }

        // Convert to u16, saturating at MAX.
        ushort minW = (ushort)Math.Min(minWidth + chromeWidth, ushort.MaxValue);
        // Only require 1 line minimum if there's actual content.
        ushort minH = preferredHeight > 0
            ? (ushort)Math.Min(1 + chromeHeight, ushort.MaxValue)
            : chromeHeight;

        ushort prefW = (ushort)Math.Min(preferredWidth + chromeWidth, ushort.MaxValue);
        ushort prefH = (ushort)Math.Min(preferredHeight + chromeHeight, ushort.MaxValue);

        return new SizeConstraints
        {
            Min = new Size(minW, minH),
            Preferred = new Size(prefW, prefH),
            Max = null, // Paragraph can use additional space for scrolling.
        };
    }

    /// <summary>
    /// Paragraph always has intrinsic size based on its text content.
    /// Port of MeasurableWidget::has_intrinsic_size (paragraph.rs:505-508).
    /// </summary>
    public bool HasIntrinsicSize() => true;

    /// <inheritdoc/>
    public SizeConstraints MeasureConstraints(Size available) => Measure(available);

    /// <inheritdoc/>
    public SizeHint MeasureAxis(Size available, LayoutDirection direction)
    {
        var c = Measure(available);
        return direction == LayoutDirection.Horizontal
            ? new SizeHint(c.Min.Width, c.Preferred.Width, c.Max?.Width)
            : new SizeHint(c.Min.Height, c.Preferred.Height, c.Max?.Height);
    }

    // ── IAccessible ───────────────────────────────────────────────────────

    /// <summary>
    /// Get the legacy compatibility projection of this widget's accessibility nodes.
    /// Port of ftui_a11y::Accessible impl for Paragraph (paragraph.rs:571-607).
    /// </summary>
    public List<A11yNodeInfo> AccessibilityNodes(Rect area) =>
        LegacyAccessibilityAdapter.FromCanonical(CanonicalAccessibilityNodes(area));

    List<CanonicalA11y.A11yNodeInfo> CanonicalA11y.IAccessible.AccessibilityNodes(Rect area) =>
        CanonicalAccessibilityNodes(area);

    private List<CanonicalA11y.A11yNodeInfo> CanonicalAccessibilityNodes(Rect area)
    {
        ulong id = WidgetDrawing.A11yNodeId(area);

        // Extract the plain-text content for the accessible name.
        var name = string.Join(" ",
            _text.Lines.Select(line =>
                string.Concat(line.Spans.Select(s => s.Content))));

        string? blockTitle = _block?.TitleText();
        string truncatedName = TruncateAccessibleText(name);

        CanonicalA11y.A11yNodeInfo node = CanonicalA11y.A11yNodeInfo.New(
            id,
            CanonicalA11y.A11yRole.Label,
            area);
        if (blockTitle != null)
        {
            node = node.WithName(blockTitle);
            if (!string.IsNullOrEmpty(name))
                node = node.WithDescription(truncatedName);
        }
        else if (!string.IsNullOrEmpty(name))
        {
            node = node.WithName(truncatedName);
        }

        return [node];
    }

    // ── Internal helpers exposed for tests ────────────────────────────────

    /// <summary>
    /// Calculate minimum required width (longest word).
    /// Port of Paragraph::calculate_min_width (paragraph.rs:513-515).
    /// </summary>
    internal int CalculateMinWidth() => CachedMetrics().MinWidth;

    /// <summary>
    /// Estimate total visual line count after wrapping at wrapWidth.
    /// Port of Paragraph::estimate_wrapped_height (paragraph.rs:517-528).
    /// </summary>
    internal int EstimateWrappedHeight(int wrapWidth)
    {
        if (wrapWidth == 0)
            return CachedMetrics().TextHeight;

        if (_wrap.HasValue)
            return Math.Max(CachedWrappedLines(wrapWidth, _wrap.Value).Lines.Length, 1);

        return Math.Max(CachedMetrics().TextHeight, 1);
    }

    // ── Private: caching ──────────────────────────────────────────────────

    private ulong TextHash() => HashTextContent(_text);

    /// <summary>Port of Paragraph::cached_metrics (paragraph.rs:192-225).</summary>
    private CachedParagraphMetrics CachedMetrics()
    {
        ulong textHash = TextHash();
        var cache = Cache;
        var existing = cache.GetMetrics(textHash);
        if (existing != null) return existing;

        int textWidth = 0;
        int minWidth = 0;
        var lineWidths = new int[_text.Lines.Length];

        for (int i = 0; i < _text.Lines.Length; i++)
        {
            var line = _text.Lines[i];
            int width = line.Width;
            textWidth = Math.Max(textWidth, width);
            int lmw = LineMinWidth(line);
            minWidth = Math.Max(minWidth, lmw);
            lineWidths[i] = width;
        }

        var metrics = new CachedParagraphMetrics(
            textWidth,
            _text.Lines.Length,
            minWidth == 0 ? textWidth : minWidth,
            lineWidths);

        cache.InsertMetrics(textHash, metrics);
        return metrics;
    }

    /// <summary>Port of Paragraph::cached_wrapped_lines (paragraph.rs:227-272).</summary>
    private CachedWrappedParagraph CachedWrappedLines(int width, WrapMode wrapMode)
    {
        var key = new ParagraphWrapCacheKey(TextHash(), wrapMode, width);
        var cache = Cache;
        var existing = cache.GetWrapped(key);
        if (existing != null) return existing;

        var lines = new List<TextLine>();
        var lineWidths = new List<int>();

        foreach (var line in _text.Lines)
        {
            int lineWidth = line.Width;
            if (wrapMode == WrapMode.None || lineWidth <= width)
            {
                lines.Add(line);
                lineWidths.Add(lineWidth);
                continue;
            }

            // Mirrors: let wrapped_lines = line.wrap(width, wrap_mode);
            var wrappedLines = WrapLine(line, width, wrapMode);
            if (wrappedLines.Count == 0)
            {
                lines.Add(TextLine.Raw(""));
                lineWidths.Add(0);
                continue;
            }

            foreach (var wl in wrappedLines)
            {
                int wlw = wl.Width;
                lineWidths.Add(wlw);
                lines.Add(wl);
            }
        }

        var result = new CachedWrappedParagraph(lines.ToArray(), lineWidths.ToArray());
        cache.InsertWrapped(key, result);
        return result;
    }

    // ── Private: line wrapping (ports of text.rs helpers) ─────────────────

    /// <summary>
    /// Dispatch to the appropriate wrap algorithm.
    /// Port of Line::wrap (text.rs:360-375):
    ///   None / width==0  -> clone
    ///   is_empty         -> vec![Line::new()]
    ///   Char             -> wrap_line_chars
    ///   Word | Optimal   -> wrap_line_words(char_fallback=false)
    ///   WordChar         -> wrap_line_words(char_fallback=true)
    /// </summary>
    private static List<TextLine> WrapLine(TextLine line, int width, WrapMode mode)
    {
        if (mode == WrapMode.None || width == 0)
            return new List<TextLine> { line };

        if (line.IsEmpty)
            return new List<TextLine> { TextLine.Raw("") };

        return mode switch
        {
            WrapMode.Char    => WrapLineChars(line, width),
            WrapMode.Word    => WrapLineWords(line, width, charFallback: false),
            WrapMode.Optimal => WrapLineWords(line, width, charFallback: false),
            WrapMode.WordChar => WrapLineWords(line, width, charFallback: true),
            _ => new List<TextLine> { line },
        };
    }

    /// <summary>
    /// Wrap by grapheme (cell) boundary, preserving span styles via PushSpanMerged.
    /// Faithful port of wrap_line_chars (text.rs:882-936).
    ///
    /// For each span, loop over remaining content:
    ///   - If current line is full: flush (TrimLineTrailing), reset.
    ///   - available = max(1, width - current_width).
    ///   - If remaining fits: PushSpanMerged, advance to next span.
    ///   - Else split at available cells via SplitAtCell.
    ///     - Force-progress: if left is empty AND line is empty AND remaining non-empty,
    ///       split at max(1, first_grapheme_width).
    ///   - PushSpanMerged(left), flush line, remaining = right, loop.
    /// Final: flush last line; always emit at least one entry.
    /// </summary>
    private static List<TextLine> WrapLineChars(TextLine line, int width)
    {
        var lines = new List<TextLine>();
        var current = new List<TextSpan>();  // Line::new()
        int currentWidth = 0;

        foreach (var span in line.Spans)
        {
            var remaining = span;

            while (!IsSpanEmpty(remaining))
            {
                if (currentWidth >= width && current.Count > 0)
                {
                    lines.Add(TrimLineTrailing(current));
                    current = new List<TextSpan>();
                    currentWidth = 0;
                }

                int available = Math.Max(1, width - currentWidth);
                int spanWidth = remaining.Width;

                if (spanWidth <= available)
                {
                    currentWidth += spanWidth;
                    PushSpanMerged(current, remaining);
                    break; // consumed this span; move to next
                }

                var (left, right) = SplitAtCell(remaining, available);

                // Force progress: if left is empty and line is empty and remaining is non-empty,
                // split at the first grapheme's width (at least 1).
                // Mirrors upstream: if left.is_empty() && current.is_empty() && !remaining.is_empty()
                if (IsSpanEmpty(left) && current.Count == 0 && !IsSpanEmpty(remaining))
                {
                    int firstW = FirstGraphemeWidth(remaining);
                    (left, right) = SplitAtCell(remaining, Math.Max(1, firstW));
                }

                if (!IsSpanEmpty(left))
                    PushSpanMerged(current, left);

                lines.Add(TrimLineTrailing(current));
                current = new List<TextSpan>();
                currentWidth = 0;
                remaining = right;
            }
        }

        // Final flush: always emit at least one line.
        if (current.Count > 0 || lines.Count == 0)
            lines.Add(TrimLineTrailing(current));

        return lines;
    }

    /// <summary>
    /// Wrap by word boundary with optional char fallback for tokens wider than the line.
    /// Faithful port of wrap_line_words (text.rs:938-1049).
    ///
    /// 1. Split all spans into word/whitespace pieces via SplitSpanWords.
    /// 2. Greedily pack pieces into lines:
    ///    - Piece fits (current_width + piece_width &lt;= width):
    ///      - Skip leading whitespace on a continuation line (current_width==0 and !firstLine).
    ///      - Else add piece.
    ///    - Piece does not fit and current line is non-empty: flush.
    ///    - Piece is wider than the whole line (piece_width > width):
    ///      - char_fallback=true:  inner char-wrap loop (SplitAtCell + force-progress).
    ///      - char_fallback=false: if non-whitespace, push as-is then flush; whitespace dropped.
    ///    - Else: TrimSpanStart on continuation line, push.
    /// 3. Final flush.
    /// </summary>
    private static List<TextLine> WrapLineWords(TextLine line, int width, bool charFallback)
    {
        // Step 1: explode all spans into word/whitespace pieces.
        var pieces = new List<TextSpan>();
        foreach (var span in line.Spans)
            pieces.AddRange(SplitSpanWords(span));

        var lines = new List<TextLine>();
        var current = new List<TextSpan>();  // Line::new()
        int currentWidth = 0;
        bool firstLine = true;

        foreach (var piece in pieces)
        {
            int pieceWidth = piece.Width;
            bool isWs = SpanIsWhitespace(piece);

            // Case: piece fits on current line.
            if (currentWidth + pieceWidth <= width)
            {
                // Skip leading whitespace on a continuation line.
                if (currentWidth == 0 && !firstLine && isWs)
                    continue;

                currentWidth += pieceWidth;
                PushSpanMerged(current, piece);
                continue;
            }

            // Piece does not fit. Flush current line if non-empty.
            if (current.Count > 0)
            {
                lines.Add(TrimLineTrailing(current));
                current = new List<TextSpan>();
                currentWidth = 0;
                firstLine = false;
            }

            // Handle token wider than the entire available width.
            if (pieceWidth > width)
            {
                if (charFallback)
                {
                    // Inner char-wrap loop (text.rs:971-1017).
                    var remaining = piece;
                    while (!IsSpanEmpty(remaining))
                    {
                        if (currentWidth >= width && current.Count > 0)
                        {
                            lines.Add(TrimLineTrailing(current));
                            current = new List<TextSpan>();
                            currentWidth = 0;
                            firstLine = false;
                        }

                        int available = Math.Max(1, width - currentWidth);
                        var (left, right) = SplitAtCell(remaining, available);

                        // Force progress (mirrors upstream forced-progress logic).
                        if (IsSpanEmpty(left) && current.Count == 0 && !IsSpanEmpty(remaining))
                        {
                            int firstW = FirstGraphemeWidth(remaining);
                            (left, right) = SplitAtCell(remaining, Math.Max(1, firstW));
                        }

                        // Trim leading whitespace on continuation lines.
                        if (currentWidth == 0 && !firstLine)
                            left = TrimSpanStart(left);

                        if (!IsSpanEmpty(left))
                        {
                            currentWidth += left.Width;
                            PushSpanMerged(current, left);
                        }

                        if (currentWidth >= width && current.Count > 0)
                        {
                            lines.Add(TrimLineTrailing(current));
                            current = new List<TextSpan>();
                            currentWidth = 0;
                            firstLine = false;
                        }

                        remaining = right;
                    }
                }
                else if (!isWs)
                {
                    // No char fallback, non-whitespace: push as-is (overflow), flush.
                    var trimmed = piece;
                    if (!firstLine)
                        trimmed = TrimSpanStart(trimmed);
                    if (!IsSpanEmpty(trimmed))
                        PushSpanMerged(current, trimmed);
                    lines.Add(TrimLineTrailing(current));
                    current = new List<TextSpan>();
                    currentWidth = 0;
                    firstLine = false;
                }
                // is_ws && !charFallback: piece is silently dropped (matches upstream).
                continue;
            }

            // Piece fits in an empty line; trim leading whitespace on continuation lines.
            var trimmedPiece = piece;
            if (!firstLine)
                trimmedPiece = TrimSpanStart(trimmedPiece);
            if (!IsSpanEmpty(trimmedPiece))
            {
                currentWidth += trimmedPiece.Width;
                PushSpanMerged(current, trimmedPiece);
            }
        }

        // Final flush: always emit at least one line.
        if (current.Count > 0 || lines.Count == 0)
            lines.Add(TrimLineTrailing(current));

        return lines;
    }

    // ── Wrap helper functions (faithful ports of text.rs:719-880) ─────────

    /// <summary>
    /// Find the UTF-16 byte-position and actual cell width when splitting a string at targetCells.
    /// Port of find_cell_boundary (text.rs:719-739).
    /// Walks graphemes accumulating cell width; stops when adding the next grapheme would
    /// exceed targetCells, or immediately when current_cells reaches targetCells.
    /// </summary>
    private static (int bytePos, int actualWidth) FindCellBoundary(string text, int targetCells)
    {
        int currentCells = 0;
        int bytePos = 0;

        var te = StringInfo.GetTextElementEnumerator(text);
        while (te.MoveNext())
        {
            string grapheme = te.GetTextElement();
            int gw = WidgetDrawing.GraphemeWidth(grapheme);

            if (currentCells + gw > targetCells)
                break;

            currentCells += gw;
            bytePos += grapheme.Length; // UTF-16 code units

            if (currentCells >= targetCells)
                break;
        }

        return (bytePos, currentCells);
    }

    /// <summary>
    /// Split a span at a cell boundary.
    /// Port of Span::split_at_cell (text.rs:102-151).
    /// Returns (left, right); both carry the original style.
    /// </summary>
    private static (TextSpan left, TextSpan right) SplitAtCell(TextSpan span, int cellPos)
    {
        var empty = TextSpan.Styled("", span.Style);

        if (span.Content.Length == 0 || cellPos == 0)
            return (empty, span);

        int totalWidth = span.Width;
        if (cellPos >= totalWidth)
            return (span, empty);

        var (bytePos, _) = FindCellBoundary(span.Content, cellPos);

        return (
            TextSpan.Styled(span.Content[..bytePos], span.Style),
            TextSpan.Styled(span.Content[bytePos..], span.Style)
        );
    }

    /// <summary>
    /// Returns true if all graphemes in the span are whitespace.
    /// Port of span_is_whitespace (text.rs:741-745).
    /// Uses char.IsWhiteSpace(c), matching the upstream plain c.is_whitespace() predicate.
    /// Note: NBSP (U+00A0) and NNBSP (U+202F) ARE considered whitespace here (unlike
    /// IsBreakingWhitespace which excludes them). Only SplitSpanWords uses IsBreakingWhitespace.
    /// </summary>
    private static bool SpanIsWhitespace(TextSpan span)
    {
        if (span.Content.Length == 0) return true;

        var te = StringInfo.GetTextElementEnumerator(span.Content);
        while (te.MoveNext())
        {
            string g = te.GetTextElement();
            foreach (char c in g)
                if (!char.IsWhiteSpace(c)) return false;
        }
        return true;
    }

    /// <summary>
    /// Returns true for characters considered breaking whitespace.
    /// Port of is_breaking_whitespace (wrap.rs:646-648):
    ///   c.is_whitespace() and c != '\u{00A0}' and c != '\u{202F}'
    /// NBSP ( ) and NNBSP ( ) are excluded even though they are whitespace.
    /// </summary>
    private static bool IsBreakingWhitespace(char c)
        => char.IsWhiteSpace(c) && c != ' ' && c != ' ';

    /// <summary>
    /// Trim leading whitespace graphemes from a span.
    /// Port of trim_span_start (text.rs:747-770).
    /// Uses char.IsWhiteSpace(c), matching the upstream plain c.is_whitespace() predicate.
    /// Returns an empty span (preserving style) if all characters are whitespace.
    /// </summary>
    private static TextSpan TrimSpanStart(TextSpan span)
    {
        string text = span.Content;
        int start = 0;
        bool found = false;

        var te = StringInfo.GetTextElementEnumerator(text);
        while (te.MoveNext())
        {
            string grapheme = te.GetTextElement();
            bool allWs = true;
            foreach (char c in grapheme)
                if (!char.IsWhiteSpace(c)) { allWs = false; break; }

            if (allWs)
            {
                start += grapheme.Length;
                continue;
            }
            found = true;
            break;
        }

        if (!found)
            return TextSpan.Styled("", span.Style);

        return TextSpan.Styled(text[start..], span.Style);
    }

    /// <summary>
    /// Trim trailing whitespace graphemes from a span.
    /// Port of trim_span_end (text.rs:772-795).
    /// Uses char.IsWhiteSpace(c), matching the upstream plain c.is_whitespace() predicate.
    /// Returns an empty span (preserving style) if all characters are whitespace.
    /// </summary>
    private static TextSpan TrimSpanEnd(TextSpan span)
    {
        string text = span.Content;

        // Walk forward collecting (startPos, length, isWs) so we can iterate in reverse.
        var graphemeList = new List<(int start, int len, bool isWs)>();
        int pos = 0;
        var te = StringInfo.GetTextElementEnumerator(text);
        while (te.MoveNext())
        {
            string g = te.GetTextElement();
            bool allWs = true;
            foreach (char c in g)
                if (!char.IsWhiteSpace(c)) { allWs = false; break; }
            graphemeList.Add((pos, g.Length, allWs));
            pos += g.Length;
        }

        int end = text.Length;
        bool found = false;
        for (int i = graphemeList.Count - 1; i >= 0; i--)
        {
            var (gStart, _, isWs) = graphemeList[i];
            if (isWs)
            {
                end = gStart;
                continue;
            }
            found = true;
            break;
        }

        if (!found)
            return TextSpan.Styled("", span.Style);

        return TextSpan.Styled(text[..end], span.Style);
    }

    /// <summary>
    /// Trim trailing whitespace spans from a line (consuming the list).
    /// Port of trim_line_trailing (text.rs:797-811).
    /// </summary>
    private static TextLine TrimLineTrailing(List<TextSpan> spans)
    {
        while (spans.Count > 0)
        {
            var last = spans[^1];
            var trimmed = TrimSpanEnd(last);
            if (IsSpanEmpty(trimmed))
            {
                spans.RemoveAt(spans.Count - 1);
                continue;
            }
            spans[^1] = trimmed;
            break;
        }
        return TextLine.FromSpans(spans.ToArray());
    }

    /// <summary>
    /// Append a span to a line, merging into the last span if styles match.
    /// Port of push_span_merged (text.rs:813-830).
    /// Skips empty spans. When the last span has the same style, concatenates content strings.
    /// (TextSpan has no link field; style equality is sufficient.)
    /// </summary>
    private static void PushSpanMerged(List<TextSpan> line, TextSpan span)
    {
        if (IsSpanEmpty(span)) return;

        if (line.Count > 0)
        {
            var last = line[^1];
            if (last.Style == span.Style)
            {
                // Merge: concatenate content strings.
                line[^1] = TextSpan.Styled(last.Content + span.Content, last.Style);
                return;
            }
        }

        line.Add(span);
    }

    /// <summary>
    /// Split a span into alternating word and whitespace sub-spans.
    /// Port of split_span_words (text.rs:832-880).
    /// A boundary occurs whenever the breaking-whitespace-ness of a grapheme changes.
    /// Uses IsBreakingWhitespace to match upstream: grapheme.chars().all(crate::wrap::is_breaking_whitespace).
    /// </summary>
    private static List<TextSpan> SplitSpanWords(TextSpan span)
    {
        string text = span.Content;
        var segments = new List<TextSpan>();
        int start = 0;
        bool? inWhitespace = null;

        int pos = 0;
        var te = StringInfo.GetTextElementEnumerator(text);
        while (te.MoveNext())
        {
            string grapheme = te.GetTextElement();
            bool isWs = true;
            foreach (char c in grapheme)
                if (!IsBreakingWhitespace(c)) { isWs = false; break; }

            if (inWhitespace == null)
                inWhitespace = isWs;

            if (isWs != inWhitespace)
            {
                // Boundary at pos — emit segment [start, pos).
                if (pos > start)
                    segments.Add(TextSpan.Styled(text[start..pos], span.Style));
                start = pos;
                inWhitespace = isWs;
            }

            pos += grapheme.Length;
        }

        // Last segment.
        if (start < text.Length)
            segments.Add(TextSpan.Styled(text[start..], span.Style));

        return segments;
    }

    /// <summary>True when a span has no content characters.</summary>
    private static bool IsSpanEmpty(TextSpan span) => span.Content.Length == 0;

    /// <summary>
    /// Cell width of the first grapheme in a span, defaulting to 1.
    /// Port of: remaining.as_str().graphemes(true).next().map(grapheme_width).unwrap_or(1)
    /// Used for forced-progress in wrap_line_chars and the char-fallback branch of wrap_line_words.
    /// </summary>
    private static int FirstGraphemeWidth(TextSpan span)
    {
        var te = StringInfo.GetTextElementEnumerator(span.Content);
        if (te.MoveNext())
            return WidgetDrawing.GraphemeWidth(te.GetTextElement());
        return 1;
    }

    // ── Private: non-wrap helpers ─────────────────────────────────────────

    /// <summary>
    /// Calculate the minimum width needed (width of longest word) in a line.
    /// Port of line_min_width (paragraph.rs:120-137).
    /// NOTE: upstream uses char::is_whitespace (not is_breaking_whitespace) for word detection
    /// here; this matches the upstream predicate exactly.
    /// </summary>
    private static int LineMinWidth(TextLine line)
    {
        int maxWordWidth = 0;
        int currentWordWidth = 0;

        foreach (var span in line.Spans)
        {
            var te = StringInfo.GetTextElementEnumerator(span.Content);
            while (te.MoveNext())
            {
                string grapheme = te.GetTextElement();
                int gw = WidgetDrawing.GraphemeWidth(grapheme);
                // Upstream: grapheme.chars().all(char::is_whitespace)
                bool isWhitespace = true;
                foreach (char c in grapheme)
                    if (!char.IsWhiteSpace(c)) { isWhitespace = false; break; }

                if (isWhitespace)
                {
                    maxWordWidth = Math.Max(maxWordWidth, currentWordWidth);
                    currentWordWidth = 0;
                }
                else
                {
                    currentWordWidth += gw;
                }
            }
        }

        return Math.Max(maxWordWidth, currentWordWidth);
    }

    /// <summary>
    /// Calculate the starting x position for a line given alignment.
    /// Port of align_x (paragraph.rs:531-542).
    /// </summary>
    private static ushort AlignX(Rect area, int lineWidth, AlignmentValue alignment)
    {
        ushort lineWidthU16 = lineWidth > ushort.MaxValue ? ushort.MaxValue : (ushort)lineWidth;
        ushort remainder = (ushort)Math.Max(0, area.Width - lineWidthU16);
        return alignment switch
        {
            AlignmentValue.Left   => area.X,
            AlignmentValue.Center => (ushort)(area.X + remainder / 2),
            AlignmentValue.Right  => (ushort)(area.X + remainder),
            _                => area.X,
        };
    }

    /// <summary>
    /// Merge a span style on top of the base text style.
    /// Equivalent to Rust's span.style.merge(&amp;text_style).
    /// </summary>
    private static WidgetStyle MergeStyles(WidgetStyle spanStyle, WidgetStyle baseStyle)
        => new(
            spanStyle.Fg ?? baseStyle.Fg,
            spanStyle.Bg ?? baseStyle.Bg,
            spanStyle.Attrs ?? baseStyle.Attrs);

    /// <summary>
    /// Truncate accessible text to at most 200 characters, breaking on grapheme boundaries.
    /// Port of truncate_accessible_text (paragraph.rs:544-565).
    /// </summary>
    private static string TruncateAccessibleText(string text)
    {
        if (text.Length == 0) return text;

        int scalarCount = 0;
        foreach (Rune _ in text.EnumerateRunes()) scalarCount++;

        if (scalarCount <= AccessibleTextLimit)
            return text;

        var prefix = new System.Text.StringBuilder();
        int prefixChars = 0;

        var te = StringInfo.GetTextElementEnumerator(text);
        while (te.MoveNext())
        {
            string grapheme = te.GetTextElement();
            int graphemeChars = 0;
            foreach (Rune _ in grapheme.EnumerateRunes()) graphemeChars++;
            if (prefixChars + graphemeChars > AccessibleTextPrefixLimit)
                break;
            prefix.Append(grapheme);
            prefixChars += graphemeChars;
        }

        return prefix.ToString() + "...";
    }

    /// <summary>
    /// Compute a stable hash for the TextContent.
    /// Port of hash_value / text_hash (paragraph.rs:114-118, 188-190).
    /// DIVERGENCE: Upstream uses DefaultHasher (SipHash-1-3). C# uses FNV-1a, which is
    /// collision-resistant enough for a small in-process cache.
    /// </summary>
    private static ulong HashTextContent(TextContent text)
    {
        ulong hash = 14695981039346656037UL; // FNV-1a offset basis
        foreach (var line in text.Lines)
        {
            foreach (var span in line.Spans)
            {
                foreach (char c in span.Content)
                {
                    hash ^= c;
                    hash *= 1099511628211UL;
                }
                hash ^= (ulong)(span.Style.Fg?.Raw ?? 0);
                hash *= 1099511628211UL;
                hash ^= (ulong)(span.Style.Bg?.Raw ?? 0);
                hash *= 1099511628211UL;
            }
            // Line separator.
            hash ^= '\n';
            hash *= 1099511628211UL;
        }
        return hash;
    }
}
