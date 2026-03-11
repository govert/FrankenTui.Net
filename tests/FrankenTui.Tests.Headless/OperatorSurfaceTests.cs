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
    public void CommandPaletteRanksDeterministically()
    {
        var entries = CommandPaletteRegistry.DefaultEntries();

        var exact = CommandPaletteSearch.Search(entries, "Go to Dashboard");
        Assert.Equal("Go to Dashboard", exact[0].Entry.Title);

        var fuzzy = CommandPaletteSearch.Search(entries, "hud");
        Assert.Equal("Show Performance HUD", fuzzy[0].Entry.Title);
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
}
