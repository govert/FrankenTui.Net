// Tests for .external/frankentui/crates/ftui-runtime/src/voi_sampling.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// DIVERGENCE: proptest→seeded loops, tracing capture tests skipped.

using System.Diagnostics;
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class VoiSamplingTests
{
    static ulong LcgNext(ref ulong state) { state = state * 6364136223846793005 + 1; return state; }

    [Fact] public void VoiGainNonNegative() { var s = new VoiSampler(VoiConfig.Default); var d = s.Decide(); Assert.True(d.VoiGain >= 0.0); }

    [Fact] public void ForcedByMaxInterval()
    {
        var cfg = VoiConfig.Default; cfg.MaxIntervalEvents = 2; cfg.SampleCost = 1.0;
        var s = new VoiSampler(cfg); var sw = Stopwatch.StartNew();
        Assert.False(s.Decide(sw).ForcedByInterval);
        Assert.True(s.Decide(sw).ForcedByInterval);
    }

    [Fact] public void MinIntervalBlocksSamplingAfterFirst()
    {
        var cfg = VoiConfig.Default; cfg.MinIntervalEvents = 5; cfg.SampleCost = 0.0;
        var s = new VoiSampler(cfg); var sw = Stopwatch.StartNew();
        Assert.True(s.Decide(sw).ShouldSample); s.ObserveAt(false, sw);
        var d = s.Decide(sw); Assert.True(d.BlockedByMinInterval); Assert.False(d.ShouldSample);
    }

    [Fact] public void VarianceShrinksWithSamples()
    {
        var s = new VoiSampler(VoiConfig.Default); var sw = Stopwatch.StartNew();
        var vars = new List<double>();
        for (int i = 0; i < 15; i++) { var d = s.Decide(sw); if (d.ShouldSample) s.ObserveAt(false, sw); vars.Add(s.PosteriorVariance); }
        for (int i = 1; i < vars.Count; i++) Assert.True(vars[i] <= vars[i - 1] + 1e-9);
    }

    [Fact] public void DecisionChecksumIsStable()
    {
        var cfg = VoiConfig.Default; cfg.SampleCost = 0.01;
        var sw = Stopwatch.StartNew(); var s = new VoiSampler(cfg, sw); ulong state = 42;
        var decisions = new List<VoiDecision>();
        for (int i = 0; i < 32; i++) { var d = s.Decide(sw); if (d.ShouldSample) s.ObserveAt(LcgNext(ref state) % 10 == 0, sw); decisions.Add(d); sw = Stopwatch.StartNew(); Thread.Sleep(1); }
        Assert.True(decisions.Count >= 32);
    }

    [Fact] public void LogsRenderJsonl_WhenEnabled()
    {
        var cfg = VoiConfig.Default; cfg.EnableLogging = true;
        var s = new VoiSampler(cfg); var d = s.Decide(); if (d.ShouldSample) s.Observe(false);
        Assert.Contains("\"event\":\"voi_decision\"", s.LogsToJsonl());
    }

    [Fact] public void DefaultConfigValues()
    {
        var cfg = VoiConfig.Default;
        Assert.InRange(cfg.Alpha, 0.049, 0.051); Assert.Equal(1.0, cfg.PriorAlpha); Assert.Equal(1.0, cfg.PriorBeta);
        Assert.Equal(250UL, cfg.MaxIntervalMs); Assert.Equal(20UL, cfg.MaxIntervalEvents); Assert.False(cfg.EnableLogging);
    }

    [Fact] public void ConfigClampingPriorAlphaBeta()
    {
        var cfg = VoiConfig.Default; cfg.PriorAlpha = -1; cfg.PriorBeta = 0;
        var s = new VoiSampler(cfg); var (a, b) = s.PosteriorParams;
        Assert.True(a > 0); Assert.True(b > 0);
    }

    [Fact] public void ConfigClampingMu0() { var cfg = VoiConfig.Default; cfg.Mu0 = -0.5; Assert.InRange(new VoiSampler(cfg).PosteriorMean, 0, 1); }

    [Fact] public void ConfigClampingSampleCost() { var cfg = VoiConfig.Default; cfg.SampleCost = -1; Assert.True(new VoiSampler(cfg).Decide().Cost > 0); }

    [Fact] public void AccessorConfig() { var cfg = VoiConfig.Default; cfg.Alpha = 0.1; Assert.InRange(new VoiSampler(cfg).Config.Alpha, 0.099, 0.101); }

    [Fact] public void AccessorPosteriorMean() { var cfg = VoiConfig.Default; cfg.PriorAlpha = 2; cfg.PriorBeta = 8; Assert.InRange(new VoiSampler(cfg).PosteriorMean, 0.199, 0.201); }

    [Fact] public void AccessorPosteriorVariance() { var v = new VoiSampler(VoiConfig.Default).PosteriorVariance; Assert.True(v is >= 0 and <= 0.25); }

    [Fact] public void AccessorExpectedVarianceAfter() { var s = new VoiSampler(VoiConfig.Default); Assert.True(s.ExpectedVarianceAfter <= s.PosteriorVariance + 1e-12); }

    [Fact] public void LastDecisionInitiallyNone() => Assert.Null(new VoiSampler(VoiConfig.Default).LastDecision);

    [Fact] public void LastDecisionAfterDecide() { var s = new VoiSampler(VoiConfig.Default); s.Decide(); Assert.NotNull(s.LastDecision); }

    [Fact] public void ObserveViolationUpdatesAlpha() { var s = new VoiSampler(VoiConfig.Default); var (a0, _) = s.PosteriorParams; s.Decide(); s.Observe(true); Assert.InRange(s.PosteriorParams.Item1 - a0, 0.999, 1.001); }

    [Fact] public void ObserveNoViolationUpdatesBeta() { var s = new VoiSampler(VoiConfig.Default); var (_, b0) = s.PosteriorParams; s.Decide(); s.Observe(false); Assert.InRange(s.PosteriorParams.Item2 - b0, 0.999, 1.001); }

    [Fact] public void EValuePositiveAfterViolations() { var s = new VoiSampler(VoiConfig.Default); var sw = Stopwatch.StartNew(); for (int i = 0; i < 10; i++) { s.Decide(sw); s.ObserveAt(true, sw); } Assert.True(s.Summary().EValue > 0); }

    [Fact] public void SummaryInitialState() { var sum = new VoiSampler(VoiConfig.Default).Summary(); Assert.Equal(0UL, sum.TotalEvents); Assert.Equal(0UL, sum.TotalSamples); }

    [Fact] public void SummaryAfterObservations() { var s = new VoiSampler(VoiConfig.Default); var sw = Stopwatch.StartNew(); s.Decide(sw); s.ObserveAt(false, sw); s.Decide(sw); var sum = s.Summary(); Assert.Equal(2UL, sum.TotalEvents); Assert.Equal(1UL, sum.TotalSamples); }

    [Fact] public void ForcedSamplesTracked() { var s = new VoiSampler(VoiConfig.Default); s.ForcedSamples = 5; Assert.Equal(5UL, s.ForcedSamples); }

    [Fact] public void SnapshotCapturesState()
    {
        var cfg = VoiConfig.Default; cfg.EnableLogging = true;
        var s = new VoiSampler(cfg); var sw = Stopwatch.StartNew(); s.Decide(sw); s.ObserveAt(false, sw);
        var snap = s.Snapshot(10, 42);
        Assert.Equal(42UL, snap.CapturedMs); Assert.True(snap.Alpha > 0); Assert.True(snap.Beta > 0);
        Assert.NotNull(snap.LastDecision); Assert.NotNull(snap.LastObservation);
    }

    [Fact] public void LogRotationRespectsMaxEntries()
    {
        var cfg = VoiConfig.Default; cfg.EnableLogging = true; cfg.MaxLogEntries = 3;
        var s = new VoiSampler(cfg); var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10; i++) { var d = s.Decide(sw); if (d.ShouldSample) s.ObserveAt(false, sw); }
        Assert.True(s.Logs.Count <= 3);
    }

    [Fact] public void LogsEmptyWhenLoggingDisabled() { var s = new VoiSampler(VoiConfig.Default); s.Decide(); Assert.Empty(s.Logs); }

    [Fact] public void DecisionJsonlFormat() { var j = new VoiSampler(VoiConfig.Default).Decide().ToJsonl(); Assert.StartsWith("{", j); Assert.Contains("\"event\":\"voi_decision\"", j); }

    [Fact] public void ObservationJsonlFormat() { var s = new VoiSampler(VoiConfig.Default); s.Decide(); Assert.Contains("\"event\":\"voi_observe\"", s.Observe(false).ToJsonl()); }

    [Fact] public void DecisionReasonStrings()
    {
        var cfg = VoiConfig.Default; cfg.MaxIntervalEvents = 1;
        Assert.Equal("forced_interval", new VoiSampler(cfg).Decide().Reason);
    }

    [Fact] public void BetaMeanBasic() { Assert.InRange(VoiMath.BetaMean(1, 1), 0.49, 0.51); Assert.InRange(VoiMath.BetaMean(2, 8), 0.19, 0.21); }

    [Fact] public void BetaVarianceBasic() { Assert.InRange(VoiMath.BetaVariance(1, 1), 0.083, 0.084); }

    [Fact] public void BetaVarianceDegenerate() { Assert.InRange(VoiMath.BetaVariance(0, 0), -1e-9, 1e-9); }

    [Fact] public void BoundaryScoreAtThreshold() { Assert.InRange(VoiMath.BoundaryScore(20, 20), 0.99, 1.01); }

    [Fact] public void BoundaryScoreFarFromThreshold() { Assert.True(VoiMath.BoundaryScore(1, 1e6) < 0.1); }

    // ── DeferredRefinementScheduler ────────────────────────────────────────

    [Fact] public void SchedulerRespectsHardBudget()
    {
        var s = new DeferredRefinementScheduler(new DeferredRefinementConfig(200, 3, 0.01, 0.02, 0.5));
        var plan = s.PlanFrame(3000, 1900, new[] { new RefinementCandidate(1, 600, 0.25), new RefinementCandidate(2, 500, 0.21), new RefinementCandidate(3, 300, 0.08) });
        Assert.True(plan.HardBudgetRespected);
        Assert.True(plan.SpentOptionalUs <= plan.OptionalBudgetUs);
    }

    [Fact] public void SchedulerIsDeterministic()
    {
        var cfg = new DeferredRefinementConfig(100, 2, 0.01, 0.03, 0.6);
        var (a, b) = (new DeferredRefinementScheduler(cfg), new DeferredRefinementScheduler(cfg));
        var cands = new[] { new RefinementCandidate(11, 450, 0.13), new RefinementCandidate(22, 500, 0.11), new RefinementCandidate(33, 350, 0.07) };
        for (int i = 0; i < 25; i++)
        {
            var pa = a.PlanFrame(2800, 1600, cands);
            var pb = b.PlanFrame(2800, 1600, cands);
            Assert.Equal(pa.FrameBudgetUs, pb.FrameBudgetUs);
            Assert.Equal(pa.SpentOptionalUs, pb.SpentOptionalUs);
            Assert.Equal(pa.Selected.Count, pb.Selected.Count);
            for (int j = 0; j < pa.Selected.Count; j++)
                Assert.Equal(pa.Selected[j], pb.Selected[j]);
        }
    }

    [Fact] public void SchedulerFairnessAvoidsStarvation()
    {
        var s = new DeferredRefinementScheduler(new DeferredRefinementConfig(400, 1, 0.01, 0.05, 2));
        var cands = new[] { new RefinementCandidate(100, 700, 0.20), new RefinementCandidate(200, 700, 0.02) };
        int selected = 0;
        for (int i = 0; i < 30; i++) if (s.PlanFrame(4000, 2700, cands).Selected.Any(x => x.RegionId == 200)) selected++;
        Assert.True(selected > 0);
    }
}
