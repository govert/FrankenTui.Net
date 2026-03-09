using FrankenTui.Testing.Harness;
using FrankenTui.Testing.Pty;

namespace FrankenTui.Tests.Pty;

public sealed class PtyIntegrationTests
{
    [Fact]
    public async Task ShowcaseDefaultSessionUsesAlternateScreenAndCleanup()
    {
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
            "2"
        ]);

        Assert.True(result.Success, result.Stderr);
        Assert.Contains("\u001b[?1049h", result.Stdout);
        Assert.Contains("\u001b[?1049l", result.Stdout);
        Assert.Contains("\u001b[?25l", result.Stdout);
        Assert.Contains("\u001b[?25h", result.Stdout);
        Assert.Contains("FrankenTui.Net", result.Stdout);
    }

    [Fact]
    public async Task ShowcaseInlineModeAvoidsAlternateScreenAndKeepsVisibleTranscript()
    {
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
            "1"
        ]);

        Assert.True(result.Success, result.Stderr);
        Assert.DoesNotContain("\u001b[?1049h", result.Stdout);
        Assert.Contains("FrankenTui.Net", result.Stdout);
    }

    [Fact]
    public async Task DoctorRunsUnderPtyAndProducesJson()
    {
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
        Assert.Contains("\"Notes\"", result.Stdout);
    }
}
