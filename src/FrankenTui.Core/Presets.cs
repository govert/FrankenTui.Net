// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-core/src/animation/presets.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

namespace FrankenTui.Core;

/// <summary>Ready-to-use deterministic animation compositions.</summary>
public static class AnimationPresets
{
    public static AnimationGroup CascadeIn(
        int count,
        TimeSpan itemDuration,
        TimeSpan staggerDelay,
        StaggerMode mode)
    {
        var offsets = AnimationStagger.Offsets(count, staggerDelay, mode);
        var group = new AnimationGroup();
        for (var index = 0; index < offsets.Count; index++)
        {
            var animation = Animations.Delay(
                offsets[index],
                new Fade(itemDuration).Easing(AnimationEasing.EaseOut));
            group.Insert($"item_{index}", animation);
        }

        return group;
    }

    public static AnimationGroup CascadeOut(
        int count,
        TimeSpan itemDuration,
        TimeSpan staggerDelay,
        StaggerMode mode)
    {
        var offsets = AnimationStagger.Offsets(count, staggerDelay, mode);
        var group = new AnimationGroup();
        for (var index = 0; index < offsets.Count; index++)
        {
            var animation = Animations.Delay(
                offsets[index],
                new InvertedFade(itemDuration).Easing(AnimationEasing.EaseIn));
            group.Insert($"item_{index}", animation);
        }

        return group;
    }

    public static AnimationGroup FanOut(int count, TimeSpan itemDuration, TimeSpan totalSpread)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be negative.");
        }

        AnimationTime.Validate(totalSpread, nameof(totalSpread));
        if (count == 0)
        {
            return new AnimationGroup();
        }

        var group = new AnimationGroup();
        for (var index = 0; index < count; index++)
        {
            var center = (count - 1.0) / 2.0;
            var distance = count <= 1
                ? 0.0
                : Math.Min(Math.Abs(index - center) / center, 1.0);
            var eased = 1.0 - ((1.0 - distance) * (1.0 - distance));
            var offset = AnimationTime.Multiply(totalSpread, eased);
            group.Insert(
                $"item_{index}",
                Animations.Delay(offset, new Fade(itemDuration).Easing(AnimationEasing.EaseOut)));
        }

        return group;
    }

    public static TypewriterAnim Typewriter(int characterCount, TimeSpan totalDuration) =>
        new(characterCount, totalDuration);

    public static AnimationGroup PulseSequence(
        int count,
        TimeSpan pulseDuration,
        TimeSpan staggerDelay)
    {
        var offsets = AnimationStagger.Offsets(count, staggerDelay, StaggerMode.Linear);
        var group = new AnimationGroup();
        for (var index = 0; index < offsets.Count; index++)
        {
            group.Insert(
                $"pulse_{index}",
                Animations.Delay(offsets[index], new PulseOnce(pulseDuration)));
        }

        return group;
    }

    public static Slide SlideInLeft(short distance, TimeSpan duration) =>
        new Slide(unchecked((short)-distance), 0, duration).Easing(AnimationEasing.EaseOut);

    public static Slide SlideInRight(short distance, TimeSpan duration) =>
        new Slide(distance, 0, duration).Easing(AnimationEasing.EaseOut);

    public static Sequence<InvertedFade, Fade> FadeThrough(TimeSpan halfDuration) =>
        Animations.Sequence(
            new InvertedFade(halfDuration).Easing(AnimationEasing.EaseIn),
            new Fade(halfDuration).Easing(AnimationEasing.EaseOut));

    private sealed class PulseOnce : IAnimation
    {
        private readonly TimeSpan _duration;
        private TimeSpan _elapsed;

        internal PulseOnce(TimeSpan duration)
        {
            _duration = AnimationTime.NonZero(duration, nameof(duration));
        }

        public bool IsComplete => _elapsed >= _duration;

        public float Value
        {
            get
            {
                var progress = (float)Math.Min(
                    _elapsed.Ticks / (double)_duration.Ticks,
                    1.0);
                return MathF.Sin(progress * MathF.PI);
            }
        }

        public TimeSpan Overshoot => AnimationTime.SaturatingSubtract(_elapsed, _duration);

        public void Tick(TimeSpan delta)
        {
            AnimationTime.Validate(delta, nameof(delta));
            _elapsed = AnimationTime.SaturatingAdd(_elapsed, delta);
        }

        public void Reset() => _elapsed = TimeSpan.Zero;
    }
}

/// <summary>Animation that progressively reveals a fixed character count.</summary>
public sealed class TypewriterAnim : IAnimation
{
    private readonly int _characterCount;
    private readonly Fade _fade;

    internal TypewriterAnim(int characterCount, TimeSpan totalDuration)
    {
        if (characterCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(characterCount), characterCount, "Count cannot be negative.");
        }

        _characterCount = characterCount;
        _fade = new Fade(totalDuration);
    }

    public int VisibleChars
    {
        get
        {
            var rounded = MathF.Round(
                _fade.Value * _characterCount,
                MidpointRounding.AwayFromZero);
            return (int)Math.Clamp(rounded, 0.0f, _characterCount);
        }
    }

    public bool IsComplete => _fade.IsComplete;

    public float Value => _fade.Value;

    public TimeSpan Overshoot => _fade.Overshoot;

    public void Tick(TimeSpan delta) => _fade.Tick(delta);

    public void Reset() => _fade.Reset();
}

/// <summary>A fade animation whose value is inverted from one to zero.</summary>
public sealed class InvertedFade : IAnimation
{
    private readonly Fade _fade;

    public InvertedFade(TimeSpan duration)
    {
        _fade = new Fade(duration);
    }

    public InvertedFade Easing(EasingFunction easing)
    {
        _fade.Easing(easing);
        return this;
    }

    public bool IsComplete => _fade.IsComplete;

    public float Value => 1.0f - _fade.Value;

    public TimeSpan Overshoot => _fade.Overshoot;

    public void Tick(TimeSpan delta) => _fade.Tick(delta);

    public void Reset() => _fade.Reset();
}
