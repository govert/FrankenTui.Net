using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public sealed record DoctorSuiteManifestEntry(
    string Profile,
    string Status,
    string RunDir,
    string? TraceId,
    string? FallbackReason,
    string? CaptureErrorReason);

public sealed record DoctorSuiteManifest(
    string SuiteName,
    string SuiteDir,
    string StartedAt,
    string FinishedAt,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string> TraceIds,
    IReadOnlyList<string> FallbackProfiles,
    IReadOnlyList<string> CaptureErrorProfiles,
    IReadOnlyList<DoctorSuiteManifestEntry> RunIndex)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);

    public static DoctorSuiteManifest Build(string runId, DoctorRunMeta runMeta)
        => Build(runId, runMeta.RunDir, [runMeta]);

    public static DoctorSuiteManifest Build(string suiteName, string suiteDir, IReadOnlyList<DoctorRunMeta> runMetas)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteName);
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteDir);
        ArgumentNullException.ThrowIfNull(runMetas);
        var orderedStarts = runMetas.Select(static run => run.StartedAt).Order(StringComparer.Ordinal).ToArray();
        var orderedFinishes = runMetas.Select(static run => run.FinishedAt).Order(StringComparer.Ordinal).ToArray();

        return new DoctorSuiteManifest(
            suiteName,
            suiteDir,
            orderedStarts.FirstOrDefault() ?? string.Empty,
            orderedFinishes.LastOrDefault() ?? string.Empty,
            runMetas.Count(static run => string.Equals(run.Status, "ok", StringComparison.Ordinal)),
            runMetas.Count(static run => !string.Equals(run.Status, "ok", StringComparison.Ordinal)),
            runMetas
                .Select(static run => run.TraceId)
                .Where(static traceId => !string.IsNullOrWhiteSpace(traceId))
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            runMetas
                .Where(static run => !string.IsNullOrWhiteSpace(run.FallbackReason))
                .Select(static run => run.Profile)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            runMetas
                .Where(static run => !string.IsNullOrWhiteSpace(run.CaptureErrorReason))
                .Select(static run => run.Profile)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            runMetas
                .Select(static run => new DoctorSuiteManifestEntry(
                    run.Profile,
                    run.Status,
                    run.RunDir,
                    run.TraceId,
                    run.FallbackReason,
                    run.CaptureErrorReason))
                .ToArray());
    }

    public static IReadOnlyList<DoctorRunMeta> LoadRunMetas(string suiteDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteDir);
        if (!Directory.Exists(suiteDir))
        {
            return [];
        }

        var files = Directory.GetFiles(suiteDir, "*.json", SearchOption.AllDirectories)
            .Where(static path =>
                path.EndsWith("run_meta.json", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("-run-meta.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();
        var metas = new List<DoctorRunMeta>(files.Length);
        foreach (var file in files)
        {
            metas.Add(JsonSerializer.Deserialize<DoctorRunMeta>(File.ReadAllText(file), HarnessJson.SnakeCase)
                ?? throw new InvalidOperationException($"Could not deserialize doctor run meta at {file}."));
        }

        return metas;
    }

    public static string WriteSuiteRunMeta(string suiteDir, string runId, DoctorRunMeta meta)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(meta);

        var runDirectory = Path.Combine(suiteDir, runId);
        Directory.CreateDirectory(runDirectory);
        var path = Path.Combine(runDirectory, "run_meta.json");
        File.WriteAllText(path, meta.ToJson());
        return path;
    }

    public static string WriteArtifact(string runId, DoctorSuiteManifest manifest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(manifest);

        var path = ArtifactPathBuilder.For("replay", $"{runId}-suite-manifest.json");
        File.WriteAllText(path, manifest.ToJson());
        return path;
    }
}
