// SPDX-License-Identifier: Apache-2.0
// Port of the Animation trait in .external/frankentui/crates/ftui-core/src/animation/mod.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

namespace FrankenTui.Core;

/// <summary>
/// A time-based animation that produces a normalized value in the range 0..1.
/// </summary>
/// <remarks>
/// This is the managed composition contract required by <see cref="AnimationGroup"/>.
/// Concrete animation primitives remain separate ports of their corresponding upstream types.
/// </remarks>
public interface IAnimation
{
    /// <summary>Advance the animation by <paramref name="delta"/>.</summary>
    void Tick(TimeSpan delta);

    /// <summary>Whether the animation has reached its end.</summary>
    bool IsComplete { get; }

    /// <summary>Current output value, clamped to the range 0..1.</summary>
    float Value { get; }

    /// <summary>Reset the animation to its initial state.</summary>
    void Reset();

    /// <summary>
    /// Time elapsed past completion. Composition types use this to forward remaining time.
    /// </summary>
    TimeSpan Overshoot => TimeSpan.Zero;
}

/// <summary>Easing function mapping normalized progress to normalized output.</summary>
public delegate float EasingFunction(float progress);

/// <summary>Core easing curves used by animation primitives and presets.</summary>
public static class AnimationEasing
{
    public static float Linear(float progress) => Math.Clamp(progress, 0.0f, 1.0f);

    public static float EaseIn(float progress)
    {
        var t = Math.Clamp(progress, 0.0f, 1.0f);
        return t * t;
    }

    public static float EaseOut(float progress)
    {
        var t = Math.Clamp(progress, 0.0f, 1.0f);
        return 1.0f - ((1.0f - t) * (1.0f - t));
    }

    public static float EaseInOut(float progress)
    {
        var t = Math.Clamp(progress, 0.0f, 1.0f);
        return t < 0.5f
            ? 2.0f * t * t
            : 1.0f - (MathF.Pow((-2.0f * t) + 2.0f, 2) / 2.0f);
    }

    public static float EaseInCubic(float progress)
    {
        var t = Math.Clamp(progress, 0.0f, 1.0f);
        return t * t * t;
    }

    public static float EaseOutCubic(float progress)
    {
        var t = Math.Clamp(progress, 0.0f, 1.0f);
        return 1.0f - MathF.Pow(1.0f - t, 3);
    }
}

/// <summary>Linear progression from zero to one over a duration.</summary>
public sealed class Fade : IAnimation
{
    private TimeSpan _elapsed;
    private readonly TimeSpan _duration;
    private EasingFunction _easing = AnimationEasing.Linear;

    public Fade(TimeSpan duration)
    {
        _duration = AnimationTime.NonZero(duration, nameof(duration));
    }

    public Fade Easing(EasingFunction easing)
    {
        ArgumentNullException.ThrowIfNull(easing);
        _easing = easing;
        return this;
    }

    public float RawProgress => (float)Math.Clamp(
        _elapsed.Ticks / (double)_duration.Ticks,
        0.0,
        1.0);

    public bool IsComplete => _elapsed >= _duration;

    public float Value => _easing(RawProgress);

    public TimeSpan Overshoot => AnimationTime.SaturatingSubtract(_elapsed, _duration);

    public void Tick(TimeSpan delta)
    {
        AnimationTime.Validate(delta, nameof(delta));
        _elapsed = AnimationTime.SaturatingAdd(_elapsed, delta);
    }

    public void Reset() => _elapsed = TimeSpan.Zero;
}

/// <summary>Interpolates a signed 16-bit position over a duration.</summary>
public sealed class Slide : IAnimation
{
    private readonly short _from;
    private readonly short _to;
    private TimeSpan _elapsed;
    private readonly TimeSpan _duration;
    private EasingFunction _easing = AnimationEasing.EaseOut;

    public Slide(short from, short to, TimeSpan duration)
    {
        _from = from;
        _to = to;
        _duration = AnimationTime.NonZero(duration, nameof(duration));
    }

    public Slide Easing(EasingFunction easing)
    {
        ArgumentNullException.ThrowIfNull(easing);
        _easing = easing;
        return this;
    }

    private float Progress => (float)Math.Clamp(
        _elapsed.Ticks / (double)_duration.Ticks,
        0.0,
        1.0);

    public short Position
    {
        get
        {
            var progress = _easing(Progress);
            var position = _from + ((_to - _from) * progress);
            var rounded = MathF.Round(position, MidpointRounding.AwayFromZero);
            return (short)Math.Clamp(rounded, short.MinValue, short.MaxValue);
        }
    }

    public bool IsComplete => _elapsed >= _duration;

    public float Value => _easing(Progress);

    public TimeSpan Overshoot => AnimationTime.SaturatingSubtract(_elapsed, _duration);

    public void Tick(TimeSpan delta)
    {
        AnimationTime.Validate(delta, nameof(delta));
        _elapsed = AnimationTime.SaturatingAdd(_elapsed, delta);
    }

    public void Reset() => _elapsed = TimeSpan.Zero;
}

/// <summary>Continuous sine-wave oscillation that never completes.</summary>
public sealed class Pulse : IAnimation
{
    private const float MinimumPositiveNormal = 1.17549435E-38f;
    private readonly float _frequency;
    private float _phase;

    public Pulse(float frequency)
    {
        var absolute = MathF.Abs(frequency);
        _frequency = float.IsNaN(absolute)
            ? MinimumPositiveNormal
            : MathF.Max(absolute, MinimumPositiveNormal);
    }

    public float Phase => _phase;

    public bool IsComplete => false;

    public float Value => (MathF.Sin(_phase) + 1.0f) / 2.0f;

    public void Tick(TimeSpan delta)
    {
        AnimationTime.Validate(delta, nameof(delta));
        _phase += MathF.Tau * _frequency * (float)delta.TotalSeconds;
        _phase %= MathF.Tau;
    }

    public void Reset() => _phase = 0.0f;
}

/// <summary>Play one animation and then another, forwarding first-stage overshoot.</summary>
public sealed class Sequence<TFirst, TSecond> : IAnimation
    where TFirst : IAnimation
    where TSecond : IAnimation
{
    private bool _firstDone;

    public Sequence(TFirst first, TSecond second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        First = first;
        Second = second;
    }

    public TFirst First { get; }

    public TSecond Second { get; }

    public bool IsComplete => _firstDone && Second.IsComplete;

    public float Value => _firstDone ? Second.Value : First.Value;

    public TimeSpan Overshoot => _firstDone ? Second.Overshoot : TimeSpan.Zero;

    public void Tick(TimeSpan delta)
    {
        AnimationTime.Validate(delta, nameof(delta));
        if (!_firstDone)
        {
            First.Tick(delta);
            if (First.IsComplete)
            {
                _firstDone = true;
                if (First.Overshoot != TimeSpan.Zero)
                {
                    Second.Tick(First.Overshoot);
                }
            }

            return;
        }

        Second.Tick(delta);
    }

    public void Reset()
    {
        First.Reset();
        Second.Reset();
        _firstDone = false;
    }
}

/// <summary>Play two animations simultaneously.</summary>
public sealed class Parallel<TFirst, TSecond> : IAnimation
    where TFirst : IAnimation
    where TSecond : IAnimation
{
    public Parallel(TFirst first, TSecond second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        First = first;
        Second = second;
    }

    public TFirst First { get; }

    public TSecond Second { get; }

    public bool IsComplete => First.IsComplete && Second.IsComplete;

    public float Value => (First.Value + Second.Value) / 2.0f;

    public void Tick(TimeSpan delta)
    {
        AnimationTime.Validate(delta, nameof(delta));
        if (!First.IsComplete)
        {
            First.Tick(delta);
        }

        if (!Second.IsComplete)
        {
            Second.Tick(delta);
        }
    }

    public void Reset()
    {
        First.Reset();
        Second.Reset();
    }
}

/// <summary>Wait for a delay and then play an inner animation.</summary>
public sealed class Delayed<TAnimation> : IAnimation where TAnimation : IAnimation
{
    private readonly TimeSpan _delay;
    private TimeSpan _elapsed;

    public Delayed(TimeSpan delay, TAnimation inner)
    {
        AnimationTime.Validate(delay, nameof(delay));
        ArgumentNullException.ThrowIfNull(inner);
        _delay = delay;
        Inner = inner;
    }

    public TAnimation Inner { get; }

    public bool HasStarted { get; private set; }

    public bool IsComplete => HasStarted && Inner.IsComplete;

    public float Value => HasStarted ? Inner.Value : 0.0f;

    public TimeSpan Overshoot => HasStarted ? Inner.Overshoot : TimeSpan.Zero;

    public void Tick(TimeSpan delta)
    {
        AnimationTime.Validate(delta, nameof(delta));
        if (!HasStarted)
        {
            _elapsed = AnimationTime.SaturatingAdd(_elapsed, delta);
            if (_elapsed >= _delay)
            {
                HasStarted = true;
                var overshoot = AnimationTime.SaturatingSubtract(_elapsed, _delay);
                if (overshoot != TimeSpan.Zero)
                {
                    Inner.Tick(overshoot);
                }
            }

            return;
        }

        Inner.Tick(delta);
    }

    public void Reset()
    {
        _elapsed = TimeSpan.Zero;
        HasStarted = false;
        Inner.Reset();
    }
}

/// <summary>Convenience constructors for animation composition.</summary>
public static class Animations
{
    public static Sequence<TFirst, TSecond> Sequence<TFirst, TSecond>(TFirst first, TSecond second)
        where TFirst : IAnimation
        where TSecond : IAnimation => new(first, second);

    public static Parallel<TFirst, TSecond> Parallel<TFirst, TSecond>(TFirst first, TSecond second)
        where TFirst : IAnimation
        where TSecond : IAnimation => new(first, second);

    public static Delayed<TAnimation> Delay<TAnimation>(TimeSpan delay, TAnimation animation)
        where TAnimation : IAnimation => new(delay, animation);
}

internal static class AnimationTime
{
    internal static TimeSpan NonZero(TimeSpan duration, string parameterName)
    {
        Validate(duration, parameterName);
        // DIVERGENCE: .NET TimeSpan resolves to 100ns, so one tick represents
        // upstream's non-zero 1ns clamp.
        return duration == TimeSpan.Zero ? TimeSpan.FromTicks(1) : duration;
    }

    internal static void Validate(TimeSpan duration, string parameterName)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, duration, "Duration cannot be negative.");
        }
    }

    internal static TimeSpan SaturatingAdd(TimeSpan left, TimeSpan right) =>
        TimeSpan.MaxValue.Ticks - left.Ticks < right.Ticks
            ? TimeSpan.MaxValue
            : TimeSpan.FromTicks(left.Ticks + right.Ticks);

    internal static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right) =>
        left <= right ? TimeSpan.Zero : left - right;

    internal static TimeSpan Multiply(TimeSpan duration, double factor)
    {
        if (double.IsNaN(factor) || factor <= 0.0 || duration == TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var ticks = duration.Ticks * factor;
        if (double.IsPositiveInfinity(ticks) || ticks >= TimeSpan.MaxValue.Ticks)
        {
            return TimeSpan.MaxValue;
        }

        return TimeSpan.FromTicks((long)Math.Round(ticks, MidpointRounding.AwayFromZero));
    }
}
