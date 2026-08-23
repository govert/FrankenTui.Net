// SPDX-License-Identifier: Apache-2.0
// Tests ported from ftui-core/src/animation/presets.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Core;

namespace FrankenTui.Tests.Headless;

public sealed class PresetsTests
{
    private static readonly TimeSpan Ms50 = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan Ms100 = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan Ms200 = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan Ms500 = TimeSpan.FromMilliseconds(500);

    [Fact]
    public void CascadeInEmpty()
    {
        var group = AnimationPresets.CascadeIn(0, Ms200, Ms50, StaggerMode.Linear);
        Assert.True(group.IsEmpty);
        Assert.True(group.AllComplete);
    }

    [Fact]
    public void CascadeInSingleItem()
    {
        var group = AnimationPresets.CascadeIn(1, Ms200, Ms50, StaggerMode.Linear);
        Assert.Equal(1, group.Count);
        Assert.False(group.AllComplete);
        group.Tick(Ms200);
        Assert.True(group.AllComplete);
    }

    [Fact]
    public void CascadeInMultipleItemsAreStaggered()
    {
        var group = AnimationPresets.CascadeIn(3, Ms200, Ms100, StaggerMode.Linear);
        group.Tick(Ms100);
        Assert.True(group.Get("item_0")!.Value > 0.0f);
        Assert.Equal(0.0f, group.Get("item_1")!.Value);
        Assert.Equal(0.0f, group.Get("item_2")!.Value);
        group.Tick(TimeSpan.FromMilliseconds(300));
        Assert.True(group.AllComplete);
    }

    [Fact]
    public void CascadeInValuesIncreaseMonotonically()
    {
        var group = AnimationPresets.CascadeIn(5, Ms500, Ms100, StaggerMode.EaseOut);
        var previous = 0.0f;
        for (var index = 0; index < 20; index++)
        {
            group.Tick(Ms50);
            Assert.True(group.OverallProgress >= previous);
            previous = group.OverallProgress;
        }
    }

    [Fact]
    public void CascadeOutStartsNearOne()
    {
        var group = AnimationPresets.CascadeOut(3, Ms200, Ms50, StaggerMode.Linear);
        group.Tick(TimeSpan.FromTicks(1));
        Assert.InRange(group.Get("item_0")!.Value, 0.99f, 1.0f);
    }

    [Fact]
    public void CascadeOutEndsAtZero()
    {
        var group = AnimationPresets.CascadeOut(3, Ms200, Ms50, StaggerMode.Linear);
        group.Tick(TimeSpan.FromSeconds(1));
        for (var index = 0; index < 3; index++)
        {
            Assert.InRange(group.Get($"item_{index}")!.Value, 0.0f, 0.01f);
        }
    }

    [Fact]
    public void FanOutEmpty() => Assert.True(AnimationPresets.FanOut(0, Ms200, Ms200).IsEmpty);

    [Fact]
    public void FanOutSingleHasNoDelay()
    {
        var group = AnimationPresets.FanOut(1, Ms200, Ms200);
        Assert.Equal(1, group.Count);
        Assert.Equal(0.0f, group.Get("item_0")!.Value);
    }

    [Fact]
    public void FanOutCenterStartsFirst()
    {
        var group = AnimationPresets.FanOut(5, Ms200, Ms200);
        group.Tick(TimeSpan.FromMilliseconds(10));
        Assert.True(group.Get("item_2")!.Value >= group.Get("item_0")!.Value);
    }

    [Fact]
    public void FanOutIsSymmetric()
    {
        var group = AnimationPresets.FanOut(5, Ms200, Ms200);
        Assert.Equal(group.Get("item_0")!.Value, group.Get("item_4")!.Value);
        Assert.Equal(group.Get("item_1")!.Value, group.Get("item_3")!.Value);
    }

    [Fact]
    public void TypewriterStartsAtZero() =>
        Assert.Equal(0, AnimationPresets.Typewriter(100, Ms500).VisibleChars);

    [Fact]
    public void TypewriterEndsAtFull()
    {
        var typewriter = AnimationPresets.Typewriter(100, Ms500);
        typewriter.Tick(Ms500);
        Assert.Equal(100, typewriter.VisibleChars);
        Assert.True(typewriter.IsComplete);
    }

    [Fact]
    public void TypewriterProgressesMonotonically()
    {
        var typewriter = AnimationPresets.Typewriter(50, Ms500);
        var previous = 0;
        for (var index = 0; index < 20; index++)
        {
            typewriter.Tick(TimeSpan.FromMilliseconds(25));
            Assert.True(typewriter.VisibleChars >= previous);
            previous = typewriter.VisibleChars;
        }
    }

    [Fact]
    public void TypewriterZeroCharsStillCompletes()
    {
        var typewriter = AnimationPresets.Typewriter(0, Ms200);
        Assert.Equal(0, typewriter.VisibleChars);
        typewriter.Tick(Ms200);
        Assert.Equal(0, typewriter.VisibleChars);
        Assert.True(typewriter.IsComplete);
    }

    [Fact]
    public void PulseSequenceEmpty() =>
        Assert.True(AnimationPresets.PulseSequence(0, Ms200, Ms100).IsEmpty);

    [Fact]
    public void PulseSequencePeaksThenReturns()
    {
        var group = AnimationPresets.PulseSequence(1, Ms200, Ms100);
        Assert.InRange(group.Get("pulse_0")!.Value, 0.0f, 0.01f);
        group.Tick(Ms100);
        Assert.True(group.Get("pulse_0")!.Value > 0.9f);
        group.Tick(Ms100);
        Assert.InRange(group.Get("pulse_0")!.Value, -0.001f, 0.1f);
    }

    [Fact]
    public void PulseSequenceItemsAreStaggered()
    {
        var group = AnimationPresets.PulseSequence(3, Ms200, Ms200);
        group.Tick(Ms100);
        Assert.True(group.Get("pulse_0")!.Value > 0.9f);
        Assert.InRange(group.Get("pulse_1")!.Value, 0.0f, 0.01f);
    }

    [Fact]
    public void SlideInLeftStartsOffscreen() =>
        Assert.Equal((short)-20, AnimationPresets.SlideInLeft(20, Ms200).Position);

    [Fact]
    public void SlideInLeftEndsAtZero()
    {
        var slide = AnimationPresets.SlideInLeft(20, Ms200);
        slide.Tick(Ms200);
        Assert.Equal((short)0, slide.Position);
        Assert.True(slide.IsComplete);
    }

    [Fact]
    public void SlideInRightStartsOffscreen() =>
        Assert.Equal((short)20, AnimationPresets.SlideInRight(20, Ms200).Position);

    [Fact]
    public void SlideInRightEndsAtZero()
    {
        var slide = AnimationPresets.SlideInRight(20, Ms200);
        slide.Tick(Ms200);
        Assert.Equal((short)0, slide.Position);
    }

    [Fact]
    public void FadeThroughStartsAtOne() =>
        Assert.InRange(AnimationPresets.FadeThrough(Ms200).Value, 0.99f, 1.0f);

    [Fact]
    public void FadeThroughMidpointIsNearZero()
    {
        var animation = AnimationPresets.FadeThrough(Ms200);
        animation.Tick(Ms200);
        Assert.InRange(animation.Value, 0.0f, 0.1f);
    }

    [Fact]
    public void FadeThroughEndsAtOne()
    {
        var animation = AnimationPresets.FadeThrough(Ms200);
        animation.Tick(TimeSpan.FromMilliseconds(400));
        Assert.True(animation.IsComplete);
        Assert.InRange(animation.Value, 0.99f, 1.0f);
    }

    [Fact]
    public void InvertedFadeStartsAtOne() => Assert.Equal(1.0f, new InvertedFade(Ms200).Value);

    [Fact]
    public void InvertedFadeEndsAtZero()
    {
        var fade = new InvertedFade(Ms200);
        fade.Tick(Ms200);
        Assert.InRange(fade.Value, 0.0f, 0.001f);
        Assert.True(fade.IsComplete);
    }

    [Fact]
    public void InvertedFadeReset()
    {
        var fade = new InvertedFade(Ms200);
        fade.Tick(Ms200);
        fade.Reset();
        Assert.False(fade.IsComplete);
        Assert.Equal(1.0f, fade.Value);
    }

    [Fact]
    public void CascadeInIsDeterministic() => Assert.Equal(RunCascade(), RunCascade());

    [Fact]
    public void TypewriterIsDeterministic() => Assert.Equal(RunTypewriter(), RunTypewriter());

    [Fact]
    public void FanOutIsDeterministic() => Assert.Equal(RunFanOut(), RunFanOut());

    private static float[] RunCascade()
    {
        var group = AnimationPresets.CascadeIn(5, Ms200, Ms50, StaggerMode.EaseInOut);
        var values = new float[10];
        for (var index = 0; index < values.Length; index++)
        {
            group.Tick(Ms50);
            values[index] = group.OverallProgress;
        }

        return values;
    }

    private static int[] RunTypewriter()
    {
        var typewriter = AnimationPresets.Typewriter(100, Ms500);
        var values = new int[20];
        for (var index = 0; index < values.Length; index++)
        {
            typewriter.Tick(TimeSpan.FromMilliseconds(25));
            values[index] = typewriter.VisibleChars;
        }

        return values;
    }

    private static float[] RunFanOut()
    {
        var group = AnimationPresets.FanOut(7, Ms200, Ms200);
        var values = new float[10];
        for (var index = 0; index < values.Length; index++)
        {
            group.Tick(Ms50);
            values[index] = group.OverallProgress;
        }

        return values;
    }
}
