using System.Net;
using System.Text.Json;

namespace FrankenTui.Testing.Harness;

public sealed record DoctorSuiteRun(
    string RunId,
    string Status,
    string Profile,
    int ArtifactCount,
    int ManifestStageCount,
    string? TraceId,
    string? FallbackReason,
    string? CaptureErrorReason);

public sealed record DoctorSuiteReport(
    string Title,
    string GeneratedAt,
    string SuiteDir,
    int TotalRuns,
    int OkRuns,
    int FailedRuns,
    IReadOnlyList<string> TraceIds,
    IReadOnlyList<string> FallbackProfiles,
    IReadOnlyList<string> CaptureErrorProfiles,
    IReadOnlyList<DoctorSuiteRun> Runs)
{
    public string ToJson() => JsonSerializer.Serialize(this, HarnessJson.IndentedSnakeCase);

    public string ToHtml()
    {
        static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var html = new System.Text.StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\">");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine($"  <title>{Encode(Title)}</title>");
        html.AppendLine("  <style>");
        html.AppendLine("    body { font-family: ui-sans-serif, -apple-system, Segoe UI, Roboto, Arial, sans-serif; margin: 24px; background: #0f1115; color: #e7ebf3; }");
        html.AppendLine("    h1, h2 { margin: 0 0 12px; }");
        html.AppendLine("    .meta { margin-bottom: 20px; color: #a8b0c5; }");
        html.AppendLine("    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 16px; }");
        html.AppendLine("    .card { border: 1px solid #2a3142; border-radius: 10px; padding: 14px; background: #171b24; }");
        html.AppendLine("    .ok { border-left: 5px solid #2cb67d; }");
        html.AppendLine("    .fail { border-left: 5px solid #ef4565; }");
        html.AppendLine("    .row { margin: 4px 0; font-size: 13px; color: #c8d0e3; }");
        html.AppendLine("    .label { color: #8a95b5; display: inline-block; min-width: 130px; }");
        html.AppendLine("    a { color: #7da6ff; text-decoration: none; }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine($"<h1>{Encode(Title)}</h1>");
        html.AppendLine($"<div class=\"meta\">generated_at={Encode(GeneratedAt)} | suite_dir={Encode(SuiteDir)} | total={TotalRuns} | ok={OkRuns} | failed={FailedRuns}</div>");
        html.AppendLine("<div class=\"grid\">");

        foreach (var run in Runs)
        {
            html.AppendLine($"<section class=\"card {(string.Equals(run.Status, "ok", StringComparison.Ordinal) ? "ok" : "fail")}\">");
            html.AppendLine($"<h2>{Encode(run.Profile)}</h2>");
            html.AppendLine($"<div class=\"row\"><span class=\"label\">run_id</span>{Encode(run.RunId)}</div>");
            html.AppendLine($"<div class=\"row\"><span class=\"label\">status</span>{Encode(run.Status)}</div>");
            html.AppendLine($"<div class=\"row\"><span class=\"label\">artifacts</span>{run.ArtifactCount}</div>");
            html.AppendLine($"<div class=\"row\"><span class=\"label\">manifest_stages</span>{run.ManifestStageCount}</div>");
            if (!string.IsNullOrWhiteSpace(run.TraceId))
            {
                html.AppendLine($"<div class=\"row\"><span class=\"label\">trace_id</span>{Encode(run.TraceId)}</div>");
            }

            if (!string.IsNullOrWhiteSpace(run.FallbackReason))
            {
                html.AppendLine($"<div class=\"row\"><span class=\"label\">fallback_reason</span>{Encode(run.FallbackReason)}</div>");
            }

            if (!string.IsNullOrWhiteSpace(run.CaptureErrorReason))
            {
                html.AppendLine($"<div class=\"row\"><span class=\"label\">capture_error_reason</span>{Encode(run.CaptureErrorReason)}</div>");
            }

            html.AppendLine("</section>");
        }

        html.AppendLine("</div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    public static DoctorSuiteReport Build(
        string runId,
        IReadOnlyDictionary<string, string>? artifactPaths,
        EvidenceManifest? manifest,
        DoctorWorkflowSummary workflow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(workflow);

        var traceId = manifest?.Stages.FirstOrDefault()?.TraceId;
        var run = new DoctorSuiteRun(
            runId,
            workflow.Status,
            "doctor-dashboard",
            artifactPaths?.Count ?? 0,
            manifest?.Stages.Count ?? 0,
            traceId,
            null,
            workflow.BenchmarkErrorCount > 0 ? "benchmark-gate-warning" : null);

        var traceIds = traceId is null ? Array.Empty<string>() : [traceId];
        var captureErrorProfiles = run.CaptureErrorReason is null ? Array.Empty<string>() : [run.Profile];

        return new DoctorSuiteReport(
            "FrankenTui.Net Doctor Suite Report",
            DateTimeOffset.UtcNow.ToString("O"),
            Path.Combine(RepositoryPaths.FindRepositoryRoot(), "artifacts"),
            1,
            string.Equals(run.Status, "ok", StringComparison.Ordinal) ? 1 : 0,
            string.Equals(run.Status, "ok", StringComparison.Ordinal) ? 0 : 1,
            traceIds,
            [],
            captureErrorProfiles,
            [run]);
    }

    public static DoctorSuiteReport Build(string title, string suiteDir, IReadOnlyList<DoctorRunMeta> runs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteDir);
        ArgumentNullException.ThrowIfNull(runs);

        return new DoctorSuiteReport(
            title,
            DateTimeOffset.UtcNow.ToString("O"),
            suiteDir,
            runs.Count,
            runs.Count(static run => string.Equals(run.Status, "ok", StringComparison.Ordinal)),
            runs.Count(static run => !string.Equals(run.Status, "ok", StringComparison.Ordinal)),
            runs.Select(static run => run.TraceId).Where(static value => !string.IsNullOrWhiteSpace(value)).Cast<string>().Distinct(StringComparer.Ordinal).ToArray(),
            runs.Where(static run => !string.IsNullOrWhiteSpace(run.FallbackReason)).Select(static run => run.Profile).Distinct(StringComparer.Ordinal).ToArray(),
            runs.Where(static run => !string.IsNullOrWhiteSpace(run.CaptureErrorReason)).Select(static run => run.Profile).Distinct(StringComparer.Ordinal).ToArray(),
            runs.Select(static run => new DoctorSuiteRun(
                Path.GetFileName(run.RunDir),
                run.Status,
                run.Profile,
                CountRunArtifacts(run.RunDir),
                CountManifestStages(run.ManifestPath),
                run.TraceId,
                run.FallbackReason,
                run.CaptureErrorReason)).ToArray());
    }

    public static string WriteHtmlArtifact(string runId, DoctorSuiteReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(report);

        var path = ArtifactPathBuilder.For("replay", $"{runId}-suite-index.html");
        File.WriteAllText(path, report.ToHtml());
        return path;
    }

    private static int CountRunArtifacts(string? runDir)
    {
        if (string.IsNullOrWhiteSpace(runDir) || !Directory.Exists(runDir))
        {
            return 0;
        }

        return Directory.GetFiles(runDir, "*", SearchOption.TopDirectoryOnly).Length;
    }

    private static int CountManifestStages(string? manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
        {
            return 0;
        }

        return JsonSerializer.Deserialize<EvidenceManifest>(File.ReadAllText(manifestPath), HarnessJson.SnakeCase)?.Stages.Count ?? 0;
    }

    public static string WriteArtifact(string runId, DoctorSuiteReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(report);

        var path = ArtifactPathBuilder.For("replay", $"{runId}-suite-report.json");
        File.WriteAllText(path, report.ToJson());
        return path;
    }
}
