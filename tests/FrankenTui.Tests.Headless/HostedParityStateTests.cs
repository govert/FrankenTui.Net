using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Testing.Harness;
using FrankenTui.Widgets;
using System.Text;

namespace FrankenTui.Tests.Headless;

public sealed class HostedParityStateTests
{
    [Fact]
    public void WidgetInputStateCyclesFocusAndCapturesPasteAnnouncements()
    {
        var state = Ui.CreateInputState(["tabs", "modules", "notes"]).Focus("tabs");

        state = state.Apply(TerminalEvent.Key(new KeyGesture(TerminalKey.Tab, TerminalModifiers.None)));
        state = state.Apply(TerminalEvent.Paste("Hosted parity evidence"));

        Assert.Equal("modules", state.EffectiveFocusId);
        Assert.Contains("Paste", state.LiveRegionText);
    }

    [Fact]
    public void HostedParitySessionProducesMeaningfulDescriptionAndSnapshot()
    {
        var session = HostedParitySession.ForFrame(
            inlineMode: false,
            frame: 2,
            scenarioId: HostedParityScenarioId.Interaction,
            language: "de-DE",
            flowDirection: WidgetFlowDirection.RightToLeft);
        var description = HostedParitySurface.Describe(session);
        var snapshot = RenderHarness.Render(HostedParitySurface.Create(session), 72, 18);

        Assert.Equal("de-DE", description.Language);
        Assert.Equal("rtl", description.Direction);
        Assert.Contains("312-WGT", description.WorkstreamCodes);
        Assert.NotEmpty(description.Accessibility.Nodes);
        Assert.Contains("Interaktion", snapshot.Text);
        Assert.Contains("Focus", snapshot.Text);
    }

    [Fact]
    public void HostedParityExtrasSessionSupportsSearchPaletteAndMacroPlayback()
    {
        var start = new DateTimeOffset(2026, 3, 11, 9, 30, 0, TimeSpan.Zero);
        var session = HostedParitySession.Create(false, HostedParityScenarioId.Extras);

        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('p')), start));
        Assert.True(session.CommandPalette.IsOpen);
        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(8)));
        Assert.False(session.CommandPalette.IsOpen);

        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('/')), start + TimeSpan.FromMilliseconds(16)));
        Assert.True(session.LogSearch.SearchOpen);
        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Escape, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(24)));
        Assert.False(session.LogSearch.SearchOpen);

        session = session with { SelectedModuleIndex = 3 };
        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('r')), start + TimeSpan.FromMilliseconds(32)));
        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('g')), start + TimeSpan.FromMilliseconds(48)));
        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('r')), start + TimeSpan.FromMilliseconds(64)));
        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('p')), start + TimeSpan.FromMilliseconds(80)));
        session = session.AdvanceTime(start + TimeSpan.FromMilliseconds(120));

        Assert.False(session.CommandPalette.IsOpen);
        Assert.Contains("g", session.InputBuffer, StringComparison.Ordinal);
        Assert.NotNull(session.Macro.Macro);
        Assert.Contains(session.LiveLogLines, static line => line.Contains("Playback", StringComparison.OrdinalIgnoreCase) || line.Contains("macro", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HostedParityExtrasSessionSupportsPaneTimelineAndMermaidControls()
    {
        var start = new DateTimeOffset(2026, 3, 12, 8, 0, 0, TimeSpan.Zero);
        var session = HostedParitySession.Create(false, HostedParityScenarioId.Extras) with
        {
            SelectedModuleIndex = 0
        };

        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Down, TerminalModifiers.None), start));
        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('+')), start + TimeSpan.FromMilliseconds(10)));
        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune(']')), start + TimeSpan.FromMilliseconds(20)));
        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('[')), start + TimeSpan.FromMilliseconds(30)));

        Assert.True(session.PaneWorkspace.Timeline.Count >= 2);
        Assert.True(session.PaneWorkspace.TimelineCursor >= 1);

        session = session with
        {
            SelectedModuleIndex = 7
        };
        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Down, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(40)));
        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('g')), start + TimeSpan.FromMilliseconds(50)));
        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('m')), start + TimeSpan.FromMilliseconds(60)));

        Assert.Equal(MermaidGlyphMode.Ascii, session.Mermaid.GlyphMode);
        Assert.False(session.Mermaid.MetricsVisible);
        Assert.True(session.Mermaid.SelectedSampleIndex > 0);
    }
}
