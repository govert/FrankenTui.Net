using System.Text.Json;
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

var report = EnvironmentDoctor.CreateReport();
BenchmarkSuiteResult? benchmarkSuite = null;
IReadOnlyList<string> benchmarkErrors = [];

if (writeArtifacts || writeManifest || runBenchmarks)
{
    var evidence = RenderHarness.CaptureHostedParity(
        "doctor-dashboard",
        DoctorDashboardViewFactory.Build(report),
        width,
        height,
        options: DoctorDashboardViewFactory.CreateWebOptions(report));
    var artifactPaths = new Dictionary<string, string>(evidence.WriteArtifacts("doctor"), StringComparer.Ordinal);

    if (runBenchmarks)
    {
        var budgets = PerformanceBenchmarkRunner.LoadBudgets(benchmarkBaseline);
        (benchmarkSuite, benchmarkErrors) = PerformanceBenchmarkRunner.RunGate(budgets);

        if (writeManifest)
        {
            var replayTape = CreateReplay(evidence, report);
            var manifestResult = EvidenceManifestBuilder.WriteHostedParityManifest(runId, evidence, artifactPaths, replayTape, benchmarkSuite);
            artifactPaths = new Dictionary<string, string>(manifestResult.ArtifactPaths, StringComparer.Ordinal);
        }
        else
        {
            artifactPaths["benchmarks"] = benchmarkSuite.WriteArtifacts($"{runId}-benchmark-suite.json");
        }
    }
    else if (writeManifest)
    {
        var replayTape = CreateReplay(evidence, report);
        var manifestResult = EvidenceManifestBuilder.WriteHostedParityManifest(runId, evidence, artifactPaths, replayTape);
        artifactPaths = new Dictionary<string, string>(manifestResult.ArtifactPaths, StringComparer.Ordinal);
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

static ReplayTape<string> CreateReplay(HostedParityEvidence evidence, DoctorReport report)
{
    var replay = new ReplayTape<string>();
    replay.Add(
        0,
        "doctor-dashboard",
        [],
        evidence.Terminal.Text,
        $"{report.OperatingSystem}|{report.HostProfile}|{report.HostValidationStatus}");
    return replay;
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
