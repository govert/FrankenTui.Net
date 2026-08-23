// Behavioral port of .external/frankentui/crates/ftui-core/src/animation/group.rs tests.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Core;

namespace FrankenTui.Tests.Headless;

public sealed class AnimationGroupTests
{
    private static readonly TimeSpan Ms100 = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan Ms200 = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan Ms300 = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan Ms500 = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

    [Fact]
    public void EmptyGroupIsCompleteWithZeroProgress()
    {
        var group = new AnimationGroup();

        Assert.True(group.IsEmpty);
        Assert.Equal(0, group.Count);
        Assert.True(group.AllComplete);
        Assert.True(group.IsComplete);
        Assert.Equal(0.0f, group.OverallProgress);
        Assert.Equal(0.0f, group.Value);
    }

    [Fact]
    public void AddAndTickAdvanceEveryIncompleteMember()
    {
        var group = new AnimationGroup()
            .Add("a", new LinearAnimation(Ms500))
            .Add("b", new LinearAnimation(Second));

        group.Tick(Ms500);

        Assert.Equal(2, group.Count);
        Assert.True(group.Get("a")!.IsComplete);
        Assert.False(group.Get("b")!.IsComplete);
        Assert.InRange(group.Get("b")!.Value, 0.49f, 0.51f);
    }

    [Fact]
    public void OverallProgressIsArithmeticMeanOfMemberValues()
    {
        var group = new AnimationGroup()
            .Add("short", new LinearAnimation(Ms200))
            .Add("long", new LinearAnimation(Second));

        group.Tick(Ms200);

        Assert.InRange(group.OverallProgress, 0.59f, 0.61f);
        Assert.Equal(group.OverallProgress, group.Value);
    }

    [Fact]
    public void AllCompleteOnlyWhenEveryMemberIsDone()
    {
        var group = new AnimationGroup()
            .Add("a", new LinearAnimation(Ms100))
            .Add("b", new LinearAnimation(Ms200));

        group.Tick(Ms100);
        Assert.False(group.AllComplete);

        group.Tick(Ms100);
        Assert.True(group.AllComplete);
        Assert.True(group.IsComplete);
    }

    [Fact]
    public void StartAllResetsEveryMember()
    {
        var group = new AnimationGroup().Add("a", new LinearAnimation(Ms100));
        group.Tick(Ms100);

        group.StartAll();

        Assert.False(group.AllComplete);
        Assert.Equal(0.0f, group.Get("a")!.Value);
    }

    [Fact]
    public void CancelAllHasUpstreamResetSemantics()
    {
        var group = new AnimationGroup().Add("a", new LinearAnimation(Ms100));
        group.Tick(Ms100);

        group.CancelAll();

        Assert.False(group.AllComplete);
        Assert.Equal(0.0f, group.Value);
    }

    [Fact]
    public void DuplicateLabelReplacesInPlace()
    {
        var group = new AnimationGroup()
            .Add("x", new LinearAnimation(Ms100))
            .Add("x", new LinearAnimation(Second));

        group.Tick(Ms100);

        Assert.Equal(1, group.Count);
        Assert.False(group.AllComplete);
        Assert.Equal(["x"], group.Labels());
    }

    [Fact]
    public void MutatingInsertAlsoReplacesInPlace()
    {
        var group = new AnimationGroup();
        group.Insert("x", new LinearAnimation(Ms100));
        group.Insert("x", new LinearAnimation(Second));

        group.Tick(Ms100);

        Assert.Equal(1, group.Count);
        Assert.False(group.AllComplete);
    }

    [Fact]
    public void RemoveReportsWhetherLabelWasPresent()
    {
        var group = new AnimationGroup()
            .Add("a", new LinearAnimation(Ms100))
            .Add("b", new LinearAnimation(Ms200));

        Assert.True(group.Remove("a"));
        Assert.Equal(1, group.Count);
        Assert.Null(group.Get("a"));
        Assert.NotNull(group.Get("b"));
        Assert.False(group.Remove("missing"));
    }

    [Fact]
    public void RemoveFromEmptyGroupReturnsFalse()
    {
        var group = new AnimationGroup();

        Assert.False(group.Remove("anything"));
        Assert.Equal(0, group.Count);
    }

    [Fact]
    public void GetAtUsesCurrentInsertionOrder()
    {
        var group = new AnimationGroup()
            .Add("a", new LinearAnimation(Ms100))
            .Add("b", new LinearAnimation(Ms200));

        Assert.NotNull(group.GetAt(0));
        Assert.NotNull(group.GetAt(1));
        Assert.Null(group.GetAt(2));
        Assert.Null(group.GetAt(-1));

        group.Remove("a");
        Assert.Same(group.Get("b"), group.GetAt(0));
        Assert.Null(group.GetAt(1));
    }

    [Fact]
    public void NamedMemberCanBeAdvancedIndividually()
    {
        var group = new AnimationGroup()
            .Add("a", new LinearAnimation(Second))
            .Add("b", new LinearAnimation(Second));

        group.Get("a")!.Tick(Ms500);

        Assert.InRange(group.Get("a")!.Value, 0.49f, 0.51f);
        Assert.Equal(0.0f, group.Get("b")!.Value);
    }

    [Fact]
    public void LabelsAndPairsPreserveInsertionOrder()
    {
        var group = new AnimationGroup()
            .Add("alpha", new LinearAnimation(Ms100))
            .Add("beta", new LinearAnimation(Ms100));

        Assert.Equal(["alpha", "beta"], group.Labels());
        var pairs = group.Iter().ToArray();
        Assert.Equal(2, pairs.Length);
        Assert.Equal("alpha", pairs[0].Label);
        Assert.Equal("beta", pairs[1].Label);
    }

    [Fact]
    public void EmptyEnumerationsContainNoItems()
    {
        var group = new AnimationGroup();

        Assert.Empty(group.Iter());
        Assert.Empty(group.Labels());
    }

    [Fact]
    public void ResetThroughAnimationContractResetsMembers()
    {
        IAnimation group = new AnimationGroup().Add("a", new LinearAnimation(Ms100));
        group.Tick(Ms100);
        Assert.True(group.IsComplete);

        group.Reset();

        Assert.False(group.IsComplete);
    }

    [Fact]
    public void CompletedMembersAreSkippedOnLaterTicks()
    {
        var shortAnimation = new LinearAnimation(Ms100);
        var longAnimation = new LinearAnimation(Second);
        var group = new AnimationGroup()
            .Add("short", shortAnimation)
            .Add("long", longAnimation);

        group.Tick(Ms200);
        group.Tick(Ms100);

        Assert.Equal(1, shortAnimation.TickCount);
        Assert.Equal(2, longAnimation.TickCount);
        Assert.Equal(1.0f, shortAnimation.Value);
    }

    [Fact]
    public void EmptyLifecycleOperationsAreNoOps()
    {
        var group = new AnimationGroup();

        group.Tick(Ms500);
        group.Reset();
        group.StartAll();
        group.CancelAll();

        Assert.True(group.IsComplete);
        Assert.True(group.IsEmpty);
    }

    [Fact]
    public void SingleAndThreeMemberProgressMatchUpstream()
    {
        var single = new AnimationGroup().Add("only", new LinearAnimation(Ms200));
        single.Tick(Ms100);
        Assert.InRange(single.OverallProgress, 0.49f, 0.51f);

        var three = new AnimationGroup()
            .Add("a", new LinearAnimation(Ms100))
            .Add("b", new LinearAnimation(Ms200))
            .Add("c", new LinearAnimation(Ms300));
        three.Tick(Ms300);
        Assert.True(three.AllComplete);
        Assert.Equal(1.0f, three.OverallProgress);
    }

    [Fact]
    public void MixedCompletionProgressAveragesFinishedAndActiveMembers()
    {
        var group = new AnimationGroup()
            .Add("done", new LinearAnimation(Ms100))
            .Add("partial", new LinearAnimation(Ms500));

        group.Tick(Ms200);

        Assert.InRange(group.OverallProgress, 0.69f, 0.71f);
        Assert.False(group.AllComplete);
    }

    [Fact]
    public void RemoveThenAddSameLabelWorks()
    {
        var group = new AnimationGroup().Add("x", new LinearAnimation(Ms100));
        Assert.True(group.Remove("x"));

        group.Insert("x", new LinearAnimation(Ms200));

        Assert.Equal(1, group.Count);
        Assert.NotNull(group.Get("x"));
    }

    [Fact]
    public void UnknownLabelsReturnNull()
    {
        var group = new AnimationGroup().Add("a", new LinearAnimation(Ms100));

        Assert.Null(group.Get("missing"));
    }

    [Fact]
    public void DebugTextIncludesProgressAndCompletionState()
    {
        var text = new AnimationGroup().Add("a", new LinearAnimation(Ms100)).ToString();

        Assert.Contains("AnimationGroup", text, StringComparison.Ordinal);
        Assert.Contains("count", text, StringComparison.Ordinal);
        Assert.Contains("progress", text, StringComparison.Ordinal);
        Assert.Contains("complete", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NegativeTickIsRejectedAtManagedDurationBoundary()
    {
        var group = new AnimationGroup();

        Assert.Throws<ArgumentOutOfRangeException>(() => group.Tick(TimeSpan.FromTicks(-1)));
    }

    [Fact]
    public void AnimationContractDefaultsOvershootToZero()
    {
        IAnimation animation = new AnimationGroup();

        Assert.Equal(TimeSpan.Zero, animation.Overshoot);
    }

    private sealed class LinearAnimation(TimeSpan duration) : IAnimation
    {
        private readonly TimeSpan _duration = duration;
        private TimeSpan _elapsed;

        public int TickCount { get; private set; }

        public bool IsComplete => _elapsed >= _duration;

        public float Value => _duration == TimeSpan.Zero
            ? 1.0f
            : Math.Clamp((float)(_elapsed.TotalSeconds / _duration.TotalSeconds), 0.0f, 1.0f);

        public TimeSpan Overshoot => _elapsed > _duration ? _elapsed - _duration : TimeSpan.Zero;

        public void Tick(TimeSpan delta)
        {
            TickCount++;
            _elapsed = TimeSpan.MaxValue - _elapsed < delta
                ? TimeSpan.MaxValue
                : _elapsed + delta;
        }

        public void Reset()
        {
            _elapsed = TimeSpan.Zero;
            TickCount = 0;
        }
    }
}
