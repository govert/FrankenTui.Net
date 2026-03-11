using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Runtime;
using FrankenTui.Text;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

internal static class ExtrasShowcaseFactory
{
    private static readonly IReadOnlyList<string> DemoLogLines =
    [
        "08:00:01 info  doctor replay refreshed",
        "08:00:02 warn  pane snapshot drift detected",
        "08:00:03 info  macro capture ready",
        "08:00:04 debug perf hud compact frame=11.8ms",
        "08:00:05 info  command palette ranked 7 results",
        "08:00:06 error log search regex parse failure",
        "08:00:07 info  web parity evidence exported"
    ];

    private static readonly IReadOnlyList<FormTextField> DemoFields =
    [
        new("repo", "Repository", "FrankenTui.Net", "Port surface"),
        new("owner", "Owner", "govert", "GitHub organization"),
        new("seed", "Seed", "42", "Deterministic sample")
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<TextValidator>> DemoValidators =
        new Dictionary<string, IReadOnlyList<TextValidator>>(StringComparer.Ordinal)
        {
            ["repo"] = [ValidationRules.Required(), ValidationRules.MinLength(6)],
            ["owner"] = [ValidationRules.Required()],
            ["seed"] = [ValidationRules.Required(), ValidationRules.ContainsDigit()]
        };

    private const string MarkdownSample =
        """
        # FrankenTui.Net Extras
        - **markdown** document rendering
        - _forms_ and validation summaries
        - [web export](https://github.com/govert/FrankenTui.Net)
        > Extras stay additive to the base runtime.

        ```csharp
        var frame = Ui.RenderHostedParity(width: 72, height: 20);
        ```
        """;

    public static IReadOnlyList<HostedParityMetric> BuildMetrics(HostedParitySession session)
    {
        var validation = FormValidator.Validate(DemoFields, DemoValidators);
        var markdown = MarkdownDocumentBuilder.Parse(MarkdownSample);
        var workspace = BuildPaneWorkspace(session);
        var paletteResults = CommandPaletteSearch.Search(
            CommandPaletteRegistry.DefaultEntries(),
            EffectiveQuery(session));
        var searchState = new LogSearchState(EffectiveQuery(session), RegexMode: session.OverlayVisible, ContextLines: session.TaskRunning ? 1 : 0);
        var searchResult = LogSearchEngine.Apply(DemoLogLines, searchState);
        var macro = MacroRecorder.FromEvents("extras-demo", session.AppliedEvents, "Hosted parity extras sample");
        var hud = PerformanceHudSnapshot.FromSession(session);
        var mermaid = MermaidShowcaseSurface.BuildState(session);
        var export = BufferExport.Capture(
            new ParagraphWidget(string.Empty)
            {
                Document = markdown,
                RenderOptions = new TextRenderOptions(TextWrapMode.Word)
            },
            new Size(36, 8));
        var stripped = ConsoleText.StripAnsi("\u001b[32mextras\u001b[0m ready");

        return
        [
            new HostedParityMetric("Module", ModuleLabels()[Math.Min(session.SelectedModuleIndex, ModuleLabels().Length - 1)]),
            new HostedParityMetric("PaneHash", workspace.SnapshotHash()),
            new HostedParityMetric("Palette", paletteResults.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new HostedParityMetric("LogSearch", searchResult.MatchCount.ToString(System.Globalization.CultureInfo.InvariantCulture), string.IsNullOrWhiteSpace(searchResult.Error)),
            new HostedParityMetric("Macro", macro.Events.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new HostedParityMetric("HUD", hud.DegradationLevel),
            new HostedParityMetric("Markdown", markdown.Lines.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new HostedParityMetric("Validation", validation.Messages.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), !validation.HasErrors),
            new HostedParityMetric("ExportHtml", export.HtmlLength.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new HostedParityMetric("Console", stripped),
            ..MermaidShowcaseSurface.BuildMetrics(session)
        ];
    }

    public static IWidget CreateDetail(HostedParitySession session)
    {
        var selection = Math.Min(session.SelectedModuleIndex, ModuleLabels().Length - 1);
        return selection switch
        {
            0 => BuildPaneWorkspaceDetail(session),
            1 => BuildCommandPaletteDetail(session),
            2 => BuildLogSearchDetail(session),
            3 => BuildMacroRecorderDetail(session),
            4 => BuildPerformanceHudDetail(session),
            5 => BuildMarkdownExportDetail(),
            6 => BuildFormsValidationDetail(),
            7 => BuildMermaidShowcaseDetail(session),
            _ => BuildTracebackConsoleDetail()
        };
    }

    public static string[] ModuleLabels() =>
        [
            "Pane Workspace",
            "Command Palette",
            "Log Search",
            "Macro Recorder",
            "Performance HUD",
            "Markdown + Export",
            "Forms + Validation",
            "Mermaid Showcase",
            "Traceback + Console"
        ];

    private static IWidget BuildPaneWorkspaceDetail(HostedParitySession session) =>
        new PaneWorkspaceWidget
        {
            Workspace = BuildPaneWorkspace(session)
        };

    private static IWidget BuildCommandPaletteDetail(HostedParitySession session)
    {
        var query = EffectiveQuery(session);
        var results = CommandPaletteSearch.Search(CommandPaletteRegistry.DefaultEntries(), query);
        return new CommandPaletteWidget
        {
            Query = query,
            Results = results,
            SelectedIndex = Math.Min(session.SelectedMetricIndex, Math.Max(results.Count - 1, 0)),
            ShowPreview = true
        };
    }

    private static IWidget BuildLogSearchDetail(HostedParitySession session) =>
        new LogSearchWidget
        {
            State = new LogSearchState(
                EffectiveQuery(session),
                RegexMode: session.OverlayVisible,
                CaseSensitive: session.ModalOpen,
                ContextLines: session.TaskRunning ? 1 : 0),
            SourceLines = DemoLogLines
        };

    private static IWidget BuildMacroRecorderDetail(HostedParitySession session) =>
        new MacroRecorderWidget
        {
            State = new MacroRecorderState(
                Recording: session.TaskRunning,
                Playing: session.OverlayVisible,
                Loop: session.ModalOpen,
                Speed: session.InlineMode ? 1.0 : 2.0,
                Macro: MacroRecorder.FromEvents("extras-demo", session.AppliedEvents, "Hosted parity extras sample"),
                Status: session.TaskRunning ? "Recording... (Esc to stop)" : "Macro ready")
        };

    private static IWidget BuildPerformanceHudDetail(HostedParitySession session) =>
        new PerformanceHudWidget
        {
            Snapshot = PerformanceHudSnapshot.FromSession(session)
        };

    private static IWidget BuildMarkdownExportDetail()
    {
        var document = MarkdownDocumentBuilder.Parse(MarkdownSample);
        var export = BufferExport.Capture(
            new ParagraphWidget(string.Empty)
            {
                Document = document,
                RenderOptions = new TextRenderOptions(TextWrapMode.Word)
            },
            new Size(32, 8));

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage(68), new PanelWidget
                {
                    Title = "Markdown",
                    Child = new ParagraphWidget(string.Empty)
                    {
                        Document = document,
                        RenderOptions = new TextRenderOptions(TextWrapMode.Word)
                    }
                }),
                (LayoutConstraint.Fill(), new PanelWidget
                {
                    Title = "Export",
                    Child = new ParagraphWidget($"text={export.TextLength} html={export.HtmlLength}")
                })
            ]);
    }

    private static IWidget BuildFormsValidationDetail()
    {
        var validation = FormValidator.Validate(DemoFields, DemoValidators);
        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage(58), new PanelWidget
                {
                    Title = "Form",
                    Child = new FormWidget
                    {
                        Fields = DemoFields,
                        Validation = validation,
                        SelectedFieldIndex = 1
                    }
                }),
                (LayoutConstraint.Fill(), new PanelWidget
                {
                    Title = "Validation",
                    Child = new ValidationSummaryWidget
                    {
                        Validation = validation
                    }
                })
            ]);
    }

    private static IWidget BuildTracebackConsoleDetail()
    {
        var exception = new InvalidOperationException(
            "Parity evidence is stale.",
            new ApplicationException("Refresh doctor and replay artifacts."));
        var transcript = ConsoleText.StripAnsi("\u001b[36mdoctor\u001b[0m summary: parity drift detected");

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage(68), new PanelWidget
                {
                    Title = "Traceback",
                    Child = new TracebackWidget
                    {
                        Exception = exception
                    }
                }),
                (LayoutConstraint.Fill(), new PanelWidget
                {
                    Title = "Console",
                    Child = new ParagraphWidget(transcript)
                })
            ]);
    }

    private static IWidget BuildMermaidShowcaseDetail(HostedParitySession session) =>
        MermaidShowcaseSurface.CreateWidget(MermaidShowcaseSurface.BuildState(session));

    private static PaneWorkspaceState BuildPaneWorkspace(HostedParitySession session)
    {
        var actions = new List<PaneWorkspaceAction>();
        for (var index = 0; index < session.StepCount; index++)
        {
            actions.Add(new PaneWorkspaceAction(
                (index % 3) switch
                {
                    0 => PaneWorkspaceActionKind.SelectNext,
                    1 => PaneWorkspaceActionKind.CycleMode,
                    _ => index % 2 == 0 ? PaneWorkspaceActionKind.GrowPrimary : PaneWorkspaceActionKind.ShrinkPrimary
                },
                DateTimeOffset.UnixEpoch + TimeSpan.FromMilliseconds(index * 32),
                "extras-demo"));
        }

        return PaneWorkspaceState.CreateDemo().Replay(actions);
    }

    private static string EffectiveQuery(HostedParitySession session) =>
        string.IsNullOrWhiteSpace(session.InputBuffer) ? "do" : session.InputBuffer;
}
