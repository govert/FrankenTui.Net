// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-core/src/animation/timeline.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.
// DIVERGENCE: upstream's 1ns zero-duration clamp is represented by one 100ns TimeSpan tick.

using System.Globalization;

namespace FrankenTui.Core;

/// <summary>How many additional times a timeline repeats after its first play.</summary>
public readonly record struct LoopCount
{
    private LoopCount(LoopKind kind, uint count = 0)
    {
        Kind = kind;
        Count = count;
    }

    internal LoopKind Kind { get; }

    internal uint Count { get; }

    public static LoopCount Once { get; } = new(LoopKind.Once);

    public static LoopCount Infinite { get; } = new(LoopKind.Infinite);

    public static LoopCount Times(uint count) => new(LoopKind.Times, count);

    internal enum LoopKind
    {
        Once,
        Times,
        Infinite,
    }
}

/// <summary>Playback state of a timeline.</summary>
public enum PlaybackState
{
    Idle,
    Playing,
    Paused,
    Finished,
}

/// <summary>Multi-event animation scheduler with looping, pause, and seek.</summary>
public sealed class Timeline : IAnimation
{
    private readonly List<TimelineEvent> _events = [];
    private TimeSpan _totalDuration = TimeSpan.FromTicks(1);
    private bool _durationExplicit;
    private LoopCount _loopCount = LoopCount.Once;
    private uint _loopsRemaining;
    private PlaybackState _state;
    private TimeSpan _currentTime;

    public int EventCount => _events.Count;

    public TimeSpan Duration => _totalDuration;

    public TimeSpan CurrentTime => _currentTime;

    public PlaybackState State => _state;

    public float Progress
    {
        get
        {
            if (_events.Count == 0 || _totalDuration == TimeSpan.Zero)
            {
                return 1.0f;
            }

            return (float)Math.Clamp(
                _currentTime.Ticks / (double)_totalDuration.Ticks,
                0.0,
                1.0);
        }
    }

    public bool IsComplete => _state == PlaybackState.Finished;

    public float Value => Progress;

    public TimeSpan Overshoot => _state == PlaybackState.Finished
        ? AnimationTime.SaturatingSubtract(_currentTime, _totalDuration)
        : TimeSpan.Zero;

    public Timeline Add(TimeSpan offset, IAnimation animation)
    {
        PushEvent(offset, animation, null);
        return this;
    }

    public Timeline AddLabeled(string label, TimeSpan offset, IAnimation animation)
    {
        ArgumentNullException.ThrowIfNull(label);
        PushEvent(offset, animation, label);
        return this;
    }

    public Timeline Then(IAnimation animation)
    {
        var offset = _events.Count == 0 ? TimeSpan.Zero : _events[^1].Offset;
        return Add(offset, animation);
    }

    public Timeline SetDuration(TimeSpan duration)
    {
        _totalDuration = AnimationTime.NonZero(duration, nameof(duration));
        _durationExplicit = true;
        return this;
    }

    public Timeline SetLoopCount(LoopCount count)
    {
        _loopCount = count;
        _loopsRemaining = InitialLoops(count);
        return this;
    }

    public void Play()
    {
        _currentTime = TimeSpan.Zero;
        _loopsRemaining = InitialLoops(_loopCount);
        foreach (var timelineEvent in _events)
        {
            timelineEvent.Animation.Reset();
        }

        _state = PlaybackState.Playing;
    }

    public void Pause()
    {
        if (_state == PlaybackState.Playing)
        {
            _state = PlaybackState.Paused;
        }
    }

    public void Resume()
    {
        if (_state == PlaybackState.Paused)
        {
            _state = PlaybackState.Playing;
        }
    }

    public void Stop()
    {
        _state = PlaybackState.Idle;
        _currentTime = TimeSpan.Zero;
        foreach (var timelineEvent in _events)
        {
            timelineEvent.Animation.Reset();
        }
    }

    public void Seek(TimeSpan time)
    {
        AnimationTime.Validate(time, nameof(time));
        var clamped = time > _totalDuration ? _totalDuration : time;

        foreach (var timelineEvent in _events)
        {
            timelineEvent.Animation.Reset();
            if (clamped > timelineEvent.Offset)
            {
                timelineEvent.Animation.Tick(AnimationTime.SaturatingSubtract(clamped, timelineEvent.Offset));
            }
        }

        _currentTime = clamped;
        if (_state is PlaybackState.Idle or PlaybackState.Finished)
        {
            _state = PlaybackState.Paused;
        }
    }

    public bool SeekLabel(string label)
    {
        ArgumentNullException.ThrowIfNull(label);
        foreach (var timelineEvent in _events)
        {
            if (!string.Equals(timelineEvent.Label, label, StringComparison.Ordinal))
            {
                continue;
            }

            Seek(timelineEvent.Offset);
            return true;
        }

        return false;
    }

    public float? EventValue(string label)
    {
        ArgumentNullException.ThrowIfNull(label);
        foreach (var timelineEvent in _events)
        {
            if (string.Equals(timelineEvent.Label, label, StringComparison.Ordinal))
            {
                return timelineEvent.Animation.Value;
            }
        }

        return null;
    }

    public float? EventValueAt(int index) =>
        index >= 0 && index < _events.Count ? _events[index].Animation.Value : null;

    public void Tick(TimeSpan delta)
    {
        AnimationTime.Validate(delta, nameof(delta));
        if (_state != PlaybackState.Playing)
        {
            return;
        }

        var newTime = AnimationTime.SaturatingAdd(_currentTime, delta);
        foreach (var timelineEvent in _events)
        {
            if (newTime <= timelineEvent.Offset || timelineEvent.Animation.IsComplete)
            {
                continue;
            }

            if (_currentTime >= timelineEvent.Offset)
            {
                timelineEvent.Animation.Tick(delta);
            }
            else
            {
                timelineEvent.Animation.Tick(AnimationTime.SaturatingSubtract(newTime, timelineEvent.Offset));
            }
        }

        _currentTime = newTime;
        if (_currentTime < _totalDuration)
        {
            return;
        }

        switch (_loopCount.Kind)
        {
            case LoopCount.LoopKind.Once:
                _currentTime = _totalDuration;
                _state = PlaybackState.Finished;
                return;

            case LoopCount.LoopKind.Times:
            case LoopCount.LoopKind.Infinite:
                if (_loopsRemaining > 0)
                {
                    if (_loopCount.Kind != LoopCount.LoopKind.Infinite)
                    {
                        _loopsRemaining--;
                    }

                    var overshoot = AnimationTime.SaturatingSubtract(_currentTime, _totalDuration);
                    _currentTime = TimeSpan.Zero;
                    foreach (var timelineEvent in _events)
                    {
                        timelineEvent.Animation.Reset();
                    }

                    if (overshoot != TimeSpan.Zero)
                    {
                        Tick(overshoot);
                    }

                    return;
                }

                _currentTime = _totalDuration;
                _state = PlaybackState.Finished;
                return;
        }
    }

    public void Reset()
    {
        _currentTime = TimeSpan.Zero;
        _loopsRemaining = InitialLoops(_loopCount);
        _state = PlaybackState.Idle;
        foreach (var timelineEvent in _events)
        {
            timelineEvent.Animation.Reset();
        }
    }

    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"Timeline {{ event_count: {_events.Count}, total_duration: {_totalDuration}, loop_count: {_loopCount}, state: {_state}, current_time: {_currentTime} }}");

    private void PushEvent(TimeSpan offset, IAnimation animation, string? label)
    {
        AnimationTime.Validate(offset, nameof(offset));
        ArgumentNullException.ThrowIfNull(animation);

        var position = 0;
        while (position < _events.Count && _events[position].Offset <= offset)
        {
            position++;
        }

        _events.Insert(position, new TimelineEvent(offset, animation, label));
        if (!_durationExplicit)
        {
            var lastOffset = _events[^1].Offset;
            _totalDuration = lastOffset == TimeSpan.Zero ? TimeSpan.FromTicks(1) : lastOffset;
        }
    }

    private static uint InitialLoops(LoopCount count) => count.Kind switch
    {
        LoopCount.LoopKind.Once => 0,
        LoopCount.LoopKind.Times => count.Count,
        LoopCount.LoopKind.Infinite => uint.MaxValue,
        _ => 0,
    };

    private sealed record TimelineEvent(TimeSpan Offset, IAnimation Animation, string? Label);
}
