namespace FrankenTui.Testing.Harness;

public sealed record BenchmarkGateEvidence(bool Passed, string Source);

public sealed record RolloutScorecardConfig(
    int MinFixtureCount,
    double MinMatchRatio,
    bool RequireBenchmarkPass,
    bool RequireChallengeCoverage)
{
    public static RolloutScorecardConfig Default() => new(1, 1d, false, false);

    public RolloutScorecardConfig WithMinFixtureCount(int count) => this with { MinFixtureCount = Math.Max(count, 0) };

    public RolloutScorecardConfig WithMinMatchRatio(double ratio) => this with { MinMatchRatio = Math.Clamp(ratio, 0d, 1d) };

    public RolloutScorecardConfig WithBenchmarkRequirement(bool required) => this with { RequireBenchmarkPass = required };

    public RolloutScorecardConfig WithChallengeCoverage(bool required) => this with { RequireChallengeCoverage = required };
}

public enum RolloutVerdict
{
    Go,
    NoGo,
    Inconclusive
}

public sealed record RolloutSummary(
    RolloutVerdict Verdict,
    int FixturesRun,
    int FixturesPassed,
    int TotalFramesCompared,
    int TotalFramesMatched,
    double AggregateMatchRatio,
    bool ChallengeCoverageMet,
    bool? BenchmarkPassed,
    int MinFixtureCountRequired,
    double MinMatchRatioRequired,
    bool BenchmarkRequired,
    bool ChallengeCoverageRequired);

public sealed class RolloutScorecard
{
    private readonly RolloutScorecardConfig _config;
    private readonly List<FixtureRunReport> _fixtures = [];
    private BenchmarkGateEvidence? _benchmark;

    public RolloutScorecard(RolloutScorecardConfig? config = null)
    {
        _config = config ?? RolloutScorecardConfig.Default();
    }

    public void AddFixture(FixtureRunReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _fixtures.Add(report);
    }

    public void AddFixtures(IEnumerable<FixtureRunReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);
        foreach (var report in reports)
        {
            AddFixture(report);
        }
    }

    public void SetBenchmarkGate(BenchmarkGateEvidence benchmark)
    {
        ArgumentNullException.ThrowIfNull(benchmark);
        _benchmark = benchmark;
    }

    public RolloutVerdict Evaluate()
    {
        if (_fixtures.Count < _config.MinFixtureCount)
        {
            return RolloutVerdict.Inconclusive;
        }

        if (_config.RequireChallengeCoverage && !_fixtures.Any(static fixture => fixture.Fixture.Partition == SuitePartition.Challenge))
        {
            return RolloutVerdict.Inconclusive;
        }

        if (_fixtures.Any(static fixture => !fixture.Passed))
        {
            return RolloutVerdict.NoGo;
        }

        if (AggregateMatchRatio() < _config.MinMatchRatio)
        {
            return RolloutVerdict.NoGo;
        }

        if (_config.RequireBenchmarkPass)
        {
            if (_benchmark is null)
            {
                return RolloutVerdict.Inconclusive;
            }

            if (!_benchmark.Passed)
            {
                return RolloutVerdict.NoGo;
            }
        }

        return RolloutVerdict.Go;
    }

    public RolloutSummary Summary() =>
        new(
            Evaluate(),
            _fixtures.Count,
            _fixtures.Count(static fixture => fixture.Passed),
            _fixtures.Sum(static fixture => fixture.FramesCompared),
            _fixtures.Sum(static fixture => fixture.FramesMatched),
            AggregateMatchRatio(),
            _fixtures.Any(static fixture => fixture.Fixture.Partition == SuitePartition.Challenge),
            _benchmark?.Passed,
            _config.MinFixtureCount,
            _config.MinMatchRatio,
            _config.RequireBenchmarkPass,
            _config.RequireChallengeCoverage);

    private double AggregateMatchRatio()
    {
        if (_fixtures.Count == 0)
        {
            return 0d;
        }

        var framesCompared = _fixtures.Sum(static fixture => fixture.FramesCompared);
        if (framesCompared == 0)
        {
            return _fixtures.All(static fixture => fixture.Passed) ? 1d : 0d;
        }

        return _fixtures.Sum(static fixture => fixture.FramesMatched) / (double)framesCompared;
    }
}
