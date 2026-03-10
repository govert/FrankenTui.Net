using FrankenTui.A11y;
using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Text;
using FrankenTui.Web;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public static class HostedParitySurface
{
    public static IWidget Create(HostedParitySession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var description = Describe(session);
        var focus = session.InputState.EffectiveFocusId;
        var scenarioLabels = Enum.GetValues<HostedParityScenarioId>()
            .Select(id => Describe(session with { ScenarioId = id }).Label)
            .ToArray();

        return new PaddingWidget(
            new StackWidget(
                LayoutDirection.Vertical,
                [
                    (LayoutConstraint.Fixed(1), new TabsWidget
                    {
                        Tabs = scenarioLabels,
                        SelectedIndex = Array.IndexOf(Enum.GetValues<HostedParityScenarioId>(), session.ScenarioId),
                        FocusedIndex = string.Equals(focus, "tabs", StringComparison.Ordinal)
                            ? Array.IndexOf(Enum.GetValues<HostedParityScenarioId>(), session.ScenarioId)
                            : -1,
                        HoveredIndex = session.InputState.PointerRow == 0
                            ? HoveredTabIndex(session.InputState.PointerColumn)
                            : -1
                    }),
                    (LayoutConstraint.Fixed(1), BuildStatusRow(session, description)),
                    (LayoutConstraint.Fixed(10), new StackWidget(
                        LayoutDirection.Horizontal,
                        [
                            (LayoutConstraint.Percentage(28), new PanelWidget
                            {
                                Title = "Modules",
                                Child = new ListWidget
                                {
                                    Items = description.Modules,
                                    SelectedIndex = session.SelectedModuleIndex,
                                    FocusedIndex = string.Equals(focus, "modules", StringComparison.Ordinal) ? session.SelectedModuleIndex : -1
                                }
                            }),
                            (LayoutConstraint.Percentage(34), new PanelWidget
                            {
                                Title = "Metrics",
                                Child = new TableWidget
                                {
                                    Headers = ["Metric", "Value"],
                                    Rows = description.Metrics.Select(metric => (IReadOnlyList<string>)new[] { metric.Label, metric.Value }).ToArray(),
                                    SelectedRow = session.SelectedMetricIndex,
                                    FocusedRow = string.Equals(focus, "metrics", StringComparison.Ordinal) ? session.SelectedMetricIndex : -1
                                }
                            }),
                            (LayoutConstraint.Fill(), new PanelWidget
                            {
                                Title = "Plan",
                                Child = new TreeWidget
                                {
                                    Nodes =
                                    [
                                        new TreeNode(
                                            "Hosted parity",
                                            description.WorkstreamCodes.Select(code => new TreeNode(code, [])).ToArray())
                                    ]
                                }
                            })
                        ])),
                    (LayoutConstraint.Fixed(5), new StackWidget(
                        LayoutDirection.Horizontal,
                        [
                            (LayoutConstraint.Percentage(45), new PanelWidget
                            {
                                Title = "Events",
                                Child = new TextAreaWidget
                                {
                                    Document = TextDocument.FromString(string.Join(Environment.NewLine, description.EventLog)),
                                    Cursor = new TextCursor(session.SelectedEventIndex, 0),
                                    HasFocus = string.Equals(focus, "events", StringComparison.Ordinal),
                                    StatusText = session.InputState.LiveRegionText
                                }
                            }),
                            (LayoutConstraint.Fill(), new PanelWidget
                            {
                                Title = DetailTitle(session.ScenarioId),
                                Child = BuildDetailWidget(session, description, focus)
                            })
                        ])),
                    (LayoutConstraint.Fill(), new ParagraphWidget(
                        "Keys: Tab focus  Left/Right scenario  Up/Down select  Enter announce  Mouse tabs  q quit"))
                ]),
            Sides.All(1));
    }

    public static HostedParityDescription Describe(HostedParitySession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var catalog = HostedParityText.CreateCatalog(session.InputState.Language);
        var language = session.InputState.Language;
        var direction = session.InputState.FlowDirection == WidgetFlowDirection.RightToLeft ? "rtl" : "ltr";
        var label = HostedParityText.Resolve(catalog, ScenarioLabelKey(session.ScenarioId), session.ScenarioId.ToString());
        var summary = HostedParityText.Resolve(catalog, ScenarioSummaryKey(session.ScenarioId), "Hosted parity baseline");
        var modules = ScenarioModuleKeys(session.ScenarioId)
            .Select(key => HostedParityText.Resolve(catalog, key, key))
            .ToArray();
        var workstreamCodes = ScenarioWorkstreams(session.ScenarioId);
        var metrics = BuildMetrics(session, direction);
        var eventLog = BuildEventLog(session, label);
        var accessibility = BuildAccessibility(session, label, modules, eventLog);

        return new HostedParityDescription(
            session.ScenarioId,
            label,
            summary,
            workstreamCodes,
            modules,
            metrics,
            eventLog,
            accessibility,
            language,
            direction);
    }

    public static WebRenderOptions CreateWebOptions(HostedParitySession session)
    {
        var description = Describe(session);
        return new WebRenderOptions(
            $"FrankenTui.Net {description.Label}",
            description.Language,
            description.Direction,
            $"Hosted parity {description.Label} surface",
            description.Accessibility,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["scenario"] = description.ScenarioId.ToString().ToLowerInvariant(),
                ["mode"] = session.InlineMode ? "inline" : "alternate",
                ["focus"] = session.InputState.EffectiveFocusId ?? "none",
                ["steps"] = session.StepCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
    }

    private static IWidget BuildStatusRow(HostedParitySession session, HostedParityDescription description) =>
        new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Fill(), new StatusWidget
                {
                    Label = "Host",
                    Value = session.InlineMode ? "inline" : "alternate",
                    IsHealthy = true
                }),
                (LayoutConstraint.Fill(), new StatusWidget
                {
                    Label = "Lang",
                    Value = description.Language,
                    IsHealthy = true
                }),
                (LayoutConstraint.Fill(), new StatusWidget
                {
                    Label = "Dir",
                    Value = description.Direction,
                    IsHealthy = true
                }),
                (LayoutConstraint.Fill(), new StatusWidget
                {
                    Label = "Step",
                    Value = session.StepCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    IsHealthy = true
                })
            ]);

    private static IReadOnlyList<HostedParityMetric> BuildMetrics(HostedParitySession session, string direction) =>
        session.ScenarioId == HostedParityScenarioId.Extras
            ? ExtrasShowcaseFactory.BuildMetrics(session)
            :
            [
                new HostedParityMetric("Surface", session.ScenarioId.ToString()),
                new HostedParityMetric("Focus", session.InputState.EffectiveFocusId ?? "none"),
                new HostedParityMetric("Mode", session.InlineMode ? "inline" : "alternate-screen"),
                new HostedParityMetric("Language", session.InputState.Language),
                new HostedParityMetric("Direction", direction),
                new HostedParityMetric("Segments", TextSegmenter.Segment(session.InputState.LiveRegionText).Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new HostedParityMetric("Search", TextSearch.FindAllNormalized(TextDocument.FromString(session.InputState.LiveRegionText), "focus").Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new HostedParityMetric("Shaping", "aot-safe"),
                new HostedParityMetric("Evidence", session.AppliedEvents.Count > 0 ? "capturing" : "baseline", session.AppliedEvents.Count > 0)
            ];

    private static IReadOnlyList<string> BuildEventLog(HostedParitySession session, string label)
    {
        if (session.AppliedEvents.Count == 0)
        {
            return
            [
                $"Scenario {label} is at rest.",
                "No replay input has been applied yet.",
                "Use the hosted parity script or arrow keys to move."
            ];
        }

        return session.AppliedEvents
            .Select(FormatEvent)
            .Prepend($"Scenario {label} step {session.StepCount}")
            .Take(6)
            .ToArray();
    }

    private static AccessibilitySnapshot BuildAccessibility(
        HostedParitySession session,
        string label,
        IReadOnlyList<string> modules,
        IReadOnlyList<string> eventLog)
    {
        var snapshot = new AccessibilitySnapshot()
            .Add("tablist", "Hosted parity scenarios", $"Selected tab {label}")
            .Add("list", "Hosted parity modules", $"Focused module {modules[Math.Min(session.SelectedModuleIndex, Math.Max(modules.Count - 1, 0))]}")
            .Add("table", "Hosted parity metrics", $"Current focus {session.InputState.EffectiveFocusId ?? "none"}")
            .Add("log", "Hosted parity events", eventLog.FirstOrDefault())
            .Add("note", "Hosted parity live region", session.InputState.LiveRegionText);
        return snapshot;
    }

    private static string FormatEvent(TerminalEvent terminalEvent) =>
        terminalEvent switch
        {
            KeyTerminalEvent keyEvent => $"key {keyEvent.Gesture.Key}",
            MouseTerminalEvent mouseEvent => $"mouse {mouseEvent.Gesture.Kind} {mouseEvent.Gesture.Column},{mouseEvent.Gesture.Row}",
            HoverTerminalEvent hoverEvent => $"hover {hoverEvent.Column},{hoverEvent.Row}",
            PasteTerminalEvent pasteEvent => $"paste {pasteEvent.Text}",
            FocusTerminalEvent focusEvent => focusEvent.Focused ? "focus gained" : "focus lost",
            ResizeTerminalEvent resizeEvent => $"resize {resizeEvent.Size.Width}x{resizeEvent.Size.Height}",
            _ => terminalEvent.GetType().Name
        };

    private static string ScenarioLabelKey(HostedParityScenarioId scenarioId) => scenarioId switch
    {
        HostedParityScenarioId.Overview => "scenario.overview.label",
        HostedParityScenarioId.Interaction => "scenario.interaction.label",
        HostedParityScenarioId.Tooling => "scenario.tooling.label",
        HostedParityScenarioId.Extras => "scenario.extras.label",
        _ => throw new ArgumentOutOfRangeException(nameof(scenarioId), scenarioId, "Unknown hosted parity scenario.")
    };

    private static string ScenarioSummaryKey(HostedParityScenarioId scenarioId) => scenarioId switch
    {
        HostedParityScenarioId.Overview => "scenario.overview.summary",
        HostedParityScenarioId.Interaction => "scenario.interaction.summary",
        HostedParityScenarioId.Tooling => "scenario.tooling.summary",
        HostedParityScenarioId.Extras => "scenario.extras.summary",
        _ => throw new ArgumentOutOfRangeException(nameof(scenarioId), scenarioId, "Unknown hosted parity scenario.")
    };

    private static IReadOnlyList<string> ScenarioModuleKeys(HostedParityScenarioId scenarioId) => scenarioId switch
    {
        HostedParityScenarioId.Overview => ["module.core", "module.runtime", "module.web", "module.tooling"],
        HostedParityScenarioId.Interaction => ["module.focus", "module.pointer", "module.language", "module.live"],
        HostedParityScenarioId.Tooling => ["module.evidence", "module.pty", "module.doctor", "module.workflow"],
        HostedParityScenarioId.Extras => ExtrasShowcaseFactory.ModuleLabels(),
        _ => throw new ArgumentOutOfRangeException(nameof(scenarioId), scenarioId, "Unknown hosted parity scenario.")
    };

    private static IReadOnlyList<string> ScenarioWorkstreams(HostedParityScenarioId scenarioId) => scenarioId switch
    {
        HostedParityScenarioId.Overview => ["322-API", "341-WEB", "342-WEB", "361-DEM", "362-DEM"],
        HostedParityScenarioId.Interaction => ["312-WGT", "314-WGT", "315-WGT", "343-WEB", "356-VRF"],
        HostedParityScenarioId.Tooling => ["357-VRF", "359-VRF", "381-TOL", "382-TOL", "383-TOL", "384-TOL"],
        HostedParityScenarioId.Extras => ["372-EXT", "362-DEM", "356-VRF", "381-TOL"],
        _ => throw new ArgumentOutOfRangeException(nameof(scenarioId), scenarioId, "Unknown hosted parity scenario.")
    };

    private static int HoveredTabIndex(int pointerColumn) =>
        Math.Clamp(pointerColumn / 12, 0, Enum.GetValues<HostedParityScenarioId>().Length - 1);

    private static string DetailTitle(HostedParityScenarioId scenarioId) => scenarioId switch
    {
        HostedParityScenarioId.Extras => "Extras",
        _ => "Notes"
    };

    private static IWidget BuildDetailWidget(
        HostedParitySession session,
        HostedParityDescription description,
        string? focus) =>
        session.ScenarioId == HostedParityScenarioId.Extras
            ? ExtrasShowcaseFactory.CreateDetail(session)
            : new ParagraphWidget(string.Empty)
            {
                Document = BuildNotesDocument(description, focus, session.InputState.LiveRegionText),
                RenderOptions = new TextRenderOptions(TextWrapMode.Word)
            };

    private static TextDocument BuildNotesDocument(
        HostedParityDescription description,
        string? focus,
        string liveRegionText)
    {
        var live = string.IsNullOrWhiteSpace(liveRegionText) ? "no announcements" : liveRegionText;
        return TextDocument.FromMarkup(
            $"**{description.Label}** {description.Summary}\n" +
            $"_Focus_: {focus ?? "none"}  `segments={TextSegmenter.Segment(description.Summary).Count}`  `direction={description.Direction}`\n" +
            $"Live: {live}");
    }
}
