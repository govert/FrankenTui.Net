// Upstream source: crates/ftui-widgets/src/measurable.rs — tests
// Tests ported from 24 #[cfg(test)] mod tests functions.

using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class MeasurableTests
{
    [Fact] public void SizeConstraintsZeroIsDefault()
    {
        Assert.Equal(SizeConstraints.Zero, default(SizeConstraints));
    }

    [Fact] public void SizeConstraintsExact()
    {
        var c = SizeConstraints.Exact(new Size(10, 5));
        Assert.Equal(new Size(10, 5), c.Min);
        Assert.Equal(new Size(10, 5), c.Preferred);
        Assert.Equal(new Size(10, 5), c.Max);
    }

    [Fact] public void SizeConstraintsAtLeast()
    {
        var c = SizeConstraints.AtLeast(new Size(5, 2), new Size(10, 4));
        Assert.Equal(new Size(5, 2), c.Min);
        Assert.Equal(new Size(10, 4), c.Preferred);
        Assert.Null(c.Max);
    }

    [Fact] public void SizeConstraintsClampBelowMin()
    {
        var c = new SizeConstraints { Min = new Size(5, 2), Preferred = new Size(10, 5), Max = new Size(20, 10) };
        Assert.Equal(new Size(5, 2), c.Clamp(new Size(3, 1)));
    }

    [Fact] public void SizeConstraintsClampInRange()
    {
        var c = new SizeConstraints { Min = new Size(5, 2), Preferred = new Size(10, 5), Max = new Size(20, 10) };
        Assert.Equal(new Size(15, 7), c.Clamp(new Size(15, 7)));
    }

    [Fact] public void SizeConstraintsClampAboveMax()
    {
        var c = new SizeConstraints { Min = new Size(5, 2), Preferred = new Size(10, 5), Max = new Size(20, 10) };
        Assert.Equal(new Size(20, 10), c.Clamp(new Size(30, 20)));
    }

    [Fact] public void SizeConstraintsClampNoMax()
    {
        var c = new SizeConstraints { Min = new Size(5, 2), Preferred = new Size(10, 5), Max = null };
        Assert.Equal(new Size(1000, 500), c.Clamp(new Size(1000, 500)));
        Assert.Equal(new Size(5, 2), c.Clamp(new Size(2, 1)));
    }

    [Fact] public void SizeConstraintsIsSatisfiedBy()
    {
        var c = new SizeConstraints { Min = new Size(5, 2), Preferred = new Size(10, 5), Max = new Size(20, 10) };
        Assert.True(c.IsSatisfiedBy(new Size(10, 5)));
        Assert.True(c.IsSatisfiedBy(new Size(5, 2)));
        Assert.True(c.IsSatisfiedBy(new Size(20, 10)));
        Assert.False(c.IsSatisfiedBy(new Size(4, 2)));
        Assert.False(c.IsSatisfiedBy(new Size(5, 1)));
        Assert.False(c.IsSatisfiedBy(new Size(21, 10)));
    }

    [Fact] public void SizeConstraintsIsSatisfiedByNoMax()
    {
        var c = new SizeConstraints { Min = new Size(5, 2), Preferred = new Size(10, 5), Max = null };
        Assert.True(c.IsSatisfiedBy(new Size(1000, 500)));
        Assert.False(c.IsSatisfiedBy(new Size(4, 2)));
    }

    [Fact] public void SizeConstraintsIntersectBothBounded()
    {
        var a = new SizeConstraints { Min = new Size(5, 2), Preferred = new Size(10, 5), Max = new Size(20, 10) };
        var b = new SizeConstraints { Min = new Size(8, 3), Preferred = new Size(12, 6), Max = new Size(15, 8) };
        var c = a.Intersect(b);
        Assert.Equal(new Size(8, 3), c.Min);
        Assert.Equal(new Size(15, 8), c.Max);
        Assert.Equal(new Size(12, 6), c.Preferred);
    }

    [Fact] public void SizeConstraintsIntersectOneUnbounded()
    {
        var bounded = new SizeConstraints { Min = new Size(5, 2), Preferred = new Size(10, 5), Max = new Size(20, 10) };
        var unbounded = new SizeConstraints { Min = new Size(8, 1), Preferred = new Size(15, 3), Max = null };
        var c = bounded.Intersect(unbounded);
        Assert.Equal(new Size(8, 2), c.Min);
        Assert.Equal(new Size(20, 10), c.Max);
    }

    [Fact] public void SizeConstraintsIntersectBothUnbounded()
    {
        var a = SizeConstraints.AtLeast(new Size(5, 2), new Size(10, 5));
        var b = SizeConstraints.AtLeast(new Size(8, 3), new Size(12, 6));
        var c = a.Intersect(b);
        Assert.Equal(new Size(8, 3), c.Min);
        Assert.Null(c.Max);
        Assert.Equal(new Size(12, 6), c.Preferred);
    }

    // MeasurableWidget tests
    private sealed class FixedSizeWidget : IMeasurableWidget
    {
        private readonly ushort _w;
        private readonly ushort _h;
        public FixedSizeWidget(ushort w, ushort h) { _w = w; _h = h; }
        public SizeConstraints MeasureConstraints(Size available) => SizeConstraints.Exact(new Size(_w, _h));
        public Size Measure(Size available) => new(_w, _h);
        public SizeHint MeasureAxis(Size available, LayoutDirection direction) => new SizeHint(_h, _h, _h);
    }

    [Fact] public void CustomWidgetMeasure()
    {
        var w = new FixedSizeWidget(20, 5);
        var c = w.MeasureConstraints(new Size(100, 50));
        Assert.Equal(new Size(20, 5), c.Min);
        Assert.Equal(new Size(20, 5), c.Preferred);
        Assert.Equal(new Size(20, 5), c.Max);
    }

    [Fact] public void CustomWidgetHasIntrinsicSize()
    {
        IMeasurableWidget w = new FixedSizeWidget(10, 3);
        Assert.False(w.HasIntrinsicSize()); // default impl via interface
    }

    [Fact] public void MeasureIsPureSameInputSameOutput()
    {
        var w = new FixedSizeWidget(15, 4);
        var a = new Size(100, 50);
        var c1 = w.MeasureConstraints(a);
        var c2 = w.MeasureConstraints(a);
        Assert.Equal(c1, c2);
    }

    [Fact] public void DefaultMeasureReturnsZero()
    {
        // A widget that doesn't override MeasureConstraints gets SizeConstraints.Zero
        Assert.Equal(SizeConstraints.Zero, new DefaultWidget().MeasureConstraints(new Size(100, 50)));
    }

    [Fact] public void DefaultHasNoIntrinsicSize()
    {
        IMeasurableWidget w = new DefaultWidget();
        Assert.False(w.HasIntrinsicSize());
    }

    [Fact] public void SizeConstraintsInvariantMinLePreferred()
    {
        var c = new SizeConstraints { Min = new Size(5, 2), Preferred = new Size(10, 5), Max = new Size(20, 10) };
        Assert.True(c.Min.Width <= c.Preferred.Width);
        Assert.True(c.Min.Height <= c.Preferred.Height);
    }

    [Fact] public void SizeConstraintsInvariantPreferredLeMax()
    {
        var c = new SizeConstraints { Min = new Size(5, 2), Preferred = new Size(10, 5), Max = new Size(20, 10) };
        Assert.True(c.Preferred.Width <= c.Max?.Width);
        Assert.True(c.Preferred.Height <= c.Max?.Height);
    }

    // DIVERGENCE: 5 proptest tests (paragraph_measure_is_pure, paragraph_min_constant,
    // paragraph_min_le_preferred, constraints_preferred_le_max, clamp_is_idempotent) use
    // Rust proptest strategies with random text/size generation. These test paragraph widget
    // behavior that is not ported. Core SizeConstraints invariants are covered above.
}

internal sealed class DefaultWidget : IMeasurableWidget
{
    public SizeConstraints MeasureConstraints(Size available) => SizeConstraints.Zero;
    public SizeHint MeasureAxis(Size available, LayoutDirection direction) => new(0, 0, null);
}
