using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public sealed record DoctorSuiteAggregate(
    string SuiteName,
    string GeneratedAt,
    int TotalRuns,
    int OkRuns,
    int FailedRuns,
    IReadOnlyList<string> TraceIds,
    IReadOnlyList<string> FallbackProfiles,
    IReadOnlyList<string> CaptureErrorProfiles,
    IReadOnlyList<DoctorSuiteManifestEntry> RunIndex)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);
}

public static class DoctorSuiteAggregator
{
    public static DoctorSuiteAggregate Build(string suiteName, IReadOnlyList<DoctorRunMeta> runs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteName);
        ArgumentNullException.ThrowIfNull(runs);

        var traceIds = runs
            .Select(static run => run.TraceId)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var fallbackProfiles = runs
            .Where(static run => !string.IsNullOrWhiteSpace(run.FallbackReason))
            .Select(static run => run.Profile)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var captureErrorProfiles = runs
            .Where(static run => !string.IsNullOrWhiteSpace(run.CaptureErrorReason))
            .Select(static run => run.Profile)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var runIndex = runs
            .Select(static run => new DoctorSuiteManifestEntry(
                run.Profile,
                run.Status,
                run.RunDir,
                run.TraceId,
                run.FallbackReason,
                run.CaptureErrorReason))
            .ToArray();

        return new DoctorSuiteAggregate(
            suiteName,
            DateTimeOffset.UtcNow.ToString("O"),
            runs.Count,
            runs.Count(static run => string.Equals(run.Status, "ok", StringComparison.Ordinal)),
            runs.Count(static run => !string.Equals(run.Status, "ok", StringComparison.Ordinal)),
            traceIds,
            fallbackProfiles,
            captureErrorProfiles,
            runIndex);
    }

    public static string WriteArtifact(string runId, DoctorSuiteAggregate aggregate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(aggregate);

        var path = ArtifactPathBuilder.For("replay", $"{runId}-suite-aggregate.json");
        File.WriteAllText(path, aggregate.ToJson());
        return path;
    }
}
