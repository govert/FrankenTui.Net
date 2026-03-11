using FrankenTui.A11y;
using FrankenTui.Core;
using FrankenTui.I18n;
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
    IReadOnlyList<string>? ResizeLog = null)
{
    private static readonly IReadOnlyList<string> DefaultFocusOrder = ["tabs", "modules", "metrics", "events", "notes"];

    public IReadOnlyList<TerminalEvent> AppliedEvents { get; init; } = AppliedEvents ?? [];
    public IReadOnlyList<string> SemanticLog { get; init; } = SemanticLog ?? [];
    public IReadOnlyList<string> PolicyLog { get; init; } = PolicyLog ?? [];
    public IReadOnlyList<string> ResizeLog { get; init; } = ResizeLog ?? [];

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
            input.ResizeDecision);
    }

    public HostedParitySession Advance(
        TerminalEvent? terminalEvent,
        IReadOnlyList<SemanticEvent>? semanticEvents = null,
        IReadOnlyList<KeybindingDecision>? policyActions = null,
        ResizeDecision? resizeDecision = null) =>
        Advance(terminalEvent, terminalEvent, semanticEvents, policyActions, resizeDecision);

    private HostedParitySession Advance(
        TerminalEvent? effectiveEvent,
        TerminalEvent? recordedEvent,
        IReadOnlyList<SemanticEvent>? semanticEvents,
        IReadOnlyList<KeybindingDecision>? policyActions,
        ResizeDecision? resizeDecision)
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

        switch (effectiveEvent)
        {
            case KeyTerminalEvent keyEvent when keyEvent.Gesture.Key == TerminalKey.Left:
                nextScenario = MoveScenario(-1);
                nextInput = nextInput.Announce($"Scenario: {nextScenario}");
                break;
            case KeyTerminalEvent keyEvent when keyEvent.Gesture.Key == TerminalKey.Right:
                nextScenario = MoveScenario(1);
                nextInput = nextInput.Announce($"Scenario: {nextScenario}");
                break;
            case KeyTerminalEvent keyEvent when keyEvent.Gesture.Key == TerminalKey.Up:
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
                nextInput = nextInput.Announce($"Activated {focusId}");
                break;
            case MouseTerminalEvent mouseEvent:
                nextInput = nextInput.Announce($"Pointer {mouseEvent.Gesture.Kind.ToString().ToLowerInvariant()} at {mouseEvent.Gesture.Column},{mouseEvent.Gesture.Row}");
                if (mouseEvent.Gesture.Row == 0)
                {
                    nextScenario = ScenarioFromColumn(mouseEvent.Gesture.Column);
                }

                break;
            case KeyTerminalEvent keyEvent when keyEvent.Gesture.Key == TerminalKey.Backspace:
                nextInputBuffer = nextInputBuffer.Length == 0 ? string.Empty : nextInputBuffer[..^1];
                break;
            case KeyTerminalEvent keyEvent when keyEvent.Gesture.IsCharacter &&
                                               keyEvent.Gesture.Character is { } rune &&
                                               keyEvent.Gesture.Modifiers == TerminalModifiers.None:
                switch (rune.ToString().ToLowerInvariant())
                {
                    case "m":
                        nextModal = !nextModal;
                        nextInput = nextInput.Announce(nextModal ? "Modal opened" : "Modal closed");
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
                    nextInput = nextInput.Announce("Modal dismissed");
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

        var moduleCount = HostedParitySurface.Describe(this with { ScenarioId = nextScenario, InputState = nextInput }).Modules.Count;
        var metricCount = HostedParitySurface.Describe(this with { ScenarioId = nextScenario, InputState = nextInput }).Metrics.Count;
        var eventCount = HostedParitySurface.Describe(this with { ScenarioId = nextScenario, InputState = nextInput }).EventLog.Count;
        var nextAppliedEvents = recordedEvent is null ? AppliedEvents : AppliedEvents.Concat([recordedEvent]).ToArray();
        var hasActivity = effectiveEvent is not null ||
            (semanticEvents?.Count ?? 0) > 0 ||
            (policyActions?.Count ?? 0) > 0 ||
            resizeDecision is not null;

        return this with
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
            ResizeLog = resizeLog
        };
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
