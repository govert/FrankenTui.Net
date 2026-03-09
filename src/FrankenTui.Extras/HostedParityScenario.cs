using FrankenTui.A11y;
using FrankenTui.Core;
using FrankenTui.I18n;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public enum HostedParityScenarioId
{
    Overview,
    Interaction,
    Tooling
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
    IReadOnlyList<TerminalEvent>? AppliedEvents = null)
{
    private static readonly IReadOnlyList<string> DefaultFocusOrder = ["tabs", "modules", "metrics", "events", "notes"];

    public IReadOnlyList<TerminalEvent> AppliedEvents { get; init; } = AppliedEvents ?? [];

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

    public HostedParitySession Advance(TerminalEvent terminalEvent)
    {
        ArgumentNullException.ThrowIfNull(terminalEvent);

        var nextInput = InputState.Apply(terminalEvent);
        var nextScenario = ScenarioId;
        var nextModule = SelectedModuleIndex;
        var nextMetric = SelectedMetricIndex;
        var nextEventIndex = SelectedEventIndex;
        var focusId = nextInput.EffectiveFocusId ?? "tabs";

        switch (terminalEvent)
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
        }

        var moduleCount = HostedParitySurface.Describe(this with { ScenarioId = nextScenario, InputState = nextInput }).Modules.Count;
        var metricCount = HostedParitySurface.Describe(this with { ScenarioId = nextScenario, InputState = nextInput }).Metrics.Count;
        var eventCount = HostedParitySurface.Describe(this with { ScenarioId = nextScenario, InputState = nextInput }).EventLog.Count;

        return this with
        {
            ScenarioId = nextScenario,
            InputState = nextInput,
            SelectedModuleIndex = Wrap(nextModule, moduleCount),
            SelectedMetricIndex = Wrap(nextMetric, metricCount),
            SelectedEventIndex = Wrap(nextEventIndex, eventCount),
            StepCount = StepCount + 1,
            AppliedEvents = AppliedEvents.Concat([terminalEvent]).ToArray()
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
        if (column < 12)
        {
            return HostedParityScenarioId.Overview;
        }

        if (column < 28)
        {
            return HostedParityScenarioId.Interaction;
        }

        return HostedParityScenarioId.Tooling;
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
                .Add("scenario.tooling.summary", "Doctor-Ausgabe, Evidence-Capture, Web-Rendering und CI-Pruefung nutzen dieselben Kern-Einstiegspunkte.");
        }

        return catalog;
    }

    public static string Resolve(LocalizationCatalog catalog, string key, string fallback) =>
        catalog.Resolve(new LocalizedString(key, fallback));
}
