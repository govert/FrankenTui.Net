using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Runtime;
using FrankenTui.Text;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

internal static class ExtrasShowcaseFactory
{
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

    private static readonly IReadOnlyList<HelpEntry> HelpEntries =
    [
        new("Tab", "Move focus"),
        new("Left/Right", "Change scenario"),
        new("Up/Down", "Select module"),
        new("Enter", "Announce focus"),
        new("q", "Quit interactive mode")
    ];

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
        var export = BufferExport.Capture(
            new ParagraphWidget(string.Empty)
            {
                Document = markdown,
                RenderOptions = new TextRenderOptions(TextWrapMode.Word)
            },
            new Size(36, 8));
        var countdown = new CountdownTimerSnapshot("Countdown", TimeSpan.FromSeconds(Math.Max(75 - session.StepCount * 3, 0)));
        var stripped = ConsoleText.StripAnsi("\u001b[32mextras\u001b[0m ready");

        return
        [
            new HostedParityMetric("Module", ModuleLabels()[Math.Min(session.SelectedModuleIndex, ModuleLabels().Length - 1)]),
            new HostedParityMetric("Markdown", markdown.Lines.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new HostedParityMetric("Validation", validation.Messages.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), !validation.HasErrors),
            new HostedParityMetric("ExportHtml", export.HtmlLength.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new HostedParityMetric("Countdown", countdown.Display, !countdown.IsExpired),
            new HostedParityMetric("Console", stripped)
        ];
    }

    public static IWidget CreateDetail(HostedParitySession session)
    {
        var selection = Math.Min(session.SelectedModuleIndex, ModuleLabels().Length - 1);
        return selection switch
        {
            0 => BuildMarkdownExportDetail(),
            1 => BuildFormsValidationDetail(),
            2 => BuildHelpTimingDetail(session),
            _ => BuildTracebackConsoleDetail()
        };
    }

    public static string[] ModuleLabels() =>
        ["Markdown + Export", "Forms + Validation", "Help + Timing", "Traceback + Console"];

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

    private static IWidget BuildHelpTimingDetail(HostedParitySession session)
    {
        var countdown = new CountdownTimerSnapshot("Release", TimeSpan.FromSeconds(Math.Max(90 - session.StepCount * 4, 0)));
        var stopwatch = new StopwatchSnapshot("Replay", TimeSpan.FromMilliseconds(session.StepCount * 145));

        return new StackWidget(
            LayoutDirection.Horizontal,
            [
                (LayoutConstraint.Percentage(66), new PanelWidget
                {
                    Title = "Help",
                    Child = new HelpWidget
                    {
                        Entries = HelpEntries,
                        HighlightedIndex = session.StepCount % HelpEntries.Count
                    }
                }),
                (LayoutConstraint.Fill(), new PanelWidget
                {
                    Title = "Timing",
                    Child = new ParagraphWidget($"{countdown.Display}  {stopwatch.Display}")
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
}
