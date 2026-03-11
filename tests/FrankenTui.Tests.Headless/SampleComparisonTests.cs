using FrankenTui.Testing.Harness;

namespace FrankenTui.Tests.Headless;

public sealed class SampleComparisonTests
{
    [Fact]
    public async Task SharedSampleSuiteMatchesUpstreamReferenceRunner()
    {
        if (!SharedSampleComparison.CanRunUpstream())
        {
            return;
        }

        var report = await SharedSampleComparison.CompareAsync();
        var artifacts = report.WriteArtifacts("vrf357-shared-samples");

        Assert.True(report.IsMatch, report.DifferenceSummary());
        Assert.True(File.Exists(artifacts["local_capture"]));
        Assert.True(File.Exists(artifacts["upstream_capture"]));
        Assert.True(File.Exists(artifacts["report"]));
        Assert.Contains("\"counter_flow\"", report.Local.Json);
        Assert.Contains("\"inline_overlay\"", report.Local.Json);
        Assert.Contains("\"command_palette\"", report.Local.Json);
        Assert.Contains("\"log_search\"", report.Local.Json);
    }
}
