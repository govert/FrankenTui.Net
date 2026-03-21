using FrankenTui.Testing.Harness;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace FrankenTui.Tests.Headless;

public sealed class DoctorWorkflowTests
{
    [Fact]
    public void CanonicalDoctorCostProfileCoversAllStagesAndTargetsCaptureAsDominant()
    {
        var report = DoctorCostProfile.Canonical().Finalize();

        Assert.NotEmpty(report.ByStage(WorkflowStage.Seed));
        Assert.NotEmpty(report.ByStage(WorkflowStage.Capture));
        Assert.NotEmpty(report.ByStage(WorkflowStage.Suite));
        Assert.NotEmpty(report.ByStage(WorkflowStage.Report));
        Assert.NotEmpty(report.ByStage(WorkflowStage.Triage));
        Assert.True(report.StageTotal(WorkflowStage.Capture) > report.StageTotal(WorkflowStage.Report));
        Assert.Contains(report.OptimizationTargets, target => target.Stage == WorkflowStage.Capture && target.Impact == OptimizationImpact.Critical);
    }

    [Fact]
    public void DoctorWorkflowSummaryTracksManifestAndContractArtifacts()
    {
        var artifactPaths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["manifest"] = "/tmp/manifest.json",
            ["artifact_manifest_summary"] = "/tmp/artifact-summary.json",
            ["failure_signatures"] = "/tmp/failure-signatures.json",
            ["dashboard_html"] = "/tmp/doctor-dashboard.html",
            ["telemetry_config"] = "/tmp/telemetry.json",
            ["mermaid_config"] = "/tmp/mermaid.json",
            ["doctor_cost_profile"] = "/tmp/doctor-cost-profile.json"
        };

        var manifest = new EvidenceManifest(
            "manifest-id",
            "evidence-manifest-v1",
            "2026-03-09",
            "doctor-run",
            new EvidenceSourceFingerprint(null, null, "/tmp", "sha256", [], new Dictionary<string, string>()),
            [
                new EvidenceStage("runtime_replay", 0, "corr-1", "303-RTM", "ev-1", "policy", "trace", "2026-03-20T00:00:00Z", "2026-03-20T00:00:01Z", "ok", "hash-a", "hash-b", ["/tmp/replay.json"], null),
                new EvidenceStage("benchmarks", 1, "corr-2", "358-VRF", "ev-2", "policy", "trace", "2026-03-20T00:00:01Z", "2026-03-20T00:00:02Z", "ok", "hash-b", "hash-c", ["/tmp/benchmarks.json"], null)
            ],
            new EvidenceGeneratedCodeFingerprint("hash", ".NET SDK 10", "dotnet build"),
            new EvidenceCertificationVerdict("accept", 0.9, 2, 0, 0, new EvidenceSemanticClauseCoverage(["303-RTM", "358-VRF"], []), new EvidenceBenchmarkSummary(1, 2, 3), []),
            new EvidenceDeterminismAttestation(2, true, false));

        var summary = DoctorWorkflowSummary.Build(
            "doctor-run",
            "text",
            true,
            true,
            true,
            artifactPaths,
            manifest,
            ["benchmark exceeded"]);

        Assert.Equal("warning", summary.Status);
        Assert.Equal(2, summary.ManifestStageCount);
        Assert.Contains("runtime_replay", summary.ManifestStages);
        Assert.True(summary.ArtifactManifestWritten);
        Assert.True(summary.FailureSignaturesWritten);
        Assert.True(summary.DashboardWritten);
        Assert.True(summary.ContractArtifactCount >= 3);
    }

    [Fact]
    public void DoctorBootstrapAndSuiteSummariesTrackStagesAndTraceIds()
    {
        var manifest = new EvidenceManifest(
            "manifest-id",
            "evidence-manifest-v1",
            "2026-03-09",
            "doctor-run",
            new EvidenceSourceFingerprint(null, null, "/tmp", "sha256", [], new Dictionary<string, string>()),
            [
                new EvidenceStage("runtime_replay", 0, "corr-1", "303-RTM", "ev-1", "policy", "trace-1", "2026-03-20T00:00:00Z", "2026-03-20T00:00:01Z", "ok", "hash-a", "hash-b", ["/tmp/replay.json"], null)
            ],
            new EvidenceGeneratedCodeFingerprint("hash", ".NET SDK 10", "dotnet build"),
            new EvidenceCertificationVerdict("accept", 0.9, 1, 0, 0, new EvidenceSemanticClauseCoverage(["303-RTM"], []), new EvidenceBenchmarkSummary(1, 2, 3), []),
            new EvidenceDeterminismAttestation(2, true, false));

        var bootstrap = DoctorBootstrapSummary.Build(
            "doctor-run",
            "text",
            true,
            true,
            true,
            true,
            "/tmp/baseline.json",
            [],
            manifest);
        var workflow = DoctorWorkflowSummary.Build(
            "doctor-run",
            "text",
            true,
            true,
            true,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["workflow_summary"] = "/tmp/workflow.json"
            },
            manifest,
            []);
        var suite = DoctorSuiteReport.Build("doctor-run", new Dictionary<string, string>(), manifest, workflow);

        Assert.Equal("ok", bootstrap.Status);
        Assert.Contains("contract-load", bootstrap.Stages);
        Assert.Contains("manifest:runtime_replay", bootstrap.Stages);
        Assert.Equal(1, suite.TotalRuns);
        Assert.Contains("trace-1", suite.TraceIds);
        Assert.Equal("doctor-dashboard", suite.Runs[0].Profile);
    }

    [Fact]
    public void DoctorRunMetaAndSuiteManifestRoundTripLocalSingleRunShape()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);

        var workflow = DoctorWorkflowSummary.Build(
            "doctor-run",
            "text",
            true,
            true,
            false,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["dashboard_html"] = "/tmp/dashboard.html",
                ["workflow_summary"] = "/tmp/workflow.json",
                ["bootstrap_summary"] = "/tmp/bootstrap.json",
                ["suite_report"] = "/tmp/suite-report.json",
                ["manifest"] = "/tmp/manifest.json"
            },
            null,
            []);
        var runMeta = DoctorRunMeta.Build(
            "doctor-run",
            "2026-03-21T00:00:00Z",
            "2026-03-21T00:00:05Z",
            temp,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["dashboard_html"] = "/tmp/dashboard.html",
                ["terminal_transcript"] = "/tmp/transcript.txt",
                ["workflow_summary"] = "/tmp/workflow.json",
                ["bootstrap_summary"] = "/tmp/bootstrap.json",
                ["suite_report"] = "/tmp/suite-report.json",
                ["manifest"] = "/tmp/manifest.json"
            },
            null,
            workflow);
        var suiteManifest = DoctorSuiteManifest.Build("doctor-run", runMeta);
        var suiteDir = Path.Combine(temp, "suite");
        Directory.CreateDirectory(suiteDir);
        File.WriteAllText(Path.Combine(suiteDir, "doctor-run-run-meta.json"), runMeta.ToJson());

        var loaded = DoctorSuiteManifest.LoadRunMetas(suiteDir);

        Assert.Equal("doctor-dashboard", runMeta.Profile);
        Assert.Equal("/tmp/dashboard.html", runMeta.Output);
        Assert.Single(suiteManifest.RunIndex);
        Assert.Equal("doctor-run", suiteManifest.SuiteName);
        Assert.Single(loaded);
        Assert.Equal(runMeta.Status, loaded[0].Status);
    }

    [Fact]
    public void DoctorSeedPlanCarriesDeterministicUpstreamShapedDefaults()
    {
        var plan = DoctorSeedPlan.BuildDefault("doctor-run");

        Assert.True(plan.Deterministic);
        Assert.Equal("http://127.0.0.1:8879/mcp/", plan.Endpoint.Endpoint);
        Assert.Equal(6, plan.MessageCount);
        Assert.Equal(3, plan.RetryPolicy.MaxAttempts);
        Assert.Contains("ensure_project", plan.Stages);
        Assert.Contains("send_messages", plan.Stages);
    }

    [Fact]
    public void DoctorSeedExecutionAndSuiteAggregateCaptureExecutionAndMultiRunState()
    {
        var execution = DoctorSeedExecution.Simulate(DoctorSeedPlan.BuildDefault("doctor-run"));
        var aggregate = DoctorSuiteAggregator.Build(
            "suite-a",
            [
                new DoctorRunMeta("ok", "2026-03-21T00:00:00Z", "2026-03-21T00:00:02Z", 2, "alpha", "run a", "/tmp/project", "/tmp/run-a", "a.html", "a.txt", "trace-a", false, null, null, null, null, null, null, null),
                new DoctorRunMeta("failed", "2026-03-21T00:00:03Z", "2026-03-21T00:00:05Z", 2, "beta", "run b", "/tmp/project", "/tmp/run-b", "b.html", "b.txt", "trace-b", true, "fallback", "capture-error", null, null, null, null, null)
            ]);

        Assert.Equal("ok", execution.Status);
        Assert.Equal("seed_complete", execution.Stages[^1].Detail);
        Assert.Contains(execution.Stages, stage => stage.Stage == "send_messages");
        Assert.Equal(2, aggregate.TotalRuns);
        Assert.Equal(1, aggregate.OkRuns);
        Assert.Equal(1, aggregate.FailedRuns);
        Assert.Contains("beta", aggregate.FallbackProfiles);
        Assert.Contains("trace-a", aggregate.TraceIds);
    }

    [Fact]
    public void DoctorSuiteReportBuildsFromSuiteDirectoryRunMetas()
    {
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var suiteDir = Path.Combine(temp, "doctor-suite");
        Directory.CreateDirectory(suiteDir);

        var runA = new DoctorRunMeta("ok", "2026-03-21T00:00:00Z", "2026-03-21T00:00:02Z", 2, "alpha", "run a", temp, Path.Combine(temp, "run-a"), "a.html", "a.txt", "trace-a", false, null, null, null, null, null, null, null);
        var runB = new DoctorRunMeta("failed", "2026-03-21T00:00:03Z", "2026-03-21T00:00:05Z", 2, "beta", "run b", temp, Path.Combine(temp, "run-b"), "b.html", "b.txt", "trace-b", true, "fallback", "capture-error", null, null, null, null, null);

        DoctorSuiteManifest.WriteSuiteRunMeta(suiteDir, "run-a", runA);
        DoctorSuiteManifest.WriteSuiteRunMeta(suiteDir, "run-b", runB);

        var loaded = DoctorSuiteManifest.LoadRunMetas(suiteDir);
        var manifest = DoctorSuiteManifest.Build("doctor-suite", suiteDir, loaded);
        var report = DoctorSuiteReport.Build("FrankenTui.Net Doctor Suite Report", suiteDir, loaded);

        Assert.Equal(2, loaded.Count);
        Assert.Equal(1, manifest.SuccessCount);
        Assert.Equal(1, manifest.FailureCount);
        Assert.Equal(2, report.TotalRuns);
        Assert.Contains("beta", report.CaptureErrorProfiles);
        Assert.Contains("alpha", report.ToHtml(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoctorSeedExecutionCanRunAgainstJsonRpcEndpoint()
    {
        var requests = new List<string>();
        using var client = new HttpClient(new StubSeedHandler(requests))
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        var execution = await DoctorSeedExecution.ExecuteAsync(
            DoctorSeedPlan.BuildDefault("doctor-run"),
            client);

        Assert.Equal("ok", execution.Status);
        Assert.Equal("actual", execution.Mode);
        Assert.Contains(execution.Stages, stage => stage.Stage == "ensure_project" && stage.Status == "ok");
        Assert.Contains(execution.Stages, stage => stage.Stage == "send_messages" && stage.Status == "ok");
        Assert.Contains("health_check", requests);
        Assert.Contains("search_messages", requests);
    }

    [Fact]
    public async Task DoctorSeedExecutionTreatsReservationFailureAsWarning()
    {
        using var client = new HttpClient(new StubSeedHandler([], failReservation: true))
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        var execution = await DoctorSeedExecution.ExecuteAsync(
            DoctorSeedPlan.BuildDefault("doctor-run"),
            client);

        Assert.Equal("ok", execution.Status);
        Assert.Contains(execution.Stages, stage => stage.Stage == "file_reservation_paths" && stage.Status == "warning");
    }

    [Fact]
    public void AsupersyncEvidenceUsesExplicitLaneAndFallbackContractShape()
    {
        var bundle = AsupersyncEvidence.Build(
            "doctor-run",
            DoctorSeedExecution.Simulate(DoctorSeedPlan.BuildDefault("doctor-run")),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["seed_execution"] = "/tmp/seed-execution.json"
            });

        Assert.Equal("orchestration-only", bundle.AdoptionBoundary);
        Assert.Equal("legacy", bundle.ActiveLane);
        Assert.Contains(bundle.Events, evt => evt.EventType == "lane_selected" && evt.ReasonCode == "orchestration_only");
        Assert.Contains(bundle.Events, evt => evt.EventType == "seed_execution" && evt.SchemaKind == "asupersync-evidence");
    }

    private sealed class StubSeedHandler(List<string> requests, bool failReservation = false) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = await request.Content!.ReadAsStringAsync(cancellationToken);
            var method = JsonDocument.Parse(payload).RootElement.GetProperty("params").GetProperty("name").GetString()!;
            requests.Add(method);

            var body = failReservation && string.Equals(method, "file_reservation_paths", StringComparison.Ordinal)
                ? """{"jsonrpc":"2.0","id":1,"result":{"isError":true}}"""
                : """{"jsonrpc":"2.0","id":1,"result":{"ok":true}}""";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }
}
