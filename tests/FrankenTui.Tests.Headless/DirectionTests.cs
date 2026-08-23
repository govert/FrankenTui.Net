// SPDX-License-Identifier: Apache-2.0
// Tests ported from .external/frankentui/crates/ftui-layout/src/direction.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

using FrankenTui.Core;
using FrankenTui.Layout;

namespace FrankenTui.Tests.Headless;

public sealed class DirectionTests
{
    [Fact]
    public void FlowDirectionDefaultIsLtr()
    {
        Assert.Equal(FlowDirection.Ltr, default);
        Assert.True(FlowDirection.Ltr.IsLtr());
        Assert.False(FlowDirection.Ltr.IsRtl());
        Assert.True(FlowDirection.Rtl.IsRtl());
        Assert.False(FlowDirection.Rtl.IsLtr());
    }

    [Fact]
    public void FlowDirectionFromLocale()
    {
        foreach (string locale in new[] { "en", "en-US", "fr", "ja" })
            Assert.Equal(FlowDirection.Ltr, FlowDirectionExtensions.FromLocale(locale));
        foreach (string locale in new[] { "ar", "ar-SA", "he", "fa", "ur", "yi" })
            Assert.Equal(FlowDirection.Rtl, FlowDirectionExtensions.FromLocale(locale));
    }

    [Fact]
    public void FlowDirectionLocaleCaseInsensitive()
    {
        Assert.Equal(FlowDirection.Rtl, FlowDirectionExtensions.FromLocale("AR"));
        Assert.Equal(FlowDirection.Rtl, FlowDirectionExtensions.FromLocale("He"));
        Assert.Equal(FlowDirection.Ltr, FlowDirectionExtensions.FromLocale("EN"));
    }

    [Fact]
    public void LogicalAlignmentLtrResolution()
    {
        Assert.Equal(Alignment.Start, LogicalAlignment.Start.Resolve(FlowDirection.Ltr));
        Assert.Equal(Alignment.End, LogicalAlignment.End.Resolve(FlowDirection.Ltr));
        Assert.Equal(Alignment.Center, LogicalAlignment.Center.Resolve(FlowDirection.Ltr));
    }

    [Fact]
    public void LogicalAlignmentRtlResolution()
    {
        Assert.Equal(Alignment.End, LogicalAlignment.Start.Resolve(FlowDirection.Rtl));
        Assert.Equal(Alignment.Start, LogicalAlignment.End.Resolve(FlowDirection.Rtl));
        Assert.Equal(Alignment.Center, LogicalAlignment.Center.Resolve(FlowDirection.Rtl));
    }

    [Fact]
    public void LogicalSidesLtrResolution()
    {
        Sides physical = new LogicalSides(1, 2, 3, 4).Resolve(FlowDirection.Ltr);
        Assert.Equal(new Sides(1, 4, 2, 3), physical);
    }

    [Fact]
    public void LogicalSidesRtlResolution()
    {
        Sides physical = new LogicalSides(1, 2, 3, 4).Resolve(FlowDirection.Rtl);
        Assert.Equal(new Sides(1, 3, 2, 4), physical);
    }

    [Fact]
    public void LogicalSidesSymmetry()
    {
        LogicalSides logical = LogicalSides.All(5);
        Assert.Equal(logical.Resolve(FlowDirection.Ltr), logical.Resolve(FlowDirection.Rtl));
    }

    [Fact]
    public void LogicalSidesRoundtrip()
    {
        var original = new LogicalSides(1, 2, 3, 4);
        foreach (FlowDirection direction in Enum.GetValues<FlowDirection>())
            Assert.Equal(original, LogicalSides.FromPhysical(original.Resolve(direction), direction));
    }

    [Fact]
    public void LogicalSidesConstructors()
    {
        Assert.Equal(new LogicalSides(5, 5, 5, 5), LogicalSides.All(5));
        Assert.Equal(new LogicalSides(2, 2, 4, 4), LogicalSides.Symmetric(2, 4));
        Assert.Equal(new LogicalSides(0, 0, 3, 7), LogicalSides.Inline(3, 7));
        Assert.Equal(new LogicalSides(1, 9, 0, 0), LogicalSides.Block(1, 9));
    }

    [Fact]
    public void LogicalSidesSums()
    {
        var sides = new LogicalSides(1, 2, 3, 4);
        Assert.Equal((ushort)7, sides.InlineSum());
        Assert.Equal((ushort)3, sides.BlockSum());
    }

    [Fact]
    public void MirrorRectsSimple()
    {
        var rects = new[]
        {
            new Rect(0, 0, 30, 20),
            new Rect(30, 0, 40, 20),
            new Rect(70, 0, 30, 20),
        };
        DirectionLayout.MirrorRectsHorizontal(rects, new Rect(0, 0, 100, 20));
        Assert.Equal(new ushort[] { 70, 30, 0 }, rects.Select(static rect => rect.X));
        Assert.Equal(new ushort[] { 30, 40, 30 }, rects.Select(static rect => rect.Width));
    }

    [Fact]
    public void MirrorRectsWithOffset()
    {
        var rects = new[] { new Rect(10, 5, 20, 20), new Rect(30, 5, 60, 20) };
        DirectionLayout.MirrorRectsHorizontal(rects, new Rect(10, 5, 80, 20));
        Assert.Equal(new ushort[] { 70, 10 }, rects.Select(static rect => rect.X));
        Assert.Equal(new ushort[] { 20, 60 }, rects.Select(static rect => rect.Width));
    }

    [Fact]
    public void MirrorRectsEmpty()
    {
        var rects = Array.Empty<Rect>();
        DirectionLayout.MirrorRectsHorizontal(rects, new Rect(0, 0, 100, 20));
        Assert.Empty(rects);
    }

    [Fact]
    public void MirrorRectsIdempotentDoubleMirror()
    {
        var original = new[]
        {
            new Rect(5, 0, 30, 20),
            new Rect(35, 0, 25, 20),
            new Rect(60, 0, 35, 20),
        };
        var rects = original.ToArray();
        var area = new Rect(5, 0, 90, 20);
        DirectionLayout.MirrorRectsHorizontal(rects, area);
        DirectionLayout.MirrorRectsHorizontal(rects, area);
        Assert.Equal(original, rects);
    }

    [Fact]
    public void FlexHorizontalRtlReversesOrder()
    {
        var area = new Rect(0, 0, 100, 10);
        IReadOnlyList<Rect> ltr = Flex.Horizontal()
            .Constraints([Constraint.Fixed(30), Constraint.Fixed(70)]).Split(area);
        IReadOnlyList<Rect> rtl = Flex.Horizontal()
            .Constraints([Constraint.Fixed(30), Constraint.Fixed(70)])
            .FlowDirection(FlowDirection.Rtl).Split(area);
        Assert.Equal((ushort)0, ltr[0].X);
        Assert.Equal((ushort)30, ltr[1].X);
        Assert.Equal(new Rect(70, 0, 30, 10), rtl[0]);
        Assert.Equal(new Rect(0, 0, 70, 10), rtl[1]);
    }

    [Fact]
    public void FlexVerticalRtlNoChange()
    {
        var area = new Rect(0, 0, 80, 40);
        IReadOnlyList<Rect> ltr = Flex.Vertical()
            .Constraints([Constraint.Fixed(10), Constraint.Fixed(30)]).Split(area);
        IReadOnlyList<Rect> rtl = Flex.Vertical()
            .Constraints([Constraint.Fixed(10), Constraint.Fixed(30)])
            .FlowDirection(FlowDirection.Rtl).Split(area);
        Assert.Equal(ltr, rtl);
    }

    [Fact]
    public void FlexHorizontalRtlWithGap()
    {
        IReadOnlyList<Rect> rects = Flex.Horizontal()
            .Constraints([Constraint.Fixed(20), Constraint.Fixed(30), Constraint.Fixed(40)])
            .Gap(5).FlowDirection(FlowDirection.Rtl).Split(new Rect(0, 0, 100, 10));
        Assert.Equal(new ushort[] { 80, 45, 0 }, rects.Select(static rect => rect.X));
        Assert.Equal(new ushort[] { 20, 30, 40 }, rects.Select(static rect => rect.Width));
    }

    [Fact]
    public void FlexLtrDefaultUnchanged()
    {
        var area = new Rect(0, 0, 100, 10);
        IReadOnlyList<Rect> implicitLtr = Flex.Horizontal()
            .Constraints([Constraint.Fixed(30), Constraint.Fixed(70)]).Split(area);
        IReadOnlyList<Rect> explicitLtr = Flex.Horizontal()
            .Constraints([Constraint.Fixed(30), Constraint.Fixed(70)])
            .FlowDirection(FlowDirection.Ltr).Split(area);
        Assert.Equal(implicitLtr, explicitLtr);
    }

    [Fact]
    public void FlexMixedDirectionNested()
    {
        IReadOnlyList<Rect> outer = Flex.Horizontal()
            .Constraints([Constraint.Fixed(40), Constraint.Fixed(60)])
            .FlowDirection(FlowDirection.Rtl).Split(new Rect(0, 0, 100, 20));
        Assert.Equal(new Rect(60, 0, 40, 20), outer[0]);
        Assert.Equal(new Rect(0, 0, 60, 20), outer[1]);
        IReadOnlyList<Rect> inner = Flex.Vertical()
            .Constraints([Constraint.Fixed(10), Constraint.Fill]).Split(outer[0]);
        Assert.Equal(new Rect(60, 0, 40, 10), inner[0]);
    }

    [Fact]
    public void LogicalAlignmentInFlex()
    {
        Alignment alignment = LogicalAlignment.Start.Resolve(FlowDirection.Rtl);
        IReadOnlyList<Rect> rects = Flex.Horizontal().Constraints([Constraint.Fixed(20)])
            .Align(alignment).Split(new Rect(0, 0, 100, 10));
        Assert.Equal(new Rect(80, 0, 20, 10), rects[0]);
    }
}
