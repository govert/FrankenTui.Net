// SPDX-License-Identifier: Apache-2.0
// Tests adapted from crates/ftui-render/src/render_context.rs at 15cc6543.

using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

public sealed class RenderContextTests : IDisposable
{
    public RenderContextTests() => RenderContext.ClearMarks();

    public void Dispose() => RenderContext.ClearMarks();

    [Fact]
    public void PushPopBasic()
    {
        RenderContext.PushMark(RenderMark.Widget("Button"));
        Assert.Equal(1, RenderContext.MarkDepth);

        var mark = RenderContext.PopMark();

        Assert.NotNull(mark);
        Assert.Equal(MarkKind.Widget, mark.Kind);
        Assert.Equal("Button", mark.Label);
        Assert.Equal(0, RenderContext.MarkDepth);
    }

    [Fact]
    public void PopEmptyReturnsNull() => Assert.Null(RenderContext.PopMark());

    [Fact]
    public void GuardAutomaticallyPopsNestedMarks()
    {
        using (MarkGuard.Widget("Outer"))
        {
            Assert.Equal(1, RenderContext.MarkDepth);
            using (MarkGuard.Constraint("Inner", "flex=1"))
            {
                Assert.Equal(2, RenderContext.MarkDepth);
            }

            Assert.Equal(1, RenderContext.MarkDepth);
        }

        Assert.Equal(0, RenderContext.MarkDepth);
    }

    [Fact]
    public void CurrentMarksIsBottomToTopSnapshot()
    {
        RenderContext.PushMark(RenderMark.Widget("Root"));
        RenderContext.PushMark(RenderMark.Style("Text", "theme.fg"));

        var marks = RenderContext.CurrentMarks();
        RenderContext.PopMark();

        Assert.Equal(2, marks.Count);
        Assert.Equal("Root", marks[0].Label);
        Assert.Equal("Text", marks[1].Label);
    }

    [Fact]
    public void ClearRemovesAll()
    {
        RenderContext.PushMark(RenderMark.Widget("A"));
        RenderContext.PushMark(RenderMark.Widget("B"));
        RenderContext.PushMark(RenderMark.Widget("C"));

        RenderContext.ClearMarks();

        Assert.Equal(0, RenderContext.MarkDepth);
    }

    [Fact]
    public void DisplayFormattingMatchesUpstream()
    {
        Assert.Equal("[constraint] FlexRow: min=20, weight=1.0",
            RenderMark.Constraint("FlexRow", "min=20, weight=1.0").ToString());
        Assert.Equal("[widget] Button", RenderMark.Widget("Button").ToString());
    }

    [Fact]
    public void GuardPopsOnEarlyReturn()
    {
        static void ReturnEarly()
        {
            using var guard = MarkGuard.Widget("Container");
            Assert.Equal(1, RenderContext.MarkDepth);
            return;
        }

        ReturnEarly();
        Assert.Equal(0, RenderContext.MarkDepth);
    }

    [Fact]
    public void KindDisplayStringsMatchUpstream()
    {
        Assert.Equal("constraint", MarkKind.Constraint.ToDisplayString());
        Assert.Equal("style", MarkKind.Style.ToDisplayString());
        Assert.Equal("widget", MarkKind.Widget.ToDisplayString());
        Assert.Equal("custom", MarkKind.Custom.ToDisplayString());
    }

    [Fact]
    public void MarkEqualityUsesAllFields()
    {
        Assert.Equal(RenderMark.Widget("X"), RenderMark.Widget("X"));
        Assert.NotEqual(RenderMark.Widget("X"), RenderMark.Widget("Y"));
    }

    [Fact]
    public void CustomGuardExposesCustomMark()
    {
        using var guard = MarkGuard.Custom("perf", "budget_exceeded=true");

        var mark = Assert.Single(RenderContext.CurrentMarks());
        Assert.Equal(MarkKind.Custom, mark.Kind);
        Assert.Equal("budget_exceeded=true", mark.Detail);
    }

    [Fact]
    public void MarkStacksAreThreadLocal()
    {
        RenderContext.PushMark(RenderMark.Widget("main"));
        string? worker = null;
        Exception? workerError = null;
        var thread = new Thread(() =>
        {
            try
            {
                Assert.Equal(0, RenderContext.MarkDepth);
                using var guard = MarkGuard.Widget("worker");
                worker = RenderContext.CurrentMarks()[0].Label;
            }
            catch (Exception exception)
            {
                workerError = exception;
            }
        });
        thread.Start();
        thread.Join();

        Assert.Null(workerError);
        Assert.Equal("worker", worker);
        Assert.Equal("main", Assert.Single(RenderContext.CurrentMarks()).Label);
    }
}
