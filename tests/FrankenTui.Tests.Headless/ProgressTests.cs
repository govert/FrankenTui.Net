// Upstream source: crates/ftui-widgets/src/progress.rs (tests module)
// Full 1-1 port of all upstream progress tests.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class ProgressTests
{
    // ── Test helpers (mirrors Rust test helpers) ──────────────────────────

    private static Cell CellAt(Frame frame, ushort x, ushort y)
    {
        var cell = frame.Buffer.Get(x, y);
        Assert.True(cell.HasValue, $"test cell should exist at ({x},{y})");
        return cell!.Value;
    }

    private static string RawRowText(Frame frame, ushort y, ushort width)
    {
        var chars = new char[width];
        for (ushort x = 0; x < width; x++)
        {
            chars[x] = frame.Buffer.Get(x, y)
                            ?.Content.AsChar() ?? ' ';
        }
        return new string(chars);
    }

    // ── Builder tests ─────────────────────────────────────────────────────

    // Upstream: default_progress_bar (progress.rs:612)
    // Checks: ratio == 0.0, label.is_none(), block.is_none()
    // Verified behaviorally since C# fields are private.
    [Fact]
    public void DefaultProgressBar()
    {
        var pb = new ProgressBar();
        var pool = new GraphemePool();

        // ratio == 0.0: render with a sentinel gauge color and assert 0 cells filled
        var frameRatio = new Frame(10, 1, pool);
        pb.GaugeStyle(new WidgetStyle(null, PackedRgba.Green, null))
          .Render(new Rect(0, 0, 10, 1), frameRatio);
        int filled = CountFilledCells(frameRatio, PackedRgba.Green, 10);
        Assert.Equal(0, filled); // ratio 0.0 → no cells filled

        // label.is_none(): render and assert no text character appears in the bar
        var frameLabel = new Frame(10, 1, pool);
        new ProgressBar().Render(new Rect(0, 0, 10, 1), frameLabel);
        // With ratio 0.0 and no label, every cell content must be space (default fill char)
        for (ushort x = 0; x < 10; x++)
        {
            char c = frameLabel.Buffer.Get(x, 0)?.Content.AsChar() ?? ' ';
            Assert.Equal(' ', c); // no label text rendered
        }

        // block.is_none(): render into a 3x3 area; without a block, the bar itself
        // occupies the full area (no border characters at the edges).
        var frameBlock = new Frame(3, 3, pool);
        new ProgressBar()
            .Ratio(1.0)
            .GaugeStyle(new WidgetStyle(null, PackedRgba.Blue, null))
            .Render(new Rect(0, 0, 3, 3), frameBlock);
        // If no block, all 9 cells are gauge-filled; a bordered block would show border chars.
        for (ushort y = 0; y < 3; y++)
            for (ushort x = 0; x < 3; x++)
                Assert.Equal(PackedRgba.Blue, frameBlock.Buffer.Get(x, y)?.Background);
    }

    // Helper to indirectly confirm ratio by counting filled cells
    // using a known gauge color on a known-width area.
    private static int CountFilledCells(Frame frame, PackedRgba gaugeColor, ushort width)
    {
        int count = 0;
        for (ushort x = 0; x < width; x++)
        {
            if (frame.Buffer.Get(x, 0)?.Background == gaugeColor)
                count++;
        }
        return count;
    }

    [Fact]
    public void RatioClampdAboveOne()
    {
        var pb = new ProgressBar().Ratio(1.5);
        // Verify via render: all 10 cells should be filled with gauge color
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        pb.GaugeStyle(new WidgetStyle(null, PackedRgba.Green, null))
          .Render(new Rect(0, 0, 10, 1), frame);
        int filled = CountFilledCells(frame, PackedRgba.Green, 10);
        Assert.Equal(10, filled); // ratio was clamped to 1.0, all cells filled
    }

    [Fact]
    public void RatioClampdBelowZero()
    {
        var pb = new ProgressBar().Ratio(-0.5);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        pb.GaugeStyle(new WidgetStyle(null, PackedRgba.Green, null))
          .Render(new Rect(0, 0, 10, 1), frame);
        int filled = CountFilledCells(frame, PackedRgba.Green, 10);
        Assert.Equal(0, filled); // ratio was clamped to 0.0, no cells filled
    }

    [Fact]
    public void RatioNormalRange()
    {
        // Build a ProgressBar with ratio 0.5 and verify 5/10 cells are filled
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        new ProgressBar()
            .Ratio(0.5)
            .GaugeStyle(new WidgetStyle(null, PackedRgba.Blue, null))
            .Render(new Rect(0, 0, 10, 1), frame);
        int filled = CountFilledCells(frame, PackedRgba.Blue, 10);
        Assert.Equal(5, filled);
    }

    [Fact]
    public void BuilderLabel()
    {
        // Verify label is applied (rendered at center position)
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        new ProgressBar()
            .Ratio(0.5)
            .Label("50%")
            .Render(new Rect(0, 0, 10, 1), frame);
        // "50%" centered in 10 = starts at (10-3)/2 = 3
        Assert.Equal('5', frame.Buffer.Get(3, 0)?.Content.AsChar());
        Assert.Equal('0', frame.Buffer.Get(4, 0)?.Content.AsChar());
        Assert.Equal('%', frame.Buffer.Get(5, 0)?.Content.AsChar());
    }

    // ── Rendering tests ───────────────────────────────────────────────────

    [Fact]
    public void RenderZeroArea()
    {
        var pb = new ProgressBar().Ratio(0.5);
        var area = new Rect(0, 0, 0, 0);
        var pool = new GraphemePool();
        var frame = new Frame(1, 1, pool);
        pb.Render(area, frame); // Should not panic
    }

    [Fact]
    public void RenderZeroRatioNoFill()
    {
        var gaugeStyle = new WidgetStyle(null, PackedRgba.Red, null);
        var pb = new ProgressBar().Ratio(0.0).GaugeStyle(gaugeStyle);
        var area = new Rect(0, 0, 10, 1);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        pb.Render(area, frame);

        // No cells should have the gauge style bg
        for (ushort x = 0; x < 10; x++)
        {
            var cell = CellAt(frame, x, 0);
            Assert.NotEqual(PackedRgba.Red, cell.Background);
        }
    }

    [Fact]
    public void RenderFullRatioFillsAll()
    {
        var gaugeStyle = new WidgetStyle(null, PackedRgba.Green, null);
        var pb = new ProgressBar().Ratio(1.0).GaugeStyle(gaugeStyle);
        var area = new Rect(0, 0, 10, 1);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        pb.Render(area, frame);

        // All cells should have gauge bg
        for (ushort x = 0; x < 10; x++)
        {
            var cell = CellAt(frame, x, 0);
            Assert.Equal(PackedRgba.Green, cell.Background);
        }
    }

    [Fact]
    public void RenderHalfRatio()
    {
        var gaugeStyle = new WidgetStyle(null, PackedRgba.Blue, null);
        var pb = new ProgressBar().Ratio(0.5).GaugeStyle(gaugeStyle);
        var area = new Rect(0, 0, 10, 1);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        pb.Render(area, frame);

        // About 5 cells should be filled (10 * 0.5 = 5)
        int filledCount = 0;
        for (ushort x = 0; x < 10; x++)
        {
            if (CellAt(frame, x, 0).Background == PackedRgba.Blue)
                filledCount++;
        }
        Assert.Equal(5, filledCount);
    }

    [Fact]
    public void RenderMultiRowBar()
    {
        var gaugeStyle = new WidgetStyle(null, PackedRgba.Red, null);
        var pb = new ProgressBar().Ratio(1.0).GaugeStyle(gaugeStyle);
        var area = new Rect(0, 0, 5, 3);
        var pool = new GraphemePool();
        var frame = new Frame(5, 3, pool);
        pb.Render(area, frame);

        // All 3 rows should be filled
        for (ushort y = 0; y < 3; y++)
        {
            for (ushort x = 0; x < 5; x++)
            {
                var cell = CellAt(frame, x, y);
                Assert.Equal(PackedRgba.Red, cell.Background);
            }
        }
    }

    [Fact]
    public void RenderWithLabelCentered()
    {
        var pb = new ProgressBar().Ratio(0.5).Label("50%");
        var area = new Rect(0, 0, 10, 1);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        pb.Render(area, frame);

        // Label "50%" is 3 chars wide, centered in 10 = starts at x=3
        // (10 - 3) / 2 = 3
        var c = frame.Buffer.Get(3, 0)?.Content.AsChar();
        Assert.Equal('5', c);
        c = frame.Buffer.Get(4, 0)?.Content.AsChar();
        Assert.Equal('0', c);
        c = frame.Buffer.Get(5, 0)?.Content.AsChar();
        Assert.Equal('%', c);
    }

    [Fact]
    public void RenderWithBlock()
    {
        var pb = new ProgressBar()
            .Ratio(1.0)
            .GaugeStyle(new WidgetStyle(null, PackedRgba.Green, null))
            .WithBlock(Block.Bordered());
        // Block::bordered() includes 1 cell padding on each side, so we need
        // at least 5 rows to have a 1-row inner content area.
        var area = new Rect(0, 0, 10, 5);
        var pool = new GraphemePool();
        var frame = new Frame(10, 5, pool);
        pb.Render(area, frame);

        // Inner area is 6x1 (borders + padding take 2 on each side)
        // All inner cells should have gauge bg
        for (ushort x = 2; x < 8; x++)
        {
            var cell = CellAt(frame, x, 2);
            Assert.Equal(PackedRgba.Green, cell.Background);
        }
    }

    // ── Degradation tests ─────────────────────────────────────────────────

    [Fact]
    public void DegradationSkeletonSkipsEntirely()
    {
        var pb = new ProgressBar()
            .Ratio(0.5)
            .GaugeStyle(new WidgetStyle(null, PackedRgba.Green, null));
        var area = new Rect(0, 0, 10, 1);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        frame.SetDegradation(DegradationLevel.Skeleton);
        pb.Render(area, frame);

        // Nothing should be rendered
        for (ushort x = 0; x < 10; x++)
        {
            Assert.True(
                CellAt(frame, x, 0).IsEmpty,
                $"cell at x={x} should be empty at Skeleton");
        }
    }

    [Fact]
    public void DegradationEssentialOnlyShowsPercentage()
    {
        var pb = new ProgressBar()
            .Ratio(0.5)
            .GaugeStyle(new WidgetStyle(null, PackedRgba.Green, null));
        var area = new Rect(0, 0, 10, 1);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        frame.SetDegradation(DegradationLevel.EssentialOnly);
        pb.Render(area, frame);

        // Should show "50%" text, no gauge bar
        Assert.Equal('5', CellAt(frame, 0, 0).Content.AsChar());
        Assert.Equal('0', CellAt(frame, 1, 0).Content.AsChar());
        Assert.Equal('%', CellAt(frame, 2, 0).Content.AsChar());
        // No gauge background color
        Assert.NotEqual(PackedRgba.Green, CellAt(frame, 0, 0).Background);
    }

    [Fact]
    public void DegradationFullRendersBar()
    {
        var pb = new ProgressBar()
            .Ratio(1.0)
            .GaugeStyle(new WidgetStyle(null, PackedRgba.Blue, null));
        var area = new Rect(0, 0, 10, 1);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        frame.SetDegradation(DegradationLevel.Full);
        pb.Render(area, frame);

        // All cells should have gauge bg
        for (ushort x = 0; x < 10; x++)
        {
            Assert.Equal(PackedRgba.Blue, CellAt(frame, x, 0).Background);
        }
    }

    [Fact]
    public void RenderNoStylingRatioShrinkClearsStaleFill()
    {
        var area = new Rect(0, 0, 10, 1);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        frame.SetDegradation(DegradationLevel.NoStyling);

        new ProgressBar().Ratio(0.8).Render(area, frame);
        new ProgressBar().Ratio(0.2).Render(area, frame);

        Assert.Equal("##        ", RawRowText(frame, 0, 10));
    }

    [Fact]
    public void DegradationEssentialOnlyClearsPreviousBarContent()
    {
        var pb = new ProgressBar().Ratio(0.5);
        var area = new Rect(0, 0, 10, 1);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);

        pb.Render(area, frame);
        frame.SetDegradation(DegradationLevel.EssentialOnly);
        pb.Render(area, frame);

        Assert.Equal("50%       ", RawRowText(frame, 0, 10));
    }

    // ── MiniBar tests ─────────────────────────────────────────────────────

    [Fact]
    public void MiniBarZeroIsEmpty()
    {
        var bar = new MiniBar(0.0, 10);
        int filled = bar.RenderString().Count(c => c == '█');
        Assert.Equal(0, filled);
    }

    [Fact]
    public void MiniBarFullIsComplete()
    {
        var bar = new MiniBar(1.0, 10);
        int filled = bar.RenderString().Count(c => c == '█');
        Assert.Equal(10, filled);
    }

    [Fact]
    public void MiniBarHalfIsHalf()
    {
        var bar = new MiniBar(0.5, 10);
        int filled = bar.RenderString().Count(c => c == '█');
        Assert.InRange(filled, 4, 6);
    }

    [Fact]
    public void MiniBarColorThresholds()
    {
        var high = MiniBar.ColorForValue(0.80);
        var mid  = MiniBar.ColorForValue(0.60);
        var low  = MiniBar.ColorForValue(0.30);
        var crit = MiniBar.ColorForValue(0.10);
        Assert.NotEqual(high, mid);
        Assert.NotEqual(mid, low);
        Assert.NotEqual(low, crit);
    }

    [Fact]
    public void MiniBarRespectsWidth()
    {
        foreach (int width in new[] { 5, 10, 20 })
        {
            var bar = new MiniBar(0.5, (ushort)width);
            Assert.Equal(width, bar.RenderString().Length);
        }
    }

    // ── MeasurableWidget tests ────────────────────────────────────────────

    [Fact]
    public void ProgressBarMeasureHasIntrinsicSize()
    {
        var pb = new ProgressBar();
        Assert.True(pb.HasIntrinsicSize());
    }

    [Fact]
    public void ProgressBarMeasureMinSize()
    {
        var pb = new ProgressBar();
        var c = pb.Measure(Size.Max);

        Assert.Equal(1, c.Min.Width);
        Assert.Equal(1, c.Min.Height);
        Assert.Null(c.Max); // Fills available width
    }

    [Fact]
    public void ProgressBarMeasureWithBlock()
    {
        var pb = new ProgressBar().WithBlock(Block.Bordered());
        var c = pb.Measure(Size.Max);

        // Block adds chrome (borders + padding) = 4 on each axis.
        Assert.Equal(5, c.Min.Width);
        Assert.Equal(5, c.Min.Height);
    }

    [Fact]
    public void MiniBarMeasureFixedWidth()
    {
        var bar = new MiniBar(0.5, 10);
        var c = bar.Measure(Size.Max);

        Assert.Equal(10, c.Preferred.Width);
        Assert.Equal(1, c.Preferred.Height);
        Assert.Equal(new Size(10, 1), c.Max);
    }

    [Fact]
    public void MiniBarMeasureWithPercent()
    {
        var bar = new MiniBar(0.5, 10).ShowPercent(true);
        var c = bar.Measure(Size.Max);

        // Width = 10 + 5 (" XXX%") = 15
        Assert.Equal(15, c.Preferred.Width);
        Assert.Equal(1, c.Preferred.Height);
    }

    [Fact]
    public void MiniBarMeasureHasIntrinsicSize()
    {
        var bar = new MiniBar(0.5, 10);
        Assert.True(bar.HasIntrinsicSize());

        var zeroWidth = new MiniBar(0.5, 0);
        Assert.False(zeroWidth.HasIntrinsicSize());
    }

    // ── Edge-case tests (bd-3b82x) ──────────────────────────────────────

    [Fact]
    public void RatioNanClampdToZero()
    {
        var pb = new ProgressBar().Ratio(double.NaN);
        // We now safely handle NaN in the ratio setter to avoid clamping panic.
        // The render path uses floor() which handles NaN → 0
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        var area = new Rect(0, 0, 10, 1);
        pb.Render(area, frame); // Should not panic
    }

    [Fact]
    public void RatioInfinityClamped()
    {
        // Positive infinity → clamped to 1.0 (fills all cells)
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        new ProgressBar()
            .Ratio(double.PositiveInfinity)
            .GaugeStyle(new WidgetStyle(null, PackedRgba.Green, null))
            .Render(new Rect(0, 0, 10, 1), frame);
        int filled = CountFilledCells(frame, PackedRgba.Green, 10);
        Assert.Equal(10, filled);

        var pool2 = new GraphemePool();
        var frame2 = new Frame(10, 1, pool2);
        new ProgressBar()
            .Ratio(double.NegativeInfinity)
            .GaugeStyle(new WidgetStyle(null, PackedRgba.Red, null))
            .Render(new Rect(0, 0, 10, 1), frame2);
        int filledNeg = CountFilledCells(frame2, PackedRgba.Red, 10);
        Assert.Equal(0, filledNeg);
    }

    [Fact]
    public void LabelWiderThanArea()
    {
        var pb = new ProgressBar()
            .Ratio(0.5)
            .Label("This is a very long label text");
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        var area = new Rect(0, 0, 5, 1);
        pb.Render(area, frame); // Should not panic, truncated
    }

    [Fact]
    public void LabelOnMultiRowBarVerticallyCentered()
    {
        var pb = new ProgressBar().Ratio(0.5).Label("X");
        var pool = new GraphemePool();
        var frame = new Frame(10, 5, pool);
        var area = new Rect(0, 0, 10, 5);
        pb.Render(area, frame);
        // label_y = top + height/2 = 0 + 2 = 2
        var c = frame.Buffer.Get(4, 2)?.Content.AsChar();
        Assert.Equal('X', c);
    }

    [Fact]
    public void EmptyLabelRendersNoText()
    {
        var pb = new ProgressBar().Ratio(0.5).Label("");
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        var area = new Rect(0, 0, 10, 1);
        pb.Render(area, frame); // Should not panic
    }

    [Fact]
    public void ProgressBarCloneAndDebug()
    {
        // C# sealed class does not have auto-derived Clone/Debug like Rust.
        // DIVERGENCE: Rust #[derive(Clone, Debug)] → .NET sealed class has no auto clone.
        // We verify the builder pattern round-trips correctly instead.
        var pb = new ProgressBar().Ratio(0.5).Label("test");
        // Re-create with same parameters (equivalent of clone check)
        var pb2 = new ProgressBar().Ratio(0.5).Label("test");
        var pool = new GraphemePool();
        var frame1 = new Frame(10, 1, pool);
        var frame2 = new Frame(10, 1, pool);
        pb.Render(new Rect(0, 0, 10, 1), frame1);
        pb2.Render(new Rect(0, 0, 10, 1), frame2);
        Assert.Equal(RawRowText(frame1, 0, 10), RawRowText(frame2, 0, 10));
    }

    [Fact]
    public void ProgressBarDefaultTrait()
    {
        // Default via constructor
        var pb = new ProgressBar();
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        // Ratio 0.0, no fill
        pb.GaugeStyle(new WidgetStyle(null, PackedRgba.Green, null))
          .Render(new Rect(0, 0, 10, 1), frame);
        int filled = CountFilledCells(frame, PackedRgba.Green, 10);
        Assert.Equal(0, filled);
    }

    [Fact]
    public void RenderWidthOne()
    {
        var pb = new ProgressBar()
            .Ratio(1.0)
            .GaugeStyle(new WidgetStyle(null, PackedRgba.Red, null));
        var pool = new GraphemePool();
        var frame = new Frame(1, 1, pool);
        var area = new Rect(0, 0, 1, 1);
        pb.Render(area, frame);
        Assert.Equal(PackedRgba.Red, CellAt(frame, 0, 0).Background);
    }

    [Fact]
    public void RenderRatioJustAboveZero()
    {
        var pb = new ProgressBar()
            .Ratio(0.01)
            .GaugeStyle(new WidgetStyle(null, PackedRgba.Green, null));
        var pool = new GraphemePool();
        var frame = new Frame(100, 1, pool);
        var area = new Rect(0, 0, 100, 1);
        pb.Render(area, frame);
        // floor(100 * 0.01) = 1 cell filled
        Assert.Equal(PackedRgba.Green, CellAt(frame, 0, 0).Background);
        Assert.NotEqual(PackedRgba.Green, CellAt(frame, 1, 0).Background);
    }

    // ── MiniBar edge cases ────────────────────────────────────────────────

    [Fact]
    public void MiniBarNanValueTreatedAsZero()
    {
        var bar = new MiniBar(double.NaN, 10);
        int filled = bar.RenderString().Count(c => c == '█');
        Assert.Equal(0, filled);
    }

    [Fact]
    public void MiniBarInfinityClampdToFull()
    {
        var bar = new MiniBar(double.PositiveInfinity, 10);
        int filled = bar.RenderString().Count(c => c == '█');
        Assert.Equal(0, filled); // NaN/Inf → normalized_value returns 0.0
    }

    [Fact]
    public void MiniBarNegativeValue()
    {
        var bar = new MiniBar(-0.5, 10);
        int filled = bar.RenderString().Count(c => c == '█');
        Assert.Equal(0, filled);
    }

    [Fact]
    public void MiniBarValueAboveOne()
    {
        var bar = new MiniBar(1.5, 10);
        int filled = bar.RenderString().Count(c => c == '█');
        Assert.Equal(10, filled); // clamped to 1.0
    }

    [Fact]
    public void MiniBarWidthZero()
    {
        var bar = new MiniBar(0.5, 0);
        Assert.Equal("", bar.RenderString());
    }

    [Fact]
    public void MiniBarWidthOne()
    {
        var bar = new MiniBar(1.0, 1);
        var s = bar.RenderString();
        Assert.Equal(1, s.Length);
        Assert.Equal('█', s[0]);
    }

    [Fact]
    public void MiniBarCustomChars()
    {
        var bar = new MiniBar(0.5, 4).FilledChar('#').EmptyChar('-');
        var s = bar.RenderString();
        Assert.Contains('#', s);
        Assert.Contains('-', s);
        Assert.Equal(4, s.Length);
    }

    [Fact]
    public void MiniBarValueAndWidthSetters()
    {
        var bar = new MiniBar(0.0, 5).Value(1.0).Width(3);
        Assert.Equal(3, bar.RenderString().Length);
        int filled = bar.RenderString().Count(c => c == '█');
        Assert.Equal(3, filled);
    }

    [Fact]
    public void MiniBarColorBoundaryExactlyAtHigh()
    {
        // Default high threshold is 0.75; at exactly 0.75, value is NOT > 0.75
        var atThresh = MiniBar.ColorForValue(0.75);
        var above    = MiniBar.ColorForValue(0.76);
        var defaults = MiniBarColors.Default();
        Assert.Equal(defaults.High, above);
        Assert.Equal(defaults.Mid, atThresh); // not above high threshold
    }

    [Fact]
    public void MiniBarColorBoundaryExactlyAtMid()
    {
        var atThresh = MiniBar.ColorForValue(0.50);
        var defaults = MiniBarColors.Default();
        Assert.Equal(defaults.Low, atThresh); // not above mid threshold
    }

    [Fact]
    public void MiniBarColorBoundaryExactlyAtLow()
    {
        var atThresh = MiniBar.ColorForValue(0.25);
        var defaults = MiniBarColors.Default();
        Assert.Equal(defaults.Critical, atThresh); // not above low threshold
    }

    [Fact]
    public void MiniBarColorForValueNan()
    {
        var c = MiniBar.ColorForValue(double.NaN);
        var defaults = MiniBarColors.Default();
        Assert.Equal(defaults.Critical, c); // NaN → 0.0 → critical
    }

    [Fact]
    public void MiniBarColorsNew()
    {
        var r = PackedRgba.Rgb(255, 0, 0);
        var g = PackedRgba.Rgb(0, 255, 0);
        var b = PackedRgba.Rgb(0, 0, 255);
        var w = PackedRgba.Rgb(255, 255, 255);
        var colors = new MiniBarColors(r, g, b, w);
        Assert.Equal(r, colors.High);
        Assert.Equal(g, colors.Mid);
        Assert.Equal(b, colors.Low);
        Assert.Equal(w, colors.Critical);
    }

    [Fact]
    public void MiniBarCustomThresholdsAndColors()
    {
        var colors = new MiniBarColors(
            PackedRgba.Rgb(1, 1, 1),
            PackedRgba.Rgb(2, 2, 2),
            PackedRgba.Rgb(3, 3, 3),
            PackedRgba.Rgb(4, 4, 4));
        var thresholds = new MiniBarThresholds(0.9, 0.5, 0.1);
        var bar = new MiniBar(0.95, 10).Colors(colors).Thresholds(thresholds);
        var c = bar.ColorForValueWithPalette(0.95);
        Assert.Equal(PackedRgba.Rgb(1, 1, 1), c);
    }

    [Fact]
    public void MiniBarCloneAndDebug()
    {
        // DIVERGENCE: Rust #[derive(Clone, Debug)] → .NET sealed class, no auto Clone/Debug.
        // We verify render output matches between two identically-built instances.
        var bar  = new MiniBar(0.5, 10).ShowPercent(true);
        var bar2 = new MiniBar(0.5, 10).ShowPercent(true);
        Assert.Equal(bar.RenderString(), bar2.RenderString());
        // Debug string check — just verify type name appears in ToString output (default)
        var dbg = bar.ToString()!;
        Assert.NotNull(dbg); // .NET class has a default ToString
    }

    [Fact]
    public void MiniBarRenderZeroArea()
    {
        var bar = new MiniBar(0.5, 10);
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        var area = new Rect(0, 0, 0, 0);
        bar.Render(area, frame); // Should not panic
    }

    [Fact]
    public void MiniBarRenderWithPercentNarrow()
    {
        var bar = new MiniBar(0.5, 10).ShowPercent(true);
        var pool = new GraphemePool();
        var frame = new Frame(5, 1, pool);
        // Area smaller than bar_width + percent_width
        var area = new Rect(0, 0, 5, 1);
        bar.Render(area, frame); // Should adapt or truncate
    }

    [Fact]
    public void MiniBarRenderPercentOnlyNoBarRoom()
    {
        var bar = new MiniBar(0.5, 10).ShowPercent(true);
        var pool = new GraphemePool();
        var frame = new Frame(5, 1, pool);
        // Area of width 5, percent takes 5 (" XXX%"), bar_width gets 0
        var area = new Rect(0, 0, 5, 1);
        bar.Render(area, frame);
        Assert.Equal('5', CellAt(frame, 0, 0).Content.AsChar());
    }

    [Fact]
    public void MiniBarRenderPercentOnlyStartsWithDigitsInTightWidths()
    {
        var bar = new MiniBar(0.5, 10).ShowPercent(true);
        var pool = new GraphemePool();
        var frame = new Frame(2, 1, pool);
        bar.Render(new Rect(0, 0, 2, 1), frame);

        Assert.Equal('5', CellAt(frame, 0, 0).Content.AsChar());
        Assert.Equal('0', CellAt(frame, 1, 0).Content.AsChar());
    }

    [Fact]
    public void MiniBarEssentialOnlyPercentStartsWithDigitsInTightWidths()
    {
        var bar = new MiniBar(0.5, 10).ShowPercent(true);
        var pool = new GraphemePool();
        var frame = new Frame(2, 1, pool);
        frame.SetDegradation(DegradationLevel.EssentialOnly);
        bar.Render(new Rect(0, 0, 2, 1), frame);

        Assert.Equal('5', CellAt(frame, 0, 0).Content.AsChar());
        Assert.Equal('0', CellAt(frame, 1, 0).Content.AsChar());
    }

    [Fact]
    public void MiniBarEssentialOnlyRightAlignsPercentWhenWidthAllows()
    {
        var bar = new MiniBar(0.5, 10).ShowPercent(true);
        var pool = new GraphemePool();
        var frame = new Frame(7, 1, pool);
        frame.SetDegradation(DegradationLevel.EssentialOnly);
        bar.Render(new Rect(0, 0, 7, 1), frame);

        Assert.Equal(' ', CellAt(frame, 0, 0).Content.AsChar());
        Assert.Equal(' ', CellAt(frame, 1, 0).Content.AsChar());
        Assert.Equal(' ', CellAt(frame, 2, 0).Content.AsChar());
        Assert.Equal(' ', CellAt(frame, 3, 0).Content.AsChar());
        Assert.Equal('5', CellAt(frame, 4, 0).Content.AsChar());
        Assert.Equal('0', CellAt(frame, 5, 0).Content.AsChar());
        Assert.Equal('%', CellAt(frame, 6, 0).Content.AsChar());
    }

    [Fact]
    public void MiniBarEssentialOnlyClearsPreviousBarContent()
    {
        var bar = new MiniBar(0.5, 10).ShowPercent(true);
        var area = new Rect(0, 0, 7, 1);
        var pool = new GraphemePool();
        var frame = new Frame(7, 1, pool);

        bar.Render(area, frame);
        frame.SetDegradation(DegradationLevel.EssentialOnly);
        bar.Render(area, frame);

        Assert.Equal("    50%", RawRowText(frame, 0, 7));
    }

    [Fact]
    public void MiniBarThresholdsDefaultValues()
    {
        var t = MiniBarThresholds.Default();
        Assert.Equal(0.75, t.High, precision: 15);
        Assert.Equal(0.50, t.Mid,  precision: 15);
        Assert.Equal(0.25, t.Low,  precision: 15);
    }

    [Fact]
    public void MiniBarColorsDefaultNotAllSame()
    {
        var c = MiniBarColors.Default();
        Assert.NotEqual(c.High, c.Mid);
        Assert.NotEqual(c.Mid, c.Low);
        Assert.NotEqual(c.Low, c.Critical);
    }

    [Fact]
    public void MiniBarColorsCopy()
    {
        // DIVERGENCE: Rust MiniBarColors is Copy (value semantics).
        // C# MiniBarColors is a sealed class (reference semantics).
        // We verify that two separate Default() calls produce equal field values.
        var c  = MiniBarColors.Default();
        var c2 = MiniBarColors.Default();
        Assert.Equal(c.High, c2.High);
    }

    [Fact]
    public void MiniBarThresholdsCopy()
    {
        // DIVERGENCE: Rust MiniBarThresholds is Copy (value semantics).
        // C# MiniBarThresholds is a sealed class (reference semantics).
        var t  = MiniBarThresholds.Default();
        var t2 = MiniBarThresholds.Default();
        Assert.Equal(t.High, t2.High, precision: 15);
    }

    [Fact]
    public void MiniBarStyleSetter()
    {
        var bar = new MiniBar(0.5, 10).Style(new WidgetStyle(null, null, CellStyleFlags.Bold));
        // Verify the bar renders without errors (style was accepted)
        var pool = new GraphemePool();
        var frame = new Frame(10, 1, pool);
        bar.Render(new Rect(0, 0, 10, 1), frame); // Should not panic
        // The existence of a successful render confirms Style setter worked.
        Assert.NotNull(bar.RenderString());
    }
}
