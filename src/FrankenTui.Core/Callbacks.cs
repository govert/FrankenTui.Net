// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-core/src/animation/callbacks.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

namespace FrankenTui.Core;

/// <summary>An event emitted by a callback-wrapped animation.</summary>
public abstract record AnimationEvent
{
    private AnimationEvent() { }

    /// <summary>The animation received its first tick.</summary>
    public sealed record Started : AnimationEvent;

    /// <summary>The animation crossed the configured threshold.</summary>
    public sealed record Progress(float Threshold) : AnimationEvent;

    /// <summary>The animation completed.</summary>
    public sealed record Completed : AnimationEvent;
}

/// <summary>Animation wrapper that queues milestone events for polling.</summary>
public sealed class Callbacks<TAnimation> : IAnimation where TAnimation : IAnimation
{
    private readonly List<float> _thresholds = [];
    private readonly List<bool> _thresholdsFired = [];
    private readonly List<AnimationEvent> _events = [];
    private bool _onStart;
    private bool _onComplete;
    private bool _startedFired;
    private bool _completedFired;

    public Callbacks(TAnimation inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        Inner = inner;
    }

    /// <summary>The wrapped animation.</summary>
    public TAnimation Inner { get; }

    /// <summary>Number of events waiting to be drained.</summary>
    public int PendingEventCount => _events.Count;

    public bool IsComplete => Inner.IsComplete;

    public float Value => Inner.Value;

    public TimeSpan Overshoot => Inner.Overshoot;

    /// <summary>Enable the Started milestone.</summary>
    public Callbacks<TAnimation> OnStart()
    {
        _onStart = true;
        return this;
    }

    /// <summary>Enable the Completed milestone.</summary>
    public Callbacks<TAnimation> OnComplete()
    {
        _onComplete = true;
        return this;
    }

    /// <summary>Add a finite progress threshold, clamped to zero through one.</summary>
    public Callbacks<TAnimation> AtProgress(float threshold)
    {
        if (!float.IsFinite(threshold))
        {
            return this;
        }

        var clamped = Math.Clamp(threshold, 0.0f, 1.0f);
        var index = 0;
        while (index < _thresholds.Count && _thresholds[index] <= clamped)
        {
            index++;
        }

        _thresholds.Insert(index, clamped);
        _thresholdsFired.Insert(index, false);
        return this;
    }

    /// <summary>Return and clear all currently pending events.</summary>
    public IReadOnlyList<AnimationEvent> DrainEvents()
    {
        var drained = _events.ToArray();
        _events.Clear();
        return drained;
    }

    public void Tick(TimeSpan delta)
    {
        Inner.Tick(delta);
        CheckEvents();
    }

    public void Reset()
    {
        Inner.Reset();
        _startedFired = false;
        _completedFired = false;
        for (var index = 0; index < _thresholdsFired.Count; index++)
        {
            _thresholdsFired[index] = false;
        }

        _events.Clear();
    }

    public override string ToString() =>
        $"Callbacks {{ inner: {Inner}, pending_events: {_events.Count} }}";

    private void CheckEvents()
    {
        var value = Inner.Value;

        if (_onStart && !_startedFired)
        {
            _startedFired = true;
            _events.Add(new AnimationEvent.Started());
        }

        for (var index = 0; index < _thresholds.Count; index++)
        {
            if (!_thresholdsFired[index] && value >= _thresholds[index])
            {
                _thresholdsFired[index] = true;
                _events.Add(new AnimationEvent.Progress(_thresholds[index]));
            }
        }

        if (_onComplete && !_completedFired && Inner.IsComplete)
        {
            _completedFired = true;
            _events.Add(new AnimationEvent.Completed());
        }
    }
}
