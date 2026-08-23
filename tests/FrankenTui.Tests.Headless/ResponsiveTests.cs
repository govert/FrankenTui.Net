// SPDX-License-Identifier: Apache-2.0
// Tests ported from .external/frankentui/crates/ftui-layout/src/responsive.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

using FrankenTui.Layout;

namespace FrankenTui.Tests.Headless;

public sealed class ResponsiveTests
{
    [Fact]
    public void BaseValueAtAllBreakpoints()
    {
        var responsive = new Responsive<int>(42);
        Assert.All(BreakpointExtensions.All, breakpoint => Assert.Equal(42, responsive.Resolve(breakpoint)));
    }

    [Fact]
    public void OverrideSingleBreakpoint()
    {
        Responsive<int> responsive = new Responsive<int>(1).At(Breakpoint.Md, 2);
        Assert.Equal(new[] { 1, 1, 2, 2, 2 }, Values(responsive));
    }

    [Fact]
    public void OverrideMultipleBreakpoints()
    {
        Responsive<int> responsive = new Responsive<int>(0)
            .At(Breakpoint.Sm, 1).At(Breakpoint.Lg, 3);
        Assert.Equal(new[] { 0, 1, 1, 3, 3 }, Values(responsive));
    }

    [Fact]
    public void SetMutating()
    {
        var responsive = new Responsive<int>(0);
        responsive.Set(Breakpoint.Xl, 5);
        Assert.Equal(5, responsive.Resolve(Breakpoint.Xl));
    }

    [Fact]
    public void ClearRevertsToInheritance()
    {
        Responsive<int> responsive = new Responsive<int>(1).At(Breakpoint.Md, 2);
        Assert.Equal(2, responsive.Resolve(Breakpoint.Md));
        responsive.Clear(Breakpoint.Md);
        Assert.Equal(1, responsive.Resolve(Breakpoint.Md));
    }

    [Fact]
    public void ClearXsIsNoop()
    {
        var responsive = new Responsive<int>(42);
        responsive.Clear(Breakpoint.Xs);
        Assert.Equal(42, responsive.Resolve(Breakpoint.Xs));
    }

    [Fact]
    public void HasExplicit()
    {
        Responsive<int> responsive = new Responsive<int>(0).At(Breakpoint.Lg, 3);
        Assert.Equal(new[] { true, false, false, true, false },
            BreakpointExtensions.All.Select(responsive.HasExplicit));
    }

    [Fact]
    public void ExplicitValuesIterator()
    {
        Responsive<int> responsive = new Responsive<int>(0)
            .At(Breakpoint.Md, 2).At(Breakpoint.Xl, 4);
        Assert.Equal(
            new[]
            {
                (Breakpoint.Xs, 0),
                (Breakpoint.Md, 2),
                (Breakpoint.Xl, 4),
            },
            responsive.ExplicitValues());
    }

    [Fact]
    public void MapValues()
    {
        Responsive<int> doubled = new Responsive<int>(10)
            .At(Breakpoint.Lg, 20).Map(static value => value * 2);
        Assert.Equal(20, doubled.Resolve(Breakpoint.Xs));
        Assert.Equal(40, doubled.Resolve(Breakpoint.Lg));
    }

    [Fact]
    public void ResolveCloned()
    {
        var responsive = new Responsive<string>("hello");
        Assert.Equal("hello", responsive.ResolveCloned(Breakpoint.Md));
    }

    [Fact]
    public void Default()
    {
        Responsive<int> responsive = Responsive<int>.Default;
        Assert.Equal(0, responsive.Resolve(Breakpoint.Xs));
    }

    [Fact]
    public void CloneIndependence()
    {
        var original = new Responsive<int>(1);
        Responsive<int> clone = original.Clone();
        clone.Set(Breakpoint.Md, 99);
        Assert.Equal(1, original.Resolve(Breakpoint.Md));
        Assert.Equal(99, clone.Resolve(Breakpoint.Md));
    }

    [Fact]
    public void DisplayFormat()
    {
        string display = new Responsive<int>(0).At(Breakpoint.Md, 2).ToString();
        Assert.Contains("xs=0", display);
        Assert.Contains("md=2", display);
    }

    [Fact]
    public void StringResponsive()
    {
        Responsive<string> responsive = new Responsive<string>("compact")
            .At(Breakpoint.Md, "standard").At(Breakpoint.Xl, "expanded");
        Assert.Equal(
            new[] { "compact", "compact", "standard", "standard", "expanded" },
            Values(responsive));
    }

    [Fact]
    public void AllBreakpointsOverridden()
    {
        Responsive<int> responsive = new Responsive<int>(0)
            .At(Breakpoint.Sm, 1).At(Breakpoint.Md, 2)
            .At(Breakpoint.Lg, 3).At(Breakpoint.Xl, 4);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, Values(responsive));
    }

    [Fact]
    public void Equality()
    {
        Responsive<int> first = new Responsive<int>(1).At(Breakpoint.Md, 2);
        Responsive<int> second = new Responsive<int>(1).At(Breakpoint.Md, 2);
        Assert.Equal(first, second);
    }

    private static T[] Values<T>(Responsive<T> responsive) =>
        BreakpointExtensions.All.Select(responsive.Resolve).ToArray();
}
