// SPDX-License-Identifier: Apache-2.0
// Tests ported from .external/frankentui/crates/ftui-layout/src/responsive_layout.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

using FrankenTui.Core;
using FrankenTui.Layout;

namespace FrankenTui.Tests.Headless;

public sealed class ResponsiveLayoutTests
{
    [Fact]
    public void BaseLayoutAtAllBreakpoints()
    {
        var layout = new ResponsiveLayout(SingleColumn());
        Assert.All(BreakpointExtensions.All,
            breakpoint => Assert.Single(layout.SplitFor(breakpoint, Area(80, 24)).Rects));
    }

    [Fact]
    public void SwitchesAtBreakpoint()
    {
        ResponsiveLayout layout = new ResponsiveLayout(SingleColumn()).At(Breakpoint.Md, TwoColumn());
        ResponsiveSplit compact = layout.Split(Area(50, 24));
        Assert.Equal(Breakpoint.Xs, compact.Breakpoint);
        Assert.Single(compact.Rects);
        ResponsiveSplit medium = layout.Split(Area(100, 24));
        Assert.Equal(Breakpoint.Md, medium.Breakpoint);
        Assert.Equal(2, medium.Rects.Count);
    }

    [Fact]
    public void InheritsFromSmaller()
    {
        ResponsiveSplit result = new ResponsiveLayout(SingleColumn())
            .At(Breakpoint.Md, TwoColumn()).Split(Area(130, 24));
        Assert.Equal(Breakpoint.Lg, result.Breakpoint);
        Assert.Equal(2, result.Rects.Count);
    }

    [Fact]
    public void ThreeTierLayout()
    {
        ResponsiveLayout layout = new ResponsiveLayout(SingleColumn())
            .At(Breakpoint.Sm, TwoColumn()).At(Breakpoint.Lg, ThreeColumn());
        Assert.Equal(new[] { 1, 2, 2, 3, 3 },
            new ushort[] { 40, 70, 100, 130, 170 }.Select(width => layout.Split(Area(width, 24)).Rects.Count));
    }

    [Fact]
    public void SplitForIgnoresWidth()
    {
        ResponsiveSplit result = new ResponsiveLayout(SingleColumn())
            .At(Breakpoint.Lg, TwoColumn()).SplitFor(Breakpoint.Lg, Area(40, 24));
        Assert.Equal(Breakpoint.Lg, result.Breakpoint);
        Assert.Equal(2, result.Rects.Count);
    }

    [Fact]
    public void CustomBreakpoints()
    {
        ResponsiveSplit result = new ResponsiveLayout(SingleColumn())
            .At(Breakpoint.Sm, TwoColumn())
            .WithBreakpoints(Breakpoints.New(40, 80, 120))
            .Split(Area(50, 24));
        Assert.Equal(Breakpoint.Sm, result.Breakpoint);
        Assert.Equal(2, result.Rects.Count);
    }

    [Fact]
    public void DetectTransitionSome()
    {
        var layout = new ResponsiveLayout(SingleColumn());
        (Breakpoint Old, Breakpoint New) transition = layout.DetectTransition(50, 100)!.Value;
        Assert.Equal(Breakpoint.Xs, transition.Old);
        Assert.Equal(Breakpoint.Md, transition.New);
    }

    [Fact]
    public void DetectTransitionNone() =>
        Assert.Null(new ResponsiveLayout(SingleColumn()).DetectTransition(70, 80));

    [Fact]
    public void ClassifyWidth()
    {
        var layout = new ResponsiveLayout(SingleColumn());
        Assert.Equal(
            new[] { Breakpoint.Xs, Breakpoint.Sm, Breakpoint.Md, Breakpoint.Lg, Breakpoint.Xl },
            new ushort[] { 40, 60, 90, 120, 160 }.Select(layout.Classify));
    }

    [Fact]
    public void ConstraintCount()
    {
        ResponsiveLayout layout = new ResponsiveLayout(SingleColumn())
            .At(Breakpoint.Md, TwoColumn()).At(Breakpoint.Lg, ThreeColumn());
        Assert.Equal(1, layout.ConstraintCount(Breakpoint.Xs));
        Assert.Equal(1, layout.ConstraintCount(Breakpoint.Sm));
        Assert.Equal(2, layout.ConstraintCount(Breakpoint.Md));
        Assert.Equal(3, layout.ConstraintCount(Breakpoint.Lg));
    }

    [Fact]
    public void LayoutForAccess()
    {
        ResponsiveLayout layout = new ResponsiveLayout(SingleColumn()).At(Breakpoint.Md, TwoColumn());
        Assert.Equal(2, layout.LayoutFor(Breakpoint.Md).ConstraintCount());
    }

    [Fact]
    public void HasExplicitCheck()
    {
        ResponsiveLayout layout = new ResponsiveLayout(SingleColumn()).At(Breakpoint.Lg, TwoColumn());
        Assert.True(layout.HasExplicit(Breakpoint.Xs));
        Assert.False(layout.HasExplicit(Breakpoint.Sm));
        Assert.False(layout.HasExplicit(Breakpoint.Md));
        Assert.True(layout.HasExplicit(Breakpoint.Lg));
    }

    [Fact]
    public void SetMutating()
    {
        var layout = new ResponsiveLayout(SingleColumn());
        layout.Set(Breakpoint.Xl, ThreeColumn());
        Assert.Equal(3, layout.ConstraintCount(Breakpoint.Xl));
    }

    [Fact]
    public void ClearRevertsToInheritance()
    {
        ResponsiveLayout layout = new ResponsiveLayout(SingleColumn()).At(Breakpoint.Md, TwoColumn());
        Assert.Equal(2, layout.ConstraintCount(Breakpoint.Md));
        layout.Clear(Breakpoint.Md);
        Assert.Equal(1, layout.ConstraintCount(Breakpoint.Md));
    }

    [Fact]
    public void EmptyAreaReturnsZeroRects()
    {
        ResponsiveSplit result = new ResponsiveLayout(TwoColumn()).Split(Area(0, 0));
        Assert.Equal(Breakpoint.Xs, result.Breakpoint);
        Assert.Equal(2, result.Rects.Count);
        Assert.All(result.Rects, static rect => Assert.True(rect.Width == 0 && rect.Height == 0));
    }

    [Fact]
    public void RectDimensionsCorrect()
    {
        ResponsiveSplit result = new ResponsiveLayout(Flex.Horizontal()
            .Constraints([Constraint.Fixed(20), Constraint.Fill])).Split(Area(100, 30));
        Assert.Equal(new Rect(0, 0, 20, 30), result.Rects[0]);
        Assert.Equal(new Rect(20, 0, 80, 30), result.Rects[1]);
    }

    [Fact]
    public void BreakpointsAccessor()
    {
        Breakpoints breakpoints = Breakpoints.New(50, 80, 110);
        var layout = new ResponsiveLayout(SingleColumn()).WithBreakpoints(breakpoints);
        Assert.Equal(breakpoints, layout.GetBreakpoints());
    }

    [Fact]
    public void ResponsiveSplitDebug()
    {
        var split = new ResponsiveSplit(Breakpoint.Md, [new Rect(0, 0, 50, 24)]);
        Assert.Contains("Md", split.ToString());
    }

    private static Flex SingleColumn() => Flex.Vertical().Constraints([Constraint.Fill]);

    private static Flex TwoColumn() => Flex.Horizontal()
        .Constraints([Constraint.Fixed(30), Constraint.Fill]);

    private static Flex ThreeColumn() => Flex.Horizontal()
        .Constraints([Constraint.Fixed(25), Constraint.Fill, Constraint.Fixed(25)]);

    private static Rect Area(ushort width, ushort height) => new(0, 0, width, height);
}
