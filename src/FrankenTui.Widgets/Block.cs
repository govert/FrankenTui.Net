// Port of .external/frankentui/crates/ftui-widgets/src/block.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Block widget with optional borders, title, padding, and degradation support.

using System.Globalization;
using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Render;
using CanonicalA11y = FrankenTui.A11y;
using Buffer = FrankenTui.Render.Buffer;
// Alias to avoid ambiguity between the Borders enum type and the Block.Borders() builder method.
using BordersFlags = FrankenTui.Widgets.Borders;

namespace FrankenTui.Widgets;

/// <summary>Text alignment.</summary>
public enum Alignment
{
    /// <summary>Align text to the left.</summary>
    Left,
    /// <summary>Center text horizontally.</summary>
    Center,
    /// <summary>Align text to the right.</summary>
    Right,
}

/// <summary>A widget that draws a block with optional borders, title, and padding.</summary>
public sealed class Block : IWidget, IMeasurableWidget, IAccessible, CanonicalA11y.IAccessible
{
    private Borders _borders;
    private WidgetStyle _borderStyle;
    private BorderType _borderType;
    private string? _title;
    private Alignment _titleAlignment;
    private WidgetStyle _style;
    private Sides _padding;

    // ── Constructors ─────────────────────────────────────────────────────────

    /// <summary>Create a new block with default settings.</summary>
    public static Block New() => new();

    /// <summary>Create a block with all borders enabled and default padding.</summary>
    public static Block Bordered() => new Block().Borders(BordersFlags.All).Padding(Sides.All(1));

    // ── Builder methods ───────────────────────────────────────────────────────

    /// <summary>Set which borders to render.</summary>
    public Block Borders(Borders borders) { _borders = borders; return this; }

    // DIVERGENCE: Rust uses snake_case on builder methods; C# uses PascalCase per convention.
    // Old-stub compat aliases kept for callers in ContainerWidgets.cs / CommandPalette.cs / VoiDebugOverlay.cs.
    /// <summary>Compatibility alias for Borders(). Kept to avoid churn in existing callers.</summary>
    public Block Borders_(Borders borders) => Borders(borders);

    /// <summary>Set the style applied to border characters.</summary>
    public Block BorderStyle(WidgetStyle style) { _borderStyle = style; return this; }

    /// <summary>Set the border character set (e.g. square, rounded, double).</summary>
    public Block BorderType(BorderType borderType) { _borderType = borderType; return this; }

    /// <summary>Set the padding inside the borders.</summary>
    public Block Padding(Sides padding) { _padding = padding; return this; }

    // DIVERGENCE: Old stub used Padding_() — kept for compat.
    /// <summary>Compatibility alias for Padding().</summary>
    public Block Padding_(Sides padding) => Padding(padding);

    /// <summary>Set the block title displayed on the top border.</summary>
    public Block Title(string title) { _title = title; return this; }

    /// <summary>Return the title text, if any.</summary>
    public string? TitleText() => _title;

    /// <summary>Set the horizontal alignment of the title.</summary>
    public Block TitleAlignment(Alignment alignment) { _titleAlignment = alignment; return this; }

    /// <summary>Set the background style for the entire block area.</summary>
    public Block Style(WidgetStyle style) { _style = style; return this; }

    // ── Accessors (internal use and test reflection) ──────────────────────────

    internal Borders BordersValue => _borders;
    internal WidgetStyle BorderStyleValue => _borderStyle;
    internal WidgetStyle StyleValue => _style;
    internal string? TitleValue => _title;
    internal Alignment TitleAlignmentValue => _titleAlignment;
    internal Sides PaddingValue => _padding;
    internal BorderType BorderTypeValue => _borderType;

    // ── Border set ────────────────────────────────────────────────────────────

    /// <summary>Get the border set for this block.</summary>
    internal BorderSet BorderSet() => _borderType.ToBorderSet();

    // DIVERGENCE: Old stub exposed GetBorderSetPublic() — kept for Table.cs caller.
    /// <summary>Compatibility: public accessor for the border set. Used by Table widget.</summary>
    public BorderSet GetBorderSetPublic() => _borderType.ToBorderSet();

    // ── Inner area computation ────────────────────────────────────────────────

    /// <summary>Compute the inner area inside the block's borders and padding.</summary>
    public Rect Inner(Rect area)
    {
        var inner = area;

        if (_borders.HasFlag(BordersFlags.Left))
        {
            inner = inner with { X = (ushort)(inner.X + 1), Width = (ushort)Math.Max(0, inner.Width - 1) };
        }
        if (_borders.HasFlag(BordersFlags.Top))
        {
            inner = inner with { Y = (ushort)(inner.Y + 1), Height = (ushort)Math.Max(0, inner.Height - 1) };
        }
        if (_borders.HasFlag(BordersFlags.Right))
        {
            inner = inner with { Width = (ushort)Math.Max(0, inner.Width - 1) };
        }
        if (_borders.HasFlag(BordersFlags.Bottom))
        {
            inner = inner with { Height = (ushort)Math.Max(0, inner.Height - 1) };
        }

        return inner.Inner(_padding);
    }

    /// <summary>
    /// Calculate the chrome (border + padding) size consumed by this block.
    /// Returns (horizontal_chrome, vertical_chrome) representing the
    /// total width and height consumed by borders and padding.
    /// </summary>
    public (ushort horizontal, ushort vertical) ChromeSize()
    {
        ushort borderH = (ushort)((_borders.HasFlag(BordersFlags.Left) ? 1 : 0) +
                                   (_borders.HasFlag(BordersFlags.Right) ? 1 : 0));
        ushort borderV = (ushort)((_borders.HasFlag(BordersFlags.Top) ? 1 : 0) +
                                   (_borders.HasFlag(BordersFlags.Bottom) ? 1 : 0));

        ushort paddingH = (ushort)(_padding.Left + _padding.Right);
        ushort paddingV = (ushort)(_padding.Top + _padding.Bottom);

        return ((ushort)(borderH + paddingH), (ushort)(borderV + paddingV));
    }

    // ── Border cell ───────────────────────────────────────────────────────────

    /// <summary>Create a styled border cell.</summary>
    private Cell BorderCell(char c, WidgetStyle style)
    {
        var cell = Cell.FromChar(c);
        WidgetDrawing.ApplyStyle(ref cell, style);
        return cell;
    }

    // ── Border rendering ──────────────────────────────────────────────────────

    private void RenderBorders(Rect area, Buffer buf, WidgetStyle style)
    {
        if (area.IsEmpty) return;

        var set = BorderSet();

        // Edges
        if (_borders.HasFlag(BordersFlags.Left))
        {
            for (ushort y = area.Y; y < area.Bottom; y++)
                buf.SetFast(area.X, y, BorderCell(set.Vertical, style));
        }
        if (_borders.HasFlag(BordersFlags.Right))
        {
            ushort x = (ushort)(area.Right - 1);
            for (ushort y = area.Y; y < area.Bottom; y++)
                buf.SetFast(x, y, BorderCell(set.Vertical, style));
        }
        if (_borders.HasFlag(BordersFlags.Top))
        {
            for (ushort x = area.X; x < area.Right; x++)
                buf.SetFast(x, area.Y, BorderCell(set.Horizontal, style));
        }
        if (_borders.HasFlag(BordersFlags.Bottom))
        {
            ushort y = (ushort)(area.Bottom - 1);
            for (ushort x = area.X; x < area.Right; x++)
                buf.SetFast(x, y, BorderCell(set.Horizontal, style));
        }

        // Corners (drawn after edges to overwrite edge characters at corners)
        if (_borders.HasFlag(BordersFlags.Left | BordersFlags.Top))
            buf.SetFast(area.X, area.Y, BorderCell(set.TopLeft, style));
        if (_borders.HasFlag(BordersFlags.Right | BordersFlags.Top))
            buf.SetFast((ushort)(area.Right - 1), area.Y, BorderCell(set.TopRight, style));
        if (_borders.HasFlag(BordersFlags.Left | BordersFlags.Bottom))
            buf.SetFast(area.X, (ushort)(area.Bottom - 1), BorderCell(set.BottomLeft, style));
        if (_borders.HasFlag(BordersFlags.Right | BordersFlags.Bottom))
            buf.SetFast((ushort)(area.Right - 1), (ushort)(area.Bottom - 1), BorderCell(set.BottomRight, style));
    }

    /// <summary>Render borders using ASCII characters regardless of configured border_type.</summary>
    private void RenderBordersAscii(Rect area, Buffer buf, WidgetStyle style)
    {
        if (area.IsEmpty) return;

        var set = FrankenTui.Widgets.BorderSet.Ascii;

        if (_borders.HasFlag(BordersFlags.Left))
        {
            for (ushort y = area.Y; y < area.Bottom; y++)
                buf.SetFast(area.X, y, BorderCell(set.Vertical, style));
        }
        if (_borders.HasFlag(BordersFlags.Right))
        {
            ushort x = (ushort)(area.Right - 1);
            for (ushort y = area.Y; y < area.Bottom; y++)
                buf.SetFast(x, y, BorderCell(set.Vertical, style));
        }
        if (_borders.HasFlag(BordersFlags.Top))
        {
            for (ushort x = area.X; x < area.Right; x++)
                buf.SetFast(x, area.Y, BorderCell(set.Horizontal, style));
        }
        if (_borders.HasFlag(BordersFlags.Bottom))
        {
            ushort y = (ushort)(area.Bottom - 1);
            for (ushort x = area.X; x < area.Right; x++)
                buf.SetFast(x, y, BorderCell(set.Horizontal, style));
        }

        if (_borders.HasFlag(BordersFlags.Left | BordersFlags.Top))
            buf.SetFast(area.X, area.Y, BorderCell(set.TopLeft, style));
        if (_borders.HasFlag(BordersFlags.Right | BordersFlags.Top))
            buf.SetFast((ushort)(area.Right - 1), area.Y, BorderCell(set.TopRight, style));
        if (_borders.HasFlag(BordersFlags.Left | BordersFlags.Bottom))
            buf.SetFast(area.X, (ushort)(area.Bottom - 1), BorderCell(set.BottomLeft, style));
        if (_borders.HasFlag(BordersFlags.Right | BordersFlags.Bottom))
            buf.SetFast((ushort)(area.Right - 1), (ushort)(area.Bottom - 1), BorderCell(set.BottomRight, style));
    }

    // ── Title rendering ───────────────────────────────────────────────────────

    private void RenderTitle(Rect area, Frame frame)
    {
        if (_title == null) return;
        if (!_borders.HasFlag(BordersFlags.Top) || area.Width < 3) return;

        int availableWidth = area.Width - 2;
        if (availableWidth == 0) return;

        int displayWidth = FittedTextWidth(_title, availableWidth);
        if (displayWidth == 0) return;

        ushort x = _titleAlignment switch
        {
            Alignment.Left => (ushort)(area.X + 1),
            Alignment.Center => (ushort)(area.X + 1 + (availableWidth - displayWidth) / 2),
            Alignment.Right => (ushort)(area.Right - 1 - displayWidth),
            _ => (ushort)(area.X + 1),
        };

        ushort maxX = (ushort)(area.Right - 1);
        WidgetDrawing.DrawTextSpan(frame, x, area.Y, _title, _borderStyle, maxX);
    }

    // ── IWidget ───────────────────────────────────────────────────────────────

    /// <summary>Render the block into the frame at the given area.</summary>
    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty) return;

        var deg = frame.Degradation;
        var borderStyle = deg.ApplyStyling() ? _borderStyle : WidgetStyle.Default;

        // Skeleton+: skip everything, just clear area
        if (!deg.RenderContent())
        {
            frame.Buffer.Fill(area, Cell.Empty);
            return;
        }

        // EssentialOnly: block chrome is purely decorative, so clear the owned
        // area instead of leaving stale borders/title content behind.
        if (!deg.RenderDecorative())
        {
            frame.Buffer.Fill(area, Cell.Empty);
            if (deg.ApplyStyling())
                WidgetDrawing.SetStyleArea(frame.Buffer, area, _style);
            return;
        }

        // Apply background/style
        if (deg.ApplyStyling())
            WidgetDrawing.SetStyleArea(frame.Buffer, area, _style);

        // Render borders (with possible ASCII downgrade)
        if (deg.UseUnicodeBorders())
            RenderBorders(area, frame.Buffer, borderStyle);
        else
            // Force ASCII borders regardless of configured border_type
            RenderBordersAscii(area, frame.Buffer, borderStyle);

        // Render title (skip at NoStyling to save time)
        if (deg.ApplyStyling())
        {
            RenderTitle(area, frame);
        }
        else if (deg.RenderDecorative())
        {
            // Still show title but without styling
            // Pass frame to reuse draw_text_span
            if (_title != null
                && _borders.HasFlag(BordersFlags.Top)
                && area.Width >= 3)
            {
                int availableWidth = area.Width - 2;
                if (availableWidth > 0)
                {
                    int displayWidth = FittedTextWidth(_title, availableWidth);
                    if (displayWidth == 0) return;

                    ushort x = _titleAlignment switch
                    {
                        Alignment.Left => (ushort)(area.X + 1),
                        Alignment.Center => (ushort)(area.X + 1 + (availableWidth - displayWidth) / 2),
                        Alignment.Right => (ushort)(area.Right - 1 - displayWidth),
                        _ => (ushort)(area.X + 1),
                    };

                    ushort maxX = (ushort)(area.Right - 1);
                    // draw_text_span() preserves existing cell styles. Clear the
                    // title span first so NoStyling truly drops border styling.
                    frame.Buffer.Fill(new Rect(x, area.Y, (ushort)displayWidth, 1), Cell.Empty);
                    WidgetDrawing.DrawTextSpan(frame, x, area.Y, _title, WidgetStyle.Default, maxX);
                }
            }
        }
    }

    // ── IMeasurableWidget ─────────────────────────────────────────────────────

    /// <summary>Measure the intrinsic size constraints of this block.</summary>
    public SizeConstraints Measure(Size available)
    {
        var (chromeWidth, chromeHeight) = ChromeSize();
        var chrome = new Size(chromeWidth, chromeHeight);

        // Block's intrinsic size is just its chrome (borders).
        // The minimum is the chrome size - less than this and borders overlap.
        // Preferred is also the chrome size - any inner content adds to this.
        // Maximum is unbounded - block can fill available space.
        return SizeConstraints.AtLeast(chrome, chrome);
    }

    /// <summary>Block has intrinsic size only if it has borders.</summary>
    public bool HasIntrinsicSize() => _borders != BordersFlags.None;

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

    // ── Equality ──────────────────────────────────────────────────────────────

    public override bool Equals(object? obj)
    {
        if (obj is not Block other) return false;
        return _borders == other._borders
            && _borderStyle == other._borderStyle
            && _borderType == other._borderType
            && _title == other._title
            && _titleAlignment == other._titleAlignment
            && _style == other._style
            && _padding == other._padding;
    }

    public override int GetHashCode()
        => HashCode.Combine(_borders, _borderStyle, _borderType, _title, _titleAlignment, _style, _padding);

    // ── Accessibility ─────────────────────────────────────────────────────────

    /// <summary>Get the legacy compatibility projection of this block's accessibility node.</summary>
    public List<A11yNodeInfo> AccessibilityNodes(Rect area) =>
        LegacyAccessibilityAdapter.FromCanonical(CanonicalAccessibilityNodes(area));

    List<CanonicalA11y.A11yNodeInfo> CanonicalA11y.IAccessible.AccessibilityNodes(Rect area) =>
        CanonicalAccessibilityNodes(area);

    private List<CanonicalA11y.A11yNodeInfo> CanonicalAccessibilityNodes(Rect area)
    {
        ulong id = WidgetDrawing.A11yNodeId(area);
        CanonicalA11y.A11yNodeInfo node = CanonicalA11y.A11yNodeInfo.New(
            id,
            CanonicalA11y.A11yRole.Group,
            area);
        if (TitleText() is { } title)
            node = node.WithName(title);
        return [node];
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int FittedTextWidth(string text, int maxWidth)
    {
        int width = 0;
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            var grapheme = enumerator.GetTextElement();
            int w = WidgetDrawing.GraphemeWidth(grapheme);
            if (w == 0) continue;
            if (width + w > maxWidth) break;
            width += w;
        }
        return width;
    }
}
