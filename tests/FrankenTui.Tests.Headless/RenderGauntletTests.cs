using System.Text;
using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Testing.Harness;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class RenderGauntletTests
{
    [Fact]
    public void PresenterEquivalenceAcceptsFullAndDirtyDiffOutputs()
    {
        var previous = new RenderBuffer(12, 2);
        var next = new RenderBuffer(12, 2);
        next.Set(0, 0, Cell.FromChar('A'));
        next.Set(1, 0, Cell.FromRune(new Rune(0x1F600)));

        var presenter = new Presenter(TerminalCapabilities.Modern());
        var full = presenter.Present(next, BufferDiff.ComputeFull(previous, next));
        presenter.Reset();
        var dirty = presenter.Present(next, BufferDiff.ComputeDirty(previous, next));

        var report = PresenterEquivalence.Compare(full.Output, dirty.Output, next.Width, next.Height);

        Assert.True(report.Equivalent);
        Assert.Empty(report.Violations);
    }

    [Fact]
    public void PresenterEquivalenceDetectsContentMismatch()
    {
        var report = PresenterEquivalence.Compare("A", "B", 1, 1);

        Assert.False(report.Equivalent);
        Assert.Contains(report.Violations, violation => violation.Variation == NonEquivalentVariation.ContentMismatch);
    }

    [Fact]
    public void LayoutReuseContractMatchesEquivalentCacheKeys()
    {
        var constraints = new[] { LayoutConstraint.Fixed(4), LayoutConstraint.Fill() };
        var trace = LayoutSolver.SplitWithTrace(new Rect(0, 0, 20, 4), LayoutDirection.Horizontal, constraints);
        var plan = new LayoutPlan(new Rect(0, 0, 20, 4), LayoutDirection.Horizontal, constraints);

        Assert.True(LayoutReuseContract.EquivalentKey(plan, trace));
        Assert.True(LayoutReuseContract.IsSafeToReuse(ReusableComputation.LayoutSolve));
        Assert.Contains("Focus", LayoutReuseContract.UnsafeReason(NonReusableComputation.CursorPosition), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderGauntletPassesDefaultFixtureSuite()
    {
        var gauntlet = new RenderGauntlet();

        var report = gauntlet.Run();

        Assert.True(report.Passed, report.Summary());
        Assert.All(report.Results, result => Assert.True(result.Passed, $"{result.Gate}: {result.Summary}"));
    }
}
