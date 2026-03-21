using FrankenTui.Testing.Harness;

namespace FrankenTui.Tests.Headless;

public sealed class FixtureSuiteTests
{
    [Fact]
    public void BaselineCaptureComputesPercentilesAndStability()
    {
        var capture = new BaselineCapture("render_sparse_80x24", FixtureFamily.Render);
        capture.RecordSample(Sample.LatencyUs("buffer_diff", 100));
        capture.RecordSample(Sample.LatencyUs("buffer_diff", 102));
        capture.RecordSample(Sample.LatencyUs("buffer_diff", 98));
        capture.RecordSample(Sample.OutputCost("dirty_cells", 12));

        var baseline = capture.Finalize();
        var latency = Assert.Single(baseline.Metrics, metric => metric.Metric == "buffer_diff");

        Assert.Equal(3, latency.SampleCount);
        Assert.Equal(StabilityClass.Stable, latency.Stability);
        Assert.Equal(98d, latency.Percentiles.Min);
        Assert.Equal(102d, latency.Percentiles.Max);
        Assert.True(baseline.IsStable);
    }

    [Fact]
    public void CanonicalFixtureRegistryExposesExpectedFamiliesAndPartitions()
    {
        var registry = FixtureRegistry.Canonical();

        Assert.NotNull(registry.Find("render_sparse_80x24"));
        Assert.NotEmpty(registry.ByFamily(FixtureFamily.Render));
        Assert.NotEmpty(registry.ByFamily(FixtureFamily.Runtime));
        Assert.NotEmpty(registry.ByFamily(FixtureFamily.Doctor));
        Assert.NotEmpty(registry.ByPartition(SuitePartition.Challenge));
        Assert.NotEmpty(registry.ByPartition(SuitePartition.NegativeControl));
    }

    [Fact]
    public void FixtureRunnerExecutesCanonicalSuiteAndProducesStableBaselines()
    {
        var runner = new FixtureRunner(FixtureRegistry.Canonical());

        var report = runner.RunAll();

        Assert.Equal(5, report.Fixtures.Count);
        Assert.All(report.Fixtures, fixture =>
        {
            Assert.True(fixture.Passed, fixture.Fixture.Id);
            Assert.NotEmpty(fixture.Baseline.Metrics);
            Assert.True(fixture.MatchRatio >= 1d, fixture.Fixture.Id);
        });
    }

    [Fact]
    public void RolloutScorecardProducesGoAndNoGoVerdictsFromFixtureEvidence()
    {
        var runner = new FixtureRunner(FixtureRegistry.Canonical());
        var suite = runner.RunAll();

        var goScorecard = new RolloutScorecard(
            RolloutScorecardConfig.Default()
                .WithMinFixtureCount(3)
                .WithMinMatchRatio(1d)
                .WithChallengeCoverage(true)
                .WithBenchmarkRequirement(true));
        goScorecard.AddFixtures(suite.Fixtures);
        goScorecard.SetBenchmarkGate(new BenchmarkGateEvidence(true, "local-benchmark-gate"));

        var goSummary = goScorecard.Summary();

        Assert.Equal(RolloutVerdict.Go, goSummary.Verdict);
        Assert.True(goSummary.ChallengeCoverageMet);
        Assert.Equal(1d, goSummary.AggregateMatchRatio);

        var noGoScorecard = new RolloutScorecard(RolloutScorecardConfig.Default().WithMinFixtureCount(1));
        noGoScorecard.AddFixture(
            new FixtureRunReport(
                suite.Fixtures[0].Fixture,
                suite.Fixtures[0].Baseline,
                false,
                4,
                2,
                ["forced-failure"]));

        Assert.Equal(RolloutVerdict.NoGo, noGoScorecard.Evaluate());
    }
}
