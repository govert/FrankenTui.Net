using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public sealed record AsupersyncEvidenceEvent(
    string SchemaKind,
    string SchemaVersion,
    string EventType,
    string TimestampUtc,
    string RunId,
    string CorrelationId,
    string Lane,
    string Component,
    string Operation,
    string Outcome,
    string ReasonCode,
    string PrimaryLane,
    string? ComparisonLane,
    string DivergenceClass,
    string? FallbackTrigger,
    string? FallbackDecision,
    bool RollbackRequired,
    IReadOnlyList<string> ArtifactRefs);

public sealed record AsupersyncEvidenceBundle(
    string AdoptionBoundary,
    string RequestedLane,
    string ActiveLane,
    bool ShadowEnabled,
    IReadOnlyList<AsupersyncEvidenceEvent> Events)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);
}

public static class AsupersyncEvidence
{
    public static AsupersyncEvidenceBundle Build(
        string runId,
        DoctorSeedExecution seedExecution,
        IReadOnlyDictionary<string, string>? artifactPaths = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(seedExecution);

        var artifacts = artifactPaths ?? new Dictionary<string, string>(StringComparer.Ordinal);
        var artifactRefs = artifacts.Values.OrderBy(static value => value, StringComparer.Ordinal).ToArray();
        var timestamp = DateTimeOffset.UtcNow.ToString("O");
        var laneSelected = new AsupersyncEvidenceEvent(
            "asupersync-evidence",
            "v1",
            "lane_selected",
            timestamp,
            runId,
            $"{runId}:lane",
            "legacy",
            "FrankenTui.Doctor",
            "doctor_orchestration",
            "ok",
            "orchestration_only",
            "legacy",
            null,
            "none",
            null,
            null,
            false,
            artifactRefs);

        var seedEvent = new AsupersyncEvidenceEvent(
            "asupersync-evidence",
            "v1",
            "seed_execution",
            timestamp,
            runId,
            $"{runId}:seed",
            seedExecution.Mode == "actual" ? "legacy" : "legacy",
            "FrankenTui.Doctor",
            "doctor_seed",
            seedExecution.Status,
            seedExecution.Mode == "actual" ? "remote_seed" : "simulated_seed",
            "legacy",
            seedExecution.Mode == "actual" ? "shadow" : null,
            seedExecution.Status == "failed" ? "fallback" : "none",
            seedExecution.Status == "failed" ? "seed_execution_failed" : null,
            seedExecution.Status == "failed" ? "remain_legacy" : null,
            false,
            string.IsNullOrWhiteSpace(seedExecution.LogPath)
                ? artifactRefs
                : artifactRefs.Concat([seedExecution.LogPath]).Distinct(StringComparer.Ordinal).ToArray());

        return new AsupersyncEvidenceBundle(
            "orchestration-only",
            "legacy",
            "legacy",
            false,
            [laneSelected, seedEvent]);
    }

    public static string WriteArtifact(string runId, AsupersyncEvidenceBundle bundle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(bundle);

        var path = ArtifactPathBuilder.For("replay", $"{runId}-asupersync-evidence.json");
        File.WriteAllText(path, bundle.ToJson());
        return path;
    }
}
