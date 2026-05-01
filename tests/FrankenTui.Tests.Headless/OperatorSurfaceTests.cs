using System.Text;
using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Headless;

public sealed class OperatorSurfaceTests
{
    [Fact]
    public void PaneWorkspaceReplaysDeterministicallyAndRoundTrips()
    {
        var start = new DateTimeOffset(2026, 3, 11, 9, 0, 0, TimeSpan.Zero);
        var baseline = PaneWorkspaceState.CreateDemo();
        PaneWorkspaceAction[] actions =
        [
            new PaneWorkspaceAction(PaneWorkspaceActionKind.SelectNext, start, "test"),
            new PaneWorkspaceAction(PaneWorkspaceActionKind.CycleMode, start + TimeSpan.FromMilliseconds(16), "test"),
            new PaneWorkspaceAction(PaneWorkspaceActionKind.GrowPrimary, start + TimeSpan.FromMilliseconds(32), "test")
        ];

        var replayed = baseline.Replay(actions);
        var roundTrip = PaneWorkspaceState.FromJson(replayed.ToJson());

        Assert.Equal(replayed.SnapshotHash(), roundTrip.SnapshotHash());
        Assert.Equal(PaneWorkspaceMode.Monitor, replayed.Mode);
        Assert.Equal(550, replayed.PrimaryRatioPermille);
    }

    [Fact]
    public void PaneWorkspaceSupportsUndoRedoAcrossTimeline()
    {
        var start = new DateTimeOffset(2026, 3, 12, 7, 0, 0, TimeSpan.Zero);
        var state = PaneWorkspaceState.CreateDemo()
            .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.SelectNext, start, "test"))
            .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.GrowPrimary, start + TimeSpan.FromMilliseconds(16), "test"));

        var undone = state.Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.Undo, start + TimeSpan.FromMilliseconds(32), "test"));
        var redone = undone.Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.Redo, start + TimeSpan.FromMilliseconds(48), "test"));

        Assert.Equal(500, undone.PrimaryRatioPermille);
        Assert.Equal(550, redone.PrimaryRatioPermille);
        Assert.Equal(state.SnapshotHash(), redone.SnapshotHash());
    }

    [Fact]
    public void PaneWorkspaceRecordsReplayCheckpointsAndDiagnostics()
    {
        var start = new DateTimeOffset(2026, 3, 12, 7, 30, 0, TimeSpan.Zero);
        var state = PaneWorkspaceState.CreateDemo();

        for (var index = 0; index < 16; index++)
        {
            state = state.Apply(
                new PaneWorkspaceAction(
                    index % 2 == 0 ? PaneWorkspaceActionKind.SelectNext : PaneWorkspaceActionKind.GrowPrimary,
                    start + TimeSpan.FromMilliseconds(index * 16),
                    "checkpoint"));
        }

        var diagnostics = state.ReplayDiagnostics();

        Assert.Single(state.Checkpoints);
        Assert.Equal(16, state.Checkpoints[0].AppliedCount);
        Assert.True(diagnostics.CheckpointHit);
        Assert.Equal(16, diagnostics.ReplayStartIndex);
        Assert.Equal(0, diagnostics.ReplayDepth);
    }

    [Fact]
    public void PaneWorkspaceDiscardsStaleCheckpointsWhenBranchingAfterUndo()
    {
        var start = new DateTimeOffset(2026, 3, 12, 8, 0, 0, TimeSpan.Zero);
        var state = PaneWorkspaceState.CreateDemo();

        for (var index = 0; index < 20; index++)
        {
            state = state.Apply(
                new PaneWorkspaceAction(
                    index % 3 == 0 ? PaneWorkspaceActionKind.CycleMode : PaneWorkspaceActionKind.SelectNext,
                    start + TimeSpan.FromMilliseconds(index * 16),
                    "timeline"));
        }

        Assert.Single(state.Checkpoints);

        state = state.Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.Undo, start + TimeSpan.FromSeconds(1), "timeline"));
        state = state.Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.Undo, start + TimeSpan.FromSeconds(1.1), "timeline"));
        state = state.Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.GrowPrimary, start + TimeSpan.FromSeconds(1.2), "timeline"));

        Assert.Equal(state.Timeline.Count, state.TimelineCursor);
        Assert.Single(state.Checkpoints);
        Assert.Equal(16, state.Checkpoints[0].AppliedCount);
    }

    [Fact]
    public void PaneWorkspaceCheckpointDecisionPrefersShorterIntervalsForExpensiveReplay()
    {
        var slowerReplay = PaneWorkspaceState.CheckpointDecision(10_000, 2_500);
        var cheaperReplay = PaneWorkspaceState.CheckpointDecision(10_000, 100);

        Assert.True(slowerReplay.CheckpointInterval < cheaperReplay.CheckpointInterval);
        Assert.True(slowerReplay.EstimatedReplayDepthNs > 0);
    }

    [Fact]
    public void PaneWorkspaceCanonicalJsonCorpusRoundTripsByteStably()
    {
        var start = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var cases = new Dictionary<string, PaneWorkspaceState>
        {
            ["default"] = PaneWorkspaceState.CreateDemo(),
            ["mode-and-ratio"] = PaneWorkspaceState.CreateDemo()
                .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.CycleMode, start, "corpus"))
                .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.GrowPrimary, start + TimeSpan.FromMilliseconds(16), "corpus")),
            ["undo-redo-cursor"] = PaneWorkspaceState.CreateDemo()
                .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.SelectNext, start, "corpus"))
                .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.SelectPrevious, start + TimeSpan.FromMilliseconds(16), "corpus"))
                .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.Undo, start + TimeSpan.FromMilliseconds(32), "corpus"))
                .Apply(new PaneWorkspaceAction(PaneWorkspaceActionKind.Redo, start + TimeSpan.FromMilliseconds(48), "corpus"))
        };

        foreach (var (name, state) in cases)
        {
            var firstJson = state.ToCanonicalJson();
            var first = PaneWorkspaceState.DecodeJson(firstJson);
            var secondJson = first.State.ToCanonicalJson();
            var second = PaneWorkspaceState.DecodeJson(secondJson);

            Assert.Equal(PaneWorkspaceState.CurrentJsonSchemaVersion, first.FromVersion);
            Assert.Equal(PaneWorkspaceState.CurrentJsonSchemaVersion, first.ToVersion);
            Assert.False(first.MigrationApplied);
            Assert.Equal("current_schema", first.Decision);
            Assert.Empty(first.Warnings);
            Assert.Equal(first.StateChecksum, second.StateChecksum);
            Assert.Equal(firstJson, secondJson);
            Assert.Equal(state.SnapshotHash(), first.State.SnapshotHash());
        }
    }

    [Fact]
    public void PaneWorkspaceCanonicalJsonRejectsInvalidCorpusCases()
    {
        var missingSelection = PaneWorkspaceState.CreateDemo() with
        {
            SelectedPaneId = "missing-pane"
        };
        var badCursor = PaneWorkspaceState.CreateDemo() with
        {
            TimelineCursor = 99
        };

        var missingSelectionError = Assert.Throws<InvalidOperationException>(() => missingSelection.ToCanonicalJson());
        var badCursorError = Assert.Throws<InvalidOperationException>(() => badCursor.ToCanonicalJson());

        Assert.Contains("selected pane", missingSelectionError.Message);
        Assert.Contains("timeline cursor", badCursorError.Message);
    }

    [Fact]
    public void CommandPaletteRanksDeterministically()
    {
        var entries = CommandPaletteRegistry.DefaultEntries();

        var exact = CommandPaletteSearch.Search(entries, "Go to Dashboard");
        Assert.Equal("Go to Dashboard", exact[0].Entry.Title);

        var fuzzy = CommandPaletteSearch.Search(entries, "hud");
        Assert.Equal("Show Performance HUD", fuzzy[0].Entry.Title);
    }

    [Fact]
    public void CommandPaletteRanksWordStartPrefixesAheadOfFuzzyMatches()
    {
        var entries = new[]
        {
            new CommandPaletteEntry("fuzzy", "Gadget Panel", "fuzzy candidate", CommandPaletteCategory.Navigation, ["gd"]),
            new CommandPaletteEntry("word-start", "Go to Dashboard", "word-start candidate", CommandPaletteCategory.Navigation, [])
        };

        var results = CommandPaletteSearch.Search(entries, "gd");

        Assert.Equal("word-start", results[0].Entry.Id);
        Assert.Equal(CommandPaletteMatchKind.WordStart, results[0].MatchKind);
        Assert.Equal([0, 6], results[0].MatchPositions);
        Assert.Contains(results[0].Evidence, entry => entry.Kind == "match_type" && entry.Description == "wordstart match");
        Assert.Contains(results[0].Evidence, entry => entry.Kind == "word_boundary");
    }

    [Fact]
    public void CommandPaletteExecutesSelectedCommandAndCloses()
    {
        var state = CommandPaletteController.Toggle(CommandPaletteState.Closed);
        var entries = CommandPaletteRegistry.DefaultEntries();

        var filtered = CommandPaletteController.Apply(
            state,
            new KeyTerminalEvent(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('G')), DateTimeOffset.UtcNow),
            entries);
        filtered = CommandPaletteController.Apply(
            filtered.State,
            new KeyTerminalEvent(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('o')), DateTimeOffset.UtcNow),
            entries);
        var executed = CommandPaletteController.Apply(
            filtered.State,
            new KeyTerminalEvent(new KeyGesture(TerminalKey.Enter, TerminalModifiers.None), DateTimeOffset.UtcNow),
            entries);

        Assert.NotNull(executed.Execution);
        Assert.False(executed.State.IsOpen);
        Assert.Equal("goto-dashboard", executed.Execution!.CommandId);
    }

    [Fact]
    public void LogSearchSupportsContextAndRegexErrors()
    {
        var lines = new[]
        {
            "alpha",
            "beta warning",
            "gamma",
            "delta warning",
            "epsilon"
        };

        var literal = LogSearchEngine.Apply(lines, new LogSearchState("warning", ContextLines: 1));
        Assert.Equal(5, literal.Lines.Count);

        var regexError = LogSearchEngine.Apply(lines, new LogSearchState("(", RegexMode: true));
        Assert.NotNull(regexError.Error);
        Assert.Empty(regexError.Lines);
    }

    [Fact]
    public void LogSearchHighlightsAllMatchesAndRespectsLiteTier()
    {
        var highlighted = LogSearchEngine.Apply(
            ["doctor doctor doctor"],
            new LogSearchState("doctor", Tier: LogSearchTier.Full));
        Assert.Equal("«doctor» «doctor» «doctor»", highlighted.Lines[0]);

        var lite = LogSearchEngine.Apply(
            Enumerable.Range(0, 600).Select(static index => $"line {index}").ToArray(),
            new LogSearchState("line", RegexMode: true));
        Assert.Equal(LogSearchTier.Lite, lite.Tier);
    }

    [Fact]
    public void MacroRecorderNormalizesReplayPlan()
    {
        var start = new DateTimeOffset(2026, 3, 11, 9, 10, 0, TimeSpan.Zero);
        var macro = MacroRecorder.FromEvents(
            "macro-001",
            [
                TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('g')), start),
                TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('d')), start + TimeSpan.FromMilliseconds(52))
            ],
            "Go to dashboard");

        var replay = MacroRecorder.ReplayPlan(macro, tickMs: 16);

        Assert.Equal(0, replay[0].ScheduledMs);
        Assert.Equal(48, replay[1].ScheduledMs);
        Assert.Contains("Go to dashboard", macro.Description);
    }

    [Fact]
    public void MacroRecorderControllerTransitionsThroughRecordPlayAndTick()
    {
        var start = new DateTimeOffset(2026, 3, 11, 9, 20, 0, TimeSpan.Zero);
        var state = MacroRecorderController.ToggleRecording(new MacroRecorderState(), start);
        state = MacroRecorderController.Capture(
            state,
            TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('g')), start + TimeSpan.FromMilliseconds(20)));
        state = MacroRecorderController.ToggleRecording(state, start + TimeSpan.FromMilliseconds(32));
        state = MacroRecorderController.TogglePlay(state, start + TimeSpan.FromMilliseconds(48));

        var tick = MacroRecorderController.Tick(state, start + TimeSpan.FromMilliseconds(80));

        Assert.Single(tick.EmittedEvents);
        Assert.Equal(MacroRecorderMode.Ready, tick.State.Mode);
        Assert.False(tick.State.Playing);
    }

    [Fact]
    public void PerformanceHudRendersDeterministically()
    {
        var session = HostedParitySession.Create(false, HostedParityScenarioId.Extras) with
        {
            StepCount = 4,
            OverlayVisible = true
        };
        var buffer = new RenderBuffer(42, 8);

        new PerformanceHudWidget
        {
            Snapshot = PerformanceHudSnapshot.FromSession(session)
        }.Render(new RuntimeRenderContext(buffer, Rect.FromSize(42, 8), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Performance HUD", screen);
        Assert.Contains("Frame:", screen);
        Assert.Contains("Budget:", screen);
    }

    [Fact]
    public void PerformanceHudCanRenderFromRuntimeStats()
    {
        var stats = new RuntimeFrameStats(
            StepIndex: 4,
            ChangedCells: 120,
            RunCount: 6,
            BytesEmitted: 2048,
            FrameDurationMs: 11.8,
            PresentDurationMs: 2.4,
            DiffDurationMs: 0.8,
            DirtyRows: 5,
            DegradationLevel: "FULL",
            SyncOutput: true,
            Truncated: false,
            LoadGovernorAction: "degrade",
            LoadGovernorReason: "overload_evidence_passed",
            LoadGovernorPidOutput: 0.75,
            LoadGovernorEProcessValue: 21,
            LoadGovernorPidGateMargin: 0.45,
            LoadGovernorEvidenceMargin: 1,
            LoadGovernorEProcessInWarmup: false,
            LoadGovernorTransitionSeq: 2,
            LoadGovernorTransitionCorrelationId: 8589934604);

        var snapshot = PerformanceHudSnapshot.FromRuntime(stats, syncOutput: true, scrollRegion: true, hyperlinks: true);

        Assert.Equal(11.8, snapshot.ElapsedMs);
        Assert.Equal(120, snapshot.CellsChanged);
        Assert.True(snapshot.SyncOutput);
        Assert.Equal("degrade", snapshot.LoadGovernorAction);
        Assert.Equal("overload_evidence_passed", snapshot.LoadGovernorReason);
        Assert.Equal(0.75, snapshot.LoadGovernorPidOutput);
        Assert.Equal(21, snapshot.LoadGovernorEProcessValue);
        Assert.False(snapshot.LoadGovernorInWarmup);

        var buffer = new RenderBuffer(72, 12);
        new PerformanceHudWidget
        {
            Snapshot = snapshot
        }.Render(new RuntimeRenderContext(buffer, Rect.FromSize(72, 12), Theme.DefaultTheme));

        var screen = HeadlessBufferView.ScreenString(buffer);
        Assert.Contains("Gov:", screen);
        Assert.Contains("PID/E:", screen);
        Assert.Contains("Trans:", screen);
    }
}
