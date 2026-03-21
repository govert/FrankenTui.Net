using System.Text.Json;
using FrankenTui.Extras;
using FrankenTui.Doctor;
using FrankenTui.Runtime;
using FrankenTui.Simd;
using FrankenTui.Testing.Harness;

SimdAccelerators.EnableIfSupported();

var format = Parse(args, "--format") ?? "json";
var width = ParseUShort(args, "--width", 72);
var height = ParseUShort(args, "--height", 16);
var writeArtifacts = HasFlag(args, "--write-artifacts");
var writeManifest = HasFlag(args, "--write-manifest");
var runBenchmarks = HasFlag(args, "--run-benchmarks");
var runId = Parse(args, "--run-id") ?? "doctor-dashboard";
var seedMode = Parse(args, "--seed-mode") ?? "simulate";
var seedAuthToken = Parse(args, "--seed-auth-token") ?? string.Empty;
var benchmarkBaseline = Parse(args, "--benchmark-baseline") ?? PerformanceBenchmarkRunner.DefaultBudgetPath;
var telemetry = TelemetryConfig.FromEnvironment();
var mermaid = MermaidConfig.FromEnvironment();
var openTuiContracts = OpenTuiContractSet.TryLoadUpstreamReference();
var costProfile = DoctorCostProfile.Canonical().Finalize();
var seedPlan = DoctorSeedPlan.BuildDefault(runId);
var startedAt = DateTimeOffset.UtcNow.ToString("O");
var report = EnvironmentDoctor.CreateReport(
    telemetry.ToSummary(),
    mermaid.ToSummary(MermaidShowcaseSurface.Catalog().Count),
    openTuiContracts?.Migration.ToSummary() ?? OpenTuiMigrationContractSummary.Missing("Upstream OpenTUI reference contracts are unavailable in .external/frankentui."));

BenchmarkSuiteResult? benchmarkSuite = null;
IReadOnlyList<string> benchmarkErrors = [];
EvidenceManifest? manifest = null;
DoctorBootstrapSummary bootstrap;
DoctorSuiteReport suite;
DoctorRunMeta runMeta;
DoctorSuiteManifest suiteManifest;
DoctorSuiteAggregate suiteAggregate;
DoctorSeedExecution seedExecution;

seedExecution = seedMode.ToLowerInvariant() switch
{
    "off" => DoctorSeedExecution.Disabled(seedPlan),
    "actual" => await DoctorSeedExecution.ExecuteAsync(
        seedPlan,
        authBearer: seedAuthToken,
        logPath: writeArtifacts || writeManifest || runBenchmarks
            ? ArtifactPathBuilder.For("replay", $"{runId}-seed.log")
            : null),
    _ => DoctorSeedExecution.Simulate(seedPlan)
};

if (writeArtifacts || writeManifest || runBenchmarks)
{
    var runtimePolicy = RuntimeExecutionPolicy.Default with
    {
        EmitTelemetry = telemetry.Enabled,
        Telemetry = telemetry
    };
    var runtimeCapture = await HostedParityRuntimeHarness.CaptureAsync(
        "doctor-tooling-session",
        FrankenTui.Extras.HostedParityScenarioId.Tooling,
        width,
        height,
        policy: runtimePolicy);
    var artifactPaths = new Dictionary<string, string>(runtimeCapture.WriteArtifacts("doctor-runtime"), StringComparer.Ordinal);

    foreach (var entry in RenderHarness.CaptureHostedParity(
                 "doctor-dashboard",
                 DoctorDashboardViewFactory.Build(report),
                 width,
                 height,
                 options: DoctorDashboardViewFactory.CreateWebOptions(report))
             .WriteArtifacts("doctor-dashboard"))
    {
        artifactPaths[$"dashboard_{entry.Key}"] = entry.Value;
    }

    foreach (var entry in WriteContractArtifacts(runId, telemetry, mermaid, openTuiContracts))
    {
        artifactPaths[entry.Key] = entry.Value;
    }

    artifactPaths["doctor_cost_profile"] = DoctorCostProfile.WriteArtifacts(runId, costProfile);
    artifactPaths["seed_plan"] = DoctorSeedPlan.WriteArtifact(runId, seedPlan);
    artifactPaths["seed_execution"] = DoctorSeedExecution.WriteArtifact(runId, seedExecution);
    if (!string.IsNullOrWhiteSpace(seedExecution.LogPath))
    {
        artifactPaths["seed_log"] = seedExecution.LogPath!;
    }
    artifactPaths["asupersync_evidence"] = AsupersyncEvidence.WriteArtifact(
        runId,
        AsupersyncEvidence.Build(runId, seedExecution, artifactPaths));

    if (runBenchmarks)
    {
        var budgets = PerformanceBenchmarkRunner.LoadBudgets(benchmarkBaseline);
        (benchmarkSuite, benchmarkErrors) = PerformanceBenchmarkRunner.RunGate(budgets);

        if (writeManifest)
        {
            var manifestResult = EvidenceManifestBuilder.WriteHostedParityManifest(
                runId,
                runtimeCapture.Evidence,
                artifactPaths,
                runtimeCapture.ReplayTape,
                benchmarkSuite);
            artifactPaths = new Dictionary<string, string>(manifestResult.ArtifactPaths, StringComparer.Ordinal);
            manifest = manifestResult.Manifest;
        }
        else
        {
            artifactPaths["benchmarks"] = benchmarkSuite.WriteArtifacts($"{runId}-benchmark-suite.json");
        }
    }
    else if (writeManifest)
    {
        var manifestResult = EvidenceManifestBuilder.WriteHostedParityManifest(
            runId,
            runtimeCapture.Evidence,
            artifactPaths,
            runtimeCapture.ReplayTape);
        artifactPaths = new Dictionary<string, string>(manifestResult.ArtifactPaths, StringComparer.Ordinal);
        manifest = manifestResult.Manifest;
    }

    if (writeManifest && openTuiContracts is not null && artifactPaths.TryGetValue("manifest", out var manifestPath))
    {
        manifest ??= EvidenceManifest.FromJson(File.ReadAllText(manifestPath));
        var plannerReport = OpenTuiPlanner.Build(runId, openTuiContracts, manifest);
        foreach (var entry in OpenTuiPlanner.WriteArtifacts(runId, plannerReport))
        {
            artifactPaths[entry.Key] = entry.Value;
        }

        var gateReport = OpenTuiContractGate.Evaluate(runId, openTuiContracts, manifest, plannerReport: plannerReport);
        foreach (var entry in OpenTuiContractGate.WriteArtifacts(runId, gateReport))
        {
            artifactPaths[entry.Key] = entry.Value;
        }
    }

    ArtifactManifestSummary? artifactManifest = null;
    FailureLogSummary? failureSignatures = null;
    if (writeManifest)
    {
        artifactManifest = ArtifactManifestContract.BuildSummary(artifactPaths);
        artifactPaths["artifact_manifest_summary"] = ArtifactManifestContract.WriteSummary(runId, artifactManifest);

        failureSignatures = FailureSignatures.ValidateBatch(BuildFailureLogEntries(benchmarkErrors, artifactManifest));
        artifactPaths["failure_signatures"] = FailureSignatures.WriteSummary(runId, failureSignatures);
    }

    bootstrap = DoctorBootstrapSummary.Build(
        runId,
        format,
        writeArtifacts,
        writeManifest,
        runBenchmarks,
        openTuiContracts is not null,
        benchmarkBaseline,
        benchmarkErrors,
        manifest);
    artifactPaths["bootstrap_summary"] = DoctorBootstrapSummary.WriteArtifact(runId, bootstrap);

    var workflow = DoctorWorkflowSummary.Build(
        runId,
        format,
        writeArtifacts,
        writeManifest,
        runBenchmarks,
        artifactPaths,
        manifest,
        benchmarkErrors);
    artifactPaths["workflow_summary"] = DoctorWorkflowSummary.WriteArtifact(runId, workflow);

    suite = DoctorSuiteReport.Build(runId, artifactPaths, manifest, workflow);
    artifactPaths["suite_report"] = DoctorSuiteReport.WriteArtifact(runId, suite);
    var finishedAt = DateTimeOffset.UtcNow.ToString("O");
    runMeta = DoctorRunMeta.Build(runId, startedAt, finishedAt, RepositoryPaths.FindRepositoryRoot(), artifactPaths, manifest, workflow);
    artifactPaths["run_meta"] = DoctorRunMeta.WriteArtifact(runId, runMeta);
    suiteManifest = DoctorSuiteManifest.Build(runId, runMeta);
    artifactPaths["suite_manifest"] = DoctorSuiteManifest.WriteArtifact(runId, suiteManifest);
    suiteAggregate = DoctorSuiteAggregator.Build(runId, [runMeta]);
    artifactPaths["suite_aggregate"] = DoctorSuiteAggregator.WriteArtifact(runId, suiteAggregate);

    report = report with
    {
        ArtifactPaths = artifactPaths,
        ArtifactManifest = artifactManifest,
        FailureSignatures = failureSignatures,
        CostProfile = costProfile,
        Workflow = workflow,
        Bootstrap = bootstrap,
        SeedPlan = seedPlan,
        SeedExecution = seedExecution
    };
}
else
{
    bootstrap = DoctorBootstrapSummary.Build(
        runId,
        format,
        writeArtifacts,
        writeManifest,
        runBenchmarks,
        openTuiContracts is not null,
        benchmarkBaseline,
        benchmarkErrors,
        null);
    var workflow = DoctorWorkflowSummary.Build(runId, format, writeArtifacts, writeManifest, runBenchmarks, null, null, benchmarkErrors);
    suite = DoctorSuiteReport.Build(runId, null, null, workflow);
    var finishedAt = DateTimeOffset.UtcNow.ToString("O");
    runMeta = DoctorRunMeta.Build(runId, startedAt, finishedAt, RepositoryPaths.FindRepositoryRoot(), null, null, workflow);
    suiteManifest = DoctorSuiteManifest.Build(runId, runMeta);
    suiteAggregate = DoctorSuiteAggregator.Build(runId, [runMeta]);
    report = report with
    {
        CostProfile = costProfile,
        Workflow = workflow,
        Bootstrap = bootstrap,
        Suite = suite,
        RunMeta = runMeta,
        SuiteManifest = suiteManifest,
        SeedPlan = seedPlan,
        SeedExecution = seedExecution,
        SuiteAggregate = suiteAggregate
    };
}

if (writeArtifacts || writeManifest || runBenchmarks)
{
    var artifactPaths = new Dictionary<string, string>(report.ArtifactPaths ?? new Dictionary<string, string>(StringComparer.Ordinal), StringComparer.Ordinal);
    var suiteRoot = Path.Combine(RepositoryPaths.FindRepositoryRoot(), "artifacts", "replay", "doctor-suite");
    artifactPaths["suite_run_meta"] = DoctorSuiteManifest.WriteSuiteRunMeta(suiteRoot, runId, runMeta);
    var suiteRuns = DoctorSuiteManifest.LoadRunMetas(suiteRoot);
    suiteManifest = DoctorSuiteManifest.Build("doctor-suite", suiteRoot, suiteRuns);
    artifactPaths["suite_manifest"] = DoctorSuiteManifest.WriteArtifact(runId, suiteManifest);
    suiteAggregate = DoctorSuiteAggregator.Build("doctor-suite", suiteRuns);
    artifactPaths["suite_aggregate"] = DoctorSuiteAggregator.WriteArtifact(runId, suiteAggregate);
    suite = DoctorSuiteReport.Build("FrankenTui.Net Doctor Suite Report", suiteRoot, suiteRuns);
    artifactPaths["suite_report"] = DoctorSuiteReport.WriteArtifact(runId, suite);
    artifactPaths["suite_index"] = DoctorSuiteReport.WriteHtmlArtifact(runId, suite);

    report = report with
    {
        ArtifactPaths = artifactPaths,
        Suite = suite,
        RunMeta = runMeta,
        SuiteManifest = suiteManifest,
        SuiteAggregate = suiteAggregate
    };
}

if (string.Equals(format, "text", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine(DoctorDashboardViewFactory.RenderText(report));
    if (benchmarkErrors.Count > 0)
    {
        var benchmarkLabel = PerformanceBenchmarkRunner.ShouldFailOnBudgetErrors()
            ? "Benchmark gate"
            : "Benchmark advisory";
        Console.WriteLine($"{benchmarkLabel}: {string.Join(" | ", benchmarkErrors)}");
    }
}
else
{
    Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions
    {
        WriteIndented = true
    }));
}

if (benchmarkErrors.Count > 0 && PerformanceBenchmarkRunner.ShouldFailOnBudgetErrors())
{
    Environment.ExitCode = 1;
}

static ushort ParseUShort(string[] arguments, string name, ushort fallback) =>
    ushort.TryParse(Parse(arguments, name), out var value) ? value : fallback;

static bool HasFlag(string[] arguments, string name) =>
    Array.Exists(arguments, argument => argument.Equals(name, StringComparison.OrdinalIgnoreCase));

static string? Parse(string[] arguments, string name)
{
    for (var index = 0; index < arguments.Length - 1; index++)
    {
        if (arguments[index].Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            return arguments[index + 1];
        }
    }

    return null;
}

static IReadOnlyDictionary<string, string> WriteContractArtifacts(
    string runId,
    TelemetryConfig telemetry,
    MermaidConfig mermaid,
    OpenTuiContractSet? openTuiContracts)
{
    var artifacts = new Dictionary<string, string>(StringComparer.Ordinal);

    var telemetryPath = ArtifactPathBuilder.For("contracts", $"{runId}-telemetry-config.json");
    File.WriteAllText(telemetryPath, telemetry.ToJson());
    artifacts["telemetry_config"] = telemetryPath;

    var mermaidPath = ArtifactPathBuilder.For("contracts", $"{runId}-mermaid-config.json");
    File.WriteAllText(mermaidPath, mermaid.ToJson());
    artifacts["mermaid_config"] = mermaidPath;

    if (openTuiContracts is not null)
    {
        foreach (var entry in openTuiContracts.WriteArtifacts(runId))
        {
            artifacts[entry.Key] = entry.Value;
        }
    }

    return artifacts;
}

static IReadOnlyList<FailureLogEntry> BuildFailureLogEntries(
    IReadOnlyList<string> benchmarkErrors,
    ArtifactManifestSummary artifactManifest)
{
    var entries = new List<FailureLogEntry>();

    if (benchmarkErrors.Count > 0)
    {
        entries.Add(new FailureLogEntry(
            FailureClass.ProcessFailure,
            new HashSet<string>(StringComparer.Ordinal)
            {
                "reason",
                "program",
                "exit_code",
                "sub_id"
            }));
    }

    if (artifactManifest.FailingCount > 0)
    {
        entries.Add(new FailureLogEntry(
            FailureClass.Mismatch,
            new HashSet<string>(StringComparer.Ordinal)
            {
                "reason",
                "scenario",
                "seed"
            }));
    }

    return entries;
}
