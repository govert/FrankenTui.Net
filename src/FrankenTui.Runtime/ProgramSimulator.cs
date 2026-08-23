// SPDX-License-Identifier: Apache-2.0
// Port basis: frankentui 15cc6543f76b814394c590f9e7719dedd6684e4c
// Source: .external/frankentui/crates/ftui-runtime/src/simulator.rs

using FrankenTui.Core;
using FrankenTui.Render;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Runtime;

/// <summary>A deterministic simulator operation error.</summary>
public abstract record SimulatorError
{
    private SimulatorError()
    {
    }

    public sealed record NotRunning : SimulatorError
    {
        public override string ToString() => "simulator is not running";
    }

    public sealed record InactiveSubscription(SubId Id) : SimulatorError
    {
        public override string ToString() => $"subscription {Id} is not active";
    }
}

/// <summary>An observable record of a command executed by the simulator.</summary>
public abstract record CmdRecord
{
    private CmdRecord()
    {
    }

    public sealed record None : CmdRecord;
    public sealed record Quit : CmdRecord;
    public sealed record Msg : CmdRecord;
    public sealed record Batch(int Count) : CmdRecord;
    public sealed record Sequence(int Count) : CmdRecord;
    public sealed record Tick(TimeSpan Duration) : CmdRecord;
    public sealed record Log(string Text) : CmdRecord;
    public sealed record Task : CmdRecord;
    public sealed record MouseCapture(bool Enabled) : CmdRecord;
    public sealed record Error(string Message) : CmdRecord;
    public sealed record Shutdown : CmdRecord;
}

/// <summary>
/// Runs an <see cref="IModel{M}"/> without terminal IO, real time, background
/// subscription threads, or asynchronous tasks.
/// </summary>
/// <remarks>
/// Rust obtains terminal-event and tick conversion through <c>From&lt;Event&gt;</c>.
/// The managed event union has no Tick variant, so those conversions are explicit
/// adapters. Event-free simulation remains usable without either adapter.
/// </remarks>
public sealed class ProgramSimulator<TModel, TMessage>
    where TModel : class, IModel<TMessage>
    where TMessage : class
{
    private readonly GraphemePool _pool = new();
    private readonly List<RenderBuffer> _frames = [];
    private readonly IReadOnlyList<RenderBuffer> _framesView;
    private readonly List<CmdRecord> _commandLog = [];
    private readonly IReadOnlyList<CmdRecord> _commandLogView;
    private readonly List<SubId> _activeSubscriptions = [];
    private readonly IReadOnlyList<SubId> _activeSubscriptionsView;
    private readonly List<string> _logs = [];
    private readonly IReadOnlyList<string> _logsView;
    private readonly List<string> _errors = [];
    private readonly IReadOnlyList<string> _errorsView;
    private readonly Func<TerminalEvent, TMessage>? _eventConverter;
    private readonly Func<TMessage>? _tickFactory;
    private StateRegistry? _stateRegistry;
    private bool _running = true;
    private bool _initialized;
    private bool _shutdownComplete;
    private bool _handlingError;
    private TimeSpan? _tickRate;
    private TimeSpan _now;
    private TimeSpan? _nextTickAt;

    public ProgramSimulator(
        TModel model,
        Func<TerminalEvent, TMessage>? eventConverter = null,
        Func<TMessage>? tickFactory = null)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        _eventConverter = eventConverter;
        _tickFactory = tickFactory;
        _framesView = _frames.AsReadOnly();
        _commandLogView = _commandLog.AsReadOnly();
        _activeSubscriptionsView = _activeSubscriptions.AsReadOnly();
        _logsView = _logs.AsReadOnly();
        _errorsView = _errors.AsReadOnly();
    }

    public static ProgramSimulator<TModel, TMessage> WithRegistry(
        TModel model,
        StateRegistry registry,
        Func<TerminalEvent, TMessage>? eventConverter = null,
        Func<TMessage>? tickFactory = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return new ProgramSimulator<TModel, TMessage>(model, eventConverter, tickFactory)
        {
            _stateRegistry = registry,
        };
    }

    public TModel Model { get; }

    /// <summary>
    /// Alias documenting that the reference may be mutated. One C# reference
    /// realizes Rust's separate shared and mutable borrow accessors.
    /// </summary>
    public TModel MutableModel => Model;

    public GraphemePool Pool => _pool;

    public IReadOnlyList<RenderBuffer> Frames => _framesView;

    public RenderBuffer? LastFrame => _frames.Count == 0 ? null : _frames[^1];

    public int FrameCount => _frames.Count;

    public bool IsRunning => _running;

    public bool IsShutdown => _shutdownComplete;

    public TimeSpan? TickRate => _tickRate;

    public TimeSpan Now => _now;

    public IReadOnlyList<SubId> ActiveSubscriptionIds => _activeSubscriptionsView;

    public IReadOnlyList<string> Logs => _logsView;

    public IReadOnlyList<string> Errors => _errorsView;

    public IReadOnlyList<CmdRecord> CommandLog => _commandLogView;

    public void Init()
    {
        if (_initialized || _shutdownComplete)
        {
            return;
        }

        _initialized = true;
        ExecuteCommand(Model.Init());
        PollSubscriptions();
    }

    public void InjectEvents(IEnumerable<TerminalEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        Func<TerminalEvent, TMessage>? converter = null;
        foreach (var @event in events)
        {
            ArgumentNullException.ThrowIfNull(@event);
            if (!_running)
            {
                break;
            }

            converter ??= RequireEventConverter();
            var command = Model.Update(converter(@event));
            ExecuteCommand(command);
            PollSubscriptions();
        }
    }

    public void InjectEvent(TerminalEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        InjectEvents([@event]);
    }

    public void Send(TMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!_running)
        {
            return;
        }

        var command = Model.Update(message);
        ExecuteCommand(command);
        PollSubscriptions();
    }

    public int AdvanceTime(TimeSpan delta)
    {
        RequireDuration(delta, nameof(delta));
        var target = SaturatingAdd(_now, delta);
        var delivered = 0;

        while (_running && _nextTickAt is { } due && due <= target)
        {
            var tickFactory = RequireTickFactory();
            _now = due;
            _nextTickAt = _tickRate is { } rate && rate > TimeSpan.Zero
                ? CheckedAdd(due, rate)
                : null;
            Send(tickFactory());
            if (delivered < int.MaxValue)
            {
                delivered++;
            }
        }

        _now = target;
        return delivered;
    }

    public void Tick()
    {
        if (_running)
        {
            Send(RequireTickFactory()());
        }
    }

    public IReadOnlyList<SubId> PollSubscriptions()
    {
        if (_shutdownComplete)
        {
            _activeSubscriptions.Clear();
            return _activeSubscriptionsView;
        }

        var ids = Model.Subscriptions()
            .Select(static subscription => subscription.Id)
            .Distinct()
            .OrderBy(static id => id.Value)
            .ToArray();
        _activeSubscriptions.Clear();
        _activeSubscriptions.AddRange(ids);
        return _activeSubscriptionsView;
    }

    /// <summary>Return null on success, otherwise the source-shaped typed error.</summary>
    public SimulatorError? DeliverSubscription(SubId id, TMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!_running)
        {
            return new SimulatorError.NotRunning();
        }

        if (!_activeSubscriptions.Contains(id))
        {
            var error = new SimulatorError.InactiveSubscription(id);
            ReportError(error.ToString());
            return error;
        }

        var command = Model.Update(message);
        ExecuteCommand(command);
        PollSubscriptions();
        return null;
    }

    public void ReportError(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        _errors.Add(error);
        _commandLog.Add(new CmdRecord.Error(error));

        if (_handlingError || _shutdownComplete)
        {
            return;
        }

        _handlingError = true;
        try
        {
            ExecuteLifecycleCommand(Model.OnError(error));
        }
        finally
        {
            _handlingError = false;
        }

        PollSubscriptions();
    }

    public void Cancel()
    {
        _running = false;
        Shutdown();
    }

    public void Shutdown()
    {
        if (_shutdownComplete)
        {
            return;
        }

        _shutdownComplete = true;
        _running = false;
        _commandLog.Add(new CmdRecord.Shutdown());
        ExecuteLifecycleCommand(Model.OnShutdown());
        _activeSubscriptions.Clear();
    }

    public RenderBuffer CaptureFrame(ushort width, ushort height)
    {
        var frame = new Frame(width, height, _pool);
        Model.View(frame);
        _frames.Add(frame.Buffer);
        return frame.Buffer;
    }

    public void ClearFrames() => _frames.Clear();

    public void ClearLogs() => _logs.Clear();

    internal void ExecuteCommand(Cmd<TMessage> command)
    {
        ArgumentNullException.ThrowIfNull(command);
        switch (command)
        {
            case Cmd<TMessage>.None:
                _commandLog.Add(new CmdRecord.None());
                break;

            case Cmd<TMessage>.Quit:
                _running = false;
                _commandLog.Add(new CmdRecord.Quit());
                break;

            case Cmd<TMessage>.Msg message:
                _commandLog.Add(new CmdRecord.Msg());
                ExecuteCommand(Model.Update(message.Message));
                break;

            case Cmd<TMessage>.Batch batch:
                _commandLog.Add(new CmdRecord.Batch(batch.Items.Count));
                foreach (var item in batch.Items)
                {
                    ExecuteCommand(item);
                    if (!_running)
                    {
                        break;
                    }
                }

                break;

            case Cmd<TMessage>.Sequence sequence:
                _commandLog.Add(new CmdRecord.Sequence(sequence.Items.Count));
                foreach (var item in sequence.Items)
                {
                    ExecuteCommand(item);
                    if (!_running)
                    {
                        break;
                    }
                }

                break;

            case Cmd<TMessage>.Tick tick:
                RequireDuration(tick.Duration, nameof(tick.Duration));
                _tickRate = tick.Duration;
                _nextTickAt = tick.Duration == TimeSpan.Zero
                    ? null
                    : CheckedAdd(_now, tick.Duration);
                _commandLog.Add(new CmdRecord.Tick(tick.Duration));
                break;

            case Cmd<TMessage>.Log log:
                _commandLog.Add(new CmdRecord.Log(log.Text));
                _logs.Add(log.Text);
                break;

            case Cmd<TMessage>.SetMouseCapture mouseCapture:
                _commandLog.Add(new CmdRecord.MouseCapture(mouseCapture.Enabled));
                break;

            case Cmd<TMessage>.BackgroundTask backgroundTask:
                _commandLog.Add(new CmdRecord.Task());
                TMessage result;
                try
                {
                    result = backgroundTask.Work();
                }
                catch (Exception exception)
                {
                    ReportError($"background task failed: {exception.Message}");
                    break;
                }

                ExecuteCommand(Model.Update(result));
                break;

            case Cmd<TMessage>.SaveState:
                ExecuteRegistryOperation(static registry => registry.Flush(), "state save failed");
                break;

            case Cmd<TMessage>.RestoreState:
                ExecuteRegistryOperation(static registry => registry.Load(), "state restore failed");
                break;

            case Cmd<TMessage>.SaveStateAndQuit:
                ExecuteRegistryOperation(static registry => registry.Flush(), "state save failed");
                _running = false;
                _commandLog.Add(new CmdRecord.Quit());
                break;

            default:
                throw new NotSupportedException(
                    $"Simulator command '{command.GetType().FullName}' is not supported.");
        }
    }

    private void ExecuteLifecycleCommand(Cmd<TMessage> command)
    {
        var wasRunning = _running;
        _running = true;
        ExecuteCommand(command);
        _running = wasRunning && _running;
    }

    private void ExecuteRegistryOperation<TResult>(
        Func<StateRegistry, TResult> operation,
        string errorPrefix)
    {
        if (_stateRegistry is null)
        {
            return;
        }

        try
        {
            operation(_stateRegistry);
        }
        catch (Exception exception)
        {
            ReportError($"{errorPrefix}: {exception.Message}");
        }
    }

    private Func<TerminalEvent, TMessage> RequireEventConverter() =>
        _eventConverter
        ?? throw new InvalidOperationException(
            "Terminal-event injection requires an eventConverter because the managed message type has no From<Event> trait.");

    private Func<TMessage> RequireTickFactory() =>
        _tickFactory
        ?? throw new InvalidOperationException(
            "Tick delivery requires a tickFactory because TerminalEvent has no Tick variant.");

    private static TimeSpan RequireDuration(TimeSpan value, string parameterName)
    {
        if (value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A duration cannot be negative.");
        }

        return value;
    }

    private static TimeSpan SaturatingAdd(TimeSpan left, TimeSpan right)
    {
        if (left == TimeSpan.MaxValue || right == TimeSpan.MaxValue
            || left.Ticks > long.MaxValue - right.Ticks)
        {
            return TimeSpan.MaxValue;
        }

        return TimeSpan.FromTicks(left.Ticks + right.Ticks);
    }

    private static TimeSpan? CheckedAdd(TimeSpan left, TimeSpan right)
    {
        if (left.Ticks > long.MaxValue - right.Ticks)
        {
            return null;
        }

        return TimeSpan.FromTicks(left.Ticks + right.Ticks);
    }
}
