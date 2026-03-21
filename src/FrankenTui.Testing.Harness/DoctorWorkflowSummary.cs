using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public sealed record DoctorWorkflowSummary(
    string RunId,
    string Format,
    bool WriteArtifacts,
    bool WriteManifest,
    bool RunBenchmarks,
    int ArtifactCount,
    int ManifestStageCount,
    IReadOnlyList<string> ManifestStages,
    int ContractArtifactCount,
    int BenchmarkErrorCount,
    bool DashboardWritten,
    bool ArtifactManifestWritten,
    bool FailureSignaturesWritten,
    string Status)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);

    public static DoctorWorkflowSummary Build(
        string runId,
        string format,
        bool writeArtifacts,
        bool writeManifest,
        bool runBenchmarks,
        IReadOnlyDictionary<string, string>? artifactPaths,
        EvidenceManifest? manifest,
        IReadOnlyList<string>? benchmarkErrors)
    {
        var artifacts = artifactPaths ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var errors = benchmarkErrors ?? [];
        var manifestStages = manifest?.Stages.Select(static stage => stage.StageId).ToArray() ?? [];
        var contractArtifactCount = artifacts.Keys.Count(key =>
            key.Contains("telemetry", StringComparison.Ordinal) ||
            key.Contains("mermaid", StringComparison.Ordinal) ||
            key.Contains("opentui", StringComparison.Ordinal) ||
            key.Contains("artifact_manifest", StringComparison.Ordinal) ||
            key.Contains("failure_signatures", StringComparison.Ordinal));

        var dashboardWritten = artifacts.ContainsKey("dashboard_html") || artifacts.ContainsKey("dashboard_json") || artifacts.ContainsKey("dashboard_text");
        var status = errors.Count > 0 ? "warning" : "ok";

        return new DoctorWorkflowSummary(
            runId,
            format,
            writeArtifacts,
            writeManifest,
            runBenchmarks,
            artifacts.Count,
            manifestStages.Length,
            manifestStages,
            contractArtifactCount,
            errors.Count,
            dashboardWritten,
            artifacts.ContainsKey("artifact_manifest_summary"),
            artifacts.ContainsKey("failure_signatures"),
            status);
    }

    public static string WriteArtifact(string runId, DoctorWorkflowSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(summary);

        var path = ArtifactPathBuilder.For("replay", $"{runId}-workflow-summary.json");
        File.WriteAllText(path, summary.ToJson());
        return path;
    }
}
