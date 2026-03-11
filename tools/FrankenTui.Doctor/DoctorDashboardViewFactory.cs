using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Text;
using FrankenTui.Web;
using FrankenTui.Widgets;

namespace FrankenTui.Doctor;

public static class DoctorDashboardViewFactory
{
    public static IWidget Build(DoctorReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new PaddingWidget(
            new StackWidget(
                LayoutDirection.Vertical,
                [
                    (LayoutConstraint.Fixed(1), new TabsWidget
                    {
                        Tabs = ["Doctor", "Environment", "Contracts"],
                        SelectedIndex = 0,
                        FocusedIndex = 0
                    }),
                    (LayoutConstraint.Fixed(1), new StackWidget(
                        LayoutDirection.Horizontal,
                        [
                            (LayoutConstraint.Fill(), new StatusWidget
                            {
                                Label = "OS",
                                Value = report.OperatingSystem,
                                IsHealthy = true
                            }),
                            (LayoutConstraint.Fill(), new StatusWidget
                            {
                                Label = "Host",
                                Value = report.HostProfile,
                                IsHealthy = true
                            }),
                            (LayoutConstraint.Fill(), new StatusWidget
                            {
                                Label = "Host status",
                                Value = report.HostValidationStatus,
                                IsHealthy = !string.Equals(report.HostValidationStatus, "blocked", StringComparison.Ordinal)
                            }),
                            (LayoutConstraint.Fill(), new StatusWidget
                            {
                                Label = "SIMD",
                                Value = report.SimdEnabled ? "enabled" : report.SimdSupported ? "available" : "off",
                                IsHealthy = report.SimdSupported
                            }),
                            (LayoutConstraint.Fill(), new StatusWidget
                            {
                                Label = "Mux",
                                Value = report.InMux ? "yes" : "no",
                                IsHealthy = !report.InMux
                            }),
                            (LayoutConstraint.Fill(), new StatusWidget
                            {
                                Label = "Telemetry",
                                Value = report.Telemetry?.Enabled == true ? "on" : "off",
                                IsHealthy = report.Telemetry?.Warnings.Count == 0
                            }),
                            (LayoutConstraint.Fill(), new StatusWidget
                            {
                                Label = "Contracts",
                                Value = report.OpenTuiMigration?.Status ?? "missing",
                                IsHealthy = string.Equals(report.OpenTuiMigration?.Status, "ready", StringComparison.Ordinal)
                            })
                        ])),
                    (LayoutConstraint.Fixed(8), new StackWidget(
                        LayoutDirection.Horizontal,
                        [
                            (LayoutConstraint.Percentage(44), new PanelWidget
                            {
                                Title = "Capabilities",
                                Child = new TableWidget
                                {
                                    Headers = ["Metric", "Value"],
                                    Rows =
                                    [
                                        new[] { "Runtime", report.RuntimeVersion },
                                        new[] { "TERM", report.Term },
                                        new[] { "Validation", report.HostValidationStatus },
                                        new[] { "SIMD", report.SimdEnabled ? "enabled" : report.SimdSupported ? "available" : "off" },
                                        new[] { "Hyperlinks", report.SupportsHyperlinks ? "yes" : "no" },
                                        new[] { "Sync output", report.SupportsSyncOutput ? "yes" : "no" },
                                        new[] { "Telemetry", report.Telemetry?.EnabledReason ?? "none" },
                                        new[] { "Mermaid", report.Mermaid?.Status ?? "missing" },
                                        new[] { "OpenTUI", report.OpenTuiMigration?.Status ?? "missing" }
                                    ],
                                    SelectedRow = 0,
                                    FocusedRow = 0
                                }
                            }),
                            (LayoutConstraint.Fill(), new PanelWidget
                            {
                                Title = "Notes",
                                Child = new TextAreaWidget
                                {
                                    Document = TextDocument.FromString(string.Join(Environment.NewLine, report.Notes)),
                                    Cursor = new TextCursor(0, 0),
                                    StatusText = report.Recommendations?.FirstOrDefault()
                                }
                            })
                        ])),
                    (LayoutConstraint.Fixed(6), new PanelWidget
                    {
                        Title = "Contracts",
                        Child = new ParagraphWidget(
                            string.Join(
                                Environment.NewLine,
                                [
                                    $"Telemetry: {(report.Telemetry?.Enabled == true ? "enabled" : "disabled")} {report.Telemetry?.Protocol} {report.Telemetry?.EndpointSource}",
                                    $"Mermaid: {report.Mermaid?.Status ?? "missing"} glyph={report.Mermaid?.GlyphMode ?? "n/a"} tier={report.Mermaid?.TierOverride ?? "n/a"}",
                                    $"OpenTUI: {report.OpenTuiMigration?.Status ?? "missing"} clauses={report.OpenTuiMigration?.ClauseCount.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "0"} cells={report.OpenTuiMigration?.PolicyCellCount.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "0"}",
                                    $"Warnings: {string.Join(" | ", report.Telemetry?.Warnings ?? [])}",
                                    $"Mermaid issues: {string.Join(" | ", report.Mermaid?.ValidationIssues ?? [])}",
                                    $"OpenTUI issues: {string.Join(" | ", report.OpenTuiMigration?.Issues ?? [])}"
                                ]))
                    }),
                    (LayoutConstraint.Fill(), new ParagraphWidget(
                        $"Recommendations: {string.Join(" | ", report.Recommendations ?? [])}"))
                ]),
            Sides.All(1));
    }

    public static WebRenderOptions CreateWebOptions(DoctorReport report) =>
        new(
            "FrankenTui.Net Doctor",
            "en-US",
            "ltr",
            "FrankenTui doctor dashboard",
            null,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["host-profile"] = report.HostProfile,
                ["host-validation-status"] = report.HostValidationStatus,
                ["operating-system"] = report.OperatingSystem,
                ["telemetry-enabled"] = report.Telemetry?.Enabled == true ? "true" : "false",
                ["mermaid-status"] = report.Mermaid?.Status ?? "missing",
                ["opentui-status"] = report.OpenTuiMigration?.Status ?? "missing"
            });

    public static string RenderText(DoctorReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return string.Join(
            Environment.NewLine,
            [
                "FrankenTui.Net Doctor",
                $"OS: {report.OperatingSystem}",
                $"Runtime: {report.RuntimeVersion}",
                $"TERM: {report.Term}",
                $"Host: {report.HostProfile}",
                $"Host status: {report.HostValidationStatus}",
                $"SIMD: {(report.SimdEnabled ? "enabled" : report.SimdSupported ? "available" : "off")} ({report.SimdSummary})",
                $"Hyperlinks: {(report.SupportsHyperlinks ? "yes" : "no")}",
                $"Sync output: {(report.SupportsSyncOutput ? "yes" : "no")}",
                $"In mux: {(report.InMux ? "yes" : "no")}",
                $"Telemetry: {(report.Telemetry?.Enabled == true ? "enabled" : "disabled")} reason={report.Telemetry?.EnabledReason ?? "none"} protocol={report.Telemetry?.Protocol ?? "n/a"} endpoint={report.Telemetry?.Endpoint ?? "(none)"}",
                $"Telemetry warnings: {string.Join(" | ", report.Telemetry?.Warnings ?? [])}",
                $"Mermaid: {report.Mermaid?.Status ?? "missing"} glyph={report.Mermaid?.GlyphMode ?? "n/a"} tier={report.Mermaid?.TierOverride ?? "n/a"} samples={report.Mermaid?.SampleCount.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "0"}",
                $"Mermaid issues: {string.Join(" | ", report.Mermaid?.ValidationIssues ?? [])}",
                $"OpenTUI migration: {report.OpenTuiMigration?.Status ?? "missing"} clauses={report.OpenTuiMigration?.ClauseCount.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "0"} cells={report.OpenTuiMigration?.PolicyCellCount.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "0"}",
                $"OpenTUI issues: {string.Join(" | ", report.OpenTuiMigration?.Issues ?? [])}",
                $"Host evidence: {string.Join(" | ", report.HostEvidenceSources ?? [])}",
                $"Host divergences: {string.Join(" | ", report.KnownHostDivergences ?? [])}",
                $"Capability overrides: {string.Join(" | ", report.CapabilityOverrides ?? [])}",
                $"Notes: {string.Join(" | ", report.Notes)}",
                $"Recommendations: {string.Join(" | ", report.Recommendations ?? [])}",
                report.ArtifactPaths is null
                    ? "Artifacts: not written"
                    : $"Artifacts: {string.Join(" | ", report.ArtifactPaths.Select(static entry => $"{entry.Key}={entry.Value}"))}"
            ]);
    }
}
