using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public enum ArtifactClass
{
    RunMeta,
    EvidenceLedger,
    FrameSnapshot,
    ShadowReport,
    BenchmarkGate,
    CaptureLog,
    ReplayScript,
    CoverageReport,
    Summary
}

public enum RetentionClass
{
    Ephemeral,
    Session,
    Release,
    Permanent
}

public sealed record ArtifactManifestEntry(
    ArtifactClass Class,
    string Path,
    long SizeBytes,
    IReadOnlySet<string> Fields);

public sealed record ArtifactManifestValidation(
    ArtifactClass Class,
    string Path,
    IReadOnlyList<string> MissingFields,
    bool Oversize,
    bool Passes);

public sealed record ArtifactManifestSummary(
    int EntryCount,
    int FailingCount,
    IReadOnlyList<ArtifactManifestValidation> Failures)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);
}

public static class ArtifactManifestContract
{
    public static string FilenamePattern(ArtifactClass artifactClass) => artifactClass switch
    {
        ArtifactClass.RunMeta => "run_meta.json",
        ArtifactClass.EvidenceLedger => "evidence_ledger.jsonl",
        ArtifactClass.FrameSnapshot => "frame_{index:04}.json",
        ArtifactClass.ShadowReport => "shadow_report.json",
        ArtifactClass.BenchmarkGate => "benchmark_gate.json",
        ArtifactClass.CaptureLog => "{source}.log",
        ArtifactClass.ReplayScript => "replay.sh",
        ArtifactClass.CoverageReport => "coverage_gate_report.json",
        _ => "{name}_summary.txt"
    };

    public static long MaxSizeBytes(ArtifactClass artifactClass) => artifactClass switch
    {
        ArtifactClass.RunMeta => 64 * 1024,
        ArtifactClass.EvidenceLedger => 1024 * 1024,
        ArtifactClass.FrameSnapshot => 256 * 1024,
        ArtifactClass.ShadowReport => 512 * 1024,
        ArtifactClass.BenchmarkGate => 128 * 1024,
        ArtifactClass.CaptureLog => 10 * 1024 * 1024,
        ArtifactClass.ReplayScript => 4 * 1024,
        ArtifactClass.CoverageReport => 256 * 1024,
        _ => 64 * 1024
    };

    public static RetentionClass Retention(ArtifactClass artifactClass) => artifactClass switch
    {
        ArtifactClass.FrameSnapshot => RetentionClass.Session,
        ArtifactClass.CaptureLog => RetentionClass.Session,
        ArtifactClass.ReplayScript => RetentionClass.Permanent,
        _ => RetentionClass.Release
    };

    public static IReadOnlyList<string> RequiredManifestFields(ArtifactClass artifactClass) => artifactClass switch
    {
        ArtifactClass.RunMeta => ["trace_id", "created_at", "status", "runtime_lane"],
        ArtifactClass.EvidenceLedger => ["trace_id", "entry_count", "schema_version"],
        ArtifactClass.FrameSnapshot => ["trace_id", "frame_idx", "checksum", "viewport"],
        ArtifactClass.ShadowReport => ["trace_id", "verdict", "frames_compared", "diverged_count"],
        ArtifactClass.BenchmarkGate => ["trace_id", "gate_name", "passed", "threshold"],
        ArtifactClass.CaptureLog => ["trace_id", "source", "byte_count"],
        ArtifactClass.ReplayScript => ["trace_id", "scenario", "seed", "viewport"],
        ArtifactClass.CoverageReport => ["trace_id", "line_coverage_pct", "gate_passed"],
        _ => ["trace_id", "created_at"]
    };

    public static ArtifactManifestEntry CreateEntry(string path, ArtifactClass artifactClass)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var file = new FileInfo(path);
        var fields = ReadTopLevelFields(path);
        return new ArtifactManifestEntry(artifactClass, path, file.Exists ? file.Length : 0, fields);
    }

    public static ArtifactManifestValidation ValidateEntry(ArtifactManifestEntry entry)
    {
        var missing = RequiredManifestFields(entry.Class)
            .Where(field => !entry.Fields.Contains(field))
            .ToArray();
        var oversize = entry.SizeBytes > MaxSizeBytes(entry.Class);
        return new ArtifactManifestValidation(entry.Class, entry.Path, missing, oversize, missing.Length == 0);
    }

    public static ArtifactManifestSummary BuildSummary(IReadOnlyDictionary<string, string> artifactPaths)
    {
        ArgumentNullException.ThrowIfNull(artifactPaths);

        var validations = artifactPaths
            .Select(entry => ValidateEntry(CreateEntry(entry.Value, Classify(entry.Key, entry.Value))))
            .Where(validation => !validation.Passes || validation.Oversize)
            .ToArray();
        return new ArtifactManifestSummary(artifactPaths.Count, validations.Length, validations);
    }

    public static string WriteSummary(string runId, ArtifactManifestSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(summary);

        var path = ArtifactPathBuilder.For("replay", $"{runId}-artifact-manifest-summary.json");
        File.WriteAllText(path, summary.ToJson());
        return path;
    }

    public static ArtifactClass Classify(string key, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fileName = System.IO.Path.GetFileName(path);
        return key switch
        {
            "manifest" => ArtifactClass.RunMeta,
            "run_meta" => ArtifactClass.RunMeta,
            "replay_tape" => ArtifactClass.ReplayScript,
            "runtime_trace" => ArtifactClass.FrameSnapshot,
            "diff_evidence" => ArtifactClass.ShadowReport,
            "benchmarks" => ArtifactClass.BenchmarkGate,
            _ when fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase) => ArtifactClass.CaptureLog,
            _ when fileName.Contains("coverage", StringComparison.OrdinalIgnoreCase) => ArtifactClass.CoverageReport,
            _ when fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) => ArtifactClass.Summary,
            _ => ArtifactClass.Summary
        };
    }

    private static IReadOnlySet<string> ReadTopLevelFields(string path)
    {
        if (!File.Exists(path))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var extension = System.IO.Path.GetExtension(path);
        if (!string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            return document.RootElement
                .EnumerateObject()
                .Select(property => property.Name)
                .ToHashSet(StringComparer.Ordinal);
        }
        catch
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }
}
