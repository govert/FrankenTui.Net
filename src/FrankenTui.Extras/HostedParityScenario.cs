using FrankenTui.A11y;
using FrankenTui.Core;
using FrankenTui.I18n;
using FrankenTui.Layout;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public enum HostedParityScenarioId
{
    Overview,
    Interaction,
    Tooling,
    Extras
}

public sealed record HostedParityMetric(string Label, string Value, bool Healthy = true);

public sealed record HostedParityDescription(
    HostedParityScenarioId ScenarioId,
    string Label,
    string Summary,
    IReadOnlyList<string> WorkstreamCodes,
    IReadOnlyList<string> Modules,
    IReadOnlyList<HostedParityMetric> Metrics,
    IReadOnlyList<string> EventLog,
    AccessibilitySnapshot Accessibility,
    string Language,
    string Direction);

public sealed record HostedParitySession(
    bool InlineMode,
    HostedParityScenarioId ScenarioId,
    WidgetInputState InputState,
    int SelectedModuleIndex = 0,
    int SelectedMetricIndex = 0,
    int SelectedEventIndex = 0,
    int StepCount = 0,
    bool OverlayVisible = false,
    bool ModalOpen = false,
    bool TaskRunning = false,
    string InputBuffer = "",
    IReadOnlyList<TerminalEvent>? AppliedEvents = null,
    IReadOnlyList<string>? SemanticLog = null,
    IReadOnlyList<string>? PolicyLog = null,
    IReadOnlyList<string>? ResizeLog = null,
    RuntimeFrameStats? RuntimeStats = null,
    CommandPaletteState? CommandPalette = null,
    LogSearchState? LogSearch = null,
    MacroRecorderState? Macro = null,
    IReadOnlyList<string>? LiveLogLines = null,
    PerformanceHudLevel HudLevel = PerformanceHudLevel.Compact,
    bool HudFrozen = false,
    PerformanceHudSnapshot? FrozenHudSnapshot = null,
    PaneWorkspaceState? PaneWorkspace = null,
    MermaidShowcasePreferences? Mermaid = null)
{
    private static readonly IReadOnlyList<string> DefaultFocusOrder = ["tabs", "modules", "metrics", "events", "notes"];
    private static readonly IReadOnlyList<string> ModalFocusOrder = ["modal.primary", "modal.dismiss"];

    public IReadOnlyList<TerminalEvent> AppliedEvents { get; init; } = AppliedEvents ?? [];
    public IReadOnlyList<string> SemanticLog { get; init; } = SemanticLog ?? [];
    public IReadOnlyList<string> PolicyLog { get; init; } = PolicyLog ?? [];
    public IReadOnlyList<string> ResizeLog { get; init; } = ResizeLog ?? [];
    public RuntimeFrameStats? RuntimeStats { get; init; } = RuntimeStats;
    public CommandPaletteState CommandPalette { get; init; } = CommandPalette ?? CommandPaletteState.Closed;
    public LogSearchState LogSearch { get; init; } = LogSearch ?? new LogSearchState(string.Empty, SearchOpen: false);
    public MacroRecorderState Macro { get; init; } = Macro ?? new MacroRecorderState();
    public IReadOnlyList<string> LiveLogLines { get; init; } = LiveLogLines ?? [];
    public PerformanceHudSnapshot? FrozenHudSnapshot { get; init; } = FrozenHudSnapshot;
    public PaneWorkspaceState PaneWorkspace { get; init; } = PaneWorkspace ?? PaneWorkspaceState.CreateDemo();
    public MermaidShowcasePreferences Mermaid { get; init; } = Mermaid ?? MermaidShowcasePreferences.Default;

    public KeybindingState CreateKeybindingState() =>
        new(
            !string.IsNullOrWhiteSpace(InputBuffer),
            TaskRunning,
            ModalOpen,
            OverlayVisible);

    public static HostedParitySession Create(
        bool inlineMode,
        HostedParityScenarioId scenarioId = HostedParityScenarioId.Overview,
        string language = "en-US",
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight)
    {
        var inputState = WidgetInputState.Default.WithFocusOrder(DefaultFocusOrder).Focus("tabs");
        inputState = inputState with
        {
            Language = language,
            FlowDirection = flowDirection
        };

        return new HostedParitySession(inlineMode, scenarioId, inputState);
    }

    public static HostedParitySession ForFrame(
        bool inlineMode,
        int frame,
        HostedParityScenarioId scenarioId = HostedParityScenarioId.Overview,
        string language = "en-US",
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight)
    {
        var session = Create(inlineMode, scenarioId, language, flowDirection);
        foreach (var terminalEvent in DefaultScript().Take(Math.Max(frame, 0)))
        {
            session = session.Advance(terminalEvent);
        }

        return session;
    }

    public HostedParitySession Advance(RuntimeInputEnvelope input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Advance(
            input.EffectiveEvent,
            input.SourceEvent,
            input.SemanticEvents,
            input.PolicyDecisions,
            input.ResizeDecision,
            input.Timestamp,
            input.IsTick);
    }

    public HostedParitySession Advance(
        TerminalEvent? terminalEvent,
        IReadOnlyList<SemanticEvent>? semanticEvents = null,
        IReadOnlyList<KeybindingDecision>? policyActions = null,
        ResizeDecision? resizeDecision = null) =>
        Advance(
            terminalEvent,
            terminalEvent,
            semanticEvents,
            policyActions,
            resizeDecision,
            terminalEvent?.Timestamp ?? DateTimeOffset.UtcNow,
            isTick: false);

    public HostedParitySession AdvanceTime(DateTimeOffset now) =>
        Advance(
            null,
            null,
            [],
            [],
            null,
            now,
            isTick: true);

    public HostedParitySession WithRuntimeStats(RuntimeFrameStats stats) =>
        this with
        {
            RuntimeStats = stats,
            FrozenHudSnapshot = HudFrozen
                ? (FrozenHudSnapshot ?? PerformanceHudSnapshot.FromRuntime(
                    stats,
                    stats.SyncOutput,
                    scrollRegion: true,
                    hyperlinks: true,
                    HudLevel == PerformanceHudLevel.Hidden ? PerformanceHudLevel.Compact : HudLevel))
                : null
        };

    private HostedParitySession Advance(
        TerminalEvent? effectiveEvent,
        TerminalEvent? recordedEvent,
        IReadOnlyList<SemanticEvent>? semanticEvents,
        IReadOnlyList<KeybindingDecision>? policyActions,
        ResizeDecision? resizeDecision,
        DateTimeOffset timestamp,
        bool isTick,
        bool fromMacroReplay = false)
    {
        var nextInput = effectiveEvent is null ? InputState : InputState.Apply(effectiveEvent);
        var nextScenario = ScenarioId;
        var nextModule = SelectedModuleIndex;
        var nextMetric = SelectedMetricIndex;
        var nextEventIndex = SelectedEventIndex;
        var focusId = nextInput.EffectiveFocusId ?? "tabs";
        var nextOverlay = OverlayVisible;
        var nextModal = ModalOpen;
        var nextTaskRunning = TaskRunning;
        var nextInputBuffer = InputBuffer;
        var semanticLog = SemanticLog;
        var policyLog = PolicyLog;
        var resizeLog = ResizeLog;
        var nextPalette = CommandPalette;
        var nextLogSearch = LogSearch;
        var nextMacro = Macro;
        var nextLiveLogLines = LiveLogLines;
        var nextHudLevel = HudLevel;
        var nextHudFrozen = HudFrozen;
        var nextFrozenHudSnapshot = FrozenHudSnapshot;
        var nextPaneWorkspace = PaneWorkspace;
        var nextMermaid = Mermaid;
        var consumedByOperator = false;

        if (effectiveEvent is KeyTerminalEvent operatorKey)
        {
            var entries = CommandPaletteRegistry.DefaultEntries();
            if (IsPaletteToggle(operatorKey.Gesture))
            {
                nextPalette = CommandPaletteController.Toggle(nextPalette);
                nextInput = nextInput.Announce(nextPalette.IsOpen ? "Command palette opened" : "Command palette closed");
                consumedByOperator = true;
            }
            else if (nextPalette.IsOpen)
            {
                var applied = CommandPaletteController.Apply(nextPalette, operatorKey, entries);
                nextPalette = applied.State;
                if (applied.Execution is { } execution)
                {
                    nextLiveLogLines = Append(nextLiveLogLines, $"palette {execution.CommandId}");
                    ApplyCommandExecution(
                        execution,
                        ref nextScenario,
                        ref nextModule,
                        ref nextOverlay,
                        ref nextTaskRunning,
                        ref nextPalette,
                        ref nextLogSearch,
                        ref nextMacro,
                        ref nextInput,
                        ref nextLiveLogLines,
                        ref nextHudLevel,
                        ref nextHudFrozen,
                        ref nextFrozenHudSnapshot);
                }

                consumedByOperator = true;
            }
            else if (ShouldOpenLogSearch(operatorKey.Gesture))
            {
                nextLogSearch = nextLogSearch with
                {
                    SearchOpen = true,
                    Error = null
                };
                nextModule = 2;
                nextInputBuffer = nextLogSearch.Query;
                nextInput = nextInput.Announce("Log search opened");
                consumedByOperator = true;
            }
            else if (nextLogSearch.SearchOpen)
            {
                nextLogSearch = LogSearchController.Apply(nextLogSearch, operatorKey);
                nextInputBuffer = nextLogSearch.Query;
                nextInput = nextInput.Announce(nextLogSearch.SearchOpen
                    ? $"Log search {nextLogSearch.Query}"
                    : "Log search closed");
                consumedByOperator = true;
            }
            else if (ShouldHandleMacro(operatorKey.Gesture))
            {
                ApplyMacroControl(
                    operatorKey,
                    timestamp,
                    ref nextMacro,
                    ref nextInput,
                    ref nextLiveLogLines);
                consumedByOperator = true;
            }
            else if (ShouldHandleHud(operatorKey.Gesture))
            {
                if (operatorKey.Gesture.IsCharacter &&
                    operatorKey.Gesture.Character is { } hudRune &&
                    string.Equals(hudRune.ToString(), "f", StringComparison.OrdinalIgnoreCase))
                {
                    nextHudFrozen = !nextHudFrozen;
                    nextFrozenHudSnapshot = nextHudFrozen && RuntimeStats is not null
                        ? PerformanceHudSnapshot.FromRuntime(
                            RuntimeStats,
                            RuntimeStats.SyncOutput,
                            scrollRegion: true,
                            hyperlinks: true,
                            nextHudLevel)
                        : null;
                    nextInput = nextInput.Announce(nextHudFrozen ? "HUD frozen" : "HUD live");
                    nextLiveLogLines = Append(nextLiveLogLines, nextHudFrozen ? "hud frozen" : "hud live");
                }
                else
                {
                    nextHudLevel = CycleHudLevel(nextHudLevel);
                    nextInput = nextInput.Announce($"HUD {nextHudLevel.ToString().ToLowerInvariant()}");
                    nextLiveLogLines = Append(nextLiveLogLines, $"hud {nextHudLevel.ToString().ToLowerInvariant()}");
                }

                consumedByOperator = true;
            }
            else if (ShouldHandlePaneWorkspace(operatorKey.Gesture))
            {
                ApplyPaneWorkspaceControl(
                    operatorKey,
                    timestamp,
                    ref nextPaneWorkspace,
                    ref nextInput,
                    ref nextLiveLogLines);
                consumedByOperator = true;
            }
            else if (ShouldHandleMermaid(operatorKey.Gesture))
            {
                ApplyMermaidControl(
                    operatorKey,
                    ref nextMermaid,
                    ref nextInput,
                    ref nextLiveLogLines);
                consumedByOperator = true;
            }
        }

        switch (effectiveEvent)
        {
            case KeyTerminalEvent keyEvent when keyEvent.Gesture.Key == TerminalKey.Left:
                if (consumedByOperator)
                {
                    break;
                }

                nextScenario = MoveScenario(-1);
                nextInput = nextInput.Announce($"Scenario: {nextScenario}");
                break;
            case KeyTerminalEvent keyEvent when keyEvent.Gesture.Key == TerminalKey.Right:
                if (consumedByOperator)
                {
                    break;
                }

                nextScenario = MoveScenario(1);
                nextInput = nextInput.Announce($"Scenario: {nextScenario}");
                break;
            case KeyTerminalEvent keyEvent when keyEvent.Gesture.Key == TerminalKey.Up:
                if (consumedByOperator)
                {
                    break;
                }

                if (string.Equals(focusId, "events", StringComparison.Ordinal))
                {
                    nextEventIndex--;
                }
                else if (string.Equals(focusId, "metrics", StringComparison.Ordinal))
                {
                    nextMetric--;
                }
                else
                {
                    nextModule--;
                }

                break;
            case KeyTerminalEvent keyEvent when keyEvent.Gesture.Key == TerminalKey.Down:
                if (consumedByOperator)
                {
                    break;
                }

                if (string.Equals(focusId, "events", StringComparison.Ordinal))
                {
                    nextEventIndex++;
                }
                else if (string.Equals(focusId, "metrics", StringComparison.Ordinal))
                {
                    nextMetric++;
                }
                else
                {
                    nextModule++;
                }

                break;
            case KeyTerminalEvent keyEvent when keyEvent.Gesture.Key == TerminalKey.Enter:
                if (consumedByOperator)
                {
                    break;
                }

                nextInput = nextInput.Announce($"Activated {focusId}");
                break;
            case MouseTerminalEvent mouseEvent:
                if (consumedByOperator)
                {
                    break;
                }

                nextInput = nextInput.Announce($"Pointer {mouseEvent.Gesture.Kind.ToString().ToLowerInvariant()} at {mouseEvent.Gesture.Column},{mouseEvent.Gesture.Row}");
                if (mouseEvent.Gesture.Row == 0)
                {
                    nextScenario = ScenarioFromColumn(mouseEvent.Gesture.Column);
                }

                break;
            case KeyTerminalEvent keyEvent when keyEvent.Gesture.Key == TerminalKey.Backspace:
                if (consumedByOperator)
                {
                    break;
                }

                nextInputBuffer = nextInputBuffer.Length == 0 ? string.Empty : nextInputBuffer[..^1];
                break;
            case KeyTerminalEvent keyEvent when keyEvent.Gesture.IsCharacter &&
                                               keyEvent.Gesture.Character is { } rune &&
                                               keyEvent.Gesture.Modifiers == TerminalModifiers.None:
                if (consumedByOperator)
                {
                    break;
                }

                switch (rune.ToString().ToLowerInvariant())
                {
                    case "m":
                        nextModal = !nextModal;
                        nextInput = nextModal
                            ? nextInput.PushFocusTrap(ModalFocusOrder, "modal.primary").Announce("Modal opened")
                            : nextInput.PopFocusTrap().Announce("Modal closed");
                        break;
                    case "o":
                        nextOverlay = !nextOverlay;
                        nextInput = nextInput.Announce(nextOverlay ? "Overlay visible" : "Overlay hidden");
                        break;
                    case "t":
                        nextTaskRunning = !nextTaskRunning;
                        nextInput = nextInput.Announce(nextTaskRunning ? "Task running" : "Task idle");
                        break;
                    default:
                        nextInputBuffer += rune.ToString();
                        break;
                }

                break;
            case PasteTerminalEvent pasteEvent:
                nextInputBuffer += pasteEvent.Text.ReplaceLineEndings(" ");
                break;
        }

        foreach (var action in policyActions ?? [])
        {
            policyLog = Append(policyLog, FormatPolicyAction(action));
            switch (action.Action)
            {
                case KeybindingAction.DismissModal:
                    nextModal = false;
                    nextInput = nextInput.PopFocusTrap().Announce("Modal dismissed");
                    break;
                case KeybindingAction.ClearInput:
                    nextInputBuffer = string.Empty;
                    nextInput = nextInput.Announce("Input cleared");
                    break;
                case KeybindingAction.CancelTask:
                    nextTaskRunning = false;
                    nextInput = nextInput.Announce("Task cancelled");
                    break;
                case KeybindingAction.CloseOverlay:
                    nextOverlay = false;
                    nextInput = nextInput.Announce("Overlay closed");
                    break;
                case KeybindingAction.ToggleTreeView:
                    nextOverlay = !nextOverlay;
                    nextInput = nextInput.Announce(nextOverlay ? "Overlay enabled" : "Overlay disabled");
                    break;
                case KeybindingAction.Bell:
                    nextInput = nextInput.Announce("Bell");
                    break;
                case KeybindingAction.Noop:
                    nextInput = nextInput.Announce("No-op");
                    break;
            }
        }

        foreach (var semanticEvent in semanticEvents ?? [])
        {
            semanticLog = Append(semanticLog, FormatSemanticEvent(semanticEvent));
            nextInput = nextInput.Announce(FormatSemanticAnnouncement(semanticEvent));
        }

        if (resizeDecision is not null)
        {
            resizeLog = Append(resizeLog, FormatResizeDecision(resizeDecision));
        }

        if (nextMacro.Recording && recordedEvent is not null && !fromMacroReplay && ShouldRecordMacroEvent(recordedEvent))
        {
            nextMacro = MacroRecorderController.Capture(nextMacro, recordedEvent);
        }

        var provisional = this with
        {
            ScenarioId = nextScenario,
            InputState = nextInput,
            OverlayVisible = nextOverlay,
            ModalOpen = nextModal,
            TaskRunning = nextTaskRunning,
            InputBuffer = nextInputBuffer,
            SemanticLog = semanticLog,
            PolicyLog = policyLog,
            ResizeLog = resizeLog,
            CommandPalette = nextPalette,
            LogSearch = nextLogSearch,
            Macro = nextMacro,
            LiveLogLines = nextLiveLogLines,
            HudLevel = nextHudLevel,
            HudFrozen = nextHudFrozen,
            FrozenHudSnapshot = nextFrozenHudSnapshot,
            PaneWorkspace = nextPaneWorkspace,
            Mermaid = nextMermaid
        };
        var description = HostedParitySurface.Describe(provisional);
        var moduleCount = description.Modules.Count;
        var metricCount = description.Metrics.Count;
        var eventCount = description.EventLog.Count;
        var nextAppliedEvents = recordedEvent is null ? AppliedEvents : AppliedEvents.Concat([recordedEvent]).ToArray();
        var hasActivity = effectiveEvent is not null ||
            (semanticEvents?.Count ?? 0) > 0 ||
            (policyActions?.Count ?? 0) > 0 ||
            resizeDecision is not null ||
            consumedByOperator ||
            (isTick && nextMacro.Playing);

        var nextSession = this with
        {
            ScenarioId = nextScenario,
            InputState = nextInput,
            SelectedModuleIndex = Wrap(nextModule, moduleCount),
            SelectedMetricIndex = Wrap(nextMetric, metricCount),
            SelectedEventIndex = Wrap(nextEventIndex, eventCount),
            StepCount = hasActivity ? StepCount + 1 : StepCount,
            OverlayVisible = nextOverlay,
            ModalOpen = nextModal,
            TaskRunning = nextTaskRunning,
            InputBuffer = nextInputBuffer,
            AppliedEvents = nextAppliedEvents,
            SemanticLog = semanticLog,
            PolicyLog = policyLog,
            ResizeLog = resizeLog,
            CommandPalette = nextPalette,
            LogSearch = nextLogSearch,
            Macro = nextMacro,
            LiveLogLines = nextLiveLogLines,
            HudLevel = nextHudLevel,
            HudFrozen = nextHudFrozen,
            FrozenHudSnapshot = nextHudFrozen ? nextFrozenHudSnapshot : null,
            PaneWorkspace = nextPaneWorkspace,
            Mermaid = nextMermaid
        };

        if (isTick && nextSession.Macro.Playing)
        {
            var macroTick = MacroRecorderController.Tick(nextSession.Macro, timestamp);
            nextSession = nextSession with
            {
                Macro = macroTick.State,
                LiveLogLines = macroTick.EmittedEvents.Count == 0
                    ? nextSession.LiveLogLines
                    : Append(nextSession.LiveLogLines, $"macro emitted {macroTick.EmittedEvents.Count} event(s)")
            };

            foreach (var emittedEvent in macroTick.EmittedEvents)
            {
                nextSession = nextSession.Advance(
                    emittedEvent,
                    emittedEvent,
                    [],
                    [],
                    null,
                    emittedEvent.Timestamp,
                    isTick: false,
                    fromMacroReplay: true);
            }
        }

        return nextSession;
    }

    public static IReadOnlyList<TerminalEvent> DefaultScript() =>
    [
        TerminalEvent.Key(new KeyGesture(TerminalKey.Tab, TerminalModifiers.None)),
        TerminalEvent.Key(new KeyGesture(TerminalKey.Down, TerminalModifiers.None)),
        TerminalEvent.Key(new KeyGesture(TerminalKey.Tab, TerminalModifiers.None)),
        TerminalEvent.Key(new KeyGesture(TerminalKey.Right, TerminalModifiers.None)),
        TerminalEvent.Hover(14, 0, stable: true),
        TerminalEvent.Mouse(new MouseGesture(14, 0, TerminalMouseButton.Left, TerminalMouseKind.Down)),
        TerminalEvent.Key(new KeyGesture(TerminalKey.Tab, TerminalModifiers.None)),
        TerminalEvent.Paste("Hosted parity evidence")
    ];

    private static bool IsPaletteToggle(KeyGesture gesture) =>
        gesture.IsCharacter &&
        gesture.Character is { } rune &&
        (string.Equals(rune.ToString(), "k", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(rune.ToString(), "p", StringComparison.OrdinalIgnoreCase)) &&
        gesture.Modifiers.HasFlag(TerminalModifiers.Control);

    private static bool ShouldOpenLogSearch(KeyGesture gesture) =>
        gesture.IsCharacter &&
        gesture.Character is { } rune &&
        rune.ToString() == "/" &&
        gesture.Modifiers == TerminalModifiers.None;

    private bool ShouldHandleMacro(KeyGesture gesture) =>
        ScenarioId == HostedParityScenarioId.Extras &&
        (SelectedModuleIndex == 3 || Macro.Mode is MacroRecorderMode.Recording or MacroRecorderMode.Playing or MacroRecorderMode.Ready) &&
        gesture.Modifiers == TerminalModifiers.None &&
        (gesture.Key == TerminalKey.Escape ||
         (gesture.IsCharacter && gesture.Character is { } rune &&
          rune.ToString() is "r" or "R" or "p" or "P" or "l" or "L" or "+" or "-" or "="));

    private bool ShouldHandleHud(KeyGesture gesture) =>
        ScenarioId == HostedParityScenarioId.Extras &&
        (SelectedModuleIndex == 4 || HudFrozen) &&
        gesture.Modifiers == TerminalModifiers.None &&
        (gesture.Key == TerminalKey.F2 ||
         (gesture.IsCharacter && gesture.Character is { } rune &&
          rune.ToString() is "f" or "F"));

    private bool ShouldHandlePaneWorkspace(KeyGesture gesture) =>
        ScenarioId == HostedParityScenarioId.Extras &&
        SelectedModuleIndex == 0 &&
        gesture.Modifiers == TerminalModifiers.None &&
        (gesture.Key is TerminalKey.Up or TerminalKey.Down ||
         (gesture.IsCharacter && gesture.Character is { } rune &&
          rune.ToString() is "[" or "]" or "+" or "=" or "-" or "x" or "X"));

    private bool ShouldHandleMermaid(KeyGesture gesture) =>
        ScenarioId == HostedParityScenarioId.Extras &&
        SelectedModuleIndex == 7 &&
        gesture.Modifiers == TerminalModifiers.None &&
        (gesture.Key is TerminalKey.Up or TerminalKey.Down ||
         (gesture.IsCharacter && gesture.Character is { } rune &&
          rune.ToString() is "l" or "L" or "t" or "T" or "g" or "G" or "s" or "S" or "w" or "W" or "m" or "M" or "c" or "C"));

    private static void ApplyMacroControl(
        KeyTerminalEvent keyEvent,
        DateTimeOffset timestamp,
        ref MacroRecorderState nextMacro,
        ref WidgetInputState nextInput,
        ref IReadOnlyList<string> nextLiveLogLines)
    {
        if (keyEvent.Gesture.Key == TerminalKey.Escape)
        {
            nextMacro = MacroRecorderController.Stop(nextMacro, "Macro stopped.");
            nextInput = nextInput.Announce("Macro stopped");
            nextLiveLogLines = Append(nextLiveLogLines, "macro stopped");
            return;
        }

        var value = keyEvent.Gesture.Character?.ToString();
        switch (value)
        {
            case "r":
            case "R":
                nextMacro = MacroRecorderController.ToggleRecording(nextMacro, timestamp);
                nextInput = nextInput.Announce(nextMacro.Status);
                nextLiveLogLines = Append(nextLiveLogLines, nextMacro.Status);
                break;
            case "p":
            case "P":
                nextMacro = MacroRecorderController.TogglePlay(nextMacro, timestamp);
                nextInput = nextInput.Announce(nextMacro.Status);
                nextLiveLogLines = Append(nextLiveLogLines, nextMacro.Status);
                break;
            case "l":
            case "L":
                nextMacro = MacroRecorderController.ToggleLoop(nextMacro);
                nextInput = nextInput.Announce(nextMacro.Status);
                nextLiveLogLines = Append(nextLiveLogLines, nextMacro.Status);
                break;
            case "+":
            case "=":
                nextMacro = MacroRecorderController.AdjustSpeed(nextMacro, 0.25);
                nextInput = nextInput.Announce(nextMacro.Status);
                nextLiveLogLines = Append(nextLiveLogLines, nextMacro.Status);
                break;
            case "-":
                nextMacro = MacroRecorderController.AdjustSpeed(nextMacro, -0.25);
                nextInput = nextInput.Announce(nextMacro.Status);
                nextLiveLogLines = Append(nextLiveLogLines, nextMacro.Status);
                break;
        }
    }

    private static void ApplyPaneWorkspaceControl(
        KeyTerminalEvent keyEvent,
        DateTimeOffset timestamp,
        ref PaneWorkspaceState nextPaneWorkspace,
        ref WidgetInputState nextInput,
        ref IReadOnlyList<string> nextLiveLogLines)
    {
        var action = keyEvent.Gesture.Key switch
        {
            TerminalKey.Up => PaneWorkspaceActionKind.SelectPrevious,
            TerminalKey.Down => PaneWorkspaceActionKind.SelectNext,
            _ => keyEvent.Gesture.Character?.ToString() switch
            {
                "[" => PaneWorkspaceActionKind.Undo,
                "]" => PaneWorkspaceActionKind.Redo,
                "+" or "=" => PaneWorkspaceActionKind.GrowPrimary,
                "-" => PaneWorkspaceActionKind.ShrinkPrimary,
                "x" or "X" => PaneWorkspaceActionKind.CycleMode,
                _ => (PaneWorkspaceActionKind?)null
            }
        };

        if (action is null)
        {
            return;
        }

        nextPaneWorkspace = nextPaneWorkspace.Apply(
            new PaneWorkspaceAction(action.Value, timestamp, "hosted-parity"));
        nextInput = nextInput.Announce(
            $"Pane {nextPaneWorkspace.SelectedPaneId} {nextPaneWorkspace.Mode.ToString().ToLowerInvariant()} {nextPaneWorkspace.PrimaryRatioPermille}");
        nextLiveLogLines = Append(
            nextLiveLogLines,
            $"pane {action.Value.ToString().ToLowerInvariant()} cursor={nextPaneWorkspace.TimelineCursor}");
    }

    private static void ApplyMermaidControl(
        KeyTerminalEvent keyEvent,
        ref MermaidShowcasePreferences nextMermaid,
        ref WidgetInputState nextInput,
        ref IReadOnlyList<string> nextLiveLogLines)
    {
        if (keyEvent.Gesture.Key == TerminalKey.Up)
        {
            nextMermaid = nextMermaid with
            {
                SelectedSampleIndex = Math.Max(nextMermaid.SelectedSampleIndex - 1, 0)
            };
            nextInput = nextInput.Announce($"Mermaid sample {nextMermaid.SelectedSampleIndex + 1}");
            return;
        }

        if (keyEvent.Gesture.Key == TerminalKey.Down)
        {
            nextMermaid = nextMermaid with
            {
                SelectedSampleIndex = nextMermaid.SelectedSampleIndex + 1
            };
            nextInput = nextInput.Announce($"Mermaid sample {nextMermaid.SelectedSampleIndex + 1}");
            return;
        }

        switch (keyEvent.Gesture.Character?.ToString())
        {
            case "l":
            case "L":
                nextMermaid = nextMermaid with
                {
                    LayoutMode = nextMermaid.LayoutMode switch
                    {
                        MermaidLayoutMode.Auto => MermaidLayoutMode.Dense,
                        MermaidLayoutMode.Dense => MermaidLayoutMode.Spacious,
                        _ => MermaidLayoutMode.Auto
                    }
                };
                break;
            case "t":
            case "T":
                nextMermaid = nextMermaid with
                {
                    Fidelity = nextMermaid.Fidelity switch
                    {
                        MermaidTier.Auto => MermaidTier.Rich,
                        MermaidTier.Rich => MermaidTier.Normal,
                        MermaidTier.Normal => MermaidTier.Compact,
                        MermaidTier.Compact => MermaidTier.Auto,
                        _ => MermaidTier.Auto
                    }
                };
                break;
            case "g":
            case "G":
                nextMermaid = nextMermaid with
                {
                    GlyphMode = nextMermaid.GlyphMode == MermaidGlyphMode.Unicode
                        ? MermaidGlyphMode.Ascii
                        : MermaidGlyphMode.Unicode
                };
                break;
            case "s":
            case "S":
                nextMermaid = nextMermaid with { StylesEnabled = !nextMermaid.StylesEnabled };
                break;
            case "w":
            case "W":
                nextMermaid = nextMermaid with
                {
                    WrapMode = nextMermaid.WrapMode switch
                    {
                        MermaidWrapMode.Word => MermaidWrapMode.Char,
                        MermaidWrapMode.Char => MermaidWrapMode.WordChar,
                        MermaidWrapMode.WordChar => MermaidWrapMode.None,
                        _ => MermaidWrapMode.Word
                    }
                };
                break;
            case "m":
            case "M":
                nextMermaid = nextMermaid with { MetricsVisible = !nextMermaid.MetricsVisible };
                break;
            case "c":
            case "C":
                nextMermaid = nextMermaid with { ControlsVisible = !nextMermaid.ControlsVisible };
                break;
            default:
                return;
        }

        nextInput = nextInput.Announce(
            $"Mermaid {nextMermaid.LayoutMode.ToString().ToLowerInvariant()} {nextMermaid.Fidelity.ToString().ToLowerInvariant()}");
        nextLiveLogLines = Append(
            nextLiveLogLines,
            $"mermaid sample={nextMermaid.SelectedSampleIndex + 1} glyph={nextMermaid.GlyphMode.ToString().ToLowerInvariant()}");
    }

    private static void ApplyCommandExecution(
        CommandPaletteExecution execution,
        ref HostedParityScenarioId nextScenario,
        ref int nextModule,
        ref bool nextOverlay,
        ref bool nextTaskRunning,
        ref CommandPaletteState nextPalette,
        ref LogSearchState nextLogSearch,
        ref MacroRecorderState nextMacro,
        ref WidgetInputState nextInput,
        ref IReadOnlyList<string> nextLiveLogLines,
        ref PerformanceHudLevel nextHudLevel,
        ref bool nextHudFrozen,
        ref PerformanceHudSnapshot? nextFrozenHudSnapshot)
    {
        switch (execution.CommandId)
        {
            case "goto-dashboard":
                nextScenario = HostedParityScenarioId.Overview;
                nextModule = 0;
                nextInput = nextInput.Announce("Dashboard opened");
                break;
            case "toggle-tree":
                nextOverlay = !nextOverlay;
                nextInput = nextInput.Announce(nextOverlay ? "Tree view visible" : "Tree view hidden");
                break;
            case "run-doctor":
                nextTaskRunning = true;
                nextLiveLogLines = Append(nextLiveLogLines, "doctor replay refreshed");
                nextInput = nextInput.Announce("Doctor run queued");
                break;
            case "open-log-search":
                nextLogSearch = nextLogSearch with { SearchOpen = true };
                nextModule = 2;
                nextInput = nextInput.Announce("Log search opened");
                break;
            case "show-perf-hud":
                nextHudLevel = nextHudLevel == PerformanceHudLevel.Hidden ? PerformanceHudLevel.Full : CycleHudLevel(nextHudLevel);
                nextHudFrozen = false;
                nextFrozenHudSnapshot = null;
                nextInput = nextInput.Announce($"HUD {nextHudLevel.ToString().ToLowerInvariant()}");
                break;
            case "record-macro":
                nextMacro = MacroRecorderController.ToggleRecording(nextMacro, DateTimeOffset.UtcNow);
                nextModule = 3;
                nextInput = nextInput.Announce(nextMacro.Status);
                break;
            case "switch-language":
                nextInput = nextInput with
                {
                    Language = string.Equals(nextInput.Language, "en-US", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "en-US",
                    FlowDirection = nextInput.FlowDirection == WidgetFlowDirection.LeftToRight
                        ? WidgetFlowDirection.RightToLeft
                        : WidgetFlowDirection.LeftToRight
                };
                nextInput = nextInput.Announce($"Language {nextInput.Language}");
                break;
        }
    }

    private static bool ShouldRecordMacroEvent(TerminalEvent terminalEvent) =>
        terminalEvent switch
        {
            KeyTerminalEvent keyEvent when keyEvent.Gesture.IsCharacter &&
                                          keyEvent.Gesture.Character is { } rune &&
                                          rune.ToString() is "r" or "R" => false,
            _ => true
        };

    private static PerformanceHudLevel CycleHudLevel(PerformanceHudLevel level) => level switch
    {
        PerformanceHudLevel.Hidden => PerformanceHudLevel.Compact,
        PerformanceHudLevel.Compact => PerformanceHudLevel.Full,
        PerformanceHudLevel.Full => PerformanceHudLevel.Minimal,
        _ => PerformanceHudLevel.Hidden
    };

    private HostedParityScenarioId MoveScenario(int delta)
    {
        var scenarios = Enum.GetValues<HostedParityScenarioId>();
        var index = Array.IndexOf(scenarios, ScenarioId);
        index = Wrap(index + delta, scenarios.Length);
        return scenarios[index];
    }

    private static HostedParityScenarioId ScenarioFromColumn(int column)
    {
        var scenarios = Enum.GetValues<HostedParityScenarioId>();
        var index = Math.Clamp(column / 12, 0, scenarios.Length - 1);
        return scenarios[index];
    }

    private static int Wrap(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        var wrapped = index % count;
        return wrapped < 0 ? wrapped + count : wrapped;
    }

    private static IReadOnlyList<string> Append(IReadOnlyList<string> source, string item) =>
        source.Concat([item]).TakeLast(12).ToArray();

    private static string FormatSemanticEvent(SemanticEvent semanticEvent) =>
        semanticEvent switch
        {
            ClickSemanticEvent click => $"semantic click {click.Button} {click.Position.Column},{click.Position.Row}",
            DoubleClickSemanticEvent click => $"semantic double-click {click.Button} {click.Position.Column},{click.Position.Row}",
            TripleClickSemanticEvent click => $"semantic triple-click {click.Button} {click.Position.Column},{click.Position.Row}",
            LongPressSemanticEvent longPress => $"semantic long-press {longPress.Position.Column},{longPress.Position.Row} {longPress.Duration.TotalMilliseconds:0}ms",
            DragStartSemanticEvent dragStart => $"semantic drag-start {dragStart.Position.Column},{dragStart.Position.Row}",
            DragMoveSemanticEvent dragMove => $"semantic drag-move {dragMove.To.Column},{dragMove.To.Row}",
            DragEndSemanticEvent dragEnd => $"semantic drag-end {dragEnd.Position.Column},{dragEnd.Position.Row}",
            DragCancelSemanticEvent dragCancel => $"semantic drag-cancel {dragCancel.Reason}",
            ChordSemanticEvent chord => $"semantic chord {string.Join('+', chord.Sequence.Select(static key => key.IsCharacter && key.Character is { } rune ? rune.ToString() : key.Key.ToString()))}",
            _ => semanticEvent.GetType().Name
        };

    private static string FormatSemanticAnnouncement(SemanticEvent semanticEvent) =>
        semanticEvent switch
        {
            ChordSemanticEvent chord => $"Chord: {string.Join('+', chord.Sequence.Select(static key => key.IsCharacter && key.Character is { } rune ? rune.ToString() : key.Key.ToString()))}",
            DragStartSemanticEvent => "Drag started",
            DragEndSemanticEvent => "Drag ended",
            ClickSemanticEvent => "Click",
            DoubleClickSemanticEvent => "Double click",
            TripleClickSemanticEvent => "Triple click",
            LongPressSemanticEvent => "Long press",
            _ => "Semantic input"
        };

    private static string FormatPolicyAction(KeybindingDecision decision) =>
        $"policy {decision.Action} ({decision.Reason})";

    private static string FormatResizeDecision(ResizeDecision decision) =>
        $"resize {decision.Action} {decision.Size.Width}x{decision.Size.Height} regime={decision.Regime} reason={decision.Reason}";
}

public static class HostedParityText
{
    public static LocalizationCatalog CreateCatalog(string language)
    {
        var catalog = new LocalizationCatalog()
            .Add("scenario.overview.label", "Overview")
            .Add("scenario.overview.summary", "Shared demo, web, and doctor surfaces now route through one deterministic hosted-parity view pipeline.")
            .Add("scenario.interaction.label", "Interaction")
            .Add("scenario.interaction.summary", "Focus routing, pointer state, language, and live-region hints flow through one session model.")
            .Add("scenario.tooling.label", "Tooling")
            .Add("scenario.tooling.summary", "Doctor output, evidence capture, web rendering, and CI verification share the same core-facing entry points.")
            .Add("module.core", "Core + Render baseline")
            .Add("module.runtime", "Runtime + widgets")
            .Add("module.web", "Web + WASM host")
            .Add("module.tooling", "Doctor + CI")
            .Add("module.focus", "Focus and tab order")
            .Add("module.pointer", "Pointer and hover state")
            .Add("module.language", "Language and direction")
            .Add("module.live", "Live-region output")
            .Add("module.evidence", "Replay and evidence bundle")
            .Add("module.pty", "PTY and transcript checks")
            .Add("module.doctor", "Doctor artifact output")
            .Add("module.workflow", "Workflow regression gate");

        if (language.StartsWith("de", StringComparison.OrdinalIgnoreCase))
        {
            catalog
                .Add("scenario.overview.label", "Uebersicht")
                .Add("scenario.overview.summary", "Demo-, Web- und Doctor-Oberflaechen laufen jetzt ueber eine gemeinsame deterministische Hosted-Parity-Pipeline.")
                .Add("scenario.interaction.label", "Interaktion")
                .Add("scenario.interaction.summary", "Fokus, Pointer-Zustand, Sprache und Live-Region-Hinweise laufen durch ein gemeinsames Sitzungsmodell.")
                .Add("scenario.tooling.label", "Werkzeuge")
                .Add("scenario.tooling.summary", "Doctor-Ausgabe, Evidence-Capture, Web-Rendering und CI-Pruefung nutzen dieselben Kern-Einstiegspunkte.")
                .Add("scenario.extras.label", "Extras")
                .Add("scenario.extras.summary", "Markdown, Export, Formulare, Validierung, Hilfe und Traceback-Oberflaechen sind jetzt in die .NET-Portoberflaeche eingebunden.");
        }
        else
        {
            catalog
                .Add("scenario.extras.label", "Extras")
                .Add("scenario.extras.summary", "Markdown, export, forms, validation, help, and traceback surfaces now participate in the .NET port.");
        }

        return catalog;
    }

    public static string Resolve(LocalizationCatalog catalog, string key, string fallback) =>
        catalog.Resolve(new LocalizedString(key, fallback));
}
