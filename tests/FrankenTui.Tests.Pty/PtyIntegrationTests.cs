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
            "--scenario",
            "tooling"
        ]);

        Assert.True(result.Success, result.Stderr);
        Assert.Contains("\u001b[?1049h", result.Stdout);
        Assert.Contains("\u001b[?1049l", result.Stdout);
        Assert.Contains("\u001b[?25l", result.Stdout);
        Assert.Contains("\u001b[?25h", result.Stdout);
        Assert.Contains("Tooling", result.Stdout);
        Assert.Contains("Hosted parity", result.Stdout);
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
            "--scenario",
            "interaction"
        ]);

        Assert.True(result.Success, result.Stderr);
        Assert.DoesNotContain("\u001b[?1049h", result.Stdout);
        Assert.Contains("\u001b7", result.Stdout);
        Assert.Contains("\u001b8", result.Stdout);
        Assert.Contains("Interaction", result.Stdout);
        Assert.Contains("Focus", result.Stdout);
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
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-manifest.json")));
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
                "--width",
                "60",
                "--height",
                "16"
            ],
            stdin: "\tjlq");

        Assert.True(result.Success, result.Stderr);
        Assert.Contains("Overview", result.Stdout);
        Assert.Contains("Overlay", result.Stdout);
        Assert.Contains("Notes", result.Stdout);
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
            "--scenario",
            "extras"
        ]);

        Assert.True(result.Success, result.Stderr);
        Assert.Contains("Extras", result.Stdout);
        Assert.Contains("Pane Workspace", result.Stdout);
        Assert.Contains("Command Palette", result.Stdout);
    }

    private static bool CanRunPty() => OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
}
