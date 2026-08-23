// SPDX-License-Identifier: Apache-2.0
// Tests ported from .external/frankentui/crates/ftui-layout/src/visibility.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

using FrankenTui.Core;
using FrankenTui.Layout;

namespace FrankenTui.Tests.Headless;

public sealed class VisibilityTests
{
    [Fact]
    public void AlwaysVisibleAtAll()
    {
        Assert.All(BreakpointExtensions.All, breakpoint => Assert.True(Visibility.Always.IsVisible(breakpoint)));
        Assert.True(Visibility.Always.IsAlways());
        Assert.False(Visibility.Always.IsNever());
    }

    [Fact]
    public void NeverVisibleAtNone()
    {
        Assert.All(BreakpointExtensions.All, breakpoint => Assert.True(Visibility.Never.IsHidden(breakpoint)));
        Assert.True(Visibility.Never.IsNever());
        Assert.False(Visibility.Never.IsAlways());
    }

    [Fact]
    public void VisibleAbove()
    {
        Visibility visibility = Visibility.VisibleAbove(Breakpoint.Md);
        Assert.Equal(new[] { false, false, true, true, true }, States(visibility));
    }

    [Fact]
    public void VisibleAboveXs() => Assert.True(Visibility.VisibleAbove(Breakpoint.Xs).IsAlways());

    [Fact]
    public void VisibleAboveXl()
    {
        Visibility visibility = Visibility.VisibleAbove(Breakpoint.Xl);
        Assert.True(visibility.IsVisible(Breakpoint.Xl));
        Assert.False(visibility.IsVisible(Breakpoint.Lg));
        Assert.Equal(1u, visibility.VisibleCount());
    }

    [Fact]
    public void VisibleBelow()
    {
        Visibility visibility = Visibility.VisibleBelow(Breakpoint.Md);
        Assert.Equal(new[] { true, true, true, false, false }, States(visibility));
    }

    [Fact]
    public void VisibleBelowXl() => Assert.True(Visibility.VisibleBelow(Breakpoint.Xl).IsAlways());

    [Fact]
    public void VisibleBelowXs()
    {
        Visibility visibility = Visibility.VisibleBelow(Breakpoint.Xs);
        Assert.True(visibility.IsVisible(Breakpoint.Xs));
        Assert.False(visibility.IsVisible(Breakpoint.Sm));
        Assert.Equal(1u, visibility.VisibleCount());
    }

    [Fact]
    public void OnlySingleBreakpoint()
    {
        Visibility visibility = Visibility.Only(Breakpoint.Lg);
        Assert.Equal(new[] { false, false, false, true, false }, States(visibility));
        Assert.Equal(1u, visibility.VisibleCount());
    }

    [Fact]
    public void AtMultiple()
    {
        Visibility visibility = Visibility.At([Breakpoint.Xs, Breakpoint.Lg, Breakpoint.Xl]);
        Assert.Equal(new[] { true, false, false, true, true }, States(visibility));
        Assert.Equal(3u, visibility.VisibleCount());
    }

    [Fact]
    public void HiddenBelow()
    {
        Visibility visibility = Visibility.HiddenBelow(Breakpoint.Md);
        Assert.Equal(new[] { false, false, true, true, true }, States(visibility));
    }

    [Fact]
    public void HiddenAbove()
    {
        Visibility visibility = Visibility.HiddenAbove(Breakpoint.Md);
        Assert.Equal(new[] { true, true, false, false, false }, States(visibility));
    }

    [Fact]
    public void HiddenAboveXs() => Assert.True(Visibility.HiddenAbove(Breakpoint.Xs).IsNever());

    [Fact]
    public void FromMask()
    {
        Assert.Equal(new[] { true, false, true, false, true }, States(Visibility.FromMask(0b10101)));
    }

    [Fact]
    public void FromMaskTruncates() => Assert.Equal((byte)0b11111, Visibility.FromMask(0xff).Mask());

    [Fact]
    public void VisibleBreakpointsIterator()
    {
        Assert.Equal(
            new[] { Breakpoint.Sm, Breakpoint.Lg },
            Visibility.At([Breakpoint.Sm, Breakpoint.Lg]).VisibleBreakpoints());
    }

    [Fact]
    public void FilterRectsBasic()
    {
        Rect[] rects =
        [
            new Rect(0, 0, 20, 10),
            new Rect(20, 0, 30, 10),
            new Rect(50, 0, 40, 10),
        ];
        Visibility[] visibilities =
        [
            Visibility.Always,
            Visibility.HiddenBelow(Breakpoint.Md),
            Visibility.Always,
        ];
        Assert.Equal(new[] { 0, 2 }, Visibility.FilterRects(visibilities, rects, Breakpoint.Sm)
            .Select(static item => item.Index));
        Assert.Equal(3, Visibility.FilterRects(visibilities, rects, Breakpoint.Md).Count);
    }

    [Fact]
    public void CountVisibleHelper()
    {
        Visibility[] visibilities =
        [
            Visibility.Always,
            Visibility.Only(Breakpoint.Xl),
            Visibility.VisibleAbove(Breakpoint.Lg),
        ];
        Assert.Equal(1, Visibility.CountVisible(visibilities, Breakpoint.Xs));
        Assert.Equal(2, Visibility.CountVisible(visibilities, Breakpoint.Lg));
        Assert.Equal(3, Visibility.CountVisible(visibilities, Breakpoint.Xl));
    }

    [Fact]
    public void DefaultIsAlways()
    {
        Assert.Equal(Visibility.Always, Visibility.Default);
        Assert.Equal(Visibility.Always, new Visibility());
    }

    [Fact]
    public void DisplayAlways() => Assert.Equal("always", Visibility.Always.ToString());

    [Fact]
    public void DisplayNever() => Assert.Equal("never", Visibility.Never.ToString());

    [Fact]
    public void DisplayPartial() =>
        Assert.Equal("sm+lg", Visibility.At([Breakpoint.Sm, Breakpoint.Lg]).ToString());

    [Fact]
    public void Equality() => Assert.Equal(
        Visibility.VisibleAbove(Breakpoint.Md),
        Visibility.HiddenBelow(Breakpoint.Md));

    [Fact]
    public void CloneIndependence()
    {
        Visibility first = Visibility.Only(Breakpoint.Md);
        Visibility second = first;
        Assert.Equal(first, second);
    }

    [Fact]
    public void DebugFormat() => Assert.Contains("Visibility", Visibility.Always.ToDebugString());

    private static bool[] States(Visibility visibility) =>
        BreakpointExtensions.All.Select(visibility.IsVisible).ToArray();
}
