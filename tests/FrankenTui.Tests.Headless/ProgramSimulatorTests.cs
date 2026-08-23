// Behavioral port tests for frankentui 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source denominator: 50 tests in crates/ftui-runtime/src/simulator.rs.

using System.Reflection;
using System.Text;
using System.Threading.Channels;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class ProgramSimulatorTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 16, 1, 2, 3, TimeSpan.FromHours(2));

    [Fact]
    public void ConstructorExposesSourceDefaultsAndMutableModelAlias()
    {
        var model = new TestModel { Value = 7 };
        var simulator = Create(model);

        Assert.True(simulator.IsRunning);
        Assert.False(simulator.IsShutdown);
        Assert.Equal(TimeSpan.Zero, simulator.Now);
        Assert.Null(simulator.TickRate);
        Assert.Same(model, simulator.Model);
        Assert.Same(model, simulator.MutableModel);
        Assert.NotNull(simulator.Pool);
        Assert.Empty(simulator.Frames);
        Assert.Empty(simulator.Logs);
        Assert.Empty(simulator.Errors);
        Assert.Empty(simulator.CommandLog);
        Assert.Empty(simulator.ActiveSubscriptionIds);

        simulator.MutableModel.Value = 100;
        Assert.Equal(100, simulator.Model.Value);
    }

    [Fact]
    public void InitRunsExactlyOnceBeforeUpdatesAndExecutesItsCommand()
    {
        var model = new TestModel
        {
            InitCommand = Cmd<SimMessage>.MsgCmd(new SimMessage.Step("from-init")),
        };
        var simulator = Create(model);

        simulator.Init();
        simulator.Init();

        Assert.Equal(1, model.InitCalls);
        Assert.True(model.InitSawZeroUpdates);
        Assert.Equal(1, model.UpdateCalls);
        Assert.Equal(new[] { "from-init" }, model.Trace);
        Assert.Collection(
            simulator.CommandLog,
            record => Assert.IsType<CmdRecord.Msg>(record),
            record => Assert.IsType<CmdRecord.None>(record));
    }

    [Fact]
    public void EventInjectionPreservesResizeAndKeyOrderAndStopsAtQuit()
    {
        var model = new TestModel();
        var simulator = Create(model);
        simulator.Init();

        simulator.InjectEvents(
        [
            Resize(80, 24),
            Key('+'),
            Resize(100, 40),
            Key('q'),
            Resize(120, 50),
            Key('+'),
        ]);

        Assert.Equal(1, model.Value);
        Assert.Equal(new[] { new Size(80, 24), new Size(100, 40) }, model.ResizeHistory);
        Assert.False(simulator.IsRunning);
    }

    [Fact]
    public void MissingAdaptersAreExplicitButUnusedBoundariesRemainAvailable()
    {
        var running = new ProgramSimulator<TestModel, SimMessage>(new TestModel());

        var eventError = Assert.Throws<InvalidOperationException>(() => running.InjectEvent(Key('+')));
        Assert.Contains("eventConverter", eventError.Message, StringComparison.Ordinal);
        running.InjectEvents(Array.Empty<TerminalEvent>());

        running.Send(new SimMessage.Quit());
        running.InjectEvent(Key('+'));
        Assert.Equal(0, running.Model.Value);

        var ticking = new ProgramSimulator<TestModel, SimMessage>(new TestModel());
        ticking.ExecuteCommand(Cmd<SimMessage>.TickCmd(TimeSpan.FromMilliseconds(10)));
        var tickError = Assert.Throws<InvalidOperationException>(
            () => ticking.AdvanceTime(TimeSpan.FromMilliseconds(10)));
        Assert.Contains("tickFactory", tickError.Message, StringComparison.Ordinal);
        Assert.Equal(TimeSpan.Zero, ticking.Now);
    }

    [Fact]
    public void SendBatchSequenceAndRecursiveMessagePreserveSynchronousOrder()
    {
        var model = new TestModel();
        var simulator = Create(model);
        simulator.Init();

        simulator.Send(new SimMessage.BatchIncrement(3));
        simulator.Send(new SimMessage.TriggerSequence());
        simulator.Send(new SimMessage.Recursive(0));

        Assert.Equal(3, model.Value);
        Assert.Equal(
            new[] { "seq-1", "seq-2", "seq-3", "recursive:0", "recursive:1", "recursive:2", "recursive:3" },
            model.Trace);
        Assert.Contains(new CmdRecord.Batch(3), simulator.CommandLog);
        Assert.Contains(new CmdRecord.Sequence(3), simulator.CommandLog);
        Assert.Equal(9, simulator.CommandLog.Count(record => record is CmdRecord.Msg));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BatchAndSequenceStopAtQuit(bool sequence)
    {
        var model = new TestModel();
        var simulator = Create(model);
        simulator.Init();

        simulator.Send(sequence
            ? new SimMessage.SequenceWithQuit()
            : new SimMessage.BatchWithQuit());

        Assert.False(simulator.IsRunning);
        Assert.Equal(new[] { "before-quit" }, model.Trace);
        Assert.DoesNotContain("after-quit", model.Trace);
    }

    [Fact]
    public void CommandNormalizationMatchesSourceConstructors()
    {
        Assert.IsType<Cmd<SimMessage>.None>(Cmd<SimMessage>.BatchCmd([]));
        Assert.IsType<Cmd<SimMessage>.Quit>(
            Cmd<SimMessage>.BatchCmd([Cmd<SimMessage>.QuitCmd]));
        Assert.IsType<Cmd<SimMessage>.Batch>(
            Cmd<SimMessage>.BatchCmd([Cmd<SimMessage>.NoneCmd, Cmd<SimMessage>.QuitCmd]));
        Assert.IsType<Cmd<SimMessage>.None>(Cmd<SimMessage>.SequenceCmd([]));
        Assert.IsType<Cmd<SimMessage>.Quit>(
            Cmd<SimMessage>.SequenceCmd([Cmd<SimMessage>.QuitCmd]));
    }

    [Fact]
    public void FramesRenderCorrectlyRemainIndependentAndCanBeCleared()
    {
        var model = new TestModel();
        var simulator = Create(model);

        Assert.Null(simulator.LastFrame);
        var first = simulator.CaptureFrame(20, 3);
        simulator.Send(new SimMessage.Increment());
        var second = simulator.CaptureFrame(12, 2);

        Assert.Equal((20, 3), ((int)first.Width, (int)first.Height));
        Assert.Equal((12, 2), ((int)second.Width, (int)second.Height));
        Assert.Equal("Count: 0", ReadRow(first, 8));
        Assert.Equal("Count: 1", ReadRow(second, 8));
        Assert.Equal(2, simulator.FrameCount);
        Assert.Same(second, simulator.LastFrame);
        Assert.Same(first, simulator.Frames[0]);

        simulator.ClearFrames();
        Assert.Empty(simulator.Frames);
        Assert.Null(simulator.LastFrame);
    }

    [Fact]
    public void CapturedFramesReuseOneGraphemePool()
    {
        var model = new TestModel { RenderText = "e\u0301" };
        var simulator = Create(model);

        for (var index = 0; index < 10; index++)
        {
            simulator.CaptureFrame(5, 1);
        }

        Assert.Equal(10, simulator.FrameCount);
        Assert.Equal(1, simulator.Pool.Count);
        Assert.All(simulator.Frames, frame => Assert.Equal("e\u0301", ReadRow(frame, 1)));
    }

    [Fact]
    public void LogsAccumulateInCommandOrderAndClearIndependently()
    {
        var model = new TestModel();
        var simulator = Create(model);

        simulator.Send(new SimMessage.LogCurrent());
        simulator.Send(new SimMessage.Increment());
        simulator.Send(new SimMessage.LogCurrent());
        simulator.Send(new SimMessage.Increment());
        simulator.Send(new SimMessage.LogCurrent());

        Assert.Equal(new[] { "value=0", "value=1", "value=2" }, simulator.Logs);
        Assert.Equal(
            new[] { "value=0", "value=1", "value=2" },
            simulator.CommandLog.OfType<CmdRecord.Log>().Select(record => record.Text));

        var commandCount = simulator.CommandLog.Count;
        simulator.ClearLogs();
        Assert.Empty(simulator.Logs);
        Assert.Equal(commandCount, simulator.CommandLog.Count);
    }

    [Fact]
    public void PositiveTickRateDeliversEveryExactDeadlineAndManualTickDoesNotAdvanceTime()
    {
        var model = new TestModel();
        var simulator = Create(model);
        simulator.ExecuteCommand(Cmd<SimMessage>.TickCmd(TimeSpan.FromMilliseconds(100)));

        Assert.Equal(0, simulator.AdvanceTime(TimeSpan.FromMilliseconds(99)));
        Assert.Equal(TimeSpan.FromMilliseconds(99), simulator.Now);
        Assert.Equal(1, simulator.AdvanceTime(TimeSpan.FromMilliseconds(1)));
        Assert.Equal(2, simulator.AdvanceTime(TimeSpan.FromMilliseconds(250)));
        Assert.Equal(3, model.TickCount);
        Assert.Equal(TimeSpan.FromMilliseconds(350), simulator.Now);

        simulator.Tick();
        Assert.Equal(4, model.TickCount);
        Assert.Equal(TimeSpan.FromMilliseconds(350), simulator.Now);
    }

    [Fact]
    public void TickDeliveryStopsOnQuitButClockStillAdvancesToRequestedTarget()
    {
        var model = new TestModel { QuitAfterTicks = 1 };
        var simulator = Create(model);
        simulator.ExecuteCommand(Cmd<SimMessage>.TickCmd(TimeSpan.FromMilliseconds(100)));

        Assert.Equal(1, simulator.AdvanceTime(TimeSpan.FromSeconds(1)));
        Assert.Equal(1, model.TickCount);
        Assert.False(simulator.IsRunning);
        Assert.Equal(TimeSpan.FromSeconds(1), simulator.Now);
    }

    [Fact]
    public void ZeroAndMaximumTickIntervalsHaveBoundedDeterministicSemantics()
    {
        var zero = Create(new TestModel());
        zero.ExecuteCommand(Cmd<SimMessage>.TickCmd(TimeSpan.Zero));
        Assert.Equal(TimeSpan.Zero, zero.TickRate);
        Assert.Equal(0, zero.AdvanceTime(TimeSpan.FromDays(1)));
        zero.Tick();
        Assert.Equal(1, zero.Model.TickCount);

        var maximum = Create(new TestModel());
        maximum.ExecuteCommand(Cmd<SimMessage>.TickCmd(TimeSpan.MaxValue));
        Assert.Equal(1, maximum.AdvanceTime(TimeSpan.MaxValue));
        Assert.Equal(TimeSpan.MaxValue, maximum.Now);
        Assert.Equal(0, maximum.AdvanceTime(TimeSpan.Zero));
    }

    [Fact]
    public void NegativeManagedDurationsAreRejectedAtBothPublicBoundaries()
    {
        var simulator = Create(new TestModel());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => simulator.AdvanceTime(TimeSpan.FromTicks(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => simulator.ExecuteCommand(Cmd<SimMessage>.TickCmd(TimeSpan.FromTicks(-1))));
        Assert.Equal(TimeSpan.Zero, simulator.Now);
        Assert.Null(simulator.TickRate);
    }

    [Fact]
    public void BackgroundTasksExecuteSynchronouslyAndRouteResultsThroughUpdate()
    {
        var model = new TestModel();
        var simulator = Create(model);

        simulator.Send(new SimMessage.SpawnTask());

        Assert.Equal(42, model.TaskResult);
        Assert.Equal(new[] { "task:spawn", "task:done:42" }, model.Trace);
        Assert.Contains(simulator.CommandLog, record => record is CmdRecord.Task);
    }

    [Fact]
    public void BackgroundTaskFailureBecomesEvidenceAndInvokesErrorHookOnce()
    {
        var model = new TestModel
        {
            ErrorCommand = error => Cmd<SimMessage>.MsgCmd(new SimMessage.ErrorHandled(error)),
        };
        var simulator = Create(model);

        simulator.Send(new SimMessage.SpawnFailingTask());

        var error = Assert.Single(simulator.Errors);
        Assert.Equal("background task failed: boom", error);
        Assert.Equal(1, model.ErrorCalls);
        Assert.Equal(new[] { error }, model.HookErrors);
        Assert.Equal(new[] { $"handled:{error}" }, model.Trace);
        Assert.Contains(new CmdRecord.Error(error), simulator.CommandLog);
    }

    [Fact]
    public void ErrorHookRecursionIsBoundedWhileBothErrorsRemainObservable()
    {
        var model = new TestModel
        {
            ErrorCommand = _ => Cmd<SimMessage>.TaskCmd(
                () => throw new InvalidOperationException("inner")),
        };
        var simulator = Create(model);

        simulator.ReportError("outer");

        Assert.Equal(1, model.ErrorCalls);
        Assert.Equal(
            new[] { "outer", "background task failed: inner" },
            simulator.Errors);
        Assert.Equal(2, simulator.CommandLog.Count(record => record is CmdRecord.Error));
    }

    [Fact]
    public void SubscriptionsAreSortedDeduplicatedReconciledAndDeliveredExplicitly()
    {
        var model = new TestModel { SubscriptionValues = [9, 2, 9, 4] };
        var simulator = Create(model);

        Assert.Equal(new ulong[] { 2, 4, 9 },
            simulator.PollSubscriptions().Select(id => id.Value));
        Assert.Null(simulator.DeliverSubscription(new SubId(4), new SimMessage.Increment()));
        Assert.Equal(1, model.Value);

        simulator.Send(new SimMessage.SetSubscriptions([7, 3, 7]));
        Assert.Equal(new ulong[] { 3, 7 }, simulator.ActiveSubscriptionIds.Select(id => id.Value));
    }

    [Fact]
    public void SubscriptionFailuresDistinguishInactiveFromNotRunning()
    {
        var model = new TestModel { SubscriptionValues = [2] };
        var simulator = Create(model);
        simulator.PollSubscriptions();

        var inactive = Assert.IsType<SimulatorError.InactiveSubscription>(
            simulator.DeliverSubscription(new SubId(7), new SimMessage.Increment()));
        Assert.Equal(new SubId(7), inactive.Id);
        Assert.Equal("subscription 7 is not active", inactive.ToString());
        Assert.Single(simulator.Errors);
        Assert.Equal(1, model.ErrorCalls);

        simulator.Send(new SimMessage.Quit());
        var errorCount = simulator.Errors.Count;
        var notRunning = Assert.IsType<SimulatorError.NotRunning>(
            simulator.DeliverSubscription(new SubId(2), new SimMessage.Increment()));
        Assert.Equal("simulator is not running", notRunning.ToString());
        Assert.Equal(errorCount, simulator.Errors.Count);
        Assert.Equal(0, model.Value);
    }

    [Fact]
    public void ShutdownAndCancelRunFinalCommandsExactlyOnceAndClearSubscriptions()
    {
        var model = new TestModel
        {
            SubscriptionValues = [4],
            ShutdownCommand = Cmd<SimMessage>.MsgCmd(new SimMessage.Step("shutdown-complete")),
        };
        var simulator = Create(model);
        simulator.Init();
        Assert.Single(simulator.ActiveSubscriptionIds);

        simulator.Send(new SimMessage.Quit());
        simulator.Shutdown();
        simulator.Shutdown();
        simulator.Cancel();
        simulator.Init();

        Assert.True(simulator.IsShutdown);
        Assert.False(simulator.IsRunning);
        Assert.Equal(1, model.ShutdownCalls);
        Assert.Equal(1, model.InitCalls);
        Assert.Equal(new[] { "shutdown-complete" }, model.Trace);
        Assert.Empty(simulator.ActiveSubscriptionIds);
        Assert.Equal(1, simulator.CommandLog.Count(record => record is CmdRecord.Shutdown));
    }

    [Fact]
    public void RegistrySaveAndRestoreRoundTripWhileAbsentRegistryIsANoop()
    {
        var registry = StateRegistry.InMemory();
        registry.Set("viewer", 7, [9, 8, 7]);
        var simulator = ProgramSimulator<TestModel, SimMessage>.WithRegistry(
            new TestModel(), registry, ConvertEvent, static () => new SimMessage.TickPulse());

        simulator.Send(new SimMessage.Save());
        Assert.False(registry.IsDirty);
        Assert.NotNull(registry.Remove("viewer"));
        Assert.Null(registry.Get("viewer"));
        simulator.Send(new SimMessage.Restore());

        var restored = Assert.IsType<StoredEntry>(registry.Get("viewer"));
        Assert.Equal((uint)7, restored.Version);
        Assert.Equal(new byte[] { 9, 8, 7 }, restored.Data);
        Assert.Empty(simulator.CommandLog);

        var noRegistry = Create(new TestModel { Value = 7 });
        noRegistry.ExecuteCommand(Cmd<SimMessage>.SaveStateCmd);
        noRegistry.ExecuteCommand(Cmd<SimMessage>.RestoreStateCmd);
        Assert.Empty(noRegistry.CommandLog);
        Assert.Equal(7, noRegistry.Model.Value);
        Assert.True(noRegistry.IsRunning);
    }

    [Fact]
    public void RegistryFailuresFlowThroughTheSameErrorContract()
    {
        var registry = new StateRegistry(new FailingStorage());
        registry.Set("dirty", 1, [1]);
        var model = new TestModel();
        var simulator = ProgramSimulator<TestModel, SimMessage>.WithRegistry(
            model, registry, ConvertEvent, static () => new SimMessage.TickPulse());

        simulator.Send(new SimMessage.Save());
        simulator.Send(new SimMessage.Restore());

        Assert.Equal(2, model.ErrorCalls);
        Assert.Collection(
            simulator.Errors,
            error => Assert.StartsWith("state save failed:", error, StringComparison.Ordinal),
            error => Assert.StartsWith("state restore failed:", error, StringComparison.Ordinal));
    }

    [Fact]
    public void TargetOnlySaveAndQuitExtensionPreservesSimulatorStopBoundary()
    {
        var registry = StateRegistry.InMemory();
        registry.Set("dirty", 1, [1]);
        var simulator = ProgramSimulator<TestModel, SimMessage>.WithRegistry(
            new TestModel(), registry, ConvertEvent, static () => new SimMessage.TickPulse());

        simulator.Send(new SimMessage.SaveQuit());

        Assert.False(registry.IsDirty);
        Assert.False(simulator.IsRunning);
        Assert.IsType<CmdRecord.Quit>(Assert.Single(simulator.CommandLog));
    }

    [Fact]
    public void MouseCaptureCommandsAreObservableNoOpsInSimulatorMode()
    {
        var simulator = Create(new TestModel());

        simulator.Send(new SimMessage.MouseCapture(true));
        simulator.Send(new SimMessage.MouseCapture(false));

        Assert.True(simulator.IsRunning);
        Assert.Collection(
            simulator.CommandLog,
            record => Assert.Equal(new CmdRecord.MouseCapture(true), record),
            record => Assert.Equal(new CmdRecord.MouseCapture(false), record));
    }

    [Fact]
    public void InputMacroDirectSimulatorBridgeHonorsTimingAndQuitBoundary()
    {
        var macro = TimedMacro((Key('+'), 5), (Key('+'), 0), (Key('q'), 10), (Key('+'), 20));
        var simulator = Create(new TestModel());
        var sleeps = new List<TimeSpan>();

        macro.ReplayWithSleeper(simulator, sleeps.Add);

        Assert.Equal(2, simulator.Model.Value);
        Assert.False(simulator.IsRunning);
        Assert.Equal(
            new[] { TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(10) },
            sleeps);
    }

    [Fact]
    public void MacroPlayerDirectSimulatorStepReplayAllAndReplayUntilShareOneContract()
    {
        var macro = TimedMacro((Key('+'), 5), (Key('+'), 10), (Key('+'), 20));
        var simulator = Create(new TestModel());
        var player = new MacroPlayer(macro);

        Assert.True(player.Step(simulator));
        player.ReplayUntil(simulator, TimeSpan.FromMilliseconds(15));
        Assert.Equal(2, simulator.Model.Value);
        Assert.Equal(2, player.Position);

        player.ReplayAll(simulator);
        Assert.Equal(3, simulator.Model.Value);
        Assert.True(player.IsDone);
    }

    [Fact]
    public void RepeatedScenarioProducesIdenticalStateFramesLogsAndCommandRecords()
    {
        static (int Value, string Frame, string Logs, string Commands) Run()
        {
            var simulator = Create(new TestModel());
            simulator.Init();
            simulator.Send(new SimMessage.Increment());
            simulator.Send(new SimMessage.Increment());
            simulator.Send(new SimMessage.Decrement());
            simulator.Send(new SimMessage.BatchIncrement(3));
            simulator.Send(new SimMessage.LogCurrent());
            var frame = simulator.CaptureFrame(20, 2);
            return (
                simulator.Model.Value,
                ReadRow(frame, 8),
                string.Join("\n", simulator.Logs),
                string.Join("\n", simulator.CommandLog.Select(Describe)));
        }

        var first = Run();
        var second = Run();
        var third = Run();

        Assert.Equal(4, first.Value);
        Assert.Equal(first, second);
        Assert.Equal(second, third);
    }

    [Fact]
    public void PublicRecordVariantSetsMatchTheUpstreamDenominator()
    {
        Assert.Equal(
            new[] { "Batch", "Error", "Log", "MouseCapture", "Msg", "None", "Quit", "Sequence", "Shutdown", "Task", "Tick" },
            PublicNestedTypeNames(typeof(CmdRecord)));
        Assert.Equal(
            new[] { "InactiveSubscription", "NotRunning" },
            PublicNestedTypeNames(typeof(SimulatorError)));

        Assert.Equal(new SimulatorError.NotRunning(), new SimulatorError.NotRunning());
        Assert.Equal(
            new SimulatorError.InactiveSubscription(new SubId(5)),
            new SimulatorError.InactiveSubscription(new SubId(5)));
    }

    private static ProgramSimulator<TestModel, SimMessage> Create(TestModel model) =>
        new(model, ConvertEvent, static () => new SimMessage.TickPulse());

    private static SimMessage ConvertEvent(TerminalEvent @event) => @event switch
    {
        KeyTerminalEvent { Gesture.Character.Value: (int)'+' } => new SimMessage.Increment(),
        KeyTerminalEvent { Gesture.Character.Value: (int)'-' } => new SimMessage.Decrement(),
        KeyTerminalEvent { Gesture.Character.Value: (int)'r' } => new SimMessage.Reset(),
        KeyTerminalEvent { Gesture.Character.Value: (int)'q' } => new SimMessage.Quit(),
        ResizeTerminalEvent resize => new SimMessage.Resize(resize.Size),
        _ => new SimMessage.Noop(),
    };

    private static KeyTerminalEvent Key(char character) =>
        TerminalEvent.Key(
            new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune(character)),
            Timestamp);

    private static ResizeTerminalEvent Resize(ushort width, ushort height) =>
        TerminalEvent.Resize(new Size(width, height), Timestamp);

    private static InputMacro TimedMacro(params (TerminalEvent Event, int DelayMilliseconds)[] events)
    {
        var timed = events
            .Select(item => new TimedEvent(item.Event, TimeSpan.FromMilliseconds(item.DelayMilliseconds)))
            .ToList();
        return new InputMacro(
            timed,
            new MacroMetadata(
                "simulator",
                (80, 24),
                TimeSpan.FromMilliseconds(events.Sum(item => item.DelayMilliseconds))));
    }

    private static string ReadRow(RenderBuffer buffer, int cells)
    {
        var result = new StringBuilder();
        for (ushort x = 0; x < Math.Min(cells, buffer.Width); x++)
        {
            if (buffer.Get(x, 0) is { } cell)
            {
                result.Append(buffer.ResolveText(cell));
            }
        }

        return result.ToString();
    }

    private static string Describe(CmdRecord record) => record switch
    {
        CmdRecord.None => "None",
        CmdRecord.Quit => "Quit",
        CmdRecord.Msg => "Msg",
        CmdRecord.Batch batch => $"Batch:{batch.Count}",
        CmdRecord.Sequence sequence => $"Sequence:{sequence.Count}",
        CmdRecord.Tick tick => $"Tick:{tick.Duration.Ticks}",
        CmdRecord.Log log => $"Log:{log.Text}",
        CmdRecord.Task => "Task",
        CmdRecord.MouseCapture capture => $"MouseCapture:{capture.Enabled}",
        CmdRecord.Error error => $"Error:{error.Message}",
        CmdRecord.Shutdown => "Shutdown",
        _ => throw new ArgumentOutOfRangeException(nameof(record)),
    };

    private static string[] PublicNestedTypeNames(Type type) =>
        type.GetNestedTypes(BindingFlags.Public)
            .Select(nested => nested.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private abstract record SimMessage
    {
        public sealed record Noop : SimMessage;
        public sealed record Increment : SimMessage;
        public sealed record Decrement : SimMessage;
        public sealed record Reset : SimMessage;
        public sealed record Quit : SimMessage;
        public sealed record LogCurrent : SimMessage;
        public sealed record BatchIncrement(int Count) : SimMessage;
        public sealed record TriggerSequence : SimMessage;
        public sealed record Step(string Tag) : SimMessage;
        public sealed record Recursive(int Value) : SimMessage;
        public sealed record ScheduleTick(TimeSpan Duration) : SimMessage;
        public sealed record TickPulse : SimMessage;
        public sealed record SpawnTask : SimMessage;
        public sealed record SpawnFailingTask : SimMessage;
        public sealed record TaskDone(int Value) : SimMessage;
        public sealed record Resize(Size Size) : SimMessage;
        public sealed record BatchWithQuit : SimMessage;
        public sealed record SequenceWithQuit : SimMessage;
        public sealed record SetSubscriptions(ulong[] Ids) : SimMessage;
        public sealed record ErrorHandled(string Error) : SimMessage;
        public sealed record Save : SimMessage;
        public sealed record Restore : SimMessage;
        public sealed record MouseCapture(bool Enabled) : SimMessage;
        public sealed record SaveQuit : SimMessage;
    }

    private sealed class TestModel : IModel<SimMessage>
    {
        public int Value { get; set; }
        public bool InitSawZeroUpdates { get; private set; }
        public int InitCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public int ShutdownCalls { get; private set; }
        public int ErrorCalls { get; private set; }
        public int TickCount { get; private set; }
        public int QuitAfterTicks { get; init; }
        public int? TaskResult { get; private set; }
        public string? RenderText { get; init; }
        public ulong[] SubscriptionValues { get; set; } = [];
        public List<string> Trace { get; } = [];
        public List<Size> ResizeHistory { get; } = [];
        public List<string> HookErrors { get; } = [];
        public Cmd<SimMessage> InitCommand { get; init; } = Cmd<SimMessage>.NoneCmd;
        public Cmd<SimMessage> ShutdownCommand { get; init; } = Cmd<SimMessage>.NoneCmd;
        public Func<string, Cmd<SimMessage>> ErrorCommand { get; init; } =
            static _ => Cmd<SimMessage>.NoneCmd;

        public Cmd<SimMessage> Init()
        {
            InitCalls++;
            InitSawZeroUpdates = UpdateCalls == 0;
            return InitCommand;
        }

        public Cmd<SimMessage> Update(SimMessage message)
        {
            UpdateCalls++;
            return message switch
            {
                SimMessage.Noop => Cmd<SimMessage>.NoneCmd,
                SimMessage.Increment => Increment(),
                SimMessage.Decrement => Decrement(),
                SimMessage.Reset => Reset(),
                SimMessage.Quit => Cmd<SimMessage>.QuitCmd,
                SimMessage.LogCurrent => Cmd<SimMessage>.LogCmd($"value={Value}"),
                SimMessage.BatchIncrement batch => BatchIncrement(batch.Count),
                SimMessage.TriggerSequence => Cmd<SimMessage>.SequenceCmd(
                [
                    Cmd<SimMessage>.MsgCmd(new SimMessage.Step("seq-1")),
                    Cmd<SimMessage>.MsgCmd(new SimMessage.Step("seq-2")),
                    Cmd<SimMessage>.MsgCmd(new SimMessage.Step("seq-3")),
                ]),
                SimMessage.Step step => Record(step.Tag),
                SimMessage.Recursive recursive => Recurse(recursive.Value),
                SimMessage.ScheduleTick tick => Cmd<SimMessage>.TickCmd(tick.Duration),
                SimMessage.TickPulse => Tick(),
                SimMessage.SpawnTask => SpawnTask(),
                SimMessage.SpawnFailingTask => Cmd<SimMessage>.TaskCmd(
                    () => throw new InvalidOperationException("boom")),
                SimMessage.TaskDone done => CompleteTask(done.Value),
                SimMessage.Resize resize => RecordResize(resize.Size),
                SimMessage.BatchWithQuit => QuitComposite(sequence: false),
                SimMessage.SequenceWithQuit => QuitComposite(sequence: true),
                SimMessage.SetSubscriptions subscriptions => SetSubscriptions(subscriptions.Ids),
                SimMessage.ErrorHandled handled => Record($"handled:{handled.Error}"),
                SimMessage.Save => Cmd<SimMessage>.SaveStateCmd,
                SimMessage.Restore => Cmd<SimMessage>.RestoreStateCmd,
                SimMessage.MouseCapture capture => Cmd<SimMessage>.SetMouseCaptureCmd(capture.Enabled),
                SimMessage.SaveQuit => Cmd<SimMessage>.SaveStateAndQuitCmd,
                _ => throw new ArgumentOutOfRangeException(nameof(message)),
            };
        }

        public void View(Frame frame)
        {
            if (RenderText is not null)
            {
                frame.Buffer.SetText(0, 0, RenderText, Cell.FromChar(' '));
                return;
            }

            var text = $"Count: {Value}";
            for (var index = 0; index < Math.Min(text.Length, frame.Width); index++)
            {
                frame.Buffer.SetFast((ushort)index, 0, Cell.FromChar(text[index]));
            }
        }

        public List<ISubscription<SimMessage>> Subscriptions() =>
            SubscriptionValues
                .Select(value => (ISubscription<SimMessage>)new StubSubscription(new SubId(value)))
                .ToList();

        public Cmd<SimMessage> OnShutdown()
        {
            ShutdownCalls++;
            return ShutdownCommand;
        }

        public Cmd<SimMessage> OnError(string error)
        {
            ErrorCalls++;
            HookErrors.Add(error);
            return ErrorCommand(error);
        }

        private Cmd<SimMessage> Increment()
        {
            Value++;
            return Cmd<SimMessage>.NoneCmd;
        }

        private Cmd<SimMessage> Decrement()
        {
            Value--;
            return Cmd<SimMessage>.NoneCmd;
        }

        private Cmd<SimMessage> Reset()
        {
            Value = 0;
            return Cmd<SimMessage>.NoneCmd;
        }

        private static Cmd<SimMessage> BatchIncrement(int count) =>
            Cmd<SimMessage>.BatchCmd(
                Enumerable.Range(0, count)
                    .Select(static _ => Cmd<SimMessage>.MsgCmd(new SimMessage.Increment()))
                    .ToList());

        private Cmd<SimMessage> Record(string tag)
        {
            Trace.Add(tag);
            return Cmd<SimMessage>.NoneCmd;
        }

        private Cmd<SimMessage> Recurse(int value)
        {
            Trace.Add($"recursive:{value}");
            return value < 3
                ? Cmd<SimMessage>.MsgCmd(new SimMessage.Recursive(value + 1))
                : Cmd<SimMessage>.NoneCmd;
        }

        private Cmd<SimMessage> Tick()
        {
            TickCount++;
            return QuitAfterTicks > 0 && TickCount >= QuitAfterTicks
                ? Cmd<SimMessage>.QuitCmd
                : Cmd<SimMessage>.NoneCmd;
        }

        private Cmd<SimMessage> SpawnTask()
        {
            Trace.Add("task:spawn");
            return Cmd<SimMessage>.TaskCmd(static () => new SimMessage.TaskDone(42));
        }

        private Cmd<SimMessage> CompleteTask(int value)
        {
            TaskResult = value;
            Trace.Add($"task:done:{value}");
            return Cmd<SimMessage>.NoneCmd;
        }

        private Cmd<SimMessage> RecordResize(Size size)
        {
            ResizeHistory.Add(size);
            return Cmd<SimMessage>.NoneCmd;
        }

        private static Cmd<SimMessage> QuitComposite(bool sequence)
        {
            List<Cmd<SimMessage>> commands =
            [
                Cmd<SimMessage>.MsgCmd(new SimMessage.Step("before-quit")),
                Cmd<SimMessage>.QuitCmd,
                Cmd<SimMessage>.MsgCmd(new SimMessage.Step("after-quit")),
            ];
            return sequence
                ? Cmd<SimMessage>.SequenceCmd(commands)
                : Cmd<SimMessage>.BatchCmd(commands);
        }

        private Cmd<SimMessage> SetSubscriptions(ulong[] ids)
        {
            SubscriptionValues = (ulong[])ids.Clone();
            return Cmd<SimMessage>.NoneCmd;
        }
    }

    private sealed class StubSubscription(SubId id) : ISubscription<SimMessage>
    {
        public SubId Id { get; } = id;

        public void Run(ChannelWriter<SimMessage> writer, StopSignal stop)
        {
        }
    }

    private sealed class FailingStorage : IStorageBackend
    {
        public string Name => nameof(FailingStorage);

        public IReadOnlyDictionary<string, StoredEntry> LoadAll() =>
            throw new StorageUnavailableException("load unavailable");

        public void SaveAll(IReadOnlyDictionary<string, StoredEntry> entries) =>
            throw new StorageUnavailableException("save unavailable");

        public void Clear()
        {
        }
    }
}
