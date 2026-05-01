using FrankenTui.Core;
using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

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

        if (input.EffectiveEvent is MouseTerminalEvent tourMouseEvent &&
            HandleTourMouse(tourMouseEvent, input.Timestamp, ref next))
        {
            return SyncSession(next, next.Session.WithRuntimeStats(runtimeStats));
        }

        if (input.EffectiveEvent is MouseTerminalEvent statusMouseEvent &&
            HandleStatusMouse(statusMouseEvent, ref next))
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

        if (gesture.Kind is TerminalMouseKind.Scroll && gesture.Row == 1)
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

        if (gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Up) ||
            gesture.Button is not TerminalMouseButton.Left)
        {
            return false;
        }

        if (gesture.Row == 0)
        {
            var labels = ShowcaseCatalog.Categories.Select(static category => category.ToString()).ToArray();
            if (TryResolveTabIndex(labels, gesture.Column, out var categoryIndex))
            {
                next = StopTourAndSelect(
                    next,
                    ShowcaseCatalog.FirstInCategory(ShowcaseCatalog.Categories[categoryIndex]));
                return true;
            }
        }

        if (gesture.Row == 1)
        {
            var currentScreenNumber = next.CurrentScreenNumber;
            var window = ShowcaseCatalog.WindowAround(currentScreenNumber);
            var labels = window.Select(screen => screen.Number == currentScreenNumber
                ? $"{screen.Number}:{screen.ShortLabel}"
                : screen.Number.ToString()).ToArray();
            if (TryResolveTabIndex(labels, gesture.Column, out var screenIndex))
            {
                next = StopTourAndSelect(next, window[screenIndex].Number);
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveTabIndex(
        IReadOnlyList<string> labels,
        ushort column,
        out int tabIndex)
    {
        var start = 0;
        for (var index = 0; index < labels.Count; index++)
        {
            var length = labels[index].Length + 2;
            if (column >= start && column < start + length)
            {
                tabIndex = index;
                return true;
            }

            start += length + 1;
        }

        tabIndex = -1;
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

    private static bool HandlePaneMouse(MouseTerminalEvent mouseEvent, ref ShowcaseDemoState next)
    {
        var gesture = mouseEvent.Gesture;
        if (next.Session.CommandPalette.IsOpen ||
            next.TourActive ||
            next.CurrentScreenNumber != 2 ||
            next.Viewport.Height <= 4 ||
            gesture.Row >= next.Viewport.Height - 1 ||
            gesture.Kind is not (TerminalMouseKind.Down or TerminalMouseKind.Up) ||
            gesture.Button is not TerminalMouseButton.Left)
        {
            return false;
        }

        if (!TryResolveDashboardPaneLink(next.Viewport, gesture.Column, gesture.Row, out var targetScreenNumber))
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

    private static bool TryResolveDashboardPaneLink(Size viewport, ushort column, ushort row, out int targetScreenNumber)
    {
        targetScreenNumber = 0;
        if (viewport.Width < 20 || viewport.Height < 8)
        {
            return false;
        }

        var rightColumnStart = Math.Max(1, viewport.Width * 52 / 100);
        if (column < rightColumnStart || row < 5)
        {
            return false;
        }

        var linkIndex = row - 5;
        targetScreenNumber = linkIndex switch
        {
            0 => 1,
            1 => 39,
            2 => 16,
            3 => 6,
            4 => 40,
            _ => 0
        };
        return targetScreenNumber != 0;
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

        if (next.CurrentScreenNumber == 1 && !next.TourActive && gesture.Kind is TerminalMouseKind.Scroll)
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
            gesture.Button is not TerminalMouseButton.Left)
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

        if (gesture.Column < 6)
        {
            next = next with { HelpVisible = !next.HelpVisible };
            return true;
        }

        if (gesture.Column < 15)
        {
            next = SyncSession(next, next.Session with
            {
                CommandPalette = CommandPaletteController.Toggle(next.Session.CommandPalette)
            });
            return true;
        }

        if (gesture.Column < 22)
        {
            next = next with { A11yPanelVisible = !next.A11yPanelVisible };
            return true;
        }

        if (gesture.Column < 29)
        {
            next = next with { PerfHudVisible = !next.PerfHudVisible };
            return true;
        }

        if (gesture.Column < 37)
        {
            next = next with { DebugVisible = !next.DebugVisible };
            return true;
        }

        if (gesture.Column < 47)
        {
            next = next with { EvidenceLedgerVisible = !next.EvidenceLedgerVisible };
            return true;
        }

        if (gesture.Column < 56)
        {
            next = next with { MouseCaptureEnabled = !next.MouseCaptureEnabled };
            return true;
        }

        return false;
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
