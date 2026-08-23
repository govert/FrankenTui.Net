// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-core/src/animation/stagger.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

namespace FrankenTui.Core;

/// <summary>How delay offsets are distributed across staggered items.</summary>
public readonly struct StaggerMode
{
    private readonly BuiltInMode _builtIn;
    private readonly Func<float, float>? _customEasing;

    private StaggerMode(BuiltInMode builtIn, Func<float, float>? customEasing = null)
    {
        _builtIn = builtIn;
        _customEasing = customEasing;
    }

    /// <summary>Equal spacing: offset[i] = i * delay.</summary>
    public static StaggerMode Linear { get; } = new(BuiltInMode.Linear);

    /// <summary>Slow start with accelerating gaps.</summary>
    public static StaggerMode EaseIn { get; } = new(BuiltInMode.EaseIn);

    /// <summary>Fast start with decelerating gaps.</summary>
    public static StaggerMode EaseOut { get; } = new(BuiltInMode.EaseOut);

    /// <summary>Slow start and end with a faster middle.</summary>
    public static StaggerMode EaseInOut { get; } = new(BuiltInMode.EaseInOut);

    /// <summary>Create a mode backed by a custom normalized easing function.</summary>
    public static StaggerMode Custom(Func<float, float> easing)
    {
        ArgumentNullException.ThrowIfNull(easing);
        return new StaggerMode(BuiltInMode.Custom, easing);
    }

    internal bool IsLinear => _builtIn == BuiltInMode.Linear;

    internal float Apply(float value)
    {
        var t = Math.Clamp(value, 0.0f, 1.0f);
        return _builtIn switch
        {
            BuiltInMode.Linear => t,
            BuiltInMode.EaseIn => t * t,
            BuiltInMode.EaseOut => 1.0f - ((1.0f - t) * (1.0f - t)),
            BuiltInMode.EaseInOut when t < 0.5f => 2.0f * t * t,
            BuiltInMode.EaseInOut => 1.0f - (MathF.Pow((-2.0f * t) + 2.0f, 2) / 2.0f),
            BuiltInMode.Custom => _customEasing!(t),
            _ => t,
        };
    }

    private enum BuiltInMode
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        Custom,
    }
}

/// <summary>Coordinated delay-offset utilities for animation lists.</summary>
public static class AnimationStagger
{
    private const ulong NanosecondsPerTick = 100;

    /// <summary>Compute stagger delay offsets for <paramref name="count"/> items.</summary>
    public static IReadOnlyList<TimeSpan> Offsets(int count, TimeSpan delay, StaggerMode mode)
    {
        Validate(count, delay, nameof(delay));
        return ComputeNanosecondOffsets(count, delay, mode)
            .Select(FromNanoseconds)
            .ToArray();
    }

    /// <summary>Compute stagger offsets with deterministic random jitter.</summary>
    public static IReadOnlyList<TimeSpan> OffsetsWithJitter(
        int count,
        TimeSpan delay,
        StaggerMode mode,
        TimeSpan jitter,
        ulong seed)
    {
        Validate(count, delay, nameof(delay));
        if (jitter < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(jitter), jitter, "Jitter cannot be negative.");
        }

        var offsets = ComputeNanosecondOffsets(count, delay, mode);
        if (jitter == TimeSpan.Zero || offsets.Length == 0)
        {
            return offsets.Select(FromNanoseconds).ToArray();
        }

        var state = unchecked(seed + 1UL);
        var jitterNanoseconds = (UInt128)(ulong)jitter.Ticks * NanosecondsPerTick;
        var span = (jitterNanoseconds * 2) + 1;

        for (var index = 0; index < offsets.Length; index++)
        {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;

            var random = (UInt128)state % span;
            var perturbation = (Int128)random - (Int128)jitterNanoseconds;
            var baseNanoseconds = (Int128)offsets[index];
            var jitteredNanoseconds = baseNanoseconds + perturbation;
            offsets[index] = jitteredNanoseconds <= 0 ? 0 : (UInt128)jitteredNanoseconds;
        }

        return offsets.Select(FromNanoseconds).ToArray();
    }

    private static void Validate(int count, TimeSpan duration, string durationParameter)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be negative.");
        }

        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(durationParameter, duration, "Duration cannot be negative.");
        }
    }

    private static UInt128[] ComputeNanosecondOffsets(int count, TimeSpan delay, StaggerMode mode)
    {
        if (count == 0)
        {
            return [];
        }

        var offsets = new UInt128[count];
        if (count == 1)
        {
            return offsets;
        }

        var delayNanoseconds = (UInt128)(ulong)delay.Ticks * NanosecondsPerTick;
        if (mode.IsLinear)
        {
            var current = UInt128.Zero;
            for (var index = 0; index < count; index++)
            {
                offsets[index] = current;
                current = MaxNanoseconds - current < delayNanoseconds
                    ? MaxNanoseconds
                    : current + delayNanoseconds;
            }

            return offsets;
        }

        var totalNanoseconds = delay.Ticks * (double)NanosecondsPerTick * (count - 1);
        for (var index = 0; index < count; index++)
        {
            var t = (float)index / (count - 1);
            offsets[index] = ToNanoseconds(totalNanoseconds * mode.Apply(t));
        }

        return offsets;
    }

    private static TimeSpan FromNanoseconds(UInt128 nanoseconds)
    {
        var ticks = nanoseconds / NanosecondsPerTick;
        return ticks >= (UInt128)long.MaxValue
            ? TimeSpan.MaxValue
            : TimeSpan.FromTicks((long)ticks);
    }

    private static UInt128 ToNanoseconds(double nanoseconds)
    {
        if (double.IsNaN(nanoseconds) || nanoseconds <= 0.0)
        {
            return UInt128.Zero;
        }

        if (double.IsPositiveInfinity(nanoseconds) || nanoseconds >= (double)MaxNanoseconds)
        {
            return MaxNanoseconds;
        }

        return (UInt128)nanoseconds;
    }

    private static UInt128 MaxNanoseconds => (UInt128)long.MaxValue * NanosecondsPerTick;
}
