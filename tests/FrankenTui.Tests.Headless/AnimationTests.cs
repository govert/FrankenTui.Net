// SPDX-License-Identifier: Apache-2.0
// Tests ported from ftui-core/src/animation/mod.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Core;

namespace FrankenTui.Tests.Headless;

public sealed class AnimationTests
{
    private static readonly TimeSpan Ms16 = TimeSpan.FromMilliseconds(16);
    private static readonly TimeSpan Ms100 = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan Ms500 = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan Sec1 = TimeSpan.FromSeconds(1);

    [Fact]
    public void EasingLinearEndpoints()
    {
        Assert.Equal(0.0f, AnimationEasing.Linear(0.0f));
        Assert.Equal(1.0f, AnimationEasing.Linear(1.0f));
    }

    [Fact]
    public void EasingLinearMidpoint() => Assert.Equal(0.5f, AnimationEasing.Linear(0.5f));

    [Fact]
    public void EasingClampsInput()
    {
        Assert.Equal(0.0f, AnimationEasing.Linear(-1.0f));
        Assert.Equal(1.0f, AnimationEasing.Linear(2.0f));
        Assert.Equal(0.0f, AnimationEasing.EaseIn(-0.5f));
        Assert.Equal(1.0f, AnimationEasing.EaseOut(1.5f));
    }

    [Fact]
    public void EaseInSlowerStart() =>
        Assert.True(AnimationEasing.EaseIn(0.5f) < AnimationEasing.Linear(0.5f));

    [Fact]
    public void EaseOutFasterStart() =>
        Assert.True(AnimationEasing.EaseOut(0.5f) > AnimationEasing.Linear(0.5f));

    [Fact]
    public void EaseInOutEndpoints()
    {
        Assert.Equal(0.0f, AnimationEasing.EaseInOut(0.0f));
        Assert.Equal(1.0f, AnimationEasing.EaseInOut(1.0f));
    }

    [Fact]
    public void EaseInOutMidpoint() => AssertClose(0.5f, AnimationEasing.EaseInOut(0.5f));

    [Fact]
    public void EaseInCubicEndpoints()
    {
        Assert.Equal(0.0f, AnimationEasing.EaseInCubic(0.0f));
        Assert.Equal(1.0f, AnimationEasing.EaseInCubic(1.0f));
    }

    [Fact]
    public void EaseOutCubicEndpoints()
    {
        Assert.Equal(0.0f, AnimationEasing.EaseOutCubic(0.0f));
        Assert.Equal(1.0f, AnimationEasing.EaseOutCubic(1.0f));
    }

    [Fact]
    public void EaseInCubicSlowerThanQuadratic() =>
        Assert.True(AnimationEasing.EaseInCubic(0.5f) < AnimationEasing.EaseIn(0.5f));

    [Fact]
    public void FadeStartsAtZero()
    {
        var fade = new Fade(Sec1);
        Assert.Equal(0.0f, fade.Value);
        Assert.False(fade.IsComplete);
    }

    [Fact]
    public void FadeCompletesAfterDuration()
    {
        var fade = new Fade(Sec1);
        fade.Tick(Sec1);
        Assert.True(fade.IsComplete);
        Assert.Equal(1.0f, fade.Value);
    }

    [Fact]
    public void FadeMidpoint()
    {
        var fade = new Fade(Sec1);
        fade.Tick(Ms500);
        AssertClose(0.5f, fade.Value);
    }

    [Fact]
    public void FadeIncrementalTicks()
    {
        var fade = new Fade(TimeSpan.FromMilliseconds(160));
        for (var index = 0; index < 10; index++)
        {
            fade.Tick(Ms16);
        }

        Assert.True(fade.IsComplete);
        Assert.Equal(1.0f, fade.Value);
    }

    [Fact]
    public void FadeWithEaseIn()
    {
        var fade = new Fade(Sec1).Easing(AnimationEasing.EaseIn);
        fade.Tick(Ms500);
        AssertClose(0.25f, fade.Value);
    }

    [Fact]
    public void FadeClampsOvershoot()
    {
        var fade = new Fade(Ms100);
        fade.Tick(Sec1);
        Assert.True(fade.IsComplete);
        Assert.Equal(1.0f, fade.Value);
        Assert.Equal(TimeSpan.FromMilliseconds(900), fade.Overshoot);
    }

    [Fact]
    public void FadeReset()
    {
        var fade = new Fade(Sec1);
        fade.Tick(Sec1);
        fade.Reset();
        Assert.False(fade.IsComplete);
        Assert.Equal(0.0f, fade.Value);
    }

    [Fact]
    public void FadeZeroDurationUsesManagedMinimumPositiveDuration()
    {
        var fade = new Fade(TimeSpan.Zero);
        fade.Tick(Ms16);
        Assert.True(fade.IsComplete);
    }

    [Fact]
    public void FadeRawProgressIsUneased()
    {
        var fade = new Fade(Sec1).Easing(AnimationEasing.EaseIn);
        fade.Tick(Ms500);
        AssertClose(0.5f, fade.RawProgress);
        AssertClose(0.25f, fade.Value);
    }

    [Fact]
    public void SlideStartsAtFrom() => Assert.Equal((short)0, new Slide(0, 100, Sec1).Position);

    [Fact]
    public void SlideEndsAtTo()
    {
        var slide = new Slide(0, 100, Sec1);
        slide.Tick(Sec1);
        Assert.Equal((short)100, slide.Position);
    }

    [Fact]
    public void SlideNegativeRange()
    {
        var slide = new Slide(100, -50, Sec1).Easing(AnimationEasing.Linear);
        slide.Tick(Sec1);
        Assert.Equal((short)-50, slide.Position);
    }

    [Fact]
    public void SlideMidpointWithLinear()
    {
        var slide = new Slide(0, 100, Sec1).Easing(AnimationEasing.Linear);
        slide.Tick(Ms500);
        Assert.Equal((short)50, slide.Position);
    }

    [Fact]
    public void SlideReset()
    {
        var slide = new Slide(10, 90, Sec1);
        slide.Tick(Sec1);
        slide.Reset();
        Assert.Equal((short)10, slide.Position);
    }

    [Fact]
    public void PulseStartsAtMidpoint() => Assert.Equal(0.5f, new Pulse(1.0f).Value);

    [Fact]
    public void PulseNeverCompletes()
    {
        var pulse = new Pulse(1.0f);
        for (var index = 0; index < 100; index++)
        {
            pulse.Tick(Ms100);
        }

        Assert.False(pulse.IsComplete);
    }

    [Fact]
    public void PulseValueBounded()
    {
        var pulse = new Pulse(2.0f);
        for (var index = 0; index < 200; index++)
        {
            pulse.Tick(Ms16);
            Assert.InRange(pulse.Value, 0.0f, 1.0f);
        }
    }

    [Fact]
    public void PulseQuarterCycleReachesPeak()
    {
        var pulse = new Pulse(1.0f);
        pulse.Tick(TimeSpan.FromMilliseconds(250));
        AssertClose(1.0f, pulse.Value, 0.02f);
    }

    [Fact]
    public void PulsePhaseWraps()
    {
        var pulse = new Pulse(1.0f);
        pulse.Tick(TimeSpan.FromSeconds(10));
        Assert.InRange(pulse.Phase, 0.0f, MathF.Tau);
    }

    [Fact]
    public void PulseReset()
    {
        var pulse = new Pulse(1.0f);
        pulse.Tick(Sec1);
        pulse.Reset();
        Assert.Equal(0.0f, pulse.Phase);
        Assert.Equal(0.5f, pulse.Value);
    }

    [Fact]
    public void PulseZeroFrequencyIsClamped()
    {
        var pulse = new Pulse(0.0f);
        pulse.Tick(Sec1);
        Assert.InRange(pulse.Value, 0.0f, 1.0f);
    }

    [Fact]
    public void SequencePlaysFirstThenSecond()
    {
        var sequence = Animations.Sequence(new Fade(Sec1), new Fade(Sec1));
        sequence.Tick(Ms500);
        AssertClose(0.5f, sequence.Value);
        sequence.Tick(Ms500);
        Assert.False(sequence.IsComplete);
        sequence.Tick(Ms500);
        AssertClose(0.5f, sequence.Value);
        sequence.Tick(Ms500);
        Assert.True(sequence.IsComplete);
        Assert.Equal(1.0f, sequence.Value);
    }

    [Fact]
    public void SequenceForwardsOvershootAndResets()
    {
        var sequence = Animations.Sequence(new Fade(Ms100), new Fade(Ms100));
        sequence.Tick(TimeSpan.FromMilliseconds(200));
        Assert.True(sequence.IsComplete);
        sequence.Reset();
        Assert.False(sequence.IsComplete);
        Assert.Equal(0.0f, sequence.Value);
    }

    [Fact]
    public void ParallelTicksBoth()
    {
        var parallel = Animations.Parallel(new Fade(Sec1), new Fade(Ms500));
        parallel.Tick(Ms500);
        AssertClose(0.75f, parallel.Value);
        Assert.False(parallel.IsComplete);
        parallel.Tick(Ms500);
        Assert.True(parallel.IsComplete);
    }

    [Fact]
    public void ParallelExposesComponents()
    {
        var parallel = Animations.Parallel(new Fade(Sec1), new Fade(Sec1));
        Assert.Equal(0.0f, parallel.First.Value);
        Assert.Equal(0.0f, parallel.Second.Value);
    }

    [Fact]
    public void ParallelReset()
    {
        var parallel = Animations.Parallel(new Fade(Ms100), new Fade(Ms100));
        parallel.Tick(Ms100);
        parallel.Reset();
        Assert.False(parallel.IsComplete);
    }

    [Fact]
    public void DelayedWaitsThenPlays()
    {
        var delayed = Animations.Delay(Ms500, new Fade(Ms500));
        delayed.Tick(TimeSpan.FromMilliseconds(250));
        Assert.False(delayed.HasStarted);
        Assert.Equal(0.0f, delayed.Value);
        delayed.Tick(TimeSpan.FromMilliseconds(250));
        Assert.True(delayed.HasStarted);
        delayed.Tick(Ms500);
        Assert.True(delayed.IsComplete);
    }

    [Fact]
    public void DelayedForwardsOvershoot()
    {
        var delayed = Animations.Delay(Ms100, new Fade(Sec1));
        delayed.Tick(TimeSpan.FromMilliseconds(200));
        Assert.True(delayed.HasStarted);
        AssertClose(0.1f, delayed.Value, 0.02f);
    }

    [Fact]
    public void DelayedReset()
    {
        var delayed = Animations.Delay(Ms100, new Fade(Ms100));
        delayed.Tick(TimeSpan.FromMilliseconds(200));
        delayed.Reset();
        Assert.False(delayed.HasStarted);
        Assert.False(delayed.IsComplete);
    }

    [Fact]
    public void NestedSequenceCompletesWithForwardedOvershoot()
    {
        var inner = Animations.Sequence(new Fade(Ms100), new Fade(Ms100));
        var outer = Animations.Sequence(inner, new Fade(Ms100));
        outer.Tick(TimeSpan.FromMilliseconds(300));
        Assert.True(outer.IsComplete);
    }

    [Fact]
    public void DelayedParallelCompletes()
    {
        var parallel = Animations.Parallel(
            Animations.Delay(Ms100, new Fade(Ms100)),
            new Fade(TimeSpan.FromMilliseconds(200)));
        parallel.Tick(TimeSpan.FromMilliseconds(200));
        Assert.True(parallel.IsComplete);
    }

    [Fact]
    public void ParallelOfSequencesCompletes()
    {
        var first = Animations.Sequence(new Fade(Ms100), new Fade(Ms100));
        var second = Animations.Sequence(new Fade(Ms100), new Fade(Ms100));
        var parallel = Animations.Parallel(first, second);
        parallel.Tick(TimeSpan.FromMilliseconds(200));
        Assert.True(parallel.IsComplete);
    }

    [Fact]
    public void ZeroDeltaIsNoOp()
    {
        var fade = new Fade(Sec1);
        fade.Tick(TimeSpan.Zero);
        Assert.Equal(0.0f, fade.Value);
    }

    [Fact]
    public void VerySmallDeltaBarelyMoves()
    {
        var fade = new Fade(Sec1);
        fade.Tick(TimeSpan.FromTicks(1));
        Assert.True(fade.Value < 0.001f);
    }

    [Fact]
    public void VeryLargeDeltaCompletes()
    {
        var fade = new Fade(Ms100);
        fade.Tick(TimeSpan.FromHours(1));
        Assert.True(fade.IsComplete);
        Assert.Equal(1.0f, fade.Value);
    }

    [Fact]
    public void RapidSmallTicksComplete()
    {
        var fade = new Fade(Sec1);
        for (var index = 0; index < 1_000; index++)
        {
            fade.Tick(TimeSpan.FromMilliseconds(1));
        }

        Assert.True(fade.IsComplete);
    }

    [Fact]
    public void TickAfterCompleteIsSafe()
    {
        var fade = new Fade(Ms100);
        fade.Tick(Sec1);
        fade.Tick(Sec1);
        Assert.True(fade.IsComplete);
        Assert.Equal(1.0f, fade.Value);
    }

    [Fact]
    public void NegativeDurationsAndDeltasAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Fade(TimeSpan.FromTicks(-1)));
        var fade = new Fade(Sec1);
        Assert.Throws<ArgumentOutOfRangeException>(() => fade.Tick(TimeSpan.FromTicks(-1)));
    }

    private static void AssertClose(float expected, float actual, float tolerance = 0.01f) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
}
