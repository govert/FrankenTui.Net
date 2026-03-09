using System.Text.Json;
using FrankenTui.Testing.Harness;

namespace FrankenTui.Tests.Headless;

public sealed class BenchmarkRunnerTests
{
    [Fact]
    public void DefaultBenchmarkFixtureTracksUpstreamBaselineKeys()
    {
        var upstreamPath = UpstreamReferencePaths.BenchmarkBaseline();
        Assert.True(File.Exists(upstreamPath), $"Missing upstream baseline at {upstreamPath}.");

        var budgets = PerformanceBenchmarkRunner.LoadBudgets(PerformanceBenchmarkRunner.DefaultBudgetPath);
        using var upstream = JsonDocument.Parse(File.ReadAllText(upstreamPath));
        var upstreamKeys = upstream.RootElement.EnumerateObject()
            .Where(static property => !property.Name.StartsWith('_'))
            .Select(static property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(budgets, static budget => budget.Name == "frame_render");
        Assert.Contains(budgets, static budget => budget.Name == "web_document");
        Assert.All(
            budgets.Where(static budget => !budget.UpstreamKey.StartsWith("dotnet/", StringComparison.Ordinal)),
            budget => Assert.Contains(budget.UpstreamKey, upstreamKeys));
    }

    [Fact]
    public void BenchmarkGateRunsAgainstTrackedFixture()
    {
        var budgets = PerformanceBenchmarkRunner.LoadBudgets(PerformanceBenchmarkRunner.DefaultBudgetPath);
        var (suite, errors) = PerformanceBenchmarkRunner.RunGate(budgets);

        Assert.Equal(budgets.Count, suite.Measurements.Count);
        Assert.Empty(errors);
        Assert.Contains(suite.Measurements, static measurement => measurement.Name == "runtime_dispatch");
        Assert.Contains(suite.Measurements, static measurement => measurement.Name == "ansi_emit");
    }
}
