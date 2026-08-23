// Behavioral port of .external/frankentui/crates/ftui-core/src/animation/stagger.rs tests.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Core;

namespace FrankenTui.Tests.Headless;

public sealed class AnimationStaggerTests
{
    private static readonly TimeSpan Ms50 = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan Ms100 = TimeSpan.FromMilliseconds(100);

    [Fact]
    public void ZeroCountReturnsEmpty()
    {
        Assert.Empty(AnimationStagger.Offsets(0, Ms100, StaggerMode.Linear));
    }

    [Fact]
    public void SingleItemReturnsZero()
    {
        Assert.Equal([TimeSpan.Zero], AnimationStagger.Offsets(1, Ms100, StaggerMode.Linear));
    }

    [Fact]
    public void LinearOffsetsUseExactEqualSpacing()
    {
        Assert.Equal(
            [
                TimeSpan.Zero,
                Ms50,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(150),
            ],
            AnimationStagger.Offsets(4, Ms50, StaggerMode.Linear));
    }

    [Fact]
    public void LinearLastOffsetEqualsTotalSpan()
    {
        var offsets = AnimationStagger.Offsets(5, Ms100, StaggerMode.Linear);

        Assert.Equal(TimeSpan.Zero, offsets[0]);
        Assert.Equal(TimeSpan.FromMilliseconds(400), offsets[4]);
    }

    [Fact]
    public void EaseInStartsAtZeroAndGapsIncrease()
    {
        var offsets = AnimationStagger.Offsets(5, Ms100, StaggerMode.EaseIn);
        var gaps = Gaps(offsets);

        Assert.Equal(TimeSpan.Zero, offsets[0]);
        AssertNonDecreasing(gaps);
    }

    [Fact]
    public void EaseOutGapsDecrease()
    {
        var gaps = Gaps(AnimationStagger.Offsets(5, Ms100, StaggerMode.EaseOut));

        for (var index = 1; index < gaps.Length; index++)
        {
            Assert.True(gaps[index] <= gaps[index - 1], $"ease-out gaps should decrease: {string.Join(", ", gaps)}");
        }
    }

    [Fact]
    public void EaseInOutMiddleIsHalfOfTotalSpan()
    {
        var offsets = AnimationStagger.Offsets(5, Ms100, StaggerMode.EaseInOut);
        var expected = TimeSpan.FromMilliseconds(200);

        Assert.Equal(TimeSpan.Zero, offsets[0]);
        Assert.True((offsets[2] - expected).Duration() < TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void CustomEasingControlsNormalizedDistribution()
    {
        var offsets = AnimationStagger.Offsets(
            3,
            Ms100,
            StaggerMode.Custom(t => t > 0.0f ? 1.0f : 0.0f));

        Assert.Equal(
            [TimeSpan.Zero, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200)],
            offsets);
    }

    [Fact]
    public void ZeroDelayProducesOnlyZeroOffsets()
    {
        var offsets = AnimationStagger.Offsets(5, TimeSpan.Zero, StaggerMode.Linear);

        Assert.All(offsets, offset => Assert.Equal(TimeSpan.Zero, offset));
    }

    [Fact]
    public void JitterIsDeterministicForSameSeed()
    {
        var first = AnimationStagger.OffsetsWithJitter(5, Ms100, StaggerMode.Linear, Ms50, 42);
        var second = AnimationStagger.OffsetsWithJitter(5, Ms100, StaggerMode.Linear, Ms50, 42);

        Assert.Equal(first, second);
    }

    [Fact]
    public void JitterChangesWithDifferentSeed()
    {
        var first = AnimationStagger.OffsetsWithJitter(5, Ms100, StaggerMode.Linear, Ms50, 42);
        var second = AnimationStagger.OffsetsWithJitter(5, Ms100, StaggerMode.Linear, Ms50, 99);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void JitterNeverProducesNegativeOffsets()
    {
        var offsets = AnimationStagger.OffsetsWithJitter(
            10,
            Ms50,
            StaggerMode.Linear,
            TimeSpan.FromMilliseconds(200),
            12345);

        Assert.All(offsets, offset => Assert.True(offset >= TimeSpan.Zero));
    }

    [Fact]
    public void JitteredOffsetsStayWithinRequestedBounds()
    {
        const int count = 50;
        var jitter = Ms50;
        var baseline = AnimationStagger.Offsets(count, Ms100, StaggerMode.Linear);
        var jittered = AnimationStagger.OffsetsWithJitter(count, Ms100, StaggerMode.Linear, jitter, 123);

        for (var index = 0; index < count; index++)
        {
            var lower = baseline[index] > jitter ? baseline[index] - jitter : TimeSpan.Zero;
            var upper = baseline[index] + jitter;
            Assert.InRange(jittered[index], lower, upper);
        }
    }

    [Fact]
    public void ZeroJitterIsNoOp()
    {
        var baseline = AnimationStagger.Offsets(5, Ms100, StaggerMode.Linear);
        var jittered = AnimationStagger.OffsetsWithJitter(
            5,
            Ms100,
            StaggerMode.Linear,
            TimeSpan.Zero,
            42);

        Assert.Equal(baseline, jittered);
    }

    [Theory]
    [MemberData(nameof(MonotonicModes))]
    public void BuiltInModesRemainMonotonic(StaggerMode mode)
    {
        var offsets = AnimationStagger.Offsets(10, Ms50, mode);

        AssertNonDecreasing(offsets);
    }

    [Fact]
    public void LinearArithmeticSaturatesAtTimeSpanMaximum()
    {
        var offsets = AnimationStagger.Offsets(3, TimeSpan.MaxValue, StaggerMode.Linear);

        Assert.Equal(TimeSpan.Zero, offsets[0]);
        Assert.Equal(TimeSpan.MaxValue, offsets[1]);
        Assert.Equal(TimeSpan.MaxValue, offsets[2]);
    }

    [Fact]
    public void NegativeManagedInputsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AnimationStagger.Offsets(-1, Ms100, StaggerMode.Linear));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AnimationStagger.Offsets(1, TimeSpan.FromTicks(-1), StaggerMode.Linear));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AnimationStagger.OffsetsWithJitter(1, Ms100, StaggerMode.Linear, TimeSpan.FromTicks(-1), 0));
    }

    public static TheoryData<StaggerMode> MonotonicModes => new()
    {
        StaggerMode.Linear,
        StaggerMode.EaseIn,
        StaggerMode.EaseOut,
    };

    private static TimeSpan[] Gaps(IReadOnlyList<TimeSpan> offsets)
    {
        var gaps = new TimeSpan[offsets.Count - 1];
        for (var index = 0; index < gaps.Length; index++)
        {
            gaps[index] = offsets[index + 1] - offsets[index];
        }

        return gaps;
    }

    private static void AssertNonDecreasing(IReadOnlyList<TimeSpan> values)
    {
        for (var index = 1; index < values.Count; index++)
        {
            Assert.True(values[index] >= values[index - 1], $"values should be non-decreasing: {string.Join(", ", values)}");
        }
    }
}
