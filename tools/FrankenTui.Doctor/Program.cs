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
var benchmarkBaseline = Parse(args, "--benchmark-baseline") ?? PerformanceBenchmarkRunner.DefaultBudgetPath;
var telemetry = TelemetryConfig.FromEnvironment();
var mermaid = MermaidConfig.FromEnvironment();
var openTuiContracts = OpenTuiContractSet.TryLoadUpstreamReference();
var report = EnvironmentDoctor.CreateReport(
    telemetry.ToSummary(),
    mermaid.ToSummary(MermaidShowcaseSurface.Catalog().Count),
    openTuiContracts?.Migration.ToSummary() ?? OpenTuiMigrationContractSummary.Missing("Upstream OpenTUI reference contracts are unavailable in .external/frankentui."));

BenchmarkSuiteResult? benchmarkSuite = null;
IReadOnlyList<string> benchmarkErrors = [];

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
    }

    if (writeManifest && openTuiContracts is not null && artifactPaths.TryGetValue("manifest", out var manifestPath))
    {
        var manifest = EvidenceManifest.FromJson(File.ReadAllText(manifestPath));
        var gateReport = OpenTuiContractGate.Evaluate(runId, openTuiContracts, manifest);
        foreach (var entry in OpenTuiContractGate.WriteArtifacts(runId, gateReport))
        {
            artifactPaths[entry.Key] = entry.Value;
        }
    }

    report = report with { ArtifactPaths = artifactPaths };
}

if (string.Equals(format, "text", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine(DoctorDashboardViewFactory.RenderText(report));
    if (benchmarkErrors.Count > 0)
    {
        Console.WriteLine($"Benchmark gate: {string.Join(" | ", benchmarkErrors)}");
    }
}
else
{
    Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions
    {
        WriteIndented = true
    }));
}

if (benchmarkErrors.Count > 0)
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
