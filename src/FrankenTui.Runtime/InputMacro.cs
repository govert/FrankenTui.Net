// SPDX-License-Identifier: Apache-2.0
// Port basis: frankentui 15cc6543f76b814394c590f9e7719dedd6684e4c
// Source: .external/frankentui/crates/ftui-runtime/src/input_macro.rs

using System.Diagnostics;
using FrankenTui.Core;

namespace FrankenTui.Runtime;

/// <summary>An input event and its delay from the preceding recorded event.</summary>
public sealed record TimedEvent
{
    private TerminalEvent _event = null!;
    private TimeSpan _delay;

    public TimedEvent(TerminalEvent Event, TimeSpan Delay)
    {
        this.Event = Event;
        this.Delay = Delay;
    }

    public TerminalEvent Event
    {
        get => _event;
        init => _event = value ?? throw new ArgumentNullException(nameof(value));
    }

    public TimeSpan Delay
    {
        get => _delay;
        init => _delay = MacroTime.RequireDuration(value, nameof(value));
    }

    public static TimedEvent Immediate(TerminalEvent e) => new(e, TimeSpan.Zero);

    public void Deconstruct(out TerminalEvent @event, out TimeSpan delay)
    {
        @event = Event;
        delay = Delay;
    }
}

/// <summary>Descriptive and timing metadata captured with an input macro.</summary>
public sealed record MacroMetadata(
    string Name,
    (ushort Width, ushort Height) TerminalSize,
    TimeSpan TotalDuration);

/// <summary>An immutable sequence of terminal events with relative timing.</summary>
public sealed class InputMacro
{
    private readonly IReadOnlyList<TimedEvent> _events;
    private readonly MacroMetadata _metadata;

    // Kept for source compatibility with the original managed stub.
    public InputMacro(List<TimedEvent> events, MacroMetadata meta)
        : this((IEnumerable<TimedEvent>)events, meta)
    {
    }

    public InputMacro(IEnumerable<TimedEvent> events, MacroMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(metadata.Name);
        MacroTime.RequireDuration(metadata.TotalDuration, nameof(metadata));

        var snapshot = events.ToArray();
        if (snapshot.Any(static item => item is null))
        {
            throw new ArgumentException("Macro events cannot contain null entries.", nameof(events));
        }

        _events = Array.AsReadOnly(snapshot);
        _metadata = metadata;
    }

    public static InputMacro FromEvents(string name, List<TerminalEvent> events) =>
        FromEvents(name, (IEnumerable<TerminalEvent>)events);

    public static InputMacro FromEvents(string name, IEnumerable<TerminalEvent> events)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(events);
        var timed = events.Select(TimedEvent.Immediate).ToList();
        return new InputMacro(timed, new MacroMetadata(name, (80, 24), TimeSpan.Zero));
    }

    public IReadOnlyList<TimedEvent> Events => _events;

    public MacroMetadata Metadata => _metadata;

    public int Count => _events.Count;

    public bool IsEmpty => _events.Count == 0;

    public TimeSpan TotalDuration => _metadata.TotalDuration;

    // Kept as List<T> to preserve the public shape of the earlier managed port.
    public List<TerminalEvent> BareEvents => _events.Select(static item => item.Event).ToList();

    /// <summary>Replay through a caller-supplied input injection boundary.</summary>
    public void ReplayWithTiming(
        Action<TerminalEvent> inject,
        Func<bool>? isRunning = null) =>
        new MacroPlayer(this).ReplayWithTiming(inject, isRunning);

    public void ReplayWithTiming<TModel, TMessage>(ProgramSimulator<TModel, TMessage> simulator)
        where TModel : class, IModel<TMessage>
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(simulator);
        new MacroPlayer(this).ReplayWithTiming(simulator);
    }

    /// <summary>Replay with a caller-supplied sleeper, enabling deterministic timing tests.</summary>
    public void ReplayWithSleeper(
        Action<TerminalEvent> inject,
        Action<TimeSpan> sleep,
        Func<bool>? isRunning = null) =>
        new MacroPlayer(this).ReplayWithSleeper(inject, sleep, isRunning);

    public void ReplayWithSleeper<TModel, TMessage>(
        ProgramSimulator<TModel, TMessage> simulator,
        Action<TimeSpan> sleep)
        where TModel : class, IModel<TMessage>
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(simulator);
        new MacroPlayer(this).ReplayWithSleeper(simulator, sleep);
    }

    /// <summary>Serialize using the stable, versioned managed macro schema.</summary>
    public string ToJson(bool indented = false) => InputMacroJson.Serialize(this, indented);

    public static InputMacro FromJson(string json) => InputMacroJson.Deserialize(json);
}

/// <summary>Records input events and their inter-event timing.</summary>
public sealed class MacroRecorder
{
    private readonly string _name;
    private readonly List<TimedEvent> _events = [];
    private (ushort Width, ushort Height) _terminalSize = (80, 24);
    private long _lastEventTimestamp;
    private TimeSpan _recordedDuration;

    public MacroRecorder(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _lastEventTimestamp = Stopwatch.GetTimestamp();
    }

    public MacroRecorder WithTerminalSize(ushort w, ushort h)
    {
        _terminalSize = (w, h);
        return this;
    }

    public void RecordEvent(TerminalEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var now = Stopwatch.GetTimestamp();
        var delay = now <= _lastEventTimestamp
            ? TimeSpan.Zero
            : MacroTime.FromStopwatchTicks(now - _lastEventTimestamp);

        _events.Add(new TimedEvent(e, delay));
        _recordedDuration = MacroTime.SaturatingAdd(_recordedDuration, delay);
        _lastEventTimestamp = now;
    }

    public void RecordEventWithDelay(TerminalEvent e, TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(e);
        delay = MacroTime.RequireDuration(delay, nameof(delay));
        _events.Add(new TimedEvent(e, delay));
        _recordedDuration = MacroTime.SaturatingAdd(_recordedDuration, delay);

        var stopwatchDelay = MacroTime.ToStopwatchTicks(delay);
        if (stopwatchDelay is null || _lastEventTimestamp > long.MaxValue - stopwatchDelay.Value)
        {
            // Mirrors Instant::checked_add(...).unwrap_or_else(Instant::now).
            _lastEventTimestamp = Stopwatch.GetTimestamp();
        }
        else
        {
            _lastEventTimestamp += stopwatchDelay.Value;
        }
    }

    /// <summary>
    /// Parse one terminal-input chunk and record the resulting events in parser order.
    /// Events emitted by the same chunk share one arrival instant: only the first carries
    /// the elapsed delay and all remaining events carry zero delay.
    /// </summary>
    public int RecordInput(
        TerminalInputParser parser,
        ReadOnlySpan<byte> payload,
        DateTimeOffset? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(parser);
        var parsed = parser.Parse(payload, timestamp);
        if (parsed.Count == 0)
        {
            return 0;
        }

        RecordEvent(parsed[0]);
        for (var index = 1; index < parsed.Count; index++)
        {
            RecordEventWithDelay(parsed[index], TimeSpan.Zero);
        }

        return parsed.Count;
    }

    /// <summary>Parse a chunk with a deterministic delay supplied by the caller.</summary>
    public int RecordInputWithDelay(
        TerminalInputParser parser,
        ReadOnlySpan<byte> payload,
        TimeSpan delay,
        DateTimeOffset? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(parser);
        delay = MacroTime.RequireDuration(delay, nameof(delay));
        var parsed = parser.Parse(payload, timestamp);
        if (parsed.Count == 0)
        {
            return 0;
        }

        RecordEventWithDelay(parsed[0], delay);
        for (var index = 1; index < parsed.Count; index++)
        {
            RecordEventWithDelay(parsed[index], TimeSpan.Zero);
        }

        return parsed.Count;
    }

    public int EventCount => _events.Count;

    public InputMacro Finish() =>
        new(_events, new MacroMetadata(_name, _terminalSize, _recordedDuration));

    internal void ResetClock() => _lastEventTimestamp = Stopwatch.GetTimestamp();
}

/// <summary>Sequential macro player using an explicit event injection callback.</summary>
public sealed class MacroPlayer
{
    private readonly InputMacro _macro;
    private int _position;
    private TimeSpan _elapsed;

    public MacroPlayer(InputMacro m)
    {
        _macro = m ?? throw new ArgumentNullException(nameof(m));
    }

    public int Position => _position;

    public TimeSpan Elapsed => _elapsed;

    public bool IsDone => _position >= _macro.Count;

    public int Remaining => Math.Max(0, _macro.Count - _position);

    /// <summary>
    /// Return and consume the next timed event. This method is retained from the
    /// original managed API; <see cref="Step"/> is the upstream-shaped injection API.
    /// </summary>
    public TimedEvent? Next()
    {
        if (IsDone)
        {
            return null;
        }

        var timed = _macro.Events[_position++];
        _elapsed = MacroTime.SaturatingAdd(_elapsed, timed.Delay);
        return timed;
    }

    public bool Step(Action<TerminalEvent> inject)
    {
        ArgumentNullException.ThrowIfNull(inject);
        var timed = Next();
        if (timed is null)
        {
            return false;
        }

        inject(timed.Event);
        return true;
    }

    public bool Step<TModel, TMessage>(ProgramSimulator<TModel, TMessage> simulator)
        where TModel : class, IModel<TMessage>
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(simulator);
        return Step(simulator.InjectEvent);
    }

    public void ReplayAll(Action<TerminalEvent> inject, Func<bool>? isRunning = null)
    {
        ArgumentNullException.ThrowIfNull(inject);
        while (!IsDone && (isRunning?.Invoke() ?? true))
        {
            Step(inject);
        }
    }

    public void ReplayAll<TModel, TMessage>(ProgramSimulator<TModel, TMessage> simulator)
        where TModel : class, IModel<TMessage>
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(simulator);
        ReplayAll(simulator.InjectEvent, () => simulator.IsRunning);
    }

    public void ReplayWithTiming(Action<TerminalEvent> inject, Func<bool>? isRunning = null) =>
        ReplayWithSleeper(inject, static delay => Thread.Sleep(delay), isRunning);

    public void ReplayWithTiming<TModel, TMessage>(ProgramSimulator<TModel, TMessage> simulator)
        where TModel : class, IModel<TMessage>
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(simulator);
        ReplayWithTiming(simulator.InjectEvent, () => simulator.IsRunning);
    }

    public void ReplayWithSleeper(
        Action<TerminalEvent> inject,
        Action<TimeSpan> sleep,
        Func<bool>? isRunning = null)
    {
        ArgumentNullException.ThrowIfNull(inject);
        ArgumentNullException.ThrowIfNull(sleep);

        while (!IsDone && (isRunning?.Invoke() ?? true))
        {
            var timed = _macro.Events[_position];
            if (timed.Delay > TimeSpan.Zero)
            {
                sleep(timed.Delay);
            }

            Step(inject);
        }
    }

    public void ReplayWithSleeper<TModel, TMessage>(
        ProgramSimulator<TModel, TMessage> simulator,
        Action<TimeSpan> sleep)
        where TModel : class, IModel<TMessage>
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(simulator);
        ReplayWithSleeper(simulator.InjectEvent, sleep, () => simulator.IsRunning);
    }

    public void ReplayUntil(
        Action<TerminalEvent> inject,
        TimeSpan until,
        Func<bool>? isRunning = null)
    {
        ArgumentNullException.ThrowIfNull(inject);
        until = MacroTime.RequireDuration(until, nameof(until));

        while (!IsDone && (isRunning?.Invoke() ?? true))
        {
            var nextElapsed = MacroTime.SaturatingAdd(_elapsed, _macro.Events[_position].Delay);
            if (nextElapsed > until)
            {
                break;
            }

            Step(inject);
        }
    }

    public void ReplayUntil<TModel, TMessage>(
        ProgramSimulator<TModel, TMessage> simulator,
        TimeSpan until)
        where TModel : class, IModel<TMessage>
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(simulator);
        ReplayUntil(simulator.InjectEvent, until, () => simulator.IsRunning);
    }

    public void Reset()
    {
        _position = 0;
        _elapsed = TimeSpan.Zero;
    }
}

/// <summary>Deterministic live scheduler with speed and looping controls.</summary>
public sealed class MacroPlayback
{
    internal const int MaxDueEventsPerAdvance = 4096;

    private readonly InputMacro _macro;
    private int _position;
    private TimeSpan _elapsed;
    private TimeSpan _nextDue;
    private double _speed = 1.0;
    private bool _looping;

    public MacroPlayback(InputMacro macro)
    {
        _macro = macro ?? throw new ArgumentNullException(nameof(macro));
        _nextDue = macro.Events.FirstOrDefault()?.Delay ?? TimeSpan.Zero;
    }

    public double Speed => _speed;

    public bool Looping => _looping;

    public int Position => _position;

    public TimeSpan Elapsed => _elapsed;

    public bool IsDone
    {
        get
        {
            if (_macro.IsEmpty)
            {
                return true;
            }

            if (_looping && _macro.TotalDuration > TimeSpan.Zero)
            {
                return false;
            }

            return _position >= _macro.Count;
        }
    }

    public void SetSpeed(double speed) => _speed = NormalizeSpeed(speed);

    public MacroPlayback WithSpeed(double speed)
    {
        SetSpeed(speed);
        return this;
    }

    public void SetLooping(bool looping) => _looping = looping;

    public MacroPlayback WithLooping(bool looping)
    {
        SetLooping(looping);
        return this;
    }

    public void Reset()
    {
        _position = 0;
        _elapsed = TimeSpan.Zero;
        _nextDue = _macro.Events.FirstOrDefault()?.Delay ?? TimeSpan.Zero;
    }

    public List<TerminalEvent> Advance(TimeSpan delta)
    {
        delta = MacroTime.RequireDuration(delta, nameof(delta));
        if (_macro.IsEmpty || IsDone)
        {
            return [];
        }

        var scaled = MacroTime.Scale(delta, _speed);
        var totalDuration = _macro.TotalDuration;
        if (_looping && totalDuration > TimeSpan.Zero && scaled == TimeSpan.MaxValue)
        {
            // Bound an effectively infinite backlog to one loop window.
            _elapsed = MacroTime.SaturatingAdd(
                MacroTime.Remainder(_elapsed, totalDuration),
                totalDuration);
        }
        else
        {
            _elapsed = MacroTime.SaturatingAdd(_elapsed, scaled);
        }

        return DrainDueEvents();
    }

    private List<TerminalEvent> DrainDueEvents()
    {
        var output = new List<TerminalEvent>();
        var totalDuration = _macro.TotalDuration;
        var canLoop = _looping && totalDuration > TimeSpan.Zero;

        if (canLoop && _position >= _macro.Count)
        {
            _elapsed = MacroTime.Remainder(_elapsed, totalDuration);
            _position = 0;
            _nextDue = _macro.Events[0].Delay;
        }

        while (output.Count < MaxDueEventsPerAdvance
               && _position < _macro.Count
               && _elapsed >= _nextDue)
        {
            var timed = _macro.Events[_position];
            output.Add(timed.Event);
            _position++;

            if (_position < _macro.Count)
            {
                _nextDue = MacroTime.SaturatingAdd(_nextDue, _macro.Events[_position].Delay);
            }
            else if (canLoop)
            {
                _elapsed = MacroTime.SaturatingSubtract(_elapsed, totalDuration);
                _position = 0;
                _nextDue = _macro.Events[0].Delay;
            }
        }

        if (canLoop && output.Count == MaxDueEventsPerAdvance)
        {
            _elapsed = MacroTime.Remainder(_elapsed, totalDuration);
            if (_position >= _macro.Count)
            {
                _position = 0;
                _nextDue = _macro.Events[0].Delay;
            }
        }

        return output;
    }

    private static double NormalizeSpeed(double speed)
    {
        if (!double.IsFinite(speed))
        {
            return 1.0;
        }

        return speed <= 0.0 ? 0.0 : speed;
    }
}

public enum RecordingState
{
    Idle,
    Recording,
    Paused,
}

/// <summary>Stateful live-event recorder layered over <see cref="MacroRecorder"/>.</summary>
public sealed class EventRecorder
{
    private readonly MacroRecorder _inner;
    private RecordingState _state;
    private long? _pauseStarted;
    private TimeSpan _totalPaused;
    private int _eventCount;
    private bool _completed;

    public EventRecorder(string name)
    {
        _inner = new MacroRecorder(name);
    }

    public EventRecorder WithTerminalSize(ushort width, ushort height)
    {
        _inner.WithTerminalSize(width, height);
        return this;
    }

    public RecordingState State => _state;

    public bool IsRecording => _state == RecordingState.Recording;

    public int EventCount => _eventCount;

    public TimeSpan TotalPaused
    {
        get
        {
            var total = _totalPaused;
            if (_pauseStarted is { } pauseStarted)
            {
                total = MacroTime.SaturatingAdd(total, MacroTime.ElapsedSince(pauseStarted));
            }

            return total;
        }
    }

    public void Start()
    {
        EnsureNotCompleted();
        switch (_state)
        {
            case RecordingState.Idle:
                _state = RecordingState.Recording;
                break;
            case RecordingState.Paused:
                Resume();
                break;
        }
    }

    public void Pause()
    {
        EnsureNotCompleted();
        if (_state != RecordingState.Recording)
        {
            return;
        }

        _state = RecordingState.Paused;
        _pauseStarted = Stopwatch.GetTimestamp();
    }

    public void Resume()
    {
        EnsureNotCompleted();
        if (_state != RecordingState.Paused)
        {
            return;
        }

        if (_pauseStarted is { } pauseStarted)
        {
            _totalPaused = MacroTime.SaturatingAdd(_totalPaused, MacroTime.ElapsedSince(pauseStarted));
            _pauseStarted = null;
        }

        _inner.ResetClock();
        _state = RecordingState.Recording;
    }

    public bool Record(TerminalEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        EnsureNotCompleted();
        if (!IsRecording)
        {
            return false;
        }

        _inner.RecordEvent(@event);
        _eventCount++;
        return true;
    }

    public bool RecordWithDelay(TerminalEvent @event, TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(@event);
        EnsureNotCompleted();
        if (!IsRecording)
        {
            return false;
        }

        _inner.RecordEventWithDelay(@event, delay);
        _eventCount++;
        return true;
    }

    public int RecordInput(
        TerminalInputParser parser,
        ReadOnlySpan<byte> payload,
        DateTimeOffset? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(parser);
        EnsureNotCompleted();
        if (!IsRecording)
        {
            return 0;
        }

        var count = _inner.RecordInput(parser, payload, timestamp);
        _eventCount += count;
        return count;
    }

    public int RecordInputWithDelay(
        TerminalInputParser parser,
        ReadOnlySpan<byte> payload,
        TimeSpan delay,
        DateTimeOffset? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(parser);
        EnsureNotCompleted();
        if (!IsRecording)
        {
            return 0;
        }

        var count = _inner.RecordInputWithDelay(parser, payload, delay, timestamp);
        _eventCount += count;
        return count;
    }

    public InputMacro Finish()
    {
        EnsureNotCompleted();
        _completed = true;
        _state = RecordingState.Idle;
        _pauseStarted = null;
        return _inner.Finish();
    }

    public int Discard()
    {
        EnsureNotCompleted();
        _completed = true;
        _state = RecordingState.Idle;
        _pauseStarted = null;
        return _eventCount;
    }

    private void EnsureNotCompleted()
    {
        if (_completed)
        {
            throw new InvalidOperationException("The event recorder has already been finished or discarded.");
        }
    }
}

/// <summary>Controls which terminal-event categories enter a filtered recording.</summary>
public sealed record RecordingFilter
{
    public bool Keys { get; set; } = true;
    public bool Mouse { get; set; } = true;
    public bool Resize { get; set; } = true;
    public bool Paste { get; set; } = true;

    // Reserved for source parity. TerminalEvent currently has no IME union member.
    public bool Ime { get; set; } = true;

    public bool Focus { get; set; } = true;

    // Managed TerminalEvent additionally carries hover stabilization events.
    public bool Hover { get; set; } = true;

    public static RecordingFilter KeysOnly() => new()
    {
        Mouse = false,
        Resize = false,
        Paste = false,
        Ime = false,
        Focus = false,
        Hover = false,
    };

    public bool Accepts(TerminalEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return @event switch
        {
            KeyTerminalEvent => Keys,
            MouseTerminalEvent => Mouse,
            ResizeTerminalEvent => Resize,
            PasteTerminalEvent => Paste,
            FocusTerminalEvent => Focus,
            HoverTerminalEvent => Hover,
            _ => false,
        };
    }
}

/// <summary>Live recorder which counts and rejects events excluded by a filter.</summary>
public sealed class FilteredEventRecorder
{
    private readonly EventRecorder _recorder;
    private readonly RecordingFilter _filter;
    private int _filteredCount;

    public FilteredEventRecorder(string name, RecordingFilter filter)
    {
        _recorder = new EventRecorder(name);
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    public FilteredEventRecorder WithTerminalSize(ushort width, ushort height)
    {
        _recorder.WithTerminalSize(width, height);
        return this;
    }

    public RecordingState State => _recorder.State;

    public bool IsRecording => _recorder.IsRecording;

    public int FilteredCount => _filteredCount;

    public int EventCount => _recorder.EventCount;

    public void Start() => _recorder.Start();

    public void Pause() => _recorder.Pause();

    public void Resume() => _recorder.Resume();

    public bool Record(TerminalEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        if (!_filter.Accepts(@event))
        {
            _filteredCount++;
            return false;
        }

        return _recorder.Record(@event);
    }

    public InputMacro Finish() => _recorder.Finish();
}

internal static class MacroTime
{
    public static TimeSpan RequireDuration(TimeSpan value, string parameterName)
    {
        if (value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A duration cannot be negative.");
        }

        return value;
    }

    public static TimeSpan SaturatingAdd(TimeSpan left, TimeSpan right)
    {
        if (left == TimeSpan.MaxValue || right == TimeSpan.MaxValue
            || left.Ticks > long.MaxValue - right.Ticks)
        {
            return TimeSpan.MaxValue;
        }

        return TimeSpan.FromTicks(left.Ticks + right.Ticks);
    }

    public static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right) =>
        left <= right ? TimeSpan.Zero : left - right;

    public static TimeSpan FromStopwatchTicks(long ticks)
    {
        if (ticks <= 0)
        {
            return TimeSpan.Zero;
        }

        var timeSpanTicks = ticks * (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;
        return timeSpanTicks >= long.MaxValue
            ? TimeSpan.MaxValue
            : TimeSpan.FromTicks((long)timeSpanTicks);
    }

    public static long? ToStopwatchTicks(TimeSpan duration)
    {
        if (duration == TimeSpan.MaxValue)
        {
            return null;
        }

        var ticks = (decimal)duration.Ticks * Stopwatch.Frequency / TimeSpan.TicksPerSecond;
        return ticks > long.MaxValue ? null : (long)ticks;
    }

    public static TimeSpan ElapsedSince(long timestamp)
    {
        var now = Stopwatch.GetTimestamp();
        return now <= timestamp ? TimeSpan.Zero : FromStopwatchTicks(now - timestamp);
    }

    public static TimeSpan Scale(TimeSpan delta, double speed)
    {
        if (delta == TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if (!double.IsFinite(speed))
        {
            speed = 1.0;
        }

        if (speed <= 0.0)
        {
            return TimeSpan.Zero;
        }

        if (speed == 1.0)
        {
            return delta;
        }

        var ticks = delta.Ticks * speed;
        if (double.IsNaN(ticks) || ticks <= 0.0)
        {
            return TimeSpan.Zero;
        }

        return double.IsInfinity(ticks) || ticks >= long.MaxValue
            ? TimeSpan.MaxValue
            : TimeSpan.FromTicks((long)ticks);
    }

    public static TimeSpan Remainder(TimeSpan elapsed, TimeSpan totalDuration)
    {
        if (totalDuration <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromTicks(elapsed.Ticks % totalDuration.Ticks);
    }
}
