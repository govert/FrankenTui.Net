using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public sealed record DoctorRunMeta(
    string Status,
    string StartedAt,
    string FinishedAt,
    double DurationSeconds,
    string Profile,
    string ProfileDescription,
    string ProjectDir,
    string RunDir,
    string Output,
    string Snapshot,
    string? TraceId,
    bool FallbackActive,
    string? FallbackReason,
    string? CaptureErrorReason,
    string? EvidenceLedger,
    string? ManifestPath,
    string? WorkflowSummaryPath,
    string? BootstrapSummaryPath,
    string? SuiteReportPath)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);

    public static DoctorRunMeta Build(
        string runId,
        string startedAt,
        string finishedAt,
        string projectDir,
        IReadOnlyDictionary<string, string>? artifactPaths,
        EvidenceManifest? manifest,
        DoctorWorkflowSummary workflow)
    {
        var artifacts = artifactPaths ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var output = artifacts.TryGetValue("dashboard_html", out var dashboardHtml)
            ? dashboardHtml
            : artifacts.TryGetValue("json", out var jsonPath) ? jsonPath : string.Empty;
        var snapshot = artifacts.TryGetValue("terminal_transcript", out var transcript)
            ? transcript
            : artifacts.TryGetValue("text", out var textPath) ? textPath : string.Empty;

        return new DoctorRunMeta(
            workflow.Status,
            startedAt,
            finishedAt,
            Math.Max((DateTimeOffset.Parse(finishedAt) - DateTimeOffset.Parse(startedAt)).TotalSeconds, 0d),
            "doctor-dashboard",
            "Local FrankenTui.Net doctor evidence run",
            projectDir,
            Path.Combine(projectDir, "artifacts"),
            output,
            snapshot,
            manifest?.Stages.FirstOrDefault()?.TraceId,
            workflow.BenchmarkErrorCount > 0,
            workflow.BenchmarkErrorCount > 0 ? "benchmark-gate-warning" : null,
            workflow.BenchmarkErrorCount > 0 ? "benchmark-gate-warning" : null,
            artifacts.TryGetValue("manifest", out var manifestPathForLedger) ? manifestPathForLedger : null,
            artifacts.TryGetValue("manifest", out var manifestPath) ? manifestPath : null,
            artifacts.TryGetValue("workflow_summary", out var workflowPath) ? workflowPath : null,
            artifacts.TryGetValue("bootstrap_summary", out var bootstrapPath) ? bootstrapPath : null,
            artifacts.TryGetValue("suite_report", out var suitePath) ? suitePath : null);
    }

    public static string WriteArtifact(string runId, DoctorRunMeta meta)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(meta);

        var path = ArtifactPathBuilder.For("replay", $"{runId}-run-meta.json");
        File.WriteAllText(path, meta.ToJson());
        return path;
    }
}
