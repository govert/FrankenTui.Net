using FrankenTui.Testing.Harness;
using FrankenTui.Testing.Pty;

namespace FrankenTui.Tests.Pty;

public sealed class PtyIntegrationTests
{
    [Fact]
    public async Task ShowcaseDefaultSessionUsesAlternateScreenAndCleanup()
    {
        if (!CanRunPty())
        {
            return;
        }

        var root = RepositoryPaths.FindRepositoryRoot();
        var demoProject = Path.Combine(root, "apps", "FrankenTui.Demo.Showcase", "FrankenTui.Demo.Showcase.csproj");

        var result = await ScriptPtyRunner.RunCommandAsync("dotnet",
        [
            "run",
            "--project",
            demoProject,
            "--no-restore",
            "--",
            "--width",
            "60",
            "--height",
            "16",
            "--frames",
            "2",
            "--screen",
            "14"
        ]);

        Assert.True(result.Success, result.Stderr);
        Assert.Contains("\u001b[?1049h", result.Stdout);
        Assert.Contains("\u001b[?1049l", result.Stdout);
        Assert.Contains("\u001b[?25l", result.Stdout);
        Assert.Contains("\u001b[?25h", result.Stdout);
        Assert.Contains("Performance", result.Stdout);
        Assert.Contains("Metric", result.Stdout);
    }

    [Fact]
    public async Task ShowcaseInlineModeAvoidsAlternateScreenAndKeepsVisibleTranscript()
    {
        if (!CanRunPty())
        {
            return;
        }

        var root = RepositoryPaths.FindRepositoryRoot();
        var demoProject = Path.Combine(root, "apps", "FrankenTui.Demo.Showcase", "FrankenTui.Demo.Showcase.csproj");

        var result = await ScriptPtyRunner.RunCommandAsync("dotnet",
        [
            "run",
            "--project",
            demoProject,
            "--no-restore",
            "--",
            "--inline",
            "--width",
            "60",
            "--height",
            "16",
            "--frames",
            "3",
            "--screen",
            "6"
        ]);

        Assert.True(result.Success, result.Stderr);
        Assert.DoesNotContain("\u001b[?1049h", result.Stdout);
        Assert.Contains("\u001b7", result.Stdout);
        Assert.Contains("\u001b8", result.Stdout);
        Assert.Contains("Layout", result.Stdout);
        Assert.Contains("Workspace", result.Stdout);
    }

    [Fact]
    public async Task DoctorRunsUnderPtyAndProducesJson()
    {
        if (!CanRunPty())
        {
            return;
        }

        var root = RepositoryPaths.FindRepositoryRoot();
        var doctorProject = Path.Combine(root, "tools", "FrankenTui.Doctor", "FrankenTui.Doctor.csproj");

        var result = await ScriptPtyRunner.RunCommandAsync("dotnet",
        [
            "run",
            "--project",
            doctorProject,
            "--no-restore"
        ]);

        Assert.True(result.Success, result.Stderr);
        Assert.Contains("\"OperatingSystem\"", result.Stdout);
        Assert.Contains("\"HostProfile\"", result.Stdout);
        Assert.Contains("\"HostValidationStatus\"", result.Stdout);
        Assert.Contains("\"Notes\"", result.Stdout);
    }

    [Fact]
    public async Task DoctorCanWriteArtifactsAndTextSummary()
    {
        if (!CanRunPty())
        {
            return;
        }

        var root = RepositoryPaths.FindRepositoryRoot();
        var doctorProject = Path.Combine(root, "tools", "FrankenTui.Doctor", "FrankenTui.Doctor.csproj");

        var result = await ScriptPtyRunner.RunCommandAsync("dotnet",
        [
            "run",
            "--project",
            doctorProject,
            "--no-restore",
            "--",
            "--format",
            "text",
            "--write-artifacts",
            "--write-manifest",
            "--run-benchmarks",
            "--run-id",
            "pty-doctor"
        ]);

        Assert.True(result.Success, result.Stderr);
        Assert.Contains("FrankenTui.Net Doctor", result.Stdout);
        Assert.Contains("Artifacts:", result.Stdout);
        Assert.DoesNotContain("Benchmark gate:", result.Stdout);
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "doctor-runtime-runtime-trace.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "benchmarks", "pty-doctor-benchmark-suite.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "benchmarks", "pty-doctor-doctor-cost-profile.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-manifest.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-artifact-manifest-summary.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-bootstrap-summary.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-failure-signatures.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-run-meta.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-seed-plan.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-seed-execution.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-suite-aggregate.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-suite-index.html")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-suite-manifest.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-suite-report.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "doctor-suite", "pty-doctor", "run_meta.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-workflow-summary.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "web", "doctor-dashboard.html")));
    }

    [Fact]
    public async Task DoctorReportsTelemetryWhenConfiguredByEnvironment()
    {
        if (!CanRunPty())
        {
            return;
        }

        var root = RepositoryPaths.FindRepositoryRoot();
        var doctorProject = Path.Combine(root, "tools", "FrankenTui.Doctor", "FrankenTui.Doctor.csproj");

        var result = await ScriptPtyRunner.RunCommandAsync(
            "dotnet",
            [
                "run",
                "--project",
                doctorProject,
                "--no-restore",
                "--",
                "--format",
                "text"
            ],
            environmentVariables: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://collector.invalid:4318",
                ["FTUI_OTEL_SPAN_PROCESSOR"] = "simple",
                ["OTEL_TRACE_ID"] = "0123456789abcdef0123456789abcdef",
                ["OTEL_PARENT_SPAN_ID"] = "0123456789abcdef"
            });

        Assert.True(result.Success, result.Stderr);
        Assert.Contains("Telemetry: enabled", result.Stdout);
        Assert.Contains("protocol=http/protobuf", result.Stdout);
        Assert.Contains("endpoint=http://collector.invalid:4318", result.Stdout);
    }

    [Fact]
    public async Task ShowcaseInteractiveInlineModeConsumesInputAndExitsOnQuit()
    {
        if (!CanRunPty())
        {
            return;
        }

        var root = RepositoryPaths.FindRepositoryRoot();
        var demoProject = Path.Combine(root, "apps", "FrankenTui.Demo.Showcase", "FrankenTui.Demo.Showcase.csproj");

        var result = await ScriptPtyRunner.RunCommandAsync(
            "dotnet",
            [
                "run",
                "--project",
                demoProject,
                "--no-restore",
                "--",
                "--inline",
                "--interactive",
                "--screen",
                "5",
                "--width",
                "60",
                "--height",
                "16"
            ],
            stdin: "q");

        Assert.True(result.Success, result.Stderr);
        Assert.Contains("Widget", result.Stdout);
        Assert.Contains("render completeness", result.Stdout);
    }

    [Fact]
    public async Task ShowcaseExtrasScenarioRendersMaterialExtras()
    {
        if (!CanRunPty())
        {
            return;
        }

        var root = RepositoryPaths.FindRepositoryRoot();
        var demoProject = Path.Combine(root, "apps", "FrankenTui.Demo.Showcase", "FrankenTui.Demo.Showcase.csproj");

        var result = await ScriptPtyRunner.RunCommandAsync("dotnet",
        [
            "run",
            "--project",
            demoProject,
            "--no-restore",
            "--",
            "--inline",
            "--width",
            "72",
            "--height",
            "18",
            "--frames",
            "2",
            "--screen",
            "16"
        ]);

        Assert.True(result.Success, result.Stderr);
        Assert.Contains("Mermaid", result.Stdout);
        Assert.Contains("Controls", result.Stdout);
    }

    private static bool CanRunPty() => OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
}
