// SPDX-License-Identifier: Apache-2.0
// Tests ported from ftui-core/src/animation/spring.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// DIVERGENCE: the two upstream feature-gated tracing-span tests have no
// managed counterpart because Spring deliberately does not emit tracing spans.

using FrankenTui.Core;

namespace FrankenTui.Tests.Headless;

public sealed class SpringTests
{
    private static readonly TimeSpan Ms16 = TimeSpan.FromMilliseconds(16);

    [Fact]
    public void SpringReachesTarget()
    {
        var spring = new Spring(0.0, 100.0).WithStiffness(170.0).WithDamping(26.0);
        Simulate(spring, 200);
        Assert.InRange(spring.Position, 99.9, 100.1);
        Assert.True(spring.IsComplete);
    }

    [Fact]
    public void SpringStartsAtInitial() => Assert.Equal(50.0, new Spring(50.0, 100.0).Position);

    [Fact]
    public void SpringTargetCanChange()
    {
        var spring = new Spring(0.0, 100.0);
        spring.SetTarget(200.0);
        Assert.Equal(200.0, spring.Target);
    }

    [Fact]
    public void HighDampingHasMinimalOvershoot()
    {
        var spring = new Spring(0.0, 100.0).WithStiffness(170.0).WithDamping(100.0);
        var maximumOvershoot = 0.0;
        for (var index = 0; index < 300; index++)
        {
            spring.Tick(Ms16);
            maximumOvershoot = Math.Max(maximumOvershoot, spring.Position - 100.0);
        }

        Assert.True(maximumOvershoot < 1.0, $"Overshoot was {maximumOvershoot}.");
    }

    [Fact]
    public void CriticalDampingHasNegligibleOvershoot()
    {
        var spring = SpringPresets.Critical();
        spring.SetTarget(1.0);
        var maximum = 0.0;
        for (var index = 0; index < 300; index++)
        {
            spring.Tick(Ms16);
            maximum = Math.Max(maximum, spring.Position);
        }

        Assert.True(maximum < 1.05, $"Maximum was {maximum}.");
    }

    [Fact]
    public void BouncySpringOvershoots()
    {
        var spring = SpringPresets.Bouncy();
        var maximum = 0.0;
        for (var index = 0; index < 200; index++)
        {
            spring.Tick(Ms16);
            maximum = Math.Max(maximum, spring.Position);
        }

        Assert.True(maximum > 1.0);
    }

    [Fact]
    public void NormalizedSpringValueIsClamped()
    {
        var spring = SpringPresets.Bouncy();
        for (var index = 0; index < 200; index++)
        {
            spring.Tick(Ms16);
            Assert.InRange(spring.Value, 0.0f, 1.0f);
        }
    }

    [Fact]
    public void SpringResetRestoresInitialState()
    {
        var spring = new Spring(0.0, 1.0);
        Simulate(spring, 100);
        Assert.True(spring.IsComplete);
        spring.Reset();
        Assert.False(spring.IsComplete);
        Assert.Equal(0.0, spring.Position);
        Assert.Equal(0.0, spring.Velocity);
    }

    [Fact]
    public void SpringImpulseWakesSettledSpring()
    {
        var spring = new Spring(0.0, 0.0);
        Simulate(spring, 100);
        Assert.True(spring.IsComplete);
        spring.Impulse(50.0);
        Assert.False(spring.IsComplete);
        spring.Tick(Ms16);
        Assert.NotEqual(0.0, spring.Position);
    }

    [Fact]
    public void SetTargetWakesSettledSpring()
    {
        var spring = new Spring(0.0, 1.0);
        Simulate(spring, 200);
        spring.SetTarget(2.0);
        Assert.False(spring.IsComplete);
    }

    [Fact]
    public void SetTargetToSameValueStaysAtRest()
    {
        var spring = new Spring(0.0, 1.0);
        Simulate(spring, 200);
        spring.SetTarget(1.0);
        Assert.True(spring.IsComplete);
    }

    [Fact]
    public void ZeroDeltaIsNoOp()
    {
        var spring = new Spring(0.0, 1.0);
        var before = spring.Position;
        spring.Tick(TimeSpan.Zero);
        Assert.Equal(before, spring.Position);
    }

    [Fact]
    public void LargeDeltaIsSubdividedAndConverges()
    {
        var spring = new Spring(0.0, 1.0).WithStiffness(170.0).WithDamping(26.0);
        spring.Tick(TimeSpan.FromSeconds(5));
        Assert.InRange(spring.Position, 0.99, 1.01);
    }

    [Fact]
    public void ZeroStiffnessIsClamped() =>
        Assert.True(new Spring(0.0, 1.0).WithStiffness(0.0).Stiffness >= 0.1);

    [Fact]
    public void NegativeDampingIsClamped() =>
        Assert.True(new Spring(0.0, 1.0).WithDamping(-5.0).Damping >= 0.0);

    [Fact]
    public void CriticalDampingCoefficient()
    {
        var spring = new Spring(0.0, 1.0).WithStiffness(100.0);
        Assert.Equal(20.0, spring.CriticalDamping());
    }

    [Fact]
    public void SpringSupportsNegativeTarget()
    {
        var spring = new Spring(0.0, -1.0).WithStiffness(170.0).WithDamping(26.0);
        Simulate(spring, 200);
        Assert.InRange(spring.Position, -1.01, -0.99);
    }

    [Fact]
    public void SpringSupportsReverseDirection()
    {
        var spring = new Spring(1.0, 0.0).WithStiffness(170.0).WithDamping(26.0);
        Simulate(spring, 200);
        Assert.InRange(spring.Position, -0.01, 0.01);
    }

    [Fact]
    public void AllPresetsConverge()
    {
        Spring[] presets =
        [
            SpringPresets.Gentle(),
            SpringPresets.Bouncy(),
            SpringPresets.Stiff(),
            SpringPresets.Critical(),
            SpringPresets.Slow(),
        ];
        foreach (var spring in presets)
        {
            Simulate(spring, 500);
            Assert.True(spring.IsComplete, $"Did not converge: {spring}");
        }
    }

    [Fact]
    public void SimulationIsDeterministicAcrossRuns() => Assert.Equal(RunSpring(), RunSpring());

    [Fact]
    public void AtRestSpringSkipsComputation()
    {
        var spring = new Spring(0.0, 1.0);
        Simulate(spring, 200);
        var position = spring.Position;
        spring.Tick(Ms16);
        Assert.Equal(position, spring.Position);
    }

    [Fact]
    public void AnimationValueForNormalizedSpringReachesOne()
    {
        var spring = Spring.Normalized();
        Assert.Equal(0.0f, spring.Value);
        Simulate(spring, 200);
        Assert.InRange(spring.Value, 0.99f, 1.0f);
    }

    [Fact]
    public void StiffPresetAdvancesFasterThanSlowPreset()
    {
        var stiff = SpringPresets.Stiff();
        var slow = SpringPresets.Slow();
        Simulate(stiff, 30);
        Simulate(slow, 30);
        Assert.True(Math.Abs(stiff.Position - 1.0) < Math.Abs(slow.Position - 1.0));
    }

    [Fact]
    public void CloneAdvancesIndependently()
    {
        var spring = new Spring(0.0, 1.0);
        Simulate(spring, 5);
        var originalPosition = spring.Position;
        var clone = spring.Clone();
        Simulate(clone, 5);
        Assert.True(Math.Abs(clone.Position - originalPosition) > 0.01);
        Assert.Equal(originalPosition, spring.Position);
    }

    [Fact]
    public void DebugFormatIncludesPhysicalState()
    {
        var text = new Spring(0.0, 1.0).ToString();
        Assert.Contains("Spring", text, StringComparison.Ordinal);
        Assert.Contains("position", text, StringComparison.Ordinal);
        Assert.Contains("velocity", text, StringComparison.Ordinal);
        Assert.Contains("target", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NegativeStiffnessIsClamped() =>
        Assert.True(new Spring(0.0, 1.0).WithStiffness(-100.0).Stiffness >= 0.1);

    [Fact]
    public void RestThresholdBuilderStoresValue() =>
        Assert.Equal(0.1, new Spring(0.0, 1.0).WithRestThreshold(0.1).RestThreshold);

    [Fact]
    public void NegativeRestThresholdUsesAbsoluteValue() =>
        Assert.Equal(0.05, new Spring(0.0, 1.0).WithRestThreshold(-0.05).RestThreshold);

    [Fact]
    public void VelocityThresholdBuilderStoresValue() =>
        Assert.Equal(0.5, new Spring(0.0, 1.0).WithVelocityThreshold(0.5).VelocityThreshold);

    [Fact]
    public void NegativeVelocityThresholdUsesAbsoluteValue() =>
        Assert.Equal(0.3, new Spring(0.0, 1.0).WithVelocityThreshold(-0.3).VelocityThreshold);

    [Fact]
    public void EqualInitialAndTargetSettlesOnFirstTick()
    {
        var spring = new Spring(5.0, 5.0);
        spring.Tick(Ms16);
        Assert.True(spring.IsComplete);
        Assert.Equal(5.0, spring.Position);
    }

    [Fact]
    public void NormalizedConstructorHasZeroAndOneEndpoints()
    {
        var spring = Spring.Normalized();
        Assert.Equal(0.0, spring.Position);
        Assert.Equal(1.0, spring.Target);
    }

    [Fact]
    public void NegativeImpulseMovesBelowTarget()
    {
        var spring = new Spring(0.5, 0.5);
        spring.Tick(Ms16);
        spring.Impulse(-100.0);
        Assert.False(spring.IsComplete);
        spring.Tick(Ms16);
        Assert.True(spring.Position < 0.5);
    }

    [Fact]
    public void ImpulseOnMovingSpringAddsToVelocity()
    {
        var spring = new Spring(0.0, 1.0);
        spring.Tick(Ms16);
        var before = spring.Velocity;
        spring.Impulse(10.0);
        Assert.Equal(before + 10.0, spring.Velocity);
    }

    [Fact]
    public void TargetWithinRestThresholdDoesNotWake()
    {
        var spring = new Spring(0.0, 1.0).WithRestThreshold(0.01);
        Simulate(spring, 300);
        spring.SetTarget(1.005);
        Assert.True(spring.IsComplete);
        Assert.Equal(1.0, spring.Target);
    }

    [Fact]
    public void TargetBeyondRestThresholdWakes()
    {
        var spring = new Spring(0.0, 1.0).WithRestThreshold(0.01);
        Simulate(spring, 300);
        spring.SetTarget(1.02);
        Assert.False(spring.IsComplete);
        Assert.Equal(1.02, spring.Target);
    }

    [Fact]
    public void LargeRestThresholdSettlesQuickly()
    {
        var spring = new Spring(0.0, 1.0)
            .WithStiffness(170.0)
            .WithDamping(26.0)
            .WithRestThreshold(0.5)
            .WithVelocityThreshold(10.0);
        Simulate(spring, 10);
        Assert.True(spring.IsComplete);
    }

    [Fact]
    public void ValueClampsNegativePosition()
    {
        var spring = new Spring(0.0, 0.0);
        spring.Impulse(-100.0);
        spring.Tick(Ms16);
        Assert.True(spring.Position < 0.0);
        Assert.Equal(0.0f, spring.Value);
    }

    [Fact]
    public void ValueClampsAboveOne()
    {
        var spring = new Spring(0.0, 5.0);
        Simulate(spring, 200);
        Assert.True(spring.Position > 1.0);
        Assert.Equal(1.0f, spring.Value);
    }

    [Fact]
    public void ZeroDampingOscillates()
    {
        var spring = new Spring(0.0, 1.0).WithStiffness(170.0).WithDamping(0.0);
        var crossedTarget = false;
        var crossedBack = false;
        var above = false;
        for (var index = 0; index < 200; index++)
        {
            spring.Tick(Ms16);
            if (spring.Position > 1.0)
            {
                above = true;
            }

            if (above && spring.Position < 1.0)
            {
                crossedTarget = true;
            }

            if (crossedTarget && spring.Position > 1.0)
            {
                crossedBack = true;
                break;
            }
        }

        Assert.True(crossedBack);
    }

    [Fact]
    public void AdvanceAtRestIsNoOp()
    {
        var spring = new Spring(0.0, 1.0);
        Simulate(spring, 300);
        var position = spring.Position;
        var velocity = spring.Velocity;
        spring.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(position, spring.Position);
        Assert.Equal(velocity, spring.Velocity);
    }

    [Fact]
    public void ResetRestoresNonZeroInitial()
    {
        var spring = new Spring(42.0, 100.0);
        Simulate(spring, 200);
        spring.Reset();
        Assert.Equal(42.0, spring.Position);
        Assert.Equal(0.0, spring.Velocity);
        Assert.False(spring.IsComplete);
    }

    [Fact]
    public void ResetAfterImpulseClearsVelocity()
    {
        var spring = new Spring(0.0, 0.0);
        spring.Impulse(50.0);
        spring.Tick(Ms16);
        spring.Reset();
        Assert.Equal(0.0, spring.Position);
        Assert.Equal(0.0, spring.Velocity);
    }

    [Fact]
    public void MultipleTargetChangesConvergeToLastTarget()
    {
        var spring = new Spring(0.0, 1.0);
        Simulate(spring, 50);
        spring.SetTarget(2.0);
        Simulate(spring, 50);
        spring.SetTarget(0.0);
        Simulate(spring, 300);
        Assert.InRange(spring.Position, -0.01, 0.01);
    }

    [Fact]
    public void SpringOvershootIsAlwaysZero()
    {
        var spring = new Spring(0.0, 1.0);
        Assert.Equal(TimeSpan.Zero, spring.Overshoot);
        Simulate(spring, 300);
        Assert.Equal(TimeSpan.Zero, spring.Overshoot);
    }

    [Fact]
    public void GentlePresetParameters()
    {
        var spring = SpringPresets.Gentle();
        Assert.Equal(120.0, spring.Stiffness);
        Assert.Equal(20.0, spring.Damping);
    }

    [Fact]
    public void BouncyPresetParameters()
    {
        var spring = SpringPresets.Bouncy();
        Assert.Equal(300.0, spring.Stiffness);
        Assert.Equal(10.0, spring.Damping);
    }

    [Fact]
    public void StiffPresetParameters()
    {
        var spring = SpringPresets.Stiff();
        Assert.Equal(400.0, spring.Stiffness);
        Assert.Equal(38.0, spring.Damping);
    }

    [Fact]
    public void SlowPresetParameters()
    {
        var spring = SpringPresets.Slow();
        Assert.Equal(50.0, spring.Stiffness);
        Assert.Equal(14.0, spring.Damping);
    }

    [Fact]
    public void CriticalPresetIsCriticallyDamped()
    {
        var spring = SpringPresets.Critical();
        Assert.Equal(2.0 * Math.Sqrt(spring.Stiffness), spring.Damping);
    }

    [Fact]
    public void TimeStepIndependenceCoarseVersusFine()
    {
        var one = RunWithStep(1);
        Assert.InRange(RunWithStep(4), one - 0.01, one + 0.01);
        Assert.InRange(RunWithStep(16), one - 0.01, one + 0.01);
        Assert.InRange(RunWithStep(33), one - 0.01, one + 0.01);
    }

    [Fact]
    public void TimeStepIndependenceSingleVersusMany()
    {
        var single = new Spring(0.0, 1.0).WithStiffness(170.0).WithDamping(26.0);
        single.Tick(TimeSpan.FromMilliseconds(500));
        var many = new Spring(0.0, 1.0).WithStiffness(170.0).WithDamping(26.0);
        for (var index = 0; index < 500; index++)
        {
            many.Tick(TimeSpan.FromMilliseconds(1));
        }

        Assert.InRange(many.Position, single.Position - 0.02, single.Position + 0.02);
    }

    [Fact]
    public void CriticallyDampedSettlesNoLaterThanOtherModes()
    {
        const double stiffness = 170.0;
        var criticalDamping = 2.0 * Math.Sqrt(stiffness);
        var underdamped = new Spring(0.0, 1.0)
            .WithStiffness(stiffness)
            .WithDamping(criticalDamping * 0.3);
        var critical = new Spring(0.0, 1.0)
            .WithStiffness(stiffness)
            .WithDamping(criticalDamping);
        var overdamped = new Spring(0.0, 1.0)
            .WithStiffness(stiffness)
            .WithDamping(criticalDamping * 3.0);
        var underdampedFrames = SettleFrame(underdamped);
        var criticalFrames = SettleFrame(critical);
        var overdampedFrames = SettleFrame(overdamped);
        Assert.True(criticalFrames <= underdampedFrames);
        Assert.True(criticalFrames <= overdampedFrames);
    }

    [Fact]
    public void OverdampedSpringDoesNotOscillate()
    {
        const double stiffness = 170.0;
        var spring = new Spring(0.0, 1.0)
            .WithStiffness(stiffness)
            .WithDamping(2.0 * Math.Sqrt(stiffness) * 3.0);
        var previous = 0.0;
        for (var index = 0; index < 500; index++)
        {
            spring.Tick(Ms16);
            Assert.True(spring.Position >= previous - double.Epsilon);
            previous = spring.Position;
        }
    }

    [Fact]
    public void UnderdampedSpringOscillatesThenSettles()
    {
        const double stiffness = 170.0;
        var spring = new Spring(0.0, 1.0)
            .WithStiffness(stiffness)
            .WithDamping(2.0 * Math.Sqrt(stiffness) * 0.2);
        var overshot = false;
        for (var index = 0; index < 200; index++)
        {
            spring.Tick(Ms16);
            if (spring.Position > 1.0)
            {
                overshot = true;
                break;
            }
        }

        Assert.True(overshot);
        Simulate(spring, 2_000);
        Assert.True(spring.IsComplete);
    }

    [Fact]
    public void ZeroDisplacementCompletesAfterOneTick()
    {
        var spring = new Spring(1.0, 1.0);
        spring.Tick(Ms16);
        Assert.True(spring.IsComplete);
        Assert.Equal(1.0, spring.Position);
    }

    [Fact]
    public void NegativeDeltaIsRejected()
    {
        var spring = new Spring(0.0, 1.0);
        Assert.Throws<ArgumentOutOfRangeException>(() => spring.Tick(TimeSpan.FromTicks(-1)));
    }

    private static void Simulate(Spring spring, int frames)
    {
        for (var index = 0; index < frames; index++)
        {
            spring.Tick(Ms16);
        }
    }

    private static double[] RunSpring()
    {
        var spring = new Spring(0.0, 1.0).WithStiffness(170.0).WithDamping(26.0);
        var positions = new double[50];
        for (var index = 0; index < positions.Length; index++)
        {
            spring.Tick(Ms16);
            positions[index] = spring.Position;
        }

        return positions;
    }

    private static double RunWithStep(int stepMilliseconds)
    {
        var spring = new Spring(0.0, 1.0).WithStiffness(170.0).WithDamping(26.0);
        var steps = 1_000 / stepMilliseconds;
        var delta = TimeSpan.FromMilliseconds(stepMilliseconds);
        for (var index = 0; index < steps; index++)
        {
            spring.Tick(delta);
        }

        return spring.Position;
    }

    private static int SettleFrame(Spring spring)
    {
        for (var frame = 0; frame < 1_000; frame++)
        {
            spring.Tick(Ms16);
            if (Math.Abs(spring.Position - 1.0) < 0.01 && Math.Abs(spring.Velocity) < 0.01)
            {
                return frame;
            }
        }

        return 1_000;
    }
}
