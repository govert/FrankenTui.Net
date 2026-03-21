using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public sealed record DoctorBootstrapSummary(
    string RunId,
    string ProjectDir,
    string UpstreamBasis,
    string Format,
    bool WriteArtifacts,
    bool WriteManifest,
    bool RunBenchmarks,
    bool ContractBundleAvailable,
    string BenchmarkBaseline,
    IReadOnlyList<string> Stages,
    int BenchmarkErrorCount,
    string Status)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);

    public static DoctorBootstrapSummary Build(
        string runId,
        string format,
        bool writeArtifacts,
        bool writeManifest,
        bool runBenchmarks,
        bool contractBundleAvailable,
        string benchmarkBaseline,
        IReadOnlyList<string>? benchmarkErrors,
        EvidenceManifest? manifest)
    {
        var stages = new List<string>
        {
            "environment-report"
        };

        if (writeArtifacts)
        {
            stages.Add("artifact-capture");
        }

        if (writeManifest)
        {
            stages.Add("manifest-write");
        }

        if (runBenchmarks)
        {
            stages.Add("benchmark-gate");
        }

        if (contractBundleAvailable)
        {
            stages.Add("contract-load");
        }

        if (manifest is not null)
        {
            stages.AddRange(manifest.Stages.Select(static stage => $"manifest:{stage.StageId}"));
        }

        var errors = benchmarkErrors ?? [];
        return new DoctorBootstrapSummary(
            runId,
            RepositoryPaths.FindRepositoryRoot(),
            UpstreamReferencePaths.BasisCommit,
            format,
            writeArtifacts,
            writeManifest,
            runBenchmarks,
            contractBundleAvailable,
            benchmarkBaseline,
            stages,
            errors.Count,
            errors.Count > 0 ? "warning" : "ok");
    }

    public static string WriteArtifact(string runId, DoctorBootstrapSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(summary);

        var path = ArtifactPathBuilder.For("replay", $"{runId}-bootstrap-summary.json");
        File.WriteAllText(path, summary.ToJson());
        return path;
    }
}
