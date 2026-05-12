using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Runtime;
using FrankenTui.Widgets;
using System.Globalization;

namespace FrankenTui.Demo.Showcase;

internal sealed class ShowcaseInteractiveProgram : IAppProgram<ShowcaseDemoState, ShowcaseDemoMessage>
{
    private readonly bool _inlineMode;
    private readonly int _screenNumber;
    private readonly bool _tour;
    private readonly double _tourSpeed;
    private readonly int _tourStartStep;
    private readonly string _language;
    private readonly WidgetFlowDirection _flowDirection;
    private readonly Size _initialSize;
    private readonly PaneWorkspaceState? _initialPaneWorkspace;
    private readonly ShowcasePaneWorkspaceLoadResult? _initialPaneWorkspaceLoad;
    private readonly bool _initialMouseCaptureEnabled;

    public ShowcaseInteractiveProgram(
        bool inlineMode,
        int screenNumber,
        bool tour,
        double tourSpeed,
        int tourStartStep,
        string language,
        WidgetFlowDirection flowDirection,
        Size initialSize,
        PaneWorkspaceState? initialPaneWorkspace = null,
        ShowcasePaneWorkspaceLoadResult? initialPaneWorkspaceLoad = null,
        bool initialMouseCaptureEnabled = false)
    {
        _inlineMode = inlineMode;
        _screenNumber = screenNumber;
        _tour = tour;
        _tourSpeed = tourSpeed;
        _tourStartStep = tourStartStep;
        _language = language;
        _flowDirection = flowDirection;
        _initialSize = initialSize;
        _initialPaneWorkspace = initialPaneWorkspace;
        _initialPaneWorkspaceLoad = initialPaneWorkspaceLoad;
        _initialMouseCaptureEnabled = initialMouseCaptureEnabled;
    }

    public ShowcaseDemoState Initialize() =>
        ShowcaseDemoState.Create(
            _inlineMode,
            _initialSize,
            _screenNumber,
            _language,
            _flowDirection,
            _tour,
            _tourSpeed,
            _tourStartStep,
            _initialPaneWorkspace,
            _initialPaneWorkspaceLoad,
            _initialMouseCaptureEnabled);

    public UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage> Update(ShowcaseDemoState model, ShowcaseDemoMessage message) =>
        message switch
        {
            ShowcaseInputMessage input => UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage>.FromModel(
                model.ApplyInput(input.Input, input.RuntimeStats)),
            ShowcaseTimerMessage timer => UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage>.FromModel(
                model.ApplyTick(timer.Now, timer.RuntimeStats)),
            _ => UpdateResult<ShowcaseDemoState, ShowcaseDemoMessage>.FromModel(model)
        };

    public IRuntimeView BuildView(ShowcaseDemoState model) => ShowcaseSurface.Create(model);
}

internal sealed record ShowcaseDemoState(
    bool InlineMode,
    Size Viewport,
    int CurrentScreenNumber,
    HostedParitySession Session,
    string Language,
    WidgetFlowDirection FlowDirection,
    bool TourActive = false,
    bool TourPaused = false,
    double TourSpeed = 1.0,
    int TourStartScreen = 2,
    int TourStepIndex = 0,
    DateTimeOffset? LastTourAdvance = null,
    int ScriptFrame = 0,
    string? VfxEffect = null,
    bool EvidenceLedgerVisible = false,
    bool PerfHudVisible = false,
    bool DebugVisible = false,
    bool HelpVisible = false,
    bool A11yPanelVisible = false,
    bool A11yHighContrast = false,
    bool A11yReducedMotion = false,
    bool A11yLargeText = false,
    bool MouseCaptureEnabled = false,
    ShowcasePaletteLabMatchFilter PaletteLabMatchFilter = ShowcasePaletteLabMatchFilter.All,
    int WidgetGalleryListIndex = 4,
    int WidgetGalleryTabIndex = 2,
    int WidgetGalleryTableRow = 1,
    int FormsInputSelectedFieldIndex = 1,
    int TableThemePresetIndex = 0,
    int TerminalCapabilitiesSelectedRow = 1,
    int TerminalCapabilitiesProfileIndex = 0,
    int MacroRecorderTimelineIndex = 0,
    int MacroRecorderScenarioIndex = 0,
    int PerformanceSelectedIndex = 0,
    int MarkdownActivePaneIndex = 0,
    int MarkdownRendererScroll = 0,
    int MarkdownStreamScroll = 0,
    int MarkdownWrapModeIndex = 0,
    int DataVizActivePanelIndex = 0,
    int DataVizMetricRowIndex = 0,
    int DataVizNarrativeDetailIndex = 0,
    int FileBrowserSelectedRowIndex = 0,
    int FileBrowserPreviewScroll = 0,
    int AdvancedPatternIndex = 0,
    int AdvancedCompositeModeIndex = 0,
    int NotificationsTriggerIndex = 0,
    int NotificationsToastIndex = 0,
    int NotificationsLifecycleScroll = 0,
    int ActionTimelineFilterIndex = 0,
    int ActionTimelineSelectedIndex = 0,
    bool ActionTimelineDetailExpanded = false,
    int IntrinsicSizingScenarioIndex = 0,
    int IntrinsicSizingWidthPresetIndex = 2,
    int IntrinsicSizingDetailScroll = 0,
    int LayoutInspectorScenarioIndex = 0,
    int LayoutInspectorStepIndex = 0,
    bool LayoutInspectorOverlayVisible = true,
    bool LayoutInspectorTreeVisible = true,
    int AdvancedTextEditorCursorLine = 8,
    int AdvancedTextEditorFocusIndex = 0,
    int AdvancedTextEditorHistoryIndex = 0,
    int AdvancedTextEditorDiagnosticsIndex = 0,
    int MousePlaygroundSelectedTargetIndex = 0,
    int MousePlaygroundSelectedTargetClicks = 0,
    int MousePlaygroundEventIndex = 0,
    bool MousePlaygroundOverlayVisible = false,
    bool MousePlaygroundJitterStatsVisible = false,
    int FormValidationSelectedFieldIndex = 0,
    int FormValidationSelectedErrorIndex = 0,
    int FormValidationRulesScroll = 0,
    int FormValidationDiagnosticsScroll = 0,
    bool FormValidationOnSubmitMode = false,
    bool FormValidationSubmitted = false,
    bool PaletteLabBenchEnabled = false,
    int PaletteLabBenchFrame = 0,
    int PaletteLabBenchProcessed = 0,
    bool PaneWorkspaceLoaded = false,
    string? PaneWorkspaceRecoveryError = null,
    string? PaneWorkspaceInvalidSnapshotPath = null,
    ShowcaseKanbanState? KanbanBoard = null,
    RuntimeFrameStats? RuntimeStats = null,
    bool QuitRequested = false)
{
    public ShowcaseScreen CurrentScreen => ShowcaseCatalog.Get(CurrentScreenNumber);

    public ShowcaseTourCallout? TourCallout =>
        TourActive ? ShowcaseTourStoryboard.At(TourStepIndex) : null;

    public static ShowcaseDemoState Create(
        bool inlineMode,
        Size viewport,
        int screenNumber,
        string language,
        WidgetFlowDirection flowDirection,
        bool tour = false,
        double tourSpeed = 1.0,
        int tourStartStep = 1,
        PaneWorkspaceState? paneWorkspace = null,
        ShowcasePaneWorkspaceLoadResult? paneWorkspaceLoad = null,
        bool mouseCaptureEnabled = false,
        string? vfxEffect = null)
    {
        var session = HostedParitySession.Create(inlineMode, HostedParityScenarioId.Extras, language, flowDirection) with
        {
            PaneWorkspace = paneWorkspace ?? PaneWorkspaceState.CreateDemo()
        };
        var current = ShowcaseCatalog.ClampScreenNumber(screenNumber);
        var startScreen = Math.Clamp(tourStartStep <= 1 ? 2 : tourStartStep, 2, ShowcaseCatalog.Screens.Count);
        var model = new ShowcaseDemoState(
            inlineMode,
            viewport,
            current,
            session,
            language,
            flowDirection,
            TourSpeed: Math.Clamp(tourSpeed, 0.25, 4.0),
            TourStartScreen: startScreen,
            PaneWorkspaceLoaded: paneWorkspaceLoad?.Loaded ?? false,
            PaneWorkspaceRecoveryError: paneWorkspaceLoad?.Error,
            PaneWorkspaceInvalidSnapshotPath: paneWorkspaceLoad?.InvalidSnapshotPath,
            KanbanBoard: ShowcaseKanbanState.CreateDefault(),
            MouseCaptureEnabled: mouseCaptureEnabled,
            VfxEffect: NormalizeVfxEffect(vfxEffect));
        return tour ? model.StartTour(DateTimeOffset.UtcNow) : model;
    }

    private static string? NormalizeVfxEffect(string? effect)
    {
        return ShowcaseVfxEffects.NormalizeHarnessInput(effect);
    }

    public KeybindingState CreateKeybindingState() =>
        new(
            InputNonEmpty: !string.IsNullOrWhiteSpace(Session.InputBuffer) || Session.CommandPalette.IsOpen,
            TaskRunning: false,
            ModalOpen: Session.CommandPalette.IsOpen,
            ViewOverlay: EvidenceLedgerVisible || PerfHudVisible || DebugVisible || HelpVisible || A11yPanelVisible || TourActive);

    public ShowcaseDemoState AdvanceScript(int frames)
    {
        var next = this;
        var now = DateTimeOffset.UtcNow;
        for (var index = 0; index < Math.Max(frames, 0); index++)
        {
            now += TimeSpan.FromMilliseconds(350);
            next = next.ApplyTick(now, RuntimeFrameStats.Empty) with { ScriptFrame = next.ScriptFrame + 1 };
        }

        return next;
    }

    public ShowcaseDemoState ApplyInput(RuntimeInputEnvelope input, RuntimeFrameStats runtimeStats)
    {
        ArgumentNullException.ThrowIfNull(input);

        var next = this with
        {
            Viewport = input.ResizeToApply ?? Viewport,
            RuntimeStats = runtimeStats,
            QuitRequested = QuitRequested || input.QuitRequested
        };

        if (input.EffectiveEvent is null)
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is ResizeTerminalEvent resize)
        {
            next = next with { Viewport = resize.Size };
        }

        if (input.EffectiveEvent is MouseTerminalEvent tourMouseEvent &&
            HandleTourMouse(tourMouseEvent, input.Timestamp, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent overlayMouseEvent &&
            HandleOverlayMouse(overlayMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent statusMouseEvent &&
            HandleStatusMouse(statusMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent mouseEvent &&
            HandleChromeMouse(mouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent paletteLabMouseEvent &&
            HandlePaletteLabMouse(paletteLabMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent tableThemeMouseEvent &&
            HandleTableThemeGalleryMouse(tableThemeMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent dataVizMouseEvent &&
            HandleDataVizMouse(dataVizMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent fileBrowserMouseEvent &&
            HandleFileBrowserMouse(fileBrowserMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent advancedMouseEvent &&
            HandleAdvancedMouse(advancedMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent notificationsMouseEvent &&
            HandleNotificationsMouse(notificationsMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent actionTimelineMouseEvent &&
            HandleActionTimelineMouse(actionTimelineMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent intrinsicSizingMouseEvent &&
            HandleIntrinsicSizingMouse(intrinsicSizingMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent layoutInspectorMouseEvent &&
            HandleLayoutInspectorMouse(layoutInspectorMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent advancedTextEditorMouseEvent &&
            HandleAdvancedTextEditorMouse(advancedTextEditorMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent mousePlaygroundMouseEvent &&
            HandleMousePlaygroundMouse(mousePlaygroundMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent formValidationMouseEvent &&
            HandleFormValidationMouse(formValidationMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent terminalCapabilitiesMouseEvent &&
            HandleTerminalCapabilitiesMouse(terminalCapabilitiesMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent widgetGalleryMouseEvent &&
            HandleWidgetGalleryMouse(widgetGalleryMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent formsInputMouseEvent &&
            HandleFormsInputMouse(formsInputMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent macroRecorderMouseEvent &&
            HandleMacroRecorderMouse(macroRecorderMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent performanceMouseEvent &&
            HandlePerformanceMouse(performanceMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent markdownMouseEvent &&
            HandleMarkdownMouse(markdownMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent kanbanMouseEvent &&
            HandleKanbanBoardMouse(kanbanMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent paneMouseEvent &&
            HandlePaneMouse(paneMouseEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is not KeyTerminalEvent keyEvent)
        {
            return SyncSession(next, next.Session.Advance(input).WithRuntimeStats(runtimeStats));
        }

        if (HandleShowcasePaletteKey(keyEvent, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (HandleKanbanBoardKey(keyEvent.Gesture, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (HandleGlobalKey(keyEvent, input.Timestamp, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        return SyncSession(next, next.Session.Advance(input).WithRuntimeStats(runtimeStats));
    }

    public ShowcaseDemoState ApplyTick(DateTimeOffset now, RuntimeFrameStats runtimeStats)
    {
        var nextSession = Session.AdvanceTime(now).WithRuntimeStats(runtimeStats);
        var next = SyncSession(this with { RuntimeStats = runtimeStats }, nextSession);
        next = AdvancePaletteLabBench(next);
        if (!next.TourActive || next.TourPaused)
        {
            return next;
        }

        var interval = TimeSpan.FromMilliseconds(Math.Max(220, 1000 / Math.Max(next.TourSpeed, 0.25)));
        if (next.LastTourAdvance is { } last && now - last < interval)
        {
            return next;
        }

        if (next.TourStepIndex + 1 >= ShowcaseTourStoryboard.Count)
        {
            return next with { TourActive = false, TourPaused = false, LastTourAdvance = now };
        }

        return next.MoveTourStep(1, now);
    }

    private static ShowcaseDemoState SyncSession(ShowcaseDemoState state, HostedParitySession session) =>
        state with { Session = session };

    private static ShowcaseDemoState AdvancePaletteLabBench(ShowcaseDemoState state)
    {
        if (state.CurrentScreenNumber != 39 ||
            state.Session.CommandPalette.IsOpen ||
            state.TourActive ||
            !state.PaletteLabBenchEnabled)
        {
            return state;
        }

        var nextFrame = state.PaletteLabBenchFrame + 1;
        var nextProcessed = nextFrame > 0 && nextFrame % ShowcaseSurface.PaletteLabBenchStepTicks == 0
            ? state.PaletteLabBenchProcessed + 1
            : state.PaletteLabBenchProcessed;
        return state with
        {
            PaletteLabBenchFrame = nextFrame,
            PaletteLabBenchProcessed = nextProcessed
        };
    }

    private static bool HandleShowcasePaletteKey(KeyTerminalEvent keyEvent, ref ShowcaseDemoState next)
    {
        var gesture = keyEvent.Gesture;
        if (IsControlCharacter(gesture, 'k'))
        {
            var palette = CommandPaletteController.Toggle(next.Session.CommandPalette);
            next = SyncSession(next, next.Session with
            {
                CommandPalette = palette,
                InputState = next.Session.InputState.Announce(palette.IsOpen ? "Command palette opened" : "Command palette closed")
            });
            return true;
        }

        if (!next.Session.CommandPalette.IsOpen)
        {
            return false;
        }

        var applied = CommandPaletteController.Apply(
            next.Session.CommandPalette,
            keyEvent,
            ShowcaseCommandPalette.Entries());
        var session = next.Session with
        {
            CommandPalette = applied.State
        };

        if (applied.Execution is { } execution &&
            ShowcaseCommandPalette.TryResolveScreen(execution, out var screenNumber))
        {
            var screen = ShowcaseCatalog.Get(screenNumber);
            session = session with
            {
                InputState = session.InputState.Announce($"Screen {screen.Number}: {screen.Title} opened")
            };
            next = next with
            {
                CurrentScreenNumber = screen.Number,
                TourActive = false,
                TourPaused = false,
                Session = session
            };
            return true;
        }

        next = SyncSession(next, session);
        return true;
    }

    private bool HandleGlobalKey(KeyTerminalEvent keyEvent, DateTimeOffset now, ref ShowcaseDemoState next)
    {
        var gesture = keyEvent.Gesture;
        if (next.EvidenceLedgerVisible &&
            !next.Session.CommandPalette.IsOpen &&
            gesture.Key == TerminalKey.Escape &&
            gesture.Modifiers == TerminalModifiers.None)
        {
            next = next with { EvidenceLedgerVisible = false };
            return true;
        }

        if (next.PerfHudVisible &&
            !next.Session.CommandPalette.IsOpen &&
            gesture.Key == TerminalKey.Escape &&
            gesture.Modifiers == TerminalModifiers.None)
        {
            next = next with { PerfHudVisible = false };
            return true;
        }

        if (next.DebugVisible &&
            !next.Session.CommandPalette.IsOpen &&
            gesture.Key == TerminalKey.Escape &&
            gesture.Modifiers == TerminalModifiers.None)
        {
            next = next with { DebugVisible = false };
            return true;
        }

        if (next.HelpVisible &&
            !next.PerfHudVisible &&
            !next.DebugVisible &&
            !next.Session.CommandPalette.IsOpen &&
            gesture.Key == TerminalKey.Escape &&
            gesture.Modifiers == TerminalModifiers.None)
        {
            next = next with { HelpVisible = false };
            return true;
        }

        if (next.A11yPanelVisible &&
            !next.PerfHudVisible &&
            !next.DebugVisible &&
            !next.HelpVisible &&
            !next.Session.CommandPalette.IsOpen &&
            gesture.Key == TerminalKey.Escape &&
            gesture.Modifiers == TerminalModifiers.None)
        {
            next = next with { A11yPanelVisible = false };
            return true;
        }

        if (next.A11yPanelVisible)
        {
            if (IsShiftCharacter(gesture, 'h'))
            {
                next = next with { A11yHighContrast = !next.A11yHighContrast };
                return true;
            }

            if (IsShiftCharacter(gesture, 'm'))
            {
                next = next with { A11yReducedMotion = !next.A11yReducedMotion };
                return true;
            }

            if (IsShiftCharacter(gesture, 'l'))
            {
                next = next with { A11yLargeText = !next.A11yLargeText };
                return true;
            }
        }

        if (IsControlCharacter(gesture, 'i'))
        {
            next = next with { EvidenceLedgerVisible = !next.EvidenceLedgerVisible };
            return true;
        }

        if (IsControlCharacter(gesture, 'p'))
        {
            next = next with { PerfHudVisible = !next.PerfHudVisible };
            return true;
        }

        if (HandlePaletteLabKey(gesture, ref next))
        {
            return true;
        }

        if (gesture.Key == TerminalKey.F6 || IsCharacter(gesture, 'm'))
        {
            next = next with { MouseCaptureEnabled = !next.MouseCaptureEnabled };
            return true;
        }

        if (IsShiftCharacter(gesture, 'a'))
        {
            next = next with { A11yPanelVisible = !next.A11yPanelVisible };
            return true;
        }

        if (next.TourActive)
        {
            if (gesture.Key == TerminalKey.Escape)
            {
                next = next with { TourActive = false, TourPaused = false, CurrentScreenNumber = 1 };
                return true;
            }

            if (IsCharacter(gesture, ' '))
            {
                next = next with { TourPaused = !next.TourPaused };
                return true;
            }

            if (gesture.Modifiers == TerminalModifiers.None &&
                (gesture.Key == TerminalKey.Right || IsCharacter(gesture, 'n')))
            {
                next = next.MoveTourStep(1, now);
                return true;
            }

            if (gesture.Modifiers == TerminalModifiers.None &&
                (gesture.Key == TerminalKey.Left || IsCharacter(gesture, 'p')))
            {
                next = next.MoveTourStep(-1, now);
                return true;
            }

            if (IsCharacter(gesture, '+') || IsCharacter(gesture, '='))
            {
                next = AdjustTourSpeed(next, 1.25);
                return true;
            }

            if (IsCharacter(gesture, '-'))
            {
                next = AdjustTourSpeed(next, 1.0 / 1.25);
                return true;
            }
        }

        if (gesture.Key == TerminalKey.F12)
        {
            next = next with { DebugVisible = !next.DebugVisible };
            return true;
        }

        if (IsCharacter(gesture, '?'))
        {
            next = next with { HelpVisible = !next.HelpVisible };
            return true;
        }

        if (IsCharacter(gesture, 'q'))
        {
            next = next with { QuitRequested = true };
            return true;
        }

        if (next.CurrentScreenNumber == 1 && !next.TourActive)
        {
            if (gesture.Key == TerminalKey.Escape && gesture.Modifiers == TerminalModifiers.None)
            {
                next = next with { CurrentScreenNumber = 2 };
                return true;
            }

            if (gesture.Key == TerminalKey.Enter || IsCharacter(gesture, ' '))
            {
                next = next.StartTour(now);
                return true;
            }

            if (gesture.Modifiers == TerminalModifiers.None &&
                (gesture.Key == TerminalKey.Down || IsCharacter(gesture, 'j') || IsCharacter(gesture, 'n')))
            {
                next = AdjustTourLandingStart(next, 1);
                return true;
            }

            if (gesture.Modifiers == TerminalModifiers.None &&
                (gesture.Key == TerminalKey.Up || IsCharacter(gesture, 'k') || IsCharacter(gesture, 'p')))
            {
                next = AdjustTourLandingStart(next, -1);
                return true;
            }

            if (IsCharacter(gesture, '+') || IsCharacter(gesture, '='))
            {
                next = AdjustTourSpeed(next, 1.25);
                return true;
            }

            if (IsCharacter(gesture, '-'))
            {
                next = AdjustTourSpeed(next, 1.0 / 1.25);
                return true;
            }

            if (IsCharacter(gesture, 'r'))
            {
                next = next with { TourStartScreen = 2, TourSpeed = 1.0 };
                return true;
            }
        }

        if (gesture.Key == TerminalKey.Tab)
        {
            var delta = gesture.Modifiers.HasFlag(TerminalModifiers.Shift) ? -1 : 1;
            next = next with { CurrentScreenNumber = ShowcaseCatalog.Move(next.CurrentScreenNumber, delta) };
            return true;
        }

        if (IsShiftCharacter(gesture, 'h') || IsShiftCharacter(gesture, 'l'))
        {
            next = next with
            {
                TourActive = false,
                TourPaused = false,
                CurrentScreenNumber = ShowcaseCatalog.Move(
                    next.CurrentScreenNumber,
                    IsShiftCharacter(gesture, 'h') ? -1 : 1)
            };
            return true;
        }

        if (gesture.Modifiers == TerminalModifiers.None &&
            (gesture.Key == TerminalKey.Left || gesture.Key == TerminalKey.Right))
        {
            next = next with
            {
                CurrentScreenNumber = ShowcaseCatalog.Move(
                    next.CurrentScreenNumber,
                    gesture.Key == TerminalKey.Left ? -1 : 1)
            };
            return true;
        }

        if (gesture.IsCharacter && gesture.Character is { } rune && gesture.Modifiers == TerminalModifiers.None)
        {
            var value = (char)rune.Value;
            if (value == '0')
            {
                next = next with { CurrentScreenNumber = 10 };
                return true;
            }

            if (value is >= '1' and <= '9')
            {
                next = next with { CurrentScreenNumber = value - '0' };
                return true;
            }
        }

        return false;
    }

    private static bool HandlePaletteLabKey(KeyGesture gesture, ref ShowcaseDemoState next)
    {
        if (next.CurrentScreenNumber != 39 ||
            next.Session.CommandPalette.IsOpen ||
            next.EvidenceLedgerVisible ||
            next.PerfHudVisible ||
            next.DebugVisible ||
            next.HelpVisible ||
            next.A11yPanelVisible ||
            next.TourActive)
        {
            return false;
        }

        if (IsCharacter(gesture, 'b'))
        {
            next = next with
            {
                PaletteLabBenchEnabled = !next.PaletteLabBenchEnabled,
                PaletteLabBenchFrame = 0,
                PaletteLabBenchProcessed = 0
            };
            return true;
        }

        if (IsCharacter(gesture, 'm'))
        {
            next = next with { PaletteLabMatchFilter = next.PaletteLabMatchFilter.Next() };
            return true;
        }

        if (!gesture.IsCharacter || gesture.Character is not { } rune || gesture.Modifiers != TerminalModifiers.None)
        {
            return false;
        }

        var filter = rune.Value switch
        {
            '0' => ShowcasePaletteLabMatchFilter.All,
            '1' => ShowcasePaletteLabMatchFilter.Exact,
            '2' => ShowcasePaletteLabMatchFilter.Prefix,
            '3' => ShowcasePaletteLabMatchFilter.WordStart,
            '4' => ShowcasePaletteLabMatchFilter.Substring,
            '5' => ShowcasePaletteLabMatchFilter.Fuzzy,
            _ => (ShowcasePaletteLabMatchFilter?)null
        };
        if (filter is null)
        {
            return false;
        }

        next = next with { PaletteLabMatchFilter = filter.Value };
        return true;
    }

    private static bool HandleKanbanBoardKey(KeyGesture gesture, ref ShowcaseDemoState next)
    {
        if (next.CurrentScreenNumber != 42 ||
            next.Session.CommandPalette.IsOpen ||
            next.EvidenceLedgerVisible ||
            next.PerfHudVisible ||
            next.DebugVisible ||
            next.HelpVisible ||
            next.A11yPanelVisible ||
            next.TourActive)
        {
            return false;
        }

        var board = next.KanbanBoard ?? ShowcaseKanbanState.CreateDefault();
        ShowcaseKanbanState? updated = null;

        if (gesture.Modifiers == TerminalModifiers.None)
        {
            updated = gesture.Key switch
            {
                TerminalKey.Left => board.FocusLeft(),
                TerminalKey.Right => board.FocusRight(),
                TerminalKey.Up => board.FocusUp(),
                TerminalKey.Down => board.FocusDown(),
                _ => updated
            };
        }

        if (updated is null && gesture.IsCharacter && gesture.Character is { } rune)
        {
            updated = (char)rune.Value switch
            {
                'h' => board.FocusLeft(),
                'l' => board.FocusRight(),
                'j' => board.FocusDown(),
                'k' => board.FocusUp(),
                'H' => board.MoveCardLeft(),
                'L' => board.MoveCardRight(),
                'u' => board.Undo(),
                'r' => board.Redo(),
                _ => null
            };
        }

        if (updated is null || ReferenceEquals(updated, board) || updated == board)
        {
            return updated is not null;
        }

        next = next with { KanbanBoard = updated };
        return true;
    }

    private static bool HandleChromeMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.Session.CommandPalette.IsOpen ||
            next.Viewport.Height == 0 ||
            gesture.Row >= next.Viewport.Height - 1)
        {
            return false;
        }

        if (gesture.Kind is TerminalMouseKind.Scroll)
        {
            var scrollHit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
            if (scrollHit.Layer == ShowcaseHitLayer.Tab)
            {
                if (gesture.Button is TerminalMouseButton.WheelDown)
                {
                    next = StopTourAndMove(next, 1);
                    return true;
                }

                if (gesture.Button is TerminalMouseButton.WheelUp)
                {
                    next = StopTourAndMove(next, -1);
                    return true;
                }
            }
        }

        if (gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Up) ||
            gesture.Button is not TerminalMouseButton.Left)
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if ((hit.Layer is ShowcaseHitLayer.Category or ShowcaseHitLayer.Tab) &&
            hit.TargetScreenNumber is { } targetScreenNumber)
        {
            next = StopTourAndSelect(next, targetScreenNumber);
            return true;
        }

        return false;
    }

    private static ShowcaseDemoState StopTourAndSelect(ShowcaseDemoState state, int screenNumber) =>
        state with
        {
            TourActive = false,
            TourPaused = false,
            CurrentScreenNumber = ShowcaseCatalog.ClampScreenNumber(screenNumber)
        };

    private static ShowcaseDemoState StopTourAndMove(ShowcaseDemoState state, int delta) =>
        state with
        {
            TourActive = false,
            TourPaused = false,
            CurrentScreenNumber = ShowcaseCatalog.Move(state.CurrentScreenNumber, delta)
        };

    private static bool HandleKanbanBoardMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 42 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Up or TerminalMouseKind.Drag))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !TryParseKanbanHit(hit.LocalHitId, out var col, out var row, out _))
        {
            return false;
        }

        var board = next.KanbanBoard ?? ShowcaseKanbanState.CreateDefault();
        var updated = gesture.Kind switch
        {
            TerminalMouseKind.Down when gesture.Button == TerminalMouseButton.Left => board.FocusAt(col, row),
            TerminalMouseKind.Up when gesture.Button == TerminalMouseButton.Left => board.MoveFocusedToColumn(col),
            TerminalMouseKind.Drag when gesture.Button == TerminalMouseButton.Left => board.FocusColumn(col),
            _ => board
        };

        if (updated == board)
        {
            return true;
        }

        next = next with { KanbanBoard = updated };
        return true;
    }

    private static bool TryParseKanbanHit(string localHitId, out int col, out int row, out int cardId)
    {
        col = 0;
        row = 0;
        cardId = 0;
        var parts = localHitId.Split(':');
        return parts.Length == 4 &&
            parts[0] == "kanban" &&
            int.TryParse(parts[1], out col) &&
            int.TryParse(parts[2], out row) &&
            int.TryParse(parts[3], out cardId);
    }

    private static bool HandlePaneMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            next.Viewport.Height <= 4 ||
            gesture.Row >= next.Viewport.Height - 1 ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Up) ||
            gesture.Button is not TerminalMouseButton.Left)
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit is not { Layer: ShowcaseHitLayer.Pane, TargetScreenNumber: { } targetScreenNumber })
        {
            return false;
        }

        if (next.CurrentScreenNumber != 2 || targetScreenNumber == next.CurrentScreenNumber)
        {
            return false;
        }

        next = next with
        {
            CurrentScreenNumber = targetScreenNumber,
            TourActive = false,
            TourPaused = false
        };
        return true;
    }

    private static bool HandlePaletteLabMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 39 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            next.EvidenceLedgerVisible ||
            next.PerfHudVisible ||
            next.DebugVisible ||
            next.HelpVisible ||
            next.A11yPanelVisible ||
            !TryResolvePaletteLabPaletteArea(next.Viewport, out var paletteArea) ||
            !Contains(paletteArea, gesture.Column, gesture.Row))
        {
            return false;
        }

        var palette = next.Session.CommandPalette;
        var labPalette = palette with
        {
            IsOpen = true,
            Query = ShowcaseSurface.ResolvePaletteLabQuery(next, palette)
        };
        var results = ShowcaseSurface.FilterPaletteLabResultsForDemo(
            CommandPaletteController.Results(labPalette, ShowcaseCommandPalette.EvidenceLabEntries()),
            next.PaletteLabMatchFilter);
        var selectedIndex = results.Count == 0
            ? -1
            : Math.Clamp(palette.SelectedIndex, 0, results.Count - 1);

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -3 : 3;
            next = next with
            {
                Session = next.Session with
                {
                    CommandPalette = palette with { SelectedIndex = MovePaletteLabSelection(selectedIndex, results.Count, delta) }
                }
            };
            return true;
        }

        if (gesture.Kind != TerminalMouseKind.Down ||
            gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (selectedIndex < 0 || selectedIndex >= results.Count)
        {
            next = next with
            {
                Session = next.Session with
                {
                    CommandPalette = palette with { Status = "No command selected." }
                }
            };
            return true;
        }

        var selected = results[selectedIndex].Entry;
        next = next with
        {
            Session = next.Session with
            {
                CommandPalette = palette with
                {
                    LastExecutedCommandId = selected.Id,
                    Status = $"Executed {selected.Title}."
                },
                InputState = next.Session.InputState.Announce($"Palette lab executed {selected.Title}")
            }
        };
        return true;
    }

    internal static bool TryResolvePaletteLabPaletteArea(Size viewport, out Rect area)
    {
        area = default;
        if (viewport.Width < 12 || viewport.Height < 6)
        {
            return false;
        }

        var width = Math.Max(1, viewport.Width * 34 / 100);
        var height = Math.Max(1, viewport.Height - 4);
        area = new Rect(0, 3, (ushort)Math.Min(width, ushort.MaxValue), (ushort)Math.Min(height, ushort.MaxValue));
        return true;
    }

    private static bool Contains(Rect rect, ushort column, ushort row) =>
        column >= rect.X &&
        column < rect.X + rect.Width &&
        row >= rect.Y &&
        row < rect.Y + rect.Height;

    private static int MovePaletteLabSelection(int selectedIndex, int count, int delta)
    {
        if (count <= 0)
        {
            return 0;
        }

        return Math.Clamp((selectedIndex < 0 ? 0 : selectedIndex) + delta, 0, count - 1);
    }

    private static bool HandleTableThemeGalleryMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 11 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("table_theme:preset:", StringComparison.Ordinal))
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = next with { TableThemePresetIndex = Math.Clamp(next.TableThemePresetIndex + delta, 0, 2) };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        var presetText = hit.LocalHitId["table_theme:preset:".Length..];
        if (!int.TryParse(presetText, CultureInfo.InvariantCulture, out var preset))
        {
            return false;
        }

        next = next with { TableThemePresetIndex = Math.Clamp(preset, 0, 2) };
        return true;
    }

    private static bool HandleDataVizMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 8 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "data_viz:metrics_table" => next with
                {
                    DataVizActivePanelIndex = 1,
                    DataVizMetricRowIndex = Math.Clamp(next.DataVizMetricRowIndex + delta, 0, 3)
                },
                "data_viz:narrative" => next with
                {
                    DataVizActivePanelIndex = 2,
                    DataVizNarrativeDetailIndex = Math.Clamp(next.DataVizNarrativeDetailIndex + delta, 0, 2)
                },
                _ => next
            };
            return hit.LocalHitId is "data_viz:metrics_table" or "data_viz:narrative";
        }

        if (gesture.Button != TerminalMouseButton.Left && gesture.Button != TerminalMouseButton.Right)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "data_viz:progress" => next with { DataVizActivePanelIndex = 0 },
            "data_viz:metrics_table" => next with
            {
                DataVizActivePanelIndex = 1,
                DataVizMetricRowIndex = Math.Clamp(next.DataVizMetricRowIndex + 1, 0, 3)
            },
            "data_viz:narrative" => next with
            {
                DataVizActivePanelIndex = 2,
                DataVizNarrativeDetailIndex = Math.Clamp(next.DataVizNarrativeDetailIndex + 1, 0, 2)
            },
            _ => next
        };
        return hit.LocalHitId is "data_viz:progress" or "data_viz:metrics_table" or "data_viz:narrative";
    }

    private static bool HandleFileBrowserMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 9 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                { } value when value.StartsWith("file_browser:tree:", StringComparison.Ordinal) => next with
                {
                    FileBrowserSelectedRowIndex = Math.Clamp(next.FileBrowserSelectedRowIndex + delta, 0, 5)
                },
                "file_browser:preview" => next with
                {
                    FileBrowserPreviewScroll = Math.Clamp(next.FileBrowserPreviewScroll + delta, 0, 8)
                },
                _ => next
            };
            return hit.LocalHitId.StartsWith("file_browser:tree:", StringComparison.Ordinal) ||
                hit.LocalHitId == "file_browser:preview";
        }

        if (gesture.Button != TerminalMouseButton.Left ||
            !hit.LocalHitId.StartsWith("file_browser:tree:", StringComparison.Ordinal))
        {
            return hit.LocalHitId == "file_browser:preview";
        }

        var rowText = hit.LocalHitId["file_browser:tree:".Length..];
        if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
        {
            return false;
        }

        next = next with { FileBrowserSelectedRowIndex = Math.Clamp(row, 0, 5) };
        return true;
    }

    private static bool HandleAdvancedMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 10 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "advanced:patterns" => next with { AdvancedPatternIndex = Math.Clamp(next.AdvancedPatternIndex + delta, 0, 4) },
                "advanced:composite" => next with { AdvancedCompositeModeIndex = Math.Clamp(next.AdvancedCompositeModeIndex + delta, 0, 2) },
                _ => next
            };
            return hit.LocalHitId is "advanced:patterns" or "advanced:composite";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "advanced:patterns" => next with { AdvancedPatternIndex = Math.Clamp(next.AdvancedPatternIndex + 1, 0, 4) },
            "advanced:composite" => next with { AdvancedCompositeModeIndex = Math.Clamp(next.AdvancedCompositeModeIndex + 1, 0, 2) },
            _ => next
        };
        return hit.LocalHitId is "advanced:patterns" or "advanced:composite";
    }

    private static bool HandleNotificationsMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 21 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                { } value when value.StartsWith("notifications:toast:", StringComparison.Ordinal) => next with
                {
                    NotificationsToastIndex = Math.Clamp(next.NotificationsToastIndex + delta, 0, 4)
                },
                "notifications:lifecycle" => next with
                {
                    NotificationsLifecycleScroll = Math.Clamp(next.NotificationsLifecycleScroll + delta, 0, 6)
                },
                _ => next
            };
            return hit.LocalHitId.StartsWith("notifications:toast:", StringComparison.Ordinal) ||
                hit.LocalHitId == "notifications:lifecycle";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId.StartsWith("notifications:trigger:", StringComparison.Ordinal))
        {
            var trigger = hit.LocalHitId["notifications:trigger:".Length..];
            var triggerIndex = trigger switch
            {
                "error" => 1,
                "warning" => 2,
                "info" => 3,
                "urgent" => 4,
                "dismiss_all" => 5,
                _ => 0
            };
            next = next with { NotificationsTriggerIndex = triggerIndex };
            return true;
        }

        if (hit.LocalHitId.StartsWith("notifications:toast:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["notifications:toast:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with { NotificationsToastIndex = Math.Clamp(row, 0, 4) };
            return true;
        }

        if (hit.LocalHitId == "notifications:lifecycle")
        {
            next = next with { NotificationsLifecycleScroll = Math.Clamp(next.NotificationsLifecycleScroll + 1, 0, 6) };
            return true;
        }

        return false;
    }

    private static bool HandleActionTimelineMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 22 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "action_timeline:timeline" => next with
                {
                    ActionTimelineSelectedIndex = Math.Clamp(next.ActionTimelineSelectedIndex + delta, 0, 7)
                },
                "action_timeline:filters" => next with
                {
                    ActionTimelineFilterIndex = Math.Clamp(next.ActionTimelineFilterIndex + delta, 0, 3)
                },
                _ => next
            };
            return hit.LocalHitId is "action_timeline:timeline" or "action_timeline:filters";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "action_timeline:filters" => next with { ActionTimelineFilterIndex = (next.ActionTimelineFilterIndex + 1) % 4 },
            "action_timeline:timeline" => next with { ActionTimelineSelectedIndex = Math.Clamp(next.ActionTimelineSelectedIndex + 1, 0, 7) },
            "action_timeline:detail" => next with { ActionTimelineDetailExpanded = !next.ActionTimelineDetailExpanded },
            _ => next
        };
        return hit.LocalHitId is "action_timeline:filters" or "action_timeline:timeline" or "action_timeline:detail";
    }

    private static bool HandleIntrinsicSizingMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 23 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "intrinsic_sizing:scenarios" => next with
                {
                    IntrinsicSizingScenarioIndex = Math.Clamp(next.IntrinsicSizingScenarioIndex + delta, 0, 3)
                },
                "intrinsic_sizing:detail" => next with
                {
                    IntrinsicSizingDetailScroll = Math.Clamp(next.IntrinsicSizingDetailScroll + delta, 0, 6)
                },
                "intrinsic_sizing:controls" => next with
                {
                    IntrinsicSizingWidthPresetIndex = Math.Clamp(next.IntrinsicSizingWidthPresetIndex + delta, 0, 3)
                },
                _ => next
            };
            return hit.LocalHitId is "intrinsic_sizing:scenarios" or "intrinsic_sizing:detail" or "intrinsic_sizing:controls";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "intrinsic_sizing:scenarios" => next with { IntrinsicSizingScenarioIndex = (next.IntrinsicSizingScenarioIndex + 1) % 4 },
            "intrinsic_sizing:detail" => next with { IntrinsicSizingDetailScroll = Math.Clamp(next.IntrinsicSizingDetailScroll + 1, 0, 6) },
            "intrinsic_sizing:controls" => next with { IntrinsicSizingWidthPresetIndex = (next.IntrinsicSizingWidthPresetIndex + 1) % 4 },
            _ => next
        };
        return hit.LocalHitId is "intrinsic_sizing:scenarios" or "intrinsic_sizing:detail" or "intrinsic_sizing:controls";
    }

    private static bool HandleLayoutInspectorMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 24 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "layout_inspector:info" or "layout_inspector:pane_studio" => next with
                {
                    LayoutInspectorScenarioIndex = (next.LayoutInspectorScenarioIndex + delta + 3) % 3
                },
                "layout_inspector:overlay" => next with
                {
                    LayoutInspectorStepIndex = (next.LayoutInspectorStepIndex + delta + 3) % 3
                },
                "layout_inspector:tree" => next with
                {
                    LayoutInspectorTreeVisible = !next.LayoutInspectorTreeVisible
                },
                _ => next
            };
            return hit.LocalHitId is "layout_inspector:info" or "layout_inspector:overlay" or "layout_inspector:tree" or "layout_inspector:pane_studio";
        }

        next = (hit.LocalHitId, gesture.Button) switch
        {
            ("layout_inspector:info", TerminalMouseButton.Left) => next with
            {
                LayoutInspectorScenarioIndex = (next.LayoutInspectorScenarioIndex + 1) % 3
            },
            ("layout_inspector:overlay", TerminalMouseButton.Left) => next with
            {
                LayoutInspectorStepIndex = (next.LayoutInspectorStepIndex + 1) % 3
            },
            ("layout_inspector:overlay", TerminalMouseButton.Right) => next with
            {
                LayoutInspectorOverlayVisible = !next.LayoutInspectorOverlayVisible
            },
            ("layout_inspector:tree", TerminalMouseButton.Left) => next with
            {
                LayoutInspectorTreeVisible = !next.LayoutInspectorTreeVisible
            },
            ("layout_inspector:pane_studio", TerminalMouseButton.Right) => next with
            {
                LayoutInspectorOverlayVisible = !next.LayoutInspectorOverlayVisible
            },
            ("layout_inspector:pane_studio", TerminalMouseButton.Left) => next with
            {
                LayoutInspectorStepIndex = (next.LayoutInspectorStepIndex + 1) % 3
            },
            _ => next
        };
        return hit.LocalHitId is "layout_inspector:info" or "layout_inspector:overlay" or "layout_inspector:tree" or "layout_inspector:pane_studio";
    }

    private static bool HandleAdvancedTextEditorMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 25 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                { } value when value.StartsWith("advanced_text_editor:line:", StringComparison.Ordinal) => next with
                {
                    AdvancedTextEditorCursorLine = Math.Clamp(next.AdvancedTextEditorCursorLine + delta, 0, 12),
                    AdvancedTextEditorFocusIndex = 0
                },
                { } value when value.StartsWith("advanced_text_editor:history:", StringComparison.Ordinal) => next with
                {
                    AdvancedTextEditorHistoryIndex = Math.Clamp(next.AdvancedTextEditorHistoryIndex + delta, 0, 5),
                    AdvancedTextEditorFocusIndex = 2
                },
                { } value when value.StartsWith("advanced_text_editor:diagnostic:", StringComparison.Ordinal) => next with
                {
                    AdvancedTextEditorDiagnosticsIndex = Math.Clamp(next.AdvancedTextEditorDiagnosticsIndex + delta, 0, 9),
                    AdvancedTextEditorFocusIndex = 3
                },
                "advanced_text_editor:search" => next with
                {
                    AdvancedTextEditorFocusIndex = 1
                },
                _ => next
            };
            return hit.LocalHitId == "advanced_text_editor:search" ||
                hit.LocalHitId.StartsWith("advanced_text_editor:line:", StringComparison.Ordinal) ||
                hit.LocalHitId.StartsWith("advanced_text_editor:history:", StringComparison.Ordinal) ||
                hit.LocalHitId.StartsWith("advanced_text_editor:diagnostic:", StringComparison.Ordinal);
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId.StartsWith("advanced_text_editor:line:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["advanced_text_editor:line:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                AdvancedTextEditorCursorLine = Math.Clamp(row, 0, 12),
                AdvancedTextEditorFocusIndex = 0
            };
            return true;
        }

        if (hit.LocalHitId.StartsWith("advanced_text_editor:history:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["advanced_text_editor:history:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                AdvancedTextEditorHistoryIndex = Math.Clamp(row, 0, 5),
                AdvancedTextEditorFocusIndex = 2
            };
            return true;
        }

        if (hit.LocalHitId.StartsWith("advanced_text_editor:diagnostic:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["advanced_text_editor:diagnostic:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                AdvancedTextEditorDiagnosticsIndex = Math.Clamp(row, 0, 9),
                AdvancedTextEditorFocusIndex = 3
            };
            return true;
        }

        if (hit.LocalHitId == "advanced_text_editor:search")
        {
            next = next with { AdvancedTextEditorFocusIndex = 1 };
            return true;
        }

        return false;
    }

    private static bool HandleMousePlaygroundMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 26 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("target:", StringComparison.Ordinal))
        {
            return false;
        }

        var targetText = hit.LocalHitId["target:".Length..];
        if (!int.TryParse(targetText, CultureInfo.InvariantCulture, out var targetId))
        {
            return false;
        }

        var targetIndex = Math.Clamp(targetId - 1, 0, 11);
        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = next with
            {
                MousePlaygroundSelectedTargetIndex = targetIndex,
                MousePlaygroundEventIndex = Math.Clamp(next.MousePlaygroundEventIndex + delta, 0, 9),
                MousePlaygroundJitterStatsVisible = true
            };
            return true;
        }

        next = gesture.Button switch
        {
            TerminalMouseButton.Left => next with
            {
                MousePlaygroundSelectedTargetIndex = targetIndex,
                MousePlaygroundSelectedTargetClicks = next.MousePlaygroundSelectedTargetIndex == targetIndex
                    ? Math.Clamp(next.MousePlaygroundSelectedTargetClicks + 1, 0, 99)
                    : 1,
                MousePlaygroundEventIndex = 7
            },
            TerminalMouseButton.Right => next with
            {
                MousePlaygroundSelectedTargetIndex = targetIndex,
                MousePlaygroundOverlayVisible = !next.MousePlaygroundOverlayVisible,
                MousePlaygroundEventIndex = 8
            },
            TerminalMouseButton.Middle => next with
            {
                MousePlaygroundSelectedTargetIndex = targetIndex,
                MousePlaygroundJitterStatsVisible = !next.MousePlaygroundJitterStatsVisible,
                MousePlaygroundEventIndex = 9
            },
            _ => next
        };
        return gesture.Button is TerminalMouseButton.Left or TerminalMouseButton.Right or TerminalMouseButton.Middle;
    }

    private static bool HandleFormValidationMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 27 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                { } value when value.StartsWith("form_validation:field:", StringComparison.Ordinal) => next with
                {
                    FormValidationSelectedFieldIndex = Math.Clamp(next.FormValidationSelectedFieldIndex + delta, 0, 8)
                },
                "form_validation:rules" => next with
                {
                    FormValidationRulesScroll = Math.Clamp(next.FormValidationRulesScroll + delta, 0, 6)
                },
                "form_validation:diagnostics" => next with
                {
                    FormValidationDiagnosticsScroll = Math.Clamp(next.FormValidationDiagnosticsScroll + delta, 0, 8)
                },
                _ => next
            };
            return hit.LocalHitId == "form_validation:rules" ||
                hit.LocalHitId == "form_validation:diagnostics" ||
                hit.LocalHitId.StartsWith("form_validation:field:", StringComparison.Ordinal);
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId.StartsWith("form_validation:field:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["form_validation:field:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with { FormValidationSelectedFieldIndex = Math.Clamp(row, 0, 8) };
            return true;
        }

        if (hit.LocalHitId.StartsWith("form_validation:error:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["form_validation:error:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with
            {
                FormValidationSelectedErrorIndex = Math.Clamp(row, 0, 8),
                FormValidationOnSubmitMode = !next.FormValidationOnSubmitMode
            };
            return true;
        }

        next = hit.LocalHitId switch
        {
            "form_validation:mode" => next with { FormValidationOnSubmitMode = !next.FormValidationOnSubmitMode },
            "form_validation:touched_dirty" => next with { FormValidationSubmitted = !next.FormValidationSubmitted },
            "form_validation:controls" => next with { FormValidationSubmitted = true },
            "form_validation:notifications" => next with { FormValidationSubmitted = !next.FormValidationSubmitted },
            "form_validation:rules" => next with { FormValidationRulesScroll = Math.Clamp(next.FormValidationRulesScroll + 1, 0, 6) },
            "form_validation:diagnostics" => next with { FormValidationDiagnosticsScroll = Math.Clamp(next.FormValidationDiagnosticsScroll + 1, 0, 8) },
            _ => next
        };
        return hit.LocalHitId is "form_validation:mode" or "form_validation:touched_dirty" or
            "form_validation:controls" or "form_validation:notifications" or
            "form_validation:rules" or "form_validation:diagnostics";
    }

    private static bool HandleTerminalCapabilitiesMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 12 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "terminal_capabilities:matrix" => next with
                {
                    TerminalCapabilitiesSelectedRow = Math.Clamp(next.TerminalCapabilitiesSelectedRow + delta, 0, 5)
                },
                "terminal_capabilities:simulation" => next with
                {
                    TerminalCapabilitiesProfileIndex = Math.Clamp(next.TerminalCapabilitiesProfileIndex + delta, 0, 5)
                },
                _ => next
            };
            return hit.LocalHitId is "terminal_capabilities:matrix" or "terminal_capabilities:simulation";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "terminal_capabilities:matrix" => next with
            {
                TerminalCapabilitiesSelectedRow = Math.Clamp(gesture.Row - 5, 0, 5)
            },
            "terminal_capabilities:simulation" => next with
            {
                TerminalCapabilitiesProfileIndex = Math.Clamp(next.TerminalCapabilitiesProfileIndex + 1, 0, 5)
            },
            _ => next
        };
        return hit.LocalHitId is "terminal_capabilities:matrix" or "terminal_capabilities:simulation";
    }

    private static bool HandleWidgetGalleryMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 5 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            next = hit.LocalHitId switch
            {
                "widget_gallery:list" => next with
                {
                    WidgetGalleryListIndex = Math.Clamp(next.WidgetGalleryListIndex + delta, 0, 6)
                },
                "widget_gallery:tabs" => next with
                {
                    WidgetGalleryTabIndex = Math.Clamp(next.WidgetGalleryTabIndex + delta, 0, 3)
                },
                "widget_gallery:table" => next with
                {
                    WidgetGalleryTableRow = Math.Clamp(next.WidgetGalleryTableRow + delta, 0, 3)
                },
                _ => next
            };
            return hit.LocalHitId is "widget_gallery:list" or "widget_gallery:tabs" or "widget_gallery:table";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "widget_gallery:list" => next with
            {
                WidgetGalleryListIndex = Math.Clamp(next.WidgetGalleryListIndex + 1, 0, 6)
            },
            "widget_gallery:tabs" => next with
            {
                WidgetGalleryTabIndex = Math.Clamp(next.WidgetGalleryTabIndex + 1, 0, 3)
            },
            "widget_gallery:table" => next with
            {
                WidgetGalleryTableRow = Math.Clamp(next.WidgetGalleryTableRow + 1, 0, 3)
            },
            _ => next
        };
        return hit.LocalHitId is "widget_gallery:list" or "widget_gallery:tabs" or "widget_gallery:table";
    }

    private static bool HandleFormsInputMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 7 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll && hit.LocalHitId == "forms_input:text_area")
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = next with { FormsInputSelectedFieldIndex = Math.Clamp(next.FormsInputSelectedFieldIndex + delta, 0, 2) };
            return true;
        }

        if (gesture.Kind != TerminalMouseKind.Down ||
            gesture.Button != TerminalMouseButton.Left ||
            !hit.LocalHitId.StartsWith("forms_input:field:", StringComparison.Ordinal))
        {
            return false;
        }

        var fieldText = hit.LocalHitId["forms_input:field:".Length..];
        if (!int.TryParse(fieldText, CultureInfo.InvariantCulture, out var fieldIndex))
        {
            return false;
        }

        next = next with { FormsInputSelectedFieldIndex = Math.Clamp(fieldIndex, 0, 2) };
        return true;
    }

    private static bool HandleMacroRecorderMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 13 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content)
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                { } value when value.StartsWith("macro_recorder:timeline:", StringComparison.Ordinal) => next with
                {
                    MacroRecorderTimelineIndex = Math.Clamp(next.MacroRecorderTimelineIndex + delta, 0, 4)
                },
                "macro_recorder:scenario_runner" => next with
                {
                    MacroRecorderScenarioIndex = Math.Clamp(next.MacroRecorderScenarioIndex + delta, 0, 2)
                },
                _ => next
            };
            return hit.LocalHitId.StartsWith("macro_recorder:timeline:", StringComparison.Ordinal) ||
                hit.LocalHitId == "macro_recorder:scenario_runner";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        if (hit.LocalHitId.StartsWith("macro_recorder:timeline:", StringComparison.Ordinal))
        {
            var rowText = hit.LocalHitId["macro_recorder:timeline:".Length..];
            if (!int.TryParse(rowText, CultureInfo.InvariantCulture, out var row))
            {
                return false;
            }

            next = next with { MacroRecorderTimelineIndex = Math.Clamp(row, 0, 4) };
            return true;
        }

        if (hit.LocalHitId == "macro_recorder:scenario_runner")
        {
            next = next with { MacroRecorderScenarioIndex = Math.Clamp(next.MacroRecorderScenarioIndex + 1, 0, 2) };
            return true;
        }

        return false;
    }

    private static bool HandlePerformanceMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 14 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            hit.LocalHitId is not ("performance:list" or "performance:list:selected"))
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = next with { PerformanceSelectedIndex = Math.Clamp(next.PerformanceSelectedIndex + delta, 0, 9_999) };
            return true;
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = next with { PerformanceSelectedIndex = Math.Clamp(next.PerformanceSelectedIndex + 1, 0, 9_999) };
        return true;
    }

    private static bool HandleMarkdownMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.CurrentScreenNumber != 15 ||
            next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Scroll))
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Content ||
            !hit.LocalHitId.StartsWith("markdown:", StringComparison.Ordinal))
        {
            return false;
        }

        if (gesture.Kind == TerminalMouseKind.Scroll)
        {
            var delta = gesture.Button == TerminalMouseButton.WheelUp ? -1 : 1;
            next = hit.LocalHitId switch
            {
                "markdown:renderer" => next with
                {
                    MarkdownActivePaneIndex = 0,
                    MarkdownRendererScroll = Math.Clamp(next.MarkdownRendererScroll + delta, 0, 12)
                },
                "markdown:stream" => next with
                {
                    MarkdownActivePaneIndex = 1,
                    MarkdownStreamScroll = Math.Clamp(next.MarkdownStreamScroll + delta, 0, 12)
                },
                "markdown:unicode" => next with { MarkdownActivePaneIndex = 4 },
                _ => next
            };
            return hit.LocalHitId is "markdown:renderer" or "markdown:stream" or "markdown:unicode";
        }

        if (gesture.Button != TerminalMouseButton.Left)
        {
            return false;
        }

        next = hit.LocalHitId switch
        {
            "markdown:renderer" => next with { MarkdownActivePaneIndex = 0 },
            "markdown:stream" => next with { MarkdownActivePaneIndex = 1 },
            "markdown:detection" => next with { MarkdownActivePaneIndex = 2 },
            "markdown:style" => next with { MarkdownActivePaneIndex = 3 },
            "markdown:unicode" => next with { MarkdownActivePaneIndex = 4 },
            "markdown:wrap" => next with
            {
                MarkdownActivePaneIndex = 5,
                MarkdownWrapModeIndex = (next.MarkdownWrapModeIndex + 1) % 3
            },
            _ => next
        };
        return true;
    }

    private static bool HandleTourMouse(MouseTerminalEvent mouseEvent, DateTimeOffset now, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.Session.CommandPalette.IsOpen ||
            next.EvidenceLedgerVisible ||
            next.PerfHudVisible ||
            next.DebugVisible ||
            next.HelpVisible ||
            next.A11yPanelVisible ||
            next.Viewport.Height == 0 ||
            gesture.Row == next.Viewport.Height - 1)
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (next.CurrentScreenNumber == 1 &&
            !next.TourActive &&
            gesture.Kind is TerminalMouseKind.Scroll &&
            hit is { Layer: ShowcaseHitLayer.Overlay, UpstreamHitId: ShowcaseFrameHitRegistry.OverlayTour })
        {
            if (gesture.Button is TerminalMouseButton.WheelDown)
            {
                next = AdjustTourLandingStart(next, 1);
                return true;
            }

            if (gesture.Button is TerminalMouseButton.WheelUp)
            {
                next = AdjustTourLandingStart(next, -1);
                return true;
            }
        }

        if (gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Up) ||
            gesture.Button is not TerminalMouseButton.Left ||
            hit is not { Layer: ShowcaseHitLayer.Overlay, UpstreamHitId: ShowcaseFrameHitRegistry.OverlayTour })
        {
            return false;
        }

        if (next.TourActive)
        {
            next = next with { TourActive = false, TourPaused = false, CurrentScreenNumber = 1, LastTourAdvance = now };
            return true;
        }

        if (next.CurrentScreenNumber == 1)
        {
            next = next.StartTour(now);
            return true;
        }

        return false;
    }

    private static bool HandleOverlayMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (gesture.Kind is not TerminalMouseKind.Down ||
            gesture.Button is not TerminalMouseButton.Left ||
            next.Session.CommandPalette.IsOpen)
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.Overlay || hit.UpstreamHitId is not { } rawId)
        {
            return false;
        }

        switch (rawId)
        {
            case ShowcaseFrameHitRegistry.OverlayEvidence:
                next = next with { EvidenceLedgerVisible = false };
                return true;
            case ShowcaseFrameHitRegistry.OverlayPerfHud:
                next = next with { PerfHudVisible = false };
                return true;
            case ShowcaseFrameHitRegistry.OverlayDebug:
                next = next with { DebugVisible = false };
                return true;
            case ShowcaseFrameHitRegistry.OverlayHelpClose:
            case ShowcaseFrameHitRegistry.OverlayHelpContent:
                next = next with { HelpVisible = false };
                return true;
            case ShowcaseFrameHitRegistry.OverlayA11y:
                next = next with { A11yPanelVisible = false };
                return true;
            default:
                return false;
        }
    }

    private static bool HandleStatusMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (gesture.Kind is not TerminalMouseKind.Down ||
            gesture.Button is not TerminalMouseButton.Left ||
            next.Session.CommandPalette.IsOpen ||
            next.EvidenceLedgerVisible ||
            next.PerfHudVisible ||
            next.DebugVisible ||
            next.HelpVisible ||
            next.A11yPanelVisible ||
            next.Viewport.Height == 0 ||
            gesture.Row != next.Viewport.Height - 1)
        {
            return false;
        }

        var hit = ShowcaseFrameHitRegistry.HitTest(next, gesture.Column, gesture.Row);
        if (hit.Layer != ShowcaseHitLayer.StatusToggle || hit.UpstreamHitId is not { } rawId)
        {
            return false;
        }

        switch (rawId)
        {
            case ShowcaseFrameHitRegistry.StatusHelpToggle:
                next = next with { HelpVisible = !next.HelpVisible };
                return true;
            case ShowcaseFrameHitRegistry.StatusPaletteToggle:
                next = SyncSession(next, next.Session with
                {
                    CommandPalette = CommandPaletteController.Toggle(next.Session.CommandPalette)
                });
                return true;
            case ShowcaseFrameHitRegistry.StatusA11yToggle:
                next = next with { A11yPanelVisible = !next.A11yPanelVisible };
                return true;
            case ShowcaseFrameHitRegistry.StatusPerfToggle:
                next = next with { PerfHudVisible = !next.PerfHudVisible };
                return true;
            case ShowcaseFrameHitRegistry.StatusDebugToggle:
                next = next with { DebugVisible = !next.DebugVisible };
                return true;
            case ShowcaseFrameHitRegistry.StatusMouseToggle:
                next = next with { MouseCaptureEnabled = !next.MouseCaptureEnabled };
                return true;
            default:
                return false;
        }
    }

    private ShowcaseDemoState StartTour(DateTimeOffset now)
    {
        var startIndex = ShowcaseTourStoryboard.FirstIndexForScreen(TourStartScreen);
        var callout = ShowcaseTourStoryboard.At(startIndex);
        return this with
        {
            TourActive = true,
            TourPaused = false,
            TourStepIndex = startIndex,
            CurrentScreenNumber = callout.ScreenNumber,
            LastTourAdvance = now
        };
    }

    private ShowcaseDemoState MoveTourStep(int delta, DateTimeOffset now)
    {
        var nextIndex = Math.Clamp(TourStepIndex + delta, 0, ShowcaseTourStoryboard.Count - 1);
        var callout = ShowcaseTourStoryboard.At(nextIndex);
        return this with
        {
            TourStepIndex = nextIndex,
            CurrentScreenNumber = callout.ScreenNumber,
            LastTourAdvance = now
        };
    }

    private static ShowcaseDemoState AdjustTourLandingStart(ShowcaseDemoState state, int delta) =>
        state with
        {
            TourStartScreen = Math.Clamp(
                state.TourStartScreen + delta,
                2,
                ShowcaseCatalog.Screens.Count)
        };

    private static ShowcaseDemoState AdjustTourSpeed(ShowcaseDemoState state, double multiplier) =>
        state with { TourSpeed = Math.Clamp(state.TourSpeed * multiplier, 0.25, 4.0) };

    private static bool IsCharacter(KeyGesture gesture, char expected)
    {
        if (!gesture.IsCharacter || gesture.Character is not { } rune || gesture.Modifiers != TerminalModifiers.None)
        {
            return false;
        }

        return char.ToLowerInvariant((char)rune.Value) == char.ToLowerInvariant(expected);
    }

    private static bool IsShiftCharacter(KeyGesture gesture, char expected)
    {
        if (!gesture.IsCharacter || gesture.Character is not { } rune || gesture.Modifiers != TerminalModifiers.Shift)
        {
            return false;
        }

        return char.ToLowerInvariant((char)rune.Value) == char.ToLowerInvariant(expected);
    }

    private static bool IsControlCharacter(KeyGesture gesture, char expected)
    {
        if (!gesture.IsCharacter || gesture.Character is not { } rune)
        {
            return false;
        }

        if (!gesture.Modifiers.HasFlag(TerminalModifiers.Control))
        {
            return false;
        }

        return char.ToLowerInvariant((char)rune.Value) == char.ToLowerInvariant(expected);
    }
}

internal abstract record ShowcaseDemoMessage;

internal sealed record ShowcaseInputMessage(RuntimeInputEnvelope Input, RuntimeFrameStats RuntimeStats) : ShowcaseDemoMessage;

internal sealed record ShowcaseTimerMessage(DateTimeOffset Now, RuntimeFrameStats RuntimeStats) : ShowcaseDemoMessage;

internal sealed record ShowcaseKanbanCard(int Id, string Title, string Tag);

internal sealed record ShowcaseKanbanMove(int CardId, int FromCol, int ToCol, int FromRow, int ToRow);

internal sealed record ShowcaseKanbanState(
    IReadOnlyList<ShowcaseKanbanCard> Todo,
    IReadOnlyList<ShowcaseKanbanCard> InProgress,
    IReadOnlyList<ShowcaseKanbanCard> Done,
    int FocusCol,
    int FocusRow,
    IReadOnlyList<ShowcaseKanbanMove> History,
    IReadOnlyList<ShowcaseKanbanMove> RedoStack)
{
    public static ShowcaseKanbanState CreateDefault() =>
        new(
            [
                new(1, "Design login page", "UI"),
                new(2, "Add input validation", "Logic"),
                new(3, "Write unit tests", "QA"),
                new(4, "Set up CI pipeline", "Ops")
            ],
            [
                new(5, "Build nav component", "UI"),
                new(6, "Implement auth flow", "Logic")
            ],
            [new(7, "Project scaffolding", "Ops")],
            FocusCol: 0,
            FocusRow: 0,
            History: [],
            RedoStack: []);

    public IReadOnlyList<ShowcaseKanbanCard> Column(int col) =>
        col switch
        {
            0 => Todo,
            1 => InProgress,
            _ => Done
        };

    public bool CanUndo => History.Count > 0;

    public bool CanRedo => RedoStack.Count > 0;

    public ShowcaseKanbanState FocusLeft() =>
        FocusCol == 0 ? this : WithFocus(FocusCol - 1, FocusRow);

    public ShowcaseKanbanState FocusRight() =>
        FocusCol >= 2 ? this : WithFocus(FocusCol + 1, FocusRow);

    public ShowcaseKanbanState FocusUp() =>
        FocusRow == 0 ? this : this with { FocusRow = FocusRow - 1 };

    public ShowcaseKanbanState FocusDown()
    {
        var len = Column(FocusCol).Count;
        return len == 0 || FocusRow >= len - 1 ? this : this with { FocusRow = FocusRow + 1 };
    }

    public ShowcaseKanbanState MoveCardLeft() =>
        FocusCol == 0 ? this : MoveCard(FocusCol, FocusRow, FocusCol - 1);

    public ShowcaseKanbanState MoveCardRight() =>
        FocusCol >= 2 ? this : MoveCard(FocusCol, FocusRow, FocusCol + 1);

    public ShowcaseKanbanState Undo()
    {
        if (History.Count == 0)
        {
            return this;
        }

        var move = History[^1];
        var columns = CloneColumns();
        var currentRow = columns[move.ToCol].FindIndex(card => card.Id == move.CardId);
        if (currentRow < 0)
        {
            return this;
        }

        var card = columns[move.ToCol][currentRow];
        columns[move.ToCol].RemoveAt(currentRow);
        var insertAt = Math.Min(move.FromRow, columns[move.FromCol].Count);
        columns[move.FromCol].Insert(insertAt, card);

        var history = History.Take(History.Count - 1).ToArray();
        var redo = RedoStack.Concat([move with { FromRow = insertAt, ToRow = currentRow }]).ToArray();
        return FromColumns(columns, move.FromCol, insertAt, history, redo);
    }

    public ShowcaseKanbanState Redo()
    {
        if (RedoStack.Count == 0)
        {
            return this;
        }

        var move = RedoStack[^1];
        var columns = CloneColumns();
        var currentRow = columns[move.FromCol].FindIndex(card => card.Id == move.CardId);
        if (currentRow < 0)
        {
            return this;
        }

        var card = columns[move.FromCol][currentRow];
        columns[move.FromCol].RemoveAt(currentRow);
        var insertAt = columns[move.ToCol].Count;
        columns[move.ToCol].Add(card);

        var history = History.Concat([move with { FromRow = currentRow, ToRow = insertAt }]).ToArray();
        var redo = RedoStack.Take(RedoStack.Count - 1).ToArray();
        return FromColumns(columns, move.ToCol, insertAt, history, redo);
    }

    public ShowcaseKanbanState FocusAt(int col, int row) =>
        col is < 0 or > 2 ? this : WithFocus(col, row);

    public ShowcaseKanbanState FocusColumn(int col) =>
        col is < 0 or > 2 ? this : WithFocus(col, FocusRow);

    public ShowcaseKanbanState MoveFocusedToColumn(int toCol) =>
        toCol is < 0 or > 2 ? this : MoveCard(FocusCol, FocusRow, toCol);

    private ShowcaseKanbanState MoveCard(int fromCol, int fromRow, int toCol)
    {
        var columns = CloneColumns();
        if (fromCol == toCol || fromCol is < 0 or > 2 || toCol is < 0 or > 2 || fromRow < 0 || fromRow >= columns[fromCol].Count)
        {
            return this;
        }

        var card = columns[fromCol][fromRow];
        columns[fromCol].RemoveAt(fromRow);
        var toRow = columns[toCol].Count;
        columns[toCol].Add(card);

        var history = History.Concat([new ShowcaseKanbanMove(card.Id, fromCol, toCol, fromRow, toRow)]).ToArray();
        return FromColumns(columns, toCol, toRow, history, []);
    }

    private ShowcaseKanbanState WithFocus(int col, int row)
    {
        var len = Column(col).Count;
        var clamped = len == 0 ? 0 : Math.Clamp(row, 0, len - 1);
        return this with { FocusCol = col, FocusRow = clamped };
    }

    private List<ShowcaseKanbanCard>[] CloneColumns() =>
        [Todo.ToList(), InProgress.ToList(), Done.ToList()];

    private static ShowcaseKanbanState FromColumns(
        List<ShowcaseKanbanCard>[] columns,
        int focusCol,
        int focusRow,
        IReadOnlyList<ShowcaseKanbanMove> history,
        IReadOnlyList<ShowcaseKanbanMove> redo)
    {
        var len = columns[focusCol].Count;
        var clampedRow = len == 0 ? 0 : Math.Clamp(focusRow, 0, len - 1);
        return new ShowcaseKanbanState(
            columns[0].ToArray(),
            columns[1].ToArray(),
            columns[2].ToArray(),
            focusCol,
            clampedRow,
            history,
            redo);
    }
}

internal enum ShowcasePaletteLabMatchFilter
{
    All,
    Exact,
    Prefix,
    WordStart,
    Substring,
    Fuzzy
}

internal static class ShowcasePaletteLabMatchFilterExtensions
{
    public static ShowcasePaletteLabMatchFilter Next(this ShowcasePaletteLabMatchFilter filter) =>
        filter switch
        {
            ShowcasePaletteLabMatchFilter.All => ShowcasePaletteLabMatchFilter.Exact,
            ShowcasePaletteLabMatchFilter.Exact => ShowcasePaletteLabMatchFilter.Prefix,
            ShowcasePaletteLabMatchFilter.Prefix => ShowcasePaletteLabMatchFilter.WordStart,
            ShowcasePaletteLabMatchFilter.WordStart => ShowcasePaletteLabMatchFilter.Substring,
            ShowcasePaletteLabMatchFilter.Substring => ShowcasePaletteLabMatchFilter.Fuzzy,
            _ => ShowcasePaletteLabMatchFilter.All
        };
}
