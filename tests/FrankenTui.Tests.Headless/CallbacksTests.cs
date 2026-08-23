// SPDX-License-Identifier: Apache-2.0
// Tests ported from ftui-core/src/animation/callbacks.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Core;

namespace FrankenTui.Tests.Headless;

public sealed class CallbacksTests
{
    private static readonly TimeSpan Ms100 = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan Ms250 = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan Ms500 = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan Sec1 = TimeSpan.FromSeconds(1);

    [Fact]
    public void NoEventsConfigured()
    {
        var animation = new Callbacks<Fade>(new Fade(Sec1));
        animation.Tick(Ms500);
        Assert.Empty(animation.DrainEvents());
    }

    [Fact]
    public void StartedFiresOnFirstTickOnce()
    {
        var animation = new Callbacks<Fade>(new Fade(Sec1)).OnStart();
        animation.Tick(Ms100);
        Assert.Equal([new AnimationEvent.Started()], animation.DrainEvents());
        animation.Tick(Ms100);
        Assert.Empty(animation.DrainEvents());
    }

    [Fact]
    public void CompletedFiresWhenDoneOnce()
    {
        var animation = new Callbacks<Fade>(new Fade(Ms500)).OnComplete();
        animation.Tick(Ms250);
        Assert.Empty(animation.DrainEvents());
        animation.Tick(Ms500);
        Assert.Equal([new AnimationEvent.Completed()], animation.DrainEvents());
        animation.Tick(Ms100);
        Assert.Empty(animation.DrainEvents());
    }

    [Fact]
    public void ProgressThresholdFiresOnce()
    {
        var animation = new Callbacks<Fade>(new Fade(Sec1)).AtProgress(0.5f);
        animation.Tick(Ms250);
        Assert.Empty(animation.DrainEvents());
        animation.Tick(Ms500);
        Assert.Equal([new AnimationEvent.Progress(0.5f)], animation.DrainEvents());
        animation.Tick(Ms250);
        Assert.Empty(animation.DrainEvents());
    }

    [Fact]
    public void MultipleThresholdsAreSortedAndFireWhenCrossed()
    {
        var animation = new Callbacks<Fade>(new Fade(Sec1))
            .AtProgress(0.75f)
            .AtProgress(0.25f);
        animation.Tick(Ms500);
        Assert.Equal([new AnimationEvent.Progress(0.25f)], animation.DrainEvents());
        animation.Tick(Ms500);
        Assert.Equal([new AnimationEvent.Progress(0.75f)], animation.DrainEvents());
    }

    [Fact]
    public void AllEventsAreInStartedProgressCompletedOrder()
    {
        var animation = new Callbacks<Fade>(new Fade(Ms500))
            .OnStart()
            .AtProgress(0.5f)
            .OnComplete();
        animation.Tick(Ms500);
        Assert.Equal(
            [new AnimationEvent.Started(), new AnimationEvent.Progress(0.5f), new AnimationEvent.Completed()],
            animation.DrainEvents());
    }

    [Fact]
    public void ResetAllowsEventsToFireAgain()
    {
        var animation = new Callbacks<Fade>(new Fade(Ms500)).OnStart().OnComplete();
        animation.Tick(Sec1);
        animation.DrainEvents();
        animation.Reset();
        animation.Tick(Sec1);
        Assert.Equal(
            [new AnimationEvent.Started(), new AnimationEvent.Completed()],
            animation.DrainEvents());
    }

    [Fact]
    public void DrainClearsQueue()
    {
        var animation = new Callbacks<Fade>(new Fade(Sec1)).OnStart();
        animation.Tick(Ms100);
        Assert.Equal(1, animation.PendingEventCount);
        animation.DrainEvents();
        Assert.Equal(0, animation.PendingEventCount);
    }

    [Fact]
    public void InnerAccessExposesWrappedAnimation()
    {
        var animation = new Callbacks<Fade>(new Fade(Sec1));
        Assert.False(animation.Inner.IsComplete);
    }

    [Fact]
    public void InnerReferenceCanBeMutated()
    {
        var animation = new Callbacks<Fade>(new Fade(Sec1));
        animation.Inner.Tick(Sec1);
        Assert.True(animation.Inner.IsComplete);
    }

    [Fact]
    public void ValueDelegatesToInner()
    {
        var animation = new Callbacks<Fade>(new Fade(Sec1));
        animation.Tick(Ms500);
        Assert.InRange(animation.Value, 0.48f, 0.52f);
    }

    [Fact]
    public void IsCompleteDelegatesToInner()
    {
        var animation = new Callbacks<Fade>(new Fade(Ms100));
        Assert.False(animation.IsComplete);
        animation.Tick(Ms100);
        Assert.True(animation.IsComplete);
    }

    [Fact]
    public void ThresholdsClampAndNonFiniteThresholdsAreIgnored()
    {
        var animation = new Callbacks<Fade>(new Fade(Sec1))
            .AtProgress(-0.5f)
            .AtProgress(1.5f)
            .AtProgress(float.NaN)
            .AtProgress(float.PositiveInfinity);
        animation.Tick(TimeSpan.FromTicks(1));
        Assert.Contains(new AnimationEvent.Progress(0.0f), animation.DrainEvents());
    }

    [Fact]
    public void DebugFormatIncludesWrapperAndQueue()
    {
        var text = new Callbacks<Fade>(new Fade(Ms100)).OnStart().ToString();
        Assert.Contains("Callbacks", text, StringComparison.Ordinal);
        Assert.Contains("pending_events", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OvershootDelegatesToInner()
    {
        var animation = new Callbacks<Fade>(new Fade(Ms100));
        animation.Tick(Ms500);
        Assert.True(animation.Overshoot > TimeSpan.Zero);
    }
}
