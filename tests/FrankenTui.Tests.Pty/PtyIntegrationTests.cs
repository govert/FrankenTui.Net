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
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "doctor", "doctor-dashboard.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "benchmarks", "pty-doctor-benchmark-suite.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "replay", "pty-doctor-manifest.json")));
        Assert.True(File.Exists(Path.Combine(root, "artifacts", "web", "doctor-dashboard.html")));
    }

    private static bool CanRunPty() => OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
}
