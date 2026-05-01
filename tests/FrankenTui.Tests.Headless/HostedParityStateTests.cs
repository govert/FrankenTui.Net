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
    public void WidgetInputStatePreservesDeferredFocusAcrossHostBlurRestore()
    {
        var start = Ui.CreateInputState(["tabs", "modules", "notes"]).Focus("modules");

        var blurred = start.Apply(TerminalEvent.Focus(false));
        var movedWhileBlurred = blurred.Apply(TerminalEvent.Key(new KeyGesture(TerminalKey.Tab, TerminalModifiers.None)));
        var restored = movedWhileBlurred.Apply(TerminalEvent.Focus(true));

        Assert.False(blurred.HostFocused);
        Assert.Null(blurred.EffectiveFocusId);
        Assert.Equal("modules", blurred.LogicalFocusId);
        Assert.Equal("notes", movedWhileBlurred.LogicalFocusId);
        Assert.Equal("notes", restored.EffectiveFocusId);
    }

    [Fact]
    public void WidgetInputStateConstrainsNavigationInsideFocusTrapAndRestoresOnPop()
    {
        var state = Ui.CreateInputState(["tabs", "modules", "notes"]).Focus("modules");
        state = state.PushFocusTrap(["modal.primary", "modal.dismiss"], "modal.dismiss");

        var cycled = state.Apply(TerminalEvent.Key(new KeyGesture(TerminalKey.Tab, TerminalModifiers.None)));
        var restored = cycled.PopFocusTrap();

        Assert.True(state.IsFocusTrapped);
        Assert.Equal("modal.dismiss", state.EffectiveFocusId);
        Assert.Equal("modal.primary", cycled.EffectiveFocusId);
        Assert.False(restored.IsFocusTrapped);
        Assert.Equal("modules", restored.EffectiveFocusId);
    }

    [Fact]
    public void NestedFocusTrapPopRestoresLatestUnderlyingSelection()
    {
        var state = Ui.CreateInputState(["tabs", "modules", "notes"]).Focus("modules");
        state = state.PushFocusTrap(["modal1.primary", "modal1.dismiss"], "modal1.primary", "modal1");
        state = state.Focus("modal1.dismiss");
        state = state.PushFocusTrap(["modal2.primary", "modal2.dismiss"], "modal2.primary", "modal2");

        var restored = state.PopFocusTrap();

        Assert.True(state.IsFocusTrapped);
        Assert.Equal("modal2.primary", state.EffectiveFocusId);
        Assert.Equal("modal1.dismiss", restored.EffectiveFocusId);
    }

    [Fact]
    public void RemovingInactiveMiddleTrapRetargetsSurvivingUpperRestoreChain()
    {
        var state = Ui.CreateInputState(["tabs", "modules", "notes"]).Focus("modules");
        state = state.PushFocusTrap(["modal1.primary", "modal1.dismiss"], "modal1.primary", "modal1");
        state = state.Focus("modal1.dismiss");
        state = state.PushFocusTrap(["modal2.primary"], "modal2.primary", "modal2");
        state = state.PushFocusTrap(["modal3.primary"], "modal3.primary", "modal3");

        state = state.RemoveFocusTrap("modal2");
        var restored = state.PopFocusTrap();

        Assert.Equal("modal3.primary", state.EffectiveFocusId);
        Assert.Equal("modal1.dismiss", restored.EffectiveFocusId);
    }

    [Fact]
    public void UpdatingLowerTrapOrderRetargetsUpperRestoreWhenSelectionBecomesInvalid()
    {
        var state = Ui.CreateInputState(["tabs", "modules", "notes"]).Focus("modules");
        state = state.PushFocusTrap(["modal1.primary", "modal1.dismiss"], "modal1.primary", "modal1");
        state = state.Focus("modal1.dismiss");
        state = state.PushFocusTrap(["modal2.primary"], "modal2.primary", "modal2");

        state = state.UpdateFocusTrap("modal1", ["modal1.primary"]);
        var restored = state.PopFocusTrap();

        Assert.Equal("modal2.primary", state.EffectiveFocusId);
        Assert.Equal("modal1.primary", restored.EffectiveFocusId);
    }

    [Fact]
    public void UpdatingActiveTrapToEmptyCollapsesTrapAndRestoresUnderlyingSelection()
    {
        var state = Ui.CreateInputState(["tabs", "modules", "notes"]).Focus("modules");
        state = state.PushFocusTrap(["modal1.primary", "modal1.dismiss"], "modal1.dismiss", "modal1");
        state = state.PushFocusTrap(["modal2.primary"], "modal2.primary", "modal2");

        var collapsed = state.UpdateFocusTrap("modal2", []);

        Assert.True(state.IsFocusTrapped);
        Assert.Equal("modal2.primary", state.EffectiveFocusId);
        Assert.Equal("modal1.dismiss", collapsed.EffectiveFocusId);
        Assert.True(collapsed.IsFocusTrapped);
        Assert.Equal("modal1", collapsed.ActiveTrapId);
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

    [Fact]
    public void HostedParitySessionModalTrapSurvivesBlurAndDismissRestoresBaseFocus()
    {
        var start = new DateTimeOffset(2026, 3, 12, 9, 0, 0, TimeSpan.Zero);
        var session = HostedParitySession.Create(false, HostedParityScenarioId.Interaction);
        session = session with { InputState = session.InputState.Focus("modules") };

        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.None, new Rune('m')), start));
        Assert.True(session.ModalOpen);
        Assert.True(session.InputState.IsFocusTrapped);
        Assert.Equal("modal.primary", session.InputState.EffectiveFocusId);

        session = session.Advance(TerminalEvent.Focus(false, start + TimeSpan.FromMilliseconds(10)));
        Assert.Null(session.InputState.EffectiveFocusId);
        Assert.Equal("modal.primary", session.InputState.LogicalFocusId);

        session = session.Advance(TerminalEvent.Key(new KeyGesture(TerminalKey.Tab, TerminalModifiers.None), start + TimeSpan.FromMilliseconds(20)));
        Assert.Equal("modal.dismiss", session.InputState.LogicalFocusId);

        var engine = new HostedParityInputEngine();
        var dismissed = engine.Process(
            session,
            TerminalEvent.Key(new KeyGesture(TerminalKey.Character, TerminalModifiers.Control, new Rune('c')), start + TimeSpan.FromMilliseconds(30)));

        Assert.False(dismissed.Session.ModalOpen);
        Assert.False(dismissed.Session.InputState.IsFocusTrapped);
        Assert.Equal("modules", dismissed.Session.InputState.LogicalFocusId);

        var restored = dismissed.Session.Advance(TerminalEvent.Focus(true, start + TimeSpan.FromMilliseconds(40)));
        Assert.Equal("modules", restored.InputState.EffectiveFocusId);
    }
}
