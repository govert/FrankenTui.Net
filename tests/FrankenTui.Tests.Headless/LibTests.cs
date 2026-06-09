// Upstream source: crates/ftui-widgets/src/lib.rs (tests module)
// Full 1-1 port of all upstream lib.rs tests.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;
using Xunit;
using Buffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public class LibTests
{
    // ── Helper: build WidgetStyle instances (mirrors Rust Style::new().fg()/bg()/italic()) ─────

    private static WidgetStyle StyleFg(PackedRgba fg) => new(fg, null, null);
    private static WidgetStyle StyleBg(PackedRgba bg) => new(null, bg, null);
    private static WidgetStyle StyleFgBg(PackedRgba fg, PackedRgba bg) => new(fg, bg, null);
    private static WidgetStyle StyleItalic() => new(null, null, CellStyleFlags.Italic);
    private static WidgetStyle StyleDefault() => WidgetStyle.Default;

    // ── Helper: create Frame (maps Rust: let mut pool = GraphemePool::new(); Frame::new(w,h,pool)) ─

    private static Frame NewFrame(ushort width, ushort height)
    {
        var pool = new GraphemePool();
        return new Frame(width, height, pool);
    }

    // ────────────────────────────────────────────────────────────────────────
    // apply_style tests (via WidgetDrawing.ApplyStyle — the canonical port)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyStyleSetsFg()
    {
        var cell = Cell.Empty;
        var style = StyleFg(PackedRgba.Rgb(255, 0, 0));
        WidgetDrawing.ApplyStyle(ref cell, style);
        Assert.Equal(PackedRgba.Rgb(255, 0, 0), cell.Foreground);
    }

    [Fact]
    public void ApplyStyleSetsBg()
    {
        var cell = Cell.Empty;
        var style = StyleBg(PackedRgba.Rgb(0, 255, 0));
        WidgetDrawing.ApplyStyle(ref cell, style);
        Assert.Equal(PackedRgba.Rgb(0, 255, 0), cell.Background);
    }

    [Fact]
    public void ApplyStylePreservesContent()
    {
        var cell = Cell.FromChar('Z');
        var style = StyleFg(PackedRgba.Rgb(1, 2, 3));
        WidgetDrawing.ApplyStyle(ref cell, style);
        Assert.Equal('Z', cell.Content.AsChar());
    }

    [Fact]
    public void ApplyStyleEmptyIsNoop()
    {
        var original = Cell.Empty;
        var cell = Cell.Empty;
        WidgetDrawing.ApplyStyle(ref cell, StyleDefault());
        Assert.Equal(original.Foreground, cell.Foreground);
        Assert.Equal(original.Background, cell.Background);
    }

    [Fact]
    public void ApplyStyleBgOnlyPreservesFg()
    {
        // Simulate: cell already has syntax-highlighted fg, selection overlay sets only bg.
        var cell = Cell.FromChar('x').WithForeground(PackedRgba.Rgb(0, 200, 0));
        var selection = StyleBg(PackedRgba.Rgb(0, 0, 180));
        WidgetDrawing.ApplyStyle(ref cell, selection);
        // fg must survive — the selection style didn't set a fg.
        Assert.Equal(PackedRgba.Rgb(0, 200, 0), cell.Foreground);
        Assert.Equal(PackedRgba.Rgb(0, 0, 180), cell.Background);
    }

    [Fact]
    public void ApplyStyleCompositesAlphaBg()
    {
        var baseBg = PackedRgba.Rgb(200, 0, 0);
        var cell = Cell.Empty.WithBackground(baseBg);
        var overlay = PackedRgba.Rgba(0, 0, 200, 128);
        WidgetDrawing.ApplyStyle(ref cell, StyleBg(overlay));
        Assert.Equal(overlay.Over(baseBg), cell.Background);
    }

    [Fact]
    public void ApplyStyleTransparentBgIsNoop()
    {
        var baseBg = PackedRgba.Rgb(100, 100, 100);
        var cell = Cell.Empty.WithBackground(baseBg);
        WidgetDrawing.ApplyStyle(ref cell, StyleBg(PackedRgba.Rgba(255, 0, 0, 0)));
        Assert.Equal(baseBg, cell.Background);
    }

    [Fact]
    public void ApplyStyleMergesAttrsNotReplaces()
    {
        // Cell starts with BOLD.
        var cell = Cell.Empty.WithAttributes(new CellAttributes(CellStyleFlags.Bold, 0));
        // Overlay adds ITALIC — should NOT clear BOLD.
        var overlay = StyleItalic();
        WidgetDrawing.ApplyStyle(ref cell, overlay);
        Assert.True(cell.Attributes.HasFlag(CellStyleFlags.Bold),   "BOLD must survive");
        Assert.True(cell.Attributes.HasFlag(CellStyleFlags.Italic),  "ITALIC must be added");
    }

    // ────────────────────────────────────────────────────────────────────────
    // set_style_area tests (via WidgetDrawing.SetStyleArea)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetStyleAreaBgOnlyPreservesPerCellFg()
    {
        // A 3-cell buffer where each cell has a distinct fg.
        var buf = new Buffer(3, 1);
        buf.Set(0, 0, Cell.FromChar('R').WithForeground(PackedRgba.Red));
        buf.Set(1, 0, Cell.FromChar('G').WithForeground(PackedRgba.Green));
        buf.Set(2, 0, Cell.FromChar('B').WithForeground(PackedRgba.Blue));

        // Selection highlight sets only bg.
        var highlight = StyleBg(PackedRgba.Rgb(40, 40, 40));
        WidgetDrawing.SetStyleArea(buf, new Rect(0, 0, 3, 1), highlight);

        // All fg colours must be preserved.
        Assert.Equal(PackedRgba.Red,   buf.Get(0, 0)!.Value.Foreground);
        Assert.Equal(PackedRgba.Green, buf.Get(1, 0)!.Value.Foreground);
        Assert.Equal(PackedRgba.Blue,  buf.Get(2, 0)!.Value.Foreground);
        // bg should be the highlight colour.
        Assert.Equal(PackedRgba.Rgb(40, 40, 40), buf.Get(0, 0)!.Value.Background);
    }

    [Fact]
    public void SetStyleAreaMergesAttrsNotReplaces()
    {
        var buf = new Buffer(1, 1);
        var cell = Cell.FromChar('X').WithAttributes(new CellAttributes(CellStyleFlags.Bold, 0));
        buf.Set(0, 0, cell);

        WidgetDrawing.SetStyleArea(buf, new Rect(0, 0, 1, 1), StyleItalic());

        var result = buf.Get(0, 0)!.Value;
        Assert.True(result.Attributes.HasFlag(CellStyleFlags.Bold),  "BOLD must survive");
        Assert.True(result.Attributes.HasFlag(CellStyleFlags.Italic), "ITALIC must be added");
    }

    [Fact]
    public void SetStyleAreaAppliesToAllCells()
    {
        var buf = new Buffer(3, 2);
        var area = new Rect(0, 0, 3, 2);
        var style = StyleBg(PackedRgba.Rgb(10, 20, 30));
        WidgetDrawing.SetStyleArea(buf, area, style);

        for (ushort y = 0; y < 2; y++)
        {
            for (ushort x = 0; x < 3; x++)
            {
                // Each cell in the 3x2 area should have the bg colour applied.
                Assert.Equal(PackedRgba.Rgb(10, 20, 30), buf.Get(x, y)!.Value.Background);
            }
        }
    }

    [Fact]
    public void SetStyleAreaCompositesAlphaBgOverExistingBg()
    {
        var buf = new Buffer(1, 1);
        var baseColor = PackedRgba.Rgb(200, 0, 0);
        buf.Set(0, 0, Cell.Empty.WithBackground(baseColor));

        var overlay = PackedRgba.Rgba(0, 0, 200, 128);
        WidgetDrawing.SetStyleArea(buf, new Rect(0, 0, 1, 1), StyleBg(overlay));

        var expected = overlay.Over(baseColor);
        Assert.Equal(expected, buf.Get(0, 0)!.Value.Background);
    }

    [Fact]
    public void SetStyleAreaPartialRect()
    {
        var buf = new Buffer(5, 5);
        var area = new Rect(1, 1, 2, 2);
        var style = StyleFg(PackedRgba.Rgb(99, 99, 99));
        WidgetDrawing.SetStyleArea(buf, area, style);

        // Inside area should be styled
        Assert.Equal(PackedRgba.Rgb(99, 99, 99), buf.Get(1, 1)!.Value.Foreground);
        Assert.Equal(PackedRgba.Rgb(99, 99, 99), buf.Get(2, 2)!.Value.Foreground);

        // Outside area should be default (not the styled colour)
        Assert.NotEqual(PackedRgba.Rgb(99, 99, 99), buf.Get(0, 0)!.Value.Foreground);
    }

    [Fact]
    public void SetStyleAreaEmptyStyleIsNoop()
    {
        var buf = new Buffer(3, 3);
        buf.Set(0, 0, Cell.FromChar('A'));
        var originalFg = buf.Get(0, 0)!.Value.Foreground;

        WidgetDrawing.SetStyleArea(buf, new Rect(0, 0, 3, 3), StyleDefault());

        // Should not have changed
        Assert.Equal(originalFg, buf.Get(0, 0)!.Value.Foreground);
        Assert.Equal('A', buf.Get(0, 0)!.Value.Content.AsChar());
    }

    [Fact]
    public void SetStyleAreaRespectsScissor()
    {
        var buf = new Buffer(3, 3);
        var style = StyleBg(PackedRgba.Rgb(10, 20, 30));

        buf.PushScissor(new Rect(1, 1, 1, 1));
        WidgetDrawing.SetStyleArea(buf, new Rect(0, 0, 3, 3), style);

        Assert.Equal(PackedRgba.Rgb(10, 20, 30), buf.Get(1, 1)!.Value.Background);
        Assert.NotEqual(PackedRgba.Rgb(10, 20, 30), buf.Get(0, 1)!.Value.Background);
        Assert.NotEqual(PackedRgba.Rgb(10, 20, 30), buf.Get(1, 0)!.Value.Background);
        Assert.NotEqual(PackedRgba.Rgb(10, 20, 30), buf.Get(2, 2)!.Value.Background);
    }

    [Fact]
    public void SetStyleAreaRespectsOpacityStack()
    {
        var buf = new Buffer(1, 1);
        var baseFg = PackedRgba.Rgb(20, 30, 40);
        var baseBg = PackedRgba.Rgb(50, 60, 70);
        buf.Set(0, 0, Cell.FromChar('X').WithForeground(baseFg).WithBackground(baseBg));

        var overlayFg = PackedRgba.Rgb(200, 100, 0);
        var overlayBg = PackedRgba.Rgb(0, 0, 200);
        buf.PushOpacity(0.5f);
        WidgetDrawing.SetStyleArea(buf, new Rect(0, 0, 1, 1), StyleFgBg(overlayFg, overlayBg));

        var cell = buf.Get(0, 0)!.Value;
        Assert.Equal(overlayFg.WithOpacity(0.5f), cell.Foreground);
        Assert.Equal(overlayBg.WithOpacity(0.5f).Over(baseBg), cell.Background);
    }

    // ────────────────────────────────────────────────────────────────────────
    // draw_text_span tests (via WidgetDrawing.DrawTextSpan)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DrawTextSpanBasic()
    {
        var frame = NewFrame(10, 1);
        var endX = WidgetDrawing.DrawTextSpan(frame, 0, 0, "ABC", StyleDefault(), 10);

        Assert.Equal(3, endX);
        Assert.Equal('A', frame.Buffer.Get(0, 0)!.Value.Content.AsChar());
        Assert.Equal('B', frame.Buffer.Get(1, 0)!.Value.Content.AsChar());
        Assert.Equal('C', frame.Buffer.Get(2, 0)!.Value.Content.AsChar());
    }

    [Fact]
    public void DrawTextSpanClippedAtMaxX()
    {
        var frame = NewFrame(10, 1);
        var endX = WidgetDrawing.DrawTextSpan(frame, 0, 0, "ABCDEF", StyleDefault(), 3);

        Assert.Equal(3, endX);
        Assert.Equal('A', frame.Buffer.Get(0, 0)!.Value.Content.AsChar());
        Assert.Equal('C', frame.Buffer.Get(2, 0)!.Value.Content.AsChar());
        // 'D' should not be drawn
        Assert.True(frame.Buffer.Get(3, 0)!.Value.IsEmpty);
    }

    [Fact]
    public void DrawTextSpanStartsAtOffset()
    {
        var frame = NewFrame(10, 1);
        var endX = WidgetDrawing.DrawTextSpan(frame, 5, 0, "XY", StyleDefault(), 10);

        Assert.Equal(7, endX);
        Assert.Equal('X', frame.Buffer.Get(5, 0)!.Value.Content.AsChar());
        Assert.Equal('Y', frame.Buffer.Get(6, 0)!.Value.Content.AsChar());
        Assert.True(frame.Buffer.Get(4, 0)!.Value.IsEmpty);
    }

    [Fact]
    public void DrawTextSpanEmptyString()
    {
        var frame = NewFrame(5, 1);
        var endX = WidgetDrawing.DrawTextSpan(frame, 0, 0, "", StyleDefault(), 5);
        Assert.Equal(0, endX);
    }

    [Fact]
    public void DrawTextSpanAppliesStyle()
    {
        var frame = NewFrame(5, 1);
        var style = StyleFg(PackedRgba.Rgb(255, 128, 0));
        WidgetDrawing.DrawTextSpan(frame, 0, 0, "A", style, 5);

        Assert.Equal(PackedRgba.Rgb(255, 128, 0), frame.Buffer.Get(0, 0)!.Value.Foreground);
    }

    [Fact]
    public void DrawTextSpanPreservesExistingOverlayFgAndBg()
    {
        var frame = NewFrame(3, 1);
        frame.Buffer.Set(
            0, 0,
            Cell.FromChar('x').WithForeground(PackedRgba.Rgb(200, 40, 10)));
        WidgetDrawing.SetStyleArea(
            frame.Buffer,
            new Rect(0, 0, 1, 1),
            StyleBg(PackedRgba.Rgb(20, 30, 40)));

        WidgetDrawing.DrawTextSpan(frame, 0, 0, "A", StyleDefault(), 1);

        var cell = frame.Buffer.Get(0, 0)!.Value;
        Assert.Equal('A', cell.Content.AsChar());
        Assert.Equal(PackedRgba.Rgb(200, 40, 10), cell.Foreground);
        Assert.Equal(PackedRgba.Rgb(20, 30, 40),  cell.Background);
    }

    [Fact]
    public void DrawTextSpanDropsStaleLinkIdButKeepsStyleFlags()
    {
        var frame = NewFrame(3, 1);
        frame.Buffer.Set(
            0, 0,
            Cell.FromChar('x').WithAttributes(new CellAttributes(CellStyleFlags.Underline, 42)));

        WidgetDrawing.DrawTextSpan(frame, 0, 0, "A", StyleDefault(), 1);

        var cell = frame.Buffer.Get(0, 0)!.Value;
        Assert.Equal('A', cell.Content.AsChar());
        Assert.True(cell.Attributes.HasFlag(CellStyleFlags.Underline));
        Assert.Equal(0u, cell.Attributes.LinkId);
    }

    [Fact]
    public void DrawTextSpanMaxXAtStartDrawsNothing()
    {
        var frame = NewFrame(5, 1);
        var endX = WidgetDrawing.DrawTextSpan(frame, 3, 0, "ABC", StyleDefault(), 3);
        Assert.Equal(3, endX);
        Assert.True(frame.Buffer.Get(3, 0)!.Value.IsEmpty);
    }

    // ────────────────────────────────────────────────────────────────────────
    // widget_is_essential_default_false
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void WidgetIsEssentialDefaultFalse()
    {
        // Default interface implementation: IWidget.IsEssential() returns false.
        // Must be called through the interface reference (C# DIM requirement).
        IWidget w = new DummyWidget();
        Assert.False(w.IsEssential());
    }

    // ────────────────────────────────────────────────────────────────────────
    // budgeted_new_and_inner
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BudgetedNewAndInner()
    {
        var b = new Budgeted<TestWidget>(42UL, new TestWidget());
        Assert.Equal(42UL, b.WidgetId);
        _ = b.Inner; // Should not throw
    }

    // ────────────────────────────────────────────────────────────────────────
    // budgeted_with_signal
    // Upstream (lib.rs lines 1173-1183):
    //   let sig = WidgetSignal::new(99);
    //   let b = Budgeted::new(42, TestW).with_signal(sig);
    //   assert_eq!(b.signal.widget_id, 42);
    // Verifies that with_signal() overrides the supplied signal's widget_id to
    // match the Budgeted's own widget_id (42), not the signal's original id (99).
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BudgetedWithSignal_WidgetIdIsCoherent()
    {
        var sig = WidgetSignal.New(99);
        var b = new Budgeted<TestWidget>(42UL, new TestWidget()).WithSignal(sig);
        // with_signal must override the signal's widget_id to match the Budgeted's own id.
        Assert.Equal(42UL, b.Signal.WidgetId);
    }

    // ────────────────────────────────────────────────────────────────────────
    // set_style_area_transparent_bg_is_noop
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetStyleAreaTransparentBgIsNoop()
    {
        var buf = new Buffer(1, 1);
        var baseColor = PackedRgba.Rgb(100, 100, 100);
        buf.Set(0, 0, Cell.Empty.WithBackground(baseColor));

        // Alpha=0 means fully transparent, should leave bg unchanged
        var transparent = PackedRgba.Rgba(255, 0, 0, 0);
        WidgetDrawing.SetStyleArea(buf, new Rect(0, 0, 1, 1), StyleBg(transparent));
        Assert.Equal(baseColor, buf.Get(0, 0)!.Value.Background);
    }

    // ────────────────────────────────────────────────────────────────────────
    // set_style_area_opaque_bg_replaces
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetStyleAreaOpaqueBgReplaces()
    {
        var buf = new Buffer(1, 1);
        buf.Set(0, 0, Cell.Empty.WithBackground(PackedRgba.Rgb(100, 100, 100)));

        var opaque = PackedRgba.Rgba(0, 255, 0, 255);
        WidgetDrawing.SetStyleArea(buf, new Rect(0, 0, 1, 1), StyleBg(opaque));
        Assert.Equal(opaque, buf.Get(0, 0)!.Value.Background);
    }

    // ────────────────────────────────────────────────────────────────────────
    // draw_text_span_scrolled_skips_chars
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DrawTextSpanScrolledSkipsChars()
    {
        var frame = NewFrame(10, 1);
        // Scroll past first 2 chars of "ABCDE"
        var endX = WidgetDrawing.DrawTextSpanScrolled(frame, 0, 0, "ABCDE", StyleDefault(), 10, 2);

        Assert.Equal(3, endX);
        Assert.Equal('C', frame.Buffer.Get(0, 0)!.Value.Content.AsChar());
        Assert.Equal('D', frame.Buffer.Get(1, 0)!.Value.Content.AsChar());
        Assert.Equal('E', frame.Buffer.Get(2, 0)!.Value.Content.AsChar());
    }

    // ── Private test helpers ──────────────────────────────────────────────────

    private sealed class DummyWidget : IWidget
    {
        public void Render(Rect area, Frame frame) { }
    }

    private sealed class TestWidget : IWidget
    {
        public void Render(Rect area, Frame frame) { }
    }
}
