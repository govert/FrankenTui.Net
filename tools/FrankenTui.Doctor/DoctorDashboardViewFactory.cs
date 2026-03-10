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
                        Tabs = ["Doctor", "Environment", "Parity"],
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
                                        new[] { "Sync output", report.SupportsSyncOutput ? "yes" : "no" }
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
                ["operating-system"] = report.OperatingSystem
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
