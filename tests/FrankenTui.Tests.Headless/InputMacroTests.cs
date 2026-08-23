// Behavioral port tests for frankentui 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source denominator: 53 tests in crates/ftui-runtime/src/input_macro.rs.

using System.Text;
using System.Text.Json;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public sealed class InputMacroTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 16, 1, 2, 3, TimeSpan.FromHours(2));

    [Fact]
    public void TimedEventAndInputMacroPreserveMetadataOrderAndSnapshot()
    {
        var source = new List<TimedEvent>
        {
            new(Key('a'), TimeSpan.FromMilliseconds(10)),
            TimedEvent.Immediate(Key('b')),
        };
        var macro = new InputMacro(
            source,
            new MacroMetadata("ordered", (120, 40), TimeSpan.FromMilliseconds(10)));

        source.Clear();

        Assert.Equal(2, macro.Count);
        Assert.False(macro.IsEmpty);
        Assert.Equal("ordered", macro.Metadata.Name);
        Assert.Equal((120, 40), macro.Metadata.TerminalSize);
        Assert.Equal(TimeSpan.FromMilliseconds(10), macro.TotalDuration);
        Assert.Equal(new[] { Key('a'), Key('b') }, macro.BareEvents);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimedEvent(Key('x'), TimeSpan.FromTicks(-1)));

        // Preserve the positional-record call shape from the original managed stub.
        var updated = new TimedEvent(Event: Key('x'), Delay: TimeSpan.Zero)
            with { Delay = TimeSpan.FromMilliseconds(3) };
        Assert.Equal(TimeSpan.FromMilliseconds(3), updated.Delay);
    }

    [Fact]
    public void FromEventsUsesDefaultMetadataAndImmediateTiming()
    {
        var macro = InputMacro.FromEvents("immediate", new List<TerminalEvent> { Key('a'), Key('b') });

        Assert.Equal((80, 24), macro.Metadata.TerminalSize);
        Assert.Equal(TimeSpan.Zero, macro.TotalDuration);
        Assert.All(macro.Events, timed => Assert.Equal(TimeSpan.Zero, timed.Delay));
        Assert.True(InputMacro.FromEvents("empty", new List<TerminalEvent>()).IsEmpty);
    }

    [Fact]
    public void MacroRecorderExplicitDelaysSaturateAndPreserveTerminalSize()
    {
        var recorder = new MacroRecorder("saturating").WithTerminalSize(w: 132, h: 43);
        recorder.RecordEventWithDelay(e: Key('a'), delay: TimeSpan.MaxValue);
        recorder.RecordEventWithDelay(Key('b'), TimeSpan.FromTicks(1));

        var macro = recorder.Finish();

        Assert.Equal(2, recorder.EventCount);
        Assert.Equal((132, 43), macro.Metadata.TerminalSize);
        Assert.Equal(TimeSpan.MaxValue, macro.TotalDuration);
        Assert.Equal(TimeSpan.MaxValue, macro.Events[0].Delay);
        Assert.Equal(TimeSpan.FromTicks(1), macro.Events[1].Delay);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => recorder.RecordEventWithDelay(Key('c'), TimeSpan.FromTicks(-1)));
    }

    [Fact]
    public void ParserBridgeAssignsArrivalDelayOnceAndPreservesParserOrder()
    {
        var parser = new TerminalInputParser();
        var recorder = new MacroRecorder("parser");

        var count = recorder.RecordInputWithDelay(
            parser,
            Encoding.UTF8.GetBytes("ab"),
            TimeSpan.FromMilliseconds(12),
            Timestamp);
        var macro = recorder.Finish();

        Assert.Equal(2, count);
        Assert.Equal(new[] { TimeSpan.FromMilliseconds(12), TimeSpan.Zero },
            macro.Events.Select(item => item.Delay));
        Assert.Equal(new[] { Key('a'), Key('b') }, macro.BareEvents);
        Assert.All(macro.Events, item => Assert.Equal(Timestamp, item.Event.Timestamp));
    }

    [Fact]
    public void ParserBridgeSupportsChunkedSequencesWithoutInventingEvents()
    {
        var parser = new TerminalInputParser();
        var recorder = new MacroRecorder("chunked");

        Assert.Equal(0, recorder.RecordInputWithDelay(
            parser,
            new byte[] { 0x1b, (byte)'[' },
            TimeSpan.FromMilliseconds(5),
            Timestamp));
        Assert.Equal(1, recorder.RecordInputWithDelay(
            parser,
            new byte[] { (byte)'A' },
            TimeSpan.FromMilliseconds(9),
            Timestamp));

        var timed = Assert.Single(recorder.Finish().Events);
        Assert.Equal(TimeSpan.FromMilliseconds(9), timed.Delay);
        Assert.Equal(TerminalKey.Up, Assert.IsType<KeyTerminalEvent>(timed.Event).Gesture.Key);
    }

    [Fact]
    public void MacroPlayerStepReplayUntilResetAndNextTrackVirtualTime()
    {
        var macro = TimedMacro(
            (Key('a'), 10),
            (Key('b'), 20),
            (Key('c'), 100));
        var player = new MacroPlayer(macro);
        var injected = new List<TerminalEvent>();

        player.ReplayUntil(injected.Add, TimeSpan.FromMilliseconds(50));

        Assert.Equal(new[] { Key('a'), Key('b') }, injected);
        Assert.Equal(2, player.Position);
        Assert.Equal(1, player.Remaining);
        Assert.Equal(TimeSpan.FromMilliseconds(30), player.Elapsed);
        Assert.Equal(Key('c'), player.Next()!.Event);
        Assert.True(player.IsDone);
        Assert.Null(player.Next());

        player.Reset();
        Assert.Equal(0, player.Position);
        Assert.Equal(TimeSpan.Zero, player.Elapsed);
        Assert.True(player.Step(injected.Add));
    }

    [Fact]
    public void MacroPlayerSleeperHonorsPositiveDelaysAndRunningBoundary()
    {
        var macro = TimedMacro(
            (Key('a'), 10),
            (Key('b'), 0),
            (Key('q'), 25),
            (Key('z'), 30));
        var sleeps = new List<TimeSpan>();
        var injected = new List<TerminalEvent>();
        var running = true;

        new MacroPlayer(macro).ReplayWithSleeper(
            @event =>
            {
                injected.Add(@event);
                if (@event == Key('q'))
                {
                    running = false;
                }
            },
            sleeps.Add,
            () => running);

        Assert.Equal(new[] { Key('a'), Key('b'), Key('q') }, injected);
        Assert.Equal(
            new[] { TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(25) },
            sleeps);
    }

    [Fact]
    public void InputMacroReplayWrapperUsesSameInjectionContract()
    {
        var macro = TimedMacro((Key('a'), 5), (Key('b'), 10));
        var events = new List<TerminalEvent>();
        var sleeps = new List<TimeSpan>();

        macro.ReplayWithSleeper(events.Add, sleeps.Add);

        Assert.Equal(new[] { Key('a'), Key('b') }, events);
        Assert.Equal(new[] { TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(10) }, sleeps);
    }

    [Fact]
    public void MacroPlaybackEmitsDueEventsInOrderAndScalesSpeed()
    {
        var macro = TimedMacro((Key('a'), 10), (Key('b'), 10));
        var playback = new MacroPlayback(macro).WithSpeed(2.0);

        Assert.Empty(playback.Advance(TimeSpan.FromMilliseconds(4)));
        Assert.Equal(new[] { Key('a') }, playback.Advance(TimeSpan.FromMilliseconds(1)));
        Assert.Equal(new[] { Key('b') }, playback.Advance(TimeSpan.FromMilliseconds(5)));
        Assert.True(playback.IsDone);
        Assert.Empty(playback.Advance(TimeSpan.FromMilliseconds(10)));
    }

    [Fact]
    public void MacroPlaybackLoopsCarriesOverflowAndGuardsZeroDuration()
    {
        var looping = new MacroPlayback(TimedMacro((Key('a'), 10), (Key('b'), 10)))
            .WithLooping(true);

        Assert.Equal(
            new[] { Key('a'), Key('b'), Key('a'), Key('b'), Key('a') },
            looping.Advance(TimeSpan.FromMilliseconds(50)));
        Assert.False(looping.IsDone);

        var zero = new MacroPlayback(InputMacro.FromEvents(
            "zero",
            new List<TerminalEvent> { Key('a'), Key('b') })).WithLooping(true);
        Assert.Equal(new[] { Key('a'), Key('b') }, zero.Advance(TimeSpan.Zero));
        Assert.True(zero.IsDone);
        Assert.Empty(zero.Advance(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void MacroPlaybackBoundsExtremeSpeedAndBacklog()
    {
        var oneTick = new InputMacro(
            new List<TimedEvent> { new(Key('x'), TimeSpan.FromTicks(1)) },
            new MacroMetadata("bounded", (80, 24), TimeSpan.FromTicks(1)));
        var playback = new MacroPlayback(oneTick)
            .WithSpeed(double.MaxValue)
            .WithLooping(true);

        Assert.Single(playback.Advance(TimeSpan.FromTicks(1)));
        Assert.Single(playback.Advance(TimeSpan.FromTicks(1)));

        playback.SetSpeed(1.0);
        var bounded = playback.Advance(TimeSpan.FromTicks(5_000));
        Assert.Equal(4_096, bounded.Count);
    }

    [Fact]
    public void MacroPlaybackNormalizesInvalidSpeedAndResets()
    {
        var playback = new MacroPlayback(TimedMacro((Key('x'), 10)));

        playback.SetSpeed(double.NaN);
        Assert.Equal(1.0, playback.Speed);
        Assert.Empty(playback.Advance(TimeSpan.FromMilliseconds(9)));
        playback.SetSpeed(-1.0);
        Assert.Equal(0.0, playback.Speed);
        Assert.Empty(playback.Advance(TimeSpan.FromSeconds(1)));
        playback.Reset();
        Assert.Equal(0, playback.Position);
        Assert.Equal(TimeSpan.Zero, playback.Elapsed);
    }

    [Fact]
    public void EventRecorderImplementsIdlePauseResumeFinishAndDiscard()
    {
        var recorder = new EventRecorder("stateful").WithTerminalSize(101, 31);
        Assert.Equal(RecordingState.Idle, recorder.State);
        Assert.False(recorder.Record(Key('x')));

        recorder.Start();
        Assert.True(recorder.RecordWithDelay(Key('a'), TimeSpan.FromMilliseconds(5)));
        recorder.Pause();
        Assert.Equal(RecordingState.Paused, recorder.State);
        Assert.False(recorder.Record(Key('b')));
        recorder.Start(); // Starting a paused recorder resumes it upstream.
        Assert.True(recorder.RecordWithDelay(Key('c'), TimeSpan.FromMilliseconds(7)));

        Assert.Equal(2, recorder.EventCount);
        Assert.True(recorder.TotalPaused >= TimeSpan.Zero);
        var macro = recorder.Finish();
        Assert.Equal((101, 31), macro.Metadata.TerminalSize);
        Assert.Equal(new[] { Key('a'), Key('c') }, macro.BareEvents);

        var discarded = new EventRecorder("discarded");
        discarded.Start();
        discarded.Record(Key('z'));
        Assert.Equal(1, discarded.Discard());
        Assert.Throws<InvalidOperationException>(() => discarded.Finish());
    }

    [Fact]
    public void RecordingFilterAndFilteredRecorderTrackAcceptedAndRejectedEvents()
    {
        var filter = RecordingFilter.KeysOnly();
        var recorder = new FilteredEventRecorder("keys", filter);
        recorder.Start();

        Assert.True(recorder.Record(Key('a')));
        Assert.False(recorder.Record(TerminalEvent.Focus(true, Timestamp)));
        Assert.False(recorder.Record(TerminalEvent.Hover(2, 3, true, Timestamp)));
        recorder.Pause();
        Assert.False(recorder.Record(Key('b')));
        recorder.Resume();
        Assert.True(recorder.Record(Key('c')));

        Assert.Equal(2, recorder.EventCount);
        Assert.Equal(2, recorder.FilteredCount);
        Assert.Equal(new[] { Key('a'), Key('c') }, recorder.Finish().BareEvents);
        Assert.True(new RecordingFilter().Accepts(TerminalEvent.Resize(new Size(80, 24), Timestamp)));
    }

    [Fact]
    public void JsonRoundTripCoversEveryManagedTerminalEventVariant()
    {
        var events = new List<TimedEvent>
        {
            new(TerminalEvent.Key(
                new KeyGesture(TerminalKey.Character, TerminalModifiers.Super | TerminalModifiers.Shift, new Rune(0x1F680)),
                Timestamp,
                TerminalKeyEventKind.Repeat), TimeSpan.FromTicks(1)),
            new(TerminalEvent.Key(
                new KeyGesture(TerminalKey.F24, TerminalModifiers.Control),
                Timestamp,
                TerminalKeyEventKind.Release), TimeSpan.FromTicks(2)),
            new(TerminalEvent.Mouse(
                new MouseGesture(12, 8, TerminalMouseButton.WheelDown, TerminalMouseKind.Scroll, TerminalModifiers.Alt),
                Timestamp), TimeSpan.FromTicks(3)),
            new(TerminalEvent.Resize(new Size(132, 43), Timestamp), TimeSpan.FromTicks(4)),
            new(TerminalEvent.Focus(false, Timestamp), TimeSpan.FromTicks(5)),
            new(TerminalEvent.Paste("line 1\nλ", Timestamp), TimeSpan.FromTicks(6)),
            new(TerminalEvent.Hover(9, 10, true, Timestamp), TimeSpan.FromTicks(7)),
        };
        var macro = new InputMacro(events, new MacroMetadata("all-events", (132, 43), TimeSpan.FromTicks(28)));

        var json = macro.ToJson();
        var replay = InputMacro.FromJson(json);

        Assert.Equal(json, macro.ToJson());
        Assert.Equal(macro.Metadata, replay.Metadata);
        Assert.Equal(macro.Events, replay.Events);
        Assert.Contains("\"schema\":\"frankentui.input-macro\"", json, StringComparison.Ordinal);
        Assert.Contains("\"version\":1", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("version", "\"version\":1", "\"version\":2")]
    [InlineData("negative delay", "\"delayTicks\":1", "\"delayTicks\":-1")]
    [InlineData("unknown event", "\"type\":\"key\"", "\"type\":\"mystery\"")]
    public void JsonRejectsUnsupportedOrInvalidDocuments(string _, string oldText, string newText)
    {
        var macro = new InputMacro(
            new List<TimedEvent> { new(Key('a'), TimeSpan.FromTicks(1)) },
            new MacroMetadata("invalid", (80, 24), TimeSpan.FromTicks(1)));
        var invalid = macro.ToJson().Replace(oldText, newText, StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => InputMacro.FromJson(invalid));
    }

    [Fact]
    public void ProgramRecordingHookCapturesBeforeDispatchBoundaryAndMetadata()
    {
        using var writer = new TerminalWriter(
            new StringWriter(),
            new ScreenMode.AltScreen(),
            UiAnchor.Bottom,
            TerminalCapabilities.Basic());
        var program = new FrankenTui.Runtime.Program<string>(
            new NoopModel(),
            writer,
            new ProgramConfig { ForcedSize = (111, 37) });

        Assert.False(program.IsRecording);
        Assert.False(program.RecordInput(Key('x')));
        program.StartRecording("program");
        Assert.True(program.IsRecording);
        Assert.True(program.RecordInput(Key('a')));
        var macro = program.StopRecording();

        Assert.False(program.IsRecording);
        Assert.NotNull(macro);
        Assert.Equal((111, 37), macro!.Metadata.TerminalSize);
        Assert.Equal(new[] { Key('a') }, macro.BareEvents);
        Assert.Null(program.StopRecording());
    }

    [Fact]
    public void FixedSeedRecorderAndPlayerRoundTripOrderAndElapsed()
    {
        var random = new Random(15_654);
        var recorder = new MacroRecorder("deterministic-property");
        var expectedEvents = new List<TerminalEvent>();
        var expectedTotal = TimeSpan.Zero;

        for (var index = 0; index < 64; index++)
        {
            var @event = Key((char)('a' + random.Next(26)));
            var delay = TimeSpan.FromMilliseconds(random.Next(2_001));
            recorder.RecordEventWithDelay(@event, delay);
            expectedEvents.Add(@event);
            expectedTotal += delay;
        }

        var macro = recorder.Finish();
        var actualEvents = new List<TerminalEvent>();
        var player = new MacroPlayer(macro);
        player.ReplayAll(actualEvents.Add);

        Assert.Equal(expectedEvents, actualEvents);
        Assert.Equal(expectedTotal, macro.TotalDuration);
        Assert.Equal(expectedTotal, player.Elapsed);
    }

    private static KeyTerminalEvent Key(char character) =>
        TerminalEvent.Key(
            new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune(character)),
            Timestamp);

    private static InputMacro TimedMacro(params (TerminalEvent Event, int DelayMilliseconds)[] events)
    {
        var timed = events
            .Select(item => new TimedEvent(item.Event, TimeSpan.FromMilliseconds(item.DelayMilliseconds)))
            .ToList();
        return new InputMacro(
            timed,
            new MacroMetadata(
                "timed",
                (80, 24),
                TimeSpan.FromMilliseconds(events.Sum(item => item.DelayMilliseconds))));
    }

    private sealed class NoopModel : IModel<string>
    {
        public Cmd<string> Init() => Cmd<string>.NoneCmd;

        public Cmd<string> Update(string message) => Cmd<string>.NoneCmd;

        public void View(Frame frame)
        {
        }
    }
}
