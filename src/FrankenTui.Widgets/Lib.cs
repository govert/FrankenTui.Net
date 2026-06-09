// Port of .external/frankentui/crates/ftui-widgets/src/lib.rs
// Core widget traits (IWidget, IStatefulWidget), Budgeted wrapper, shared drawing helpers,
// and crate-level utility functions for the FrankenTui.Widgets namespace.

// NOTE: This file is the authoritative port of lib.rs.  The former stub in WidgetCore.cs
// has been reduced to a redirect comment.  All canonical declarations live here.
//
// DIVERGENCE: The Rust `tracing_test_support` module (gated by
// `#[cfg(all(test, feature = "tracing"))]`) has no direct .NET equivalent.
// C# does not have Cargo feature flags, and the module's purpose (serialising
// tracing subscriber installation across tests) is handled by xUnit test
// isolation. It is intentionally omitted.
//
// DIVERGENCE: The `Borders` flags enum is declared in Borders.cs (canonical port of
// borders.rs). It is not re-declared here to avoid duplicate symbol errors.
// IWidget extends IRuntimeView to maintain compatibility with pre-existing stub
// widgets that implement the runtime view interface; the upstream Rust trait has
// no such dependency.

using System.Globalization;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using Buffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Widgets;

// ────────────────────────────────────────────────────────────────────────────
// lib.rs module-level documentation
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Core widgets for FrankenTUI.
/// <para>
/// This namespace provides the <see cref="IWidget"/> and <see cref="IStatefulWidget{S}"/>
/// interfaces, along with a collection of ready-to-use widgets for building terminal UIs.
/// </para>
/// <para>
/// Widgets render into a <see cref="Frame"/> rather than directly into a
/// <see cref="Buffer"/>.  The Frame provides access to several subsystems beyond
/// the cell grid:
/// </para>
/// <list type="bullet">
///   <item><description><c>frame.Buffer</c> – cell grid for drawing characters and styles</description></item>
///   <item><description><c>frame.RegisterHit()</c> – optional mouse hit testing (for interactive widgets)</description></item>
///   <item><description><c>frame.CursorPosition</c> – cursor placement (for input widgets)</description></item>
///   <item><description><c>frame.CursorVisible</c> – cursor visibility control</description></item>
///   <item><description><c>frame.Degradation</c> – performance budget hints (for adaptive rendering)</description></item>
/// </list>
/// <para>
/// Widget categories:
/// <list type="bullet">
///   <item><description>Category A: Simple buffer-only widgets (e.g. Block, Paragraph, Rule, StatusLine)</description></item>
///   <item><description>Category B: Interactive widgets with hit testing (e.g. List, Table, Scrollbar)</description></item>
///   <item><description>Category C: Input widgets with cursor control (e.g. TextInput)</description></item>
///   <item><description>Category D: Adaptive widgets with degradation support (e.g. ProgressBar, Spinner)</description></item>
/// </list>
/// </para>
/// </summary>
internal static class LibDoc { } // doc-anchor only; no runtime presence

// ────────────────────────────────────────────────────────────────────────────
// Borders flags: declared in Borders.cs (port of borders.rs). Not re-declared here.
// ────────────────────────────────────────────────────────────────────────────

// ────────────────────────────────────────────────────────────────────────────
// Widget trait — pub trait Widget
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A widget that can render itself into a <see cref="Frame"/>.
///
/// <para>
/// Widgets render into a <c>Frame</c> rather than directly into a <c>Buffer</c>.
/// This provides:
/// <list type="bullet">
///   <item><description>Buffer access: <c>frame.Buffer</c> for drawing cells</description></item>
///   <item><description>Hit testing: <c>frame.RegisterHit()</c> for mouse interaction</description></item>
///   <item><description>Cursor control: <c>frame.CursorPosition</c> for input widgets</description></item>
///   <item><description>Performance hints: <c>frame.Degradation</c> for adaptive rendering</description></item>
/// </list>
/// </para>
///
/// <para>
/// Degradation levels: Full, SimpleBorders, NoStyling, EssentialOnly, Skeleton.
/// </para>
///
/// <para>Port of <c>pub trait Widget</c> in lib.rs.</para>
/// </summary>
// DIVERGENCE: IWidget extends IRuntimeView to maintain compatibility with pre-existing stub
// widgets in the working tree that implement IRuntimeView.Render(RuntimeRenderContext).
// The upstream Rust Widget trait has no such dependency.
public interface IWidget : IRuntimeView
{
    /// <summary>
    /// Render the widget into the frame at the given area.
    ///
    /// <para>
    /// The <paramref name="area"/> defines the bounding rectangle within which the widget
    /// should render.  Widgets should respect the area bounds and not draw outside them.
    /// </para>
    /// </summary>
    // DIVERGENCE: Render has a default interface implementation that bridges to the
    // IRuntimeView.Render(RuntimeRenderContext) path. New-generation widgets implement
    // Render(Rect, Frame) directly; legacy widgets implement IRuntimeView.Render(context)
    // and inherit this bridge. A concrete widget must implement exactly one of the two
    // (implementing neither would recurse between the two default bridges).
    void Render(Rect area, Frame frame)
    {
        var context = new RuntimeRenderContext(
            frame.Buffer,
            area,
            Style.Theme.DefaultTheme,
            (RuntimeDegradationLevel)(int)frame.Degradation);
        ((IRuntimeView)this).Render(context);
    }

    // DIVERGENCE: IWidget extends IRuntimeView for backward compatibility with the
    // runtime view interface, but the upstream Rust Widget trait renders only into a
    // Frame. This default interface implementation bridges the RuntimeRenderContext
    // path onto the canonical Render(Rect, Frame) path by wrapping the context's buffer
    // in a transient Frame. Concrete widgets need only implement Render(Rect, Frame).
    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var frame = new Frame(context.Buffer.Width, context.Buffer.Height, new GraphemePool())
        {
            BufferOverride = context.Buffer,
        };
        frame.SetDegradation((DegradationLevel)(int)context.DegradationLevel);
        Render(context.Bounds, frame);
    }

    /// <summary>
    /// Whether this widget is essential and should always render.
    ///
    /// <para>
    /// Essential widgets render even at <c>EssentialOnly</c> degradation level.
    /// Override this to return <c>true</c> for text inputs, primary content areas,
    /// and critical status indicators.
    /// </para>
    ///
    /// <para>Returns <c>false</c> by default, appropriate for decorative widgets.</para>
    /// </summary>
    bool IsEssential() => false;
}

// ────────────────────────────────────────────────────────────────────────────
// Budgeted<W> — budget-aware wrapper
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Budget-aware wrapper that registers widget signals and respects refresh budgets.
/// <para>Port of <c>pub struct Budgeted&lt;W&gt;</c> in lib.rs.</para>
/// </summary>
public sealed class Budgeted<W> : IWidget where W : IWidget
{
    private readonly ulong _widgetId;
    // Port of `signal: WidgetSignal` field on Rust Budgeted<W> (lib.rs line 444).
    private WidgetSignal _signal;
    private readonly W _inner;

    /// <summary>
    /// Wrap a widget with a stable identifier and default signal values.
    /// Port of <c>pub fn new(widget_id: u64, inner: W) -&gt; Self</c> in lib.rs.
    /// </summary>
    public Budgeted(ulong widgetId, W inner)
    {
        _widgetId = widgetId;
        _signal = WidgetSignal.New(widgetId);
        _inner = inner;
    }

    /// <summary>The widget identifier passed at construction time.</summary>
    public ulong WidgetId => _widgetId;

    /// <summary>
    /// The stored widget signal (read-only access for tests and inspection).
    /// Exposes the <c>signal</c> field of the Rust struct.
    /// </summary>
    public WidgetSignal Signal => _signal;

    /// <summary>
    /// Override the widget signal template.
    /// The supplied signal's <c>WidgetId</c> is overwritten to match this wrapper's
    /// own <c>widget_id</c>, preserving coherence.
    /// Port of <c>pub fn with_signal(mut self, mut signal: WidgetSignal) -&gt; Self</c> in lib.rs.
    /// </summary>
    public Budgeted<W> WithSignal(WidgetSignal signal)
    {
        // Rust: signal.widget_id = self.widget_id; self.signal = signal; self
        signal.WidgetId = _widgetId;
        _signal = signal;
        return this;
    }

    /// <summary>Access the wrapped widget.
    /// Port of <c>pub fn inner(&amp;self) -&gt; &amp;W</c> in lib.rs.</summary>
    public W Inner => _inner;

    /// <inheritdoc/>
    public void Render(Rect area, Frame frame)
    {
        // Port of impl<W: Widget> Widget for Budgeted<W> { fn render … } (lib.rs lines 474-490).
        var signal = _signal.Clone();
        signal.WidgetId  = _widgetId;
        signal.Essential = _inner.IsEssential();
        signal.AreaCells = (uint)(area.Width * area.Height);
        frame.RegisterWidgetSignal(signal);

        if (frame.ShouldRenderWidget(_widgetId, _inner.IsEssential()))
            _inner.Render(area, frame);
    }

    /// <inheritdoc/>
    public bool IsEssential() => _inner.IsEssential();

    // DIVERGENCE: IWidget extends IRuntimeView for backward compat with old stubs.
    // Budgeted<W> satisfies the IRuntimeView contract with a no-op; the real render
    // path is Render(Rect, Frame) above. The upstream Rust struct has no such method.
    void IRuntimeView.Render(RuntimeRenderContext context) { }

    /// <summary>
    /// Render as a stateful widget when the inner widget also implements
    /// <see cref="IStatefulWidget{S}"/>.
    /// <para>
    /// Port of <c>impl&lt;W: StatefulWidget + Widget&gt; StatefulWidget for Budgeted&lt;W&gt;</c>
    /// (lib.rs lines 492-506).  C# cannot express blanket trait impls, so the caller must
    /// cast the inner widget to <see cref="IStatefulWidget{S}"/> and pass it explicitly.
    /// </para>
    /// </summary>
    // DIVERGENCE: Rust expresses this as a blanket impl<W: StatefulWidget + Widget>.
    // C# has no blanket interface implementations; the constraint is expressed by requiring
    // the caller to provide the IStatefulWidget<S> view of the inner widget explicitly.
    // The signal/budget logic is identical to the upstream impl.
    public void RenderStateful<S>(Rect area, Frame frame, S state, IStatefulWidget<S> innerAsStateful)
    {
        var signal = _signal.Clone();
        signal.WidgetId  = _widgetId;
        signal.Essential = _inner.IsEssential();
        signal.AreaCells = (uint)(area.Width * area.Height);
        frame.RegisterWidgetSignal(signal);

        if (frame.ShouldRenderWidget(_widgetId, _inner.IsEssential()))
            innerAsStateful.Render(area, frame, state);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// StatefulWidget trait — pub trait StatefulWidget
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A widget that renders based on mutable state.
///
/// <para>
/// Use <see cref="IStatefulWidget{S}"/> when the widget needs to update scroll
/// position during render, track selection state, cache computed layout information,
/// or synchronize view with external model.
/// </para>
///
/// <para>
/// Prefer stateless <see cref="IWidget"/> when possible.  Use
/// <see cref="IStatefulWidget{S}"/> only when the render pass genuinely needs to
/// modify state (e.g. scroll adjustment).
/// </para>
///
/// <para>Port of <c>pub trait StatefulWidget</c> in lib.rs.</para>
/// </summary>
public interface IStatefulWidget<S>
{
    /// <summary>
    /// Render the widget into the frame, potentially modifying state.
    ///
    /// <para>
    /// State modifications should be limited to scroll offset adjustments,
    /// selection clamping, and layout caching.
    /// </para>
    /// </summary>
    void Render(Rect area, Frame frame, S state);
}

// ────────────────────────────────────────────────────────────────────────────
// WidgetStyle — mirrors Rust ftui_style::Style (fg/bg/attrs)
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Minimal style descriptor used by widget drawing helpers.
/// Mirrors the four fields of <c>ftui_style::Style</c>:
/// <c>fg</c>, <c>bg</c>, <c>attrs</c>, and <c>underline_color</c>.
/// </summary>
public readonly record struct WidgetStyle(PackedRgba? Fg, PackedRgba? Bg, CellStyleFlags? Attrs, PackedRgba? UnderlineColor = null)
{
    /// <summary>
    /// Returns <c>true</c> when no fields are set.
    /// Port of <c>pub const fn is_empty(&amp;self) -&gt; bool</c> in ftui_style::Style (style.rs line 478).
    /// </summary>
    public bool IsEmpty => !Fg.HasValue && !Bg.HasValue && !Attrs.HasValue && !UnderlineColor.HasValue;

    /// <summary>The default (empty) style.</summary>
    public static WidgetStyle Default => default;
}

// ────────────────────────────────────────────────────────────────────────────
// crate-level internal helpers — ported as public static on WidgetDrawing
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Static drawing helpers ported from the crate-level functions in lib.rs.
/// All methods are direct ports; see individual summaries for Rust equivalents.
/// </summary>
public static class WidgetDrawing
{
    // ── a11y_node_id ──────────────────────────────────────────────────────────

    // FNV-1a 64-bit constants
    private const ulong FnvOffset = 14_695_981_039_346_656_037UL;
    private const ulong FnvPrime  = 1_099_511_628_211UL;

    /// <summary>
    /// Generate a deterministic accessibility node ID from a widget's bounding rect.
    ///
    /// <para>
    /// Uses FNV-1a to hash the area coordinates.  Stable across frames for widgets
    /// rendered at the same position, enabling efficient A11yTree diffing.
    /// </para>
    ///
    /// <para>Port of <c>pub(crate) fn a11y_node_id(area: Rect) -&gt; u64</c> in lib.rs.</para>
    /// </summary>
    public static ulong A11yNodeId(Rect area)
    {
        // FNV-1a 64-bit (matches Rust exactly: x, y, width, height in little-endian byte order)
        ulong h = FnvOffset;
        foreach (byte b in BitConverter.GetBytes(area.X))      { h ^= b; h = unchecked(h * FnvPrime); }
        foreach (byte b in BitConverter.GetBytes(area.Y))      { h ^= b; h = unchecked(h * FnvPrime); }
        foreach (byte b in BitConverter.GetBytes(area.Width))  { h ^= b; h = unchecked(h * FnvPrime); }
        foreach (byte b in BitConverter.GetBytes(area.Height)) { h ^= b; h = unchecked(h * FnvPrime); }
        return h;
    }

    // ── apply_style ───────────────────────────────────────────────────────────

    /// <summary>
    /// Merge a <see cref="WidgetStyle"/> into a cell, preserving existing properties
    /// for unset fields.
    ///
    /// <list type="bullet">
    ///   <item><description>Foreground/Background: only overwritten when the style explicitly sets the field.</description></item>
    ///   <item><description>Background colours with alpha &lt; 255 are composited via Porter-Duff SourceOver.</description></item>
    ///   <item><description>Attributes: new flags are OR-ed on top of existing flags (never cleared).</description></item>
    /// </list>
    ///
    /// <para>Port of <c>pub(crate) fn apply_style(cell: &amp;mut Cell, style: Style)</c> in lib.rs.</para>
    /// </summary>
    public static void ApplyStyle(ref Cell cell, WidgetStyle style)
    {
        if (style.Fg is { } fg)
            cell = cell.WithForeground(fg);
        if (style.Bg is { } bg)
        {
            if      (bg.A == 0)   { /* Fully transparent: no-op */ }
            else if (bg.A == 255) { cell = cell.WithBackground(bg); }              // Fully opaque: replace
            else                  { cell = cell.WithBackground(bg.Over(cell.Background)); } // Composite src-over-dst
        }
        if (style.Attrs is { } attrs)
            cell = cell.WithAttributes(cell.Attributes.MergedFlags(attrs));
    }

    // ── set_style_area ────────────────────────────────────────────────────────

    /// <summary>
    /// Apply a style to all cells in a rectangular area using <b>merge</b> semantics.
    ///
    /// <para>
    /// Only fields that are explicitly set in <paramref name="style"/> (i.e. non-null) are
    /// applied; unset fields leave the existing cell values intact.  This is the correct
    /// behaviour for selection / highlight overlays that specify only a background colour —
    /// per-cell foreground colours from earlier text rendering are preserved.
    /// </para>
    ///
    /// <list type="bullet">
    ///   <item><description>Background: alpha-aware compositing (Porter-Duff SourceOver).</description></item>
    ///   <item><description>Attributes: OR-ed on top of existing flags (never cleared).</description></item>
    /// </list>
    ///
    /// <para>Port of <c>pub(crate) fn set_style_area(buf: &amp;mut Buffer, area: Rect, style: Style)</c> in lib.rs.</para>
    /// </summary>
    public static void SetStyleArea(Buffer buf, Rect area, WidgetStyle style)
    {
        if (style.IsEmpty) return;
        var clipped = area.Intersection(buf.CurrentScissor);
        if (clipped.IsEmpty) return;

        float opacity = buf.CurrentOpacity;
        PackedRgba? fg = style.Fg?.WithOpacity(opacity);
        PackedRgba? bg = style.Bg?.WithOpacity(opacity);
        CellStyleFlags? attrs = style.Attrs;

        for (ushort y = clipped.Y; y < clipped.Bottom; y++)
        {
            for (ushort x = clipped.X; x < clipped.Right; x++)
            {
                var maybeCell = buf.Get(x, y);
                if (maybeCell is null) continue;
                var cell = maybeCell.Value;
                if (fg is { } f)
                    cell = cell.WithForeground(f);
                if (bg is { } b)
                {
                    if      (b.A == 0)   { /* Fully transparent: no-op */ }
                    else if (b.A == 255) { cell = cell.WithBackground(b); }
                    else                 { cell = cell.WithBackground(b.Over(cell.Background)); }
                }
                if (attrs is { } a)
                    cell = cell.WithAttributes(cell.Attributes.MergedFlags(a));
                buf.Set(x, y, cell);
            }
        }
    }

    // ── clear_text_area ───────────────────────────────────────────────────────

    /// <summary>
    /// Clear a text area with styled spaces before rendering new content.
    /// <para>Port of <c>pub(crate) fn clear_text_area(frame: &amp;mut Frame, area: Rect, style: Style)</c> in lib.rs.</para>
    /// </summary>
    public static void ClearTextArea(Frame frame, Rect area, WidgetStyle style)
    {
        if (area.Width == 0 || area.Height == 0) return;
        var cell = Cell.FromChar(' ');
        ApplyStyle(ref cell, style);
        frame.Buffer.Fill(area, cell);
    }

    // ── clear_text_row ────────────────────────────────────────────────────────

    /// <summary>
    /// Clear a single text row with styled spaces before rendering new content.
    /// <para>Port of <c>pub(crate) fn clear_text_row(frame: &amp;mut Frame, area: Rect, style: Style)</c> in lib.rs.</para>
    /// </summary>
    public static void ClearTextRow(Frame frame, Rect area, WidgetStyle style)
        => ClearTextArea(frame, new Rect(area.X, area.Y, area.Width, 1), style);

    // ── inherited_text_cell ───────────────────────────────────────────────────

    /// <summary>
    /// Build a text cell that inherits existing visual styling from the buffer.
    ///
    /// <para>
    /// Preserves foreground/background/style flags applied by prior area-wide overlays
    /// (e.g. selection/highlight passes) while intentionally dropping any stale hyperlink ID
    /// before new text is written.
    /// </para>
    ///
    /// <para>Port of <c>fn inherited_text_cell(frame: &amp;Frame, x: u16, y: u16, content: CellContent) -&gt; Cell</c> in lib.rs.</para>
    /// </summary>
    internal static Cell InheritedTextCell(Frame frame, ushort x, ushort y, CellContent content)
    {
        var existing = frame.Buffer.Get(x, y) ?? Cell.Empty;
        // Preserve fg/bg/flags but clear link id (stale hyperlink).
        return new Cell(content, existing.Foreground, existing.Background,
                        existing.Attributes.WithLink(0));
    }

    // ── draw_text_span ────────────────────────────────────────────────────────

    /// <summary>
    /// Draw a text span into a frame at the given position.
    ///
    /// <para>Returns the x position after the last drawn character.
    /// Stops at <paramref name="maxX"/> (exclusive).</para>
    ///
    /// <para>Port of <c>pub(crate) fn draw_text_span(frame: &amp;mut Frame, mut x: u16, y: u16, content: &amp;str, style: Style, max_x: u16) -&gt; u16</c> in lib.rs.</para>
    /// </summary>
    public static ushort DrawTextSpan(
        Frame frame,
        ushort x,
        ushort y,
        string content,
        WidgetStyle style,
        ushort maxX)
    {
        var te = StringInfo.GetTextElementEnumerator(content);
        while (te.MoveNext())
        {
            if (x >= maxX) break;
            var g = te.GetTextElement();
            int w = GraphemeWidth(g);
            if (w == 0) continue;
            if (x + w > maxX) break;

            CellContent cc = w > 1 || g.Length > 1
                ? CellContent.FromGrapheme(frame.InternWithWidth(g, (byte)w))
                : CellContent.FromChar(g[0]);

            var cell = InheritedTextCell(frame, x, y, cc);
            ApplyStyle(ref cell, style);

            // set_fast() skips scissor/opacity/compositing checks for common
            // single-width opaque cells; falls back to set() otherwise.
            frame.Buffer.SetFast(x, y, cell);
            x = (ushort)(x + w);
        }
        return x;
    }

    // ── draw_text_span_with_link ──────────────────────────────────────────────

    /// <summary>
    /// Draw a text span, optionally attaching a hyperlink.
    ///
    /// <para>Port of <c>pub(crate) fn draw_text_span_with_link</c> in lib.rs.</para>
    /// </summary>
    // DIVERGENCE: #[allow(dead_code)] in upstream; method is kept for full port fidelity.
    public static ushort DrawTextSpanWithLink(
        Frame frame,
        ushort x,
        ushort y,
        string content,
        WidgetStyle style,
        ushort maxX,
        string? linkUrl)
    {
        return DrawTextSpanScrolled(frame, x, y, content, style, maxX, 0, linkUrl);
    }

    // ── draw_text_span_scrolled ───────────────────────────────────────────────

    /// <summary>
    /// Draw a text span with horizontal scrolling (skip first <paramref name="scrollX"/> visual cells),
    /// optionally attaching a hyperlink.
    ///
    /// <para>Port of <c>pub(crate) fn draw_text_span_scrolled</c> in lib.rs.</para>
    /// </summary>
    // DIVERGENCE: #[allow(dead_code, clippy::too_many_arguments)] in upstream; kept for full port fidelity.
    public static ushort DrawTextSpanScrolled(
        Frame frame,
        ushort x,
        ushort y,
        string content,
        WidgetStyle style,
        ushort maxX,
        ushort scrollX,
        string? linkUrl = null)
    {
        // Register link if present
        uint linkId = linkUrl is not null ? frame.RegisterLink(linkUrl) : 0;

        ushort visualPos = 0;
        var te = StringInfo.GetTextElementEnumerator(content);
        while (te.MoveNext())
        {
            if (x >= maxX) break;
            var g = te.GetTextElement();
            int w = GraphemeWidth(g);
            if (w == 0) continue;

            ushort nextVisualPos = (ushort)(visualPos + w);

            // Check if this grapheme is visible
            if (nextVisualPos <= scrollX)
            {
                // Fully scrolled out
                visualPos = nextVisualPos;
                continue;
            }

            if (visualPos < scrollX)
            {
                // Partially scrolled out (e.g. wide char starting at scrollX - 1)
                // Skip the whole character because we cannot render half a cell.
                visualPos = nextVisualPos;
                continue;
            }

            if (x + w > maxX) break;

            CellContent cc = w > 1 || g.Length > 1
                ? CellContent.FromGrapheme(frame.InternWithWidth(g, (byte)w))
                : CellContent.FromChar(g[0]);

            var cell = InheritedTextCell(frame, x, y, cc);
            ApplyStyle(ref cell, style);

            // Apply link ID if present
            if (linkId != 0)
                cell = cell.WithAttributes(cell.Attributes.WithLink(linkId));

            frame.Buffer.SetFast(x, y, cell);
            x = (ushort)(x + w);
            visualPos = nextVisualPos;
        }
        return x;
    }

    // ── contains_ignore_case ──────────────────────────────────────────────────

    /// <summary>
    /// Helper for allocation-free case-insensitive containment check.
    ///
    /// <para>
    /// The fast path handles ASCII without allocation.  Falls back to
    /// <c>ToLowerInvariant().Contains()</c> for Unicode.
    /// </para>
    ///
    /// <para>Port of <c>pub(crate) fn contains_ignore_case(haystack: &amp;str, needle_lower: &amp;str) -&gt; bool</c> in lib.rs.</para>
    /// </summary>
    public static bool ContainsIgnoreCase(string haystack, string needleLower)
    {
        if (string.IsNullOrEmpty(needleLower)) return true;

        // Fast path for ASCII
        if (IsAscii(haystack) && IsAscii(needleLower))
        {
            if (needleLower.Length > haystack.Length) return false;
            for (int i = 0; i <= haystack.Length - needleLower.Length; i++)
            {
                bool matchFound = true;
                for (int j = 0; j < needleLower.Length; j++)
                {
                    if (char.ToLowerInvariant(haystack[i + j]) != needleLower[j])
                    {
                        matchFound = false;
                        break;
                    }
                }
                if (matchFound) return true;
            }
            return false;
        }

        // Fallback for Unicode (allocates, but correct)
        return haystack.ToLowerInvariant().Contains(needleLower, StringComparison.Ordinal);
    }

    // ── GraphemeWidth ─────────────────────────────────────────────────────────

    // DIVERGENCE: Upstream uses `ftui_text::grapheme_width` which delegates to the
    // `unicode-width` crate (UAX #11 East_Asian_Width algorithm, tables derived from
    // Unicode 15).  .NET has no BCL equivalent that covers the full UAX #11 table.
    // This port implements an inline heuristic covering the most common wide ranges
    // (CJK, Hangul, Emoji, Fullwidth).  Edge cases not covered by these ranges may
    // diverge from the unicode-width crate on uncommon code points.  This divergence
    // is documented and behaviorally adequate for the drawing helpers in this file;
    // no upstream lib.rs test exercises wide-character rendering paths.

    /// <summary>
    /// Return the display column width of a text element (grapheme cluster).
    /// Wide characters (CJK, emoji, etc.) count as 2; all others count as 1.
    ///
    /// <para>Equivalent to <c>ftui_text::grapheme_width(grapheme)</c> used in lib.rs.
    /// See DIVERGENCE note above regarding edge-case differences vs. the unicode-width crate.</para>
    /// </summary>
    public static int GraphemeWidth(string g)
    {
        if (string.IsNullOrEmpty(g)) return 0;
        int w = 0;
        foreach (char c in g)
        {
            if (char.IsControl(c)) { /* zero-width */ }
            else if (c == 0x200D || c == 0xFE0F || c == 0xFE0E) { /* ZWJ / variation selectors: zero-width */ }
            else if (c >= 0x1100 && c <= 0x115F) w += 2;          // Hangul Jamo
            else if (c >= 0x2E80 && c <= 0xA4CF) w += 2;          // CJK Radicals…Yi
            else if (c >= 0xAC00 && c <= 0xD7A3) w += 2;          // Hangul syllables
            else if (c >= 0xF900 && c <= 0xFAFF) w += 2;          // CJK Compatibility Ideographs
            else if (c >= 0xFE10 && c <= 0xFE19) w += 2;          // Vertical Forms
            else if (c >= 0xFE30 && c <= 0xFE6F) w += 2;          // CJK Compatibility Forms
            else if (c >= 0xFF01 && c <= 0xFF60) w += 2;          // Fullwidth Latin
            else if (c >= 0xFFE0 && c <= 0xFFE6) w += 2;          // Fullwidth Signs
            else if (c >= 0x1F300 && c <= 0x1F9FF) w += 2;        // Misc Symbols and Pictographs / Emoji
            else w += 1;
        }
        return w > 0 ? w : 1;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static bool IsAscii(string s)
    {
        foreach (char c in s)
            if (c > 127) return false;
        return true;
    }
}
