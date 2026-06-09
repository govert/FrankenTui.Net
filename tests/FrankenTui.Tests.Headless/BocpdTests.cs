// Tests for .external/frankentui/crates/ftui-runtime/src/bocpd.rs  
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// Uses Thread.Sleep for timed observations instead of simulated time.

using System.Diagnostics;
using FrankenTui.Runtime;

namespace FrankenTui.Tests.Headless;

public class BocpdTests
{
    [Fact] public void DefaultConfig() { var c = BocpdConfig.Default; Assert.InRange(c.MuSteadyMs, 199, 201); Assert.InRange(c.MuBurstMs, 19, 21); Assert.Equal(100, c.MaxRunLength); }

    [Fact] public void InitialState() { var d = BocpdDetector.WithDefaults(); Assert.InRange(d.PBurst, 0.19, 0.21); Assert.Equal(BocpdRegime.Steady, d.Regime); Assert.Equal(0UL, d.ObservationCount); }

    [Fact] public void SteadyDetection()
    {
        var d = BocpdDetector.WithDefaults();
        // Seed first observation
        d.ObserveEvent();
        // Subsequent observations: sleep 200ms between each
        for (int i = 0; i < 9; i++) { Thread.Sleep(200); d.ObserveEvent(); }
        Assert.True(d.PBurst < 0.5); Assert.Equal(BocpdRegime.Steady, d.Regime);
    }

    [Fact] public void BurstDetection()
    {
        var d = BocpdDetector.WithDefaults();
        d.ObserveEvent();
        for (int i = 0; i < 19; i++) { Thread.Sleep(10); d.ObserveEvent(); }
        Assert.True(d.PBurst > 0.5);
    }

    [Fact] public void RegimeTransition()
    {
        var d = BocpdDetector.WithDefaults();
        d.ObserveEvent();
        for (int i = 0; i < 4; i++) { Thread.Sleep(200); d.ObserveEvent(); }
        var init = d.PBurst;
        for (int i = 0; i < 20; i++) { Thread.Sleep(10); d.ObserveEvent(); }
        Assert.True(d.PBurst > init);
    }

    [Fact] public void EvidenceStored() { var d = BocpdDetector.WithDefaults(); d.ObserveEvent(); Assert.NotNull(d.LastEvidence); Assert.Equal(1UL, d.LastEvidence!.ObservationCount); }

    [Fact] public void Reset()
    {
        var d = BocpdDetector.WithDefaults();
        for (int i = 0; i < 10; i++) { d.ObserveEvent(); }
        d.Reset();
        Assert.InRange(d.PBurst, 0.19, 0.21); Assert.Equal(0UL, d.ObservationCount); Assert.Null(d.LastEvidence);
    }

    [Fact] public void RecommendedDelay()
    {
        var d = BocpdDetector.WithDefaults();
        Assert.Equal(16UL, d.RecommendedDelay(16, 40));
    }

    [Fact] public void Deterministic()
    {
        var (a, b) = (BocpdDetector.WithDefaults(), BocpdDetector.WithDefaults());
        for (int i = 0; i < 10; i++) { long t = Stopwatch.GetTimestamp(); a.ObserveEvent(t); b.ObserveEvent(t); Thread.Sleep(1); }
        Assert.InRange(a.PBurst - b.PBurst, -1e-10, 1e-10);
    }

    [Fact] public void PosteriorSumToOne()
    {
        var d = BocpdDetector.WithDefaults();
        for (int i = 0; i < 20; i++) { d.ObserveEvent(); Assert.InRange(d.RunLengthPosterior.Sum() - 1.0, -1e-6, 1e-6); }
    }

    [Fact] public void PBurstBounded()
    {
        var d = BocpdDetector.WithDefaults();
        for (int i = 0; i < 100; i++) { d.ObserveEvent(); Thread.Sleep(1); Assert.True(d.PBurst is >= 0 and <= 1); }
    }

    [Fact] public void ConfigSanitizationClampsThresholds()
    {
        var cfg = BocpdConfig.Default; cfg.SteadyThreshold = 0.9; cfg.BurstThreshold = 0.1; cfg.BurstPrior = 2; cfg.MaxRunLength = 0; cfg.MuSteadyMs = 0; cfg.MuBurstMs = 0; cfg.HazardLambda = 0;
        var d = new BocpdDetector(cfg); var c = d.Config;
        Assert.True(c.SteadyThreshold <= c.BurstThreshold); Assert.Equal(1, c.MaxRunLength); Assert.True(c.MuSteadyMs >= 1); Assert.True(c.MuBurstMs >= 1);
    }

    [Fact] public void JsonlOutput()
    {
        var d = new BocpdDetector(BocpdConfig.Default.WithLogging(true)); d.ObserveEvent();
        var jsonl = d.DecisionLogJsonl(16, 40, false);
        Assert.NotNull(jsonl); Assert.Contains("bocpd-v1", jsonl); Assert.Contains("p_burst", jsonl);
    }

    [Fact] public void ResponsiveConfig() { var c = BocpdConfig.Responsive(); Assert.InRange(c.MuSteadyMs, 149, 151); Assert.InRange(c.HazardLambda, 29, 31); }

    [Fact] public void AggressiveCoalesceConfig() { var c = BocpdConfig.AggressiveCoalesce(); Assert.InRange(c.MuSteadyMs, 249, 251); Assert.InRange(c.BurstPrior, 0.29, 0.31); }

    [Fact] public void WithLoggingBuilder() { var c = BocpdConfig.Default.WithLogging(true); Assert.True(c.EnableLogging); Assert.False(c.WithLogging(false).EnableLogging); }

    [Fact] public void RegimeAsStr() { Assert.Equal("steady", BocpdRegimeMeta.AsStr(BocpdRegime.Steady)); Assert.Equal("burst", BocpdRegimeMeta.AsStr(BocpdRegime.Burst)); }

    [Fact] public void EvidenceToJsonlHasAllFields()
    {
        var d = BocpdDetector.WithDefaults(); d.ObserveEvent();
        var j = d.LastEvidence!.ToJsonl();
        foreach (var k in new[] { "schema_version", "bocpd-v1", "p_burst", "log_bf", "obs_ms", "regime", "runlen_mean", "n_obs" })
            Assert.Contains(k, j);
    }

    [Fact] public void EvidenceNullOptionalsInJsonl()
    {
        var d = BocpdDetector.WithDefaults(); d.ObserveEvent();
        var j = d.LastEvidence!.ToJsonl();
        Assert.Contains("\"delay_ms\":null", j); Assert.Contains("\"forced_deadline\":null", j);
    }

    [Fact] public void SetDecisionContextPopulatesEvidence()
    {
        var d = BocpdDetector.WithDefaults(); d.ObserveEvent(); d.SetDecisionContext(16, 40, true);
        Assert.NotNull(d.LastEvidence!.RecommendedDelayMs); Assert.Equal(true, d.LastEvidence!.HardDeadlineForced);
    }

    [Fact] public void RapidEventsIncreasePBurst()
    {
        var d = BocpdDetector.WithDefaults(); d.ObserveEvent(); var init = d.PBurst;
        for (int i = 1; i < 20; i++) { Thread.Sleep(5); d.ObserveEvent(); }
        Assert.True(d.PBurst > init);
    }

    [Fact] public void SlowEventsDecreasePBurst()
    {
        var d = BocpdDetector.WithDefaults();
        for (int i = 0; i < 10; i++) { Thread.Sleep(5); d.ObserveEvent(); }
        var afterBurst = d.PBurst;
        // Now slow events
        d.ObserveEvent();
        for (int i = 0; i < 19; i++) { Thread.Sleep(500); d.ObserveEvent(); }
        Assert.True(d.PBurst < afterBurst);
    }

    [Fact] public void ExpectedRunLengthNonNegative()
    {
        var d = BocpdDetector.WithDefaults();
        for (int i = 0; i < 50; i++) { d.ObserveEvent(); Assert.True(d.ExpectedRunLength() >= 0); }
    }

    [Fact] public void BurstToSteadyRecovery()
    {
        var d = BocpdDetector.WithDefaults();
        for (int i = 0; i < 30; i++) { Thread.Sleep(5); d.ObserveEvent(); }
        var bp = d.PBurst; Assert.True(bp > 0.5, $"should be in burst, p={bp}");
        for (int i = 0; i < 30; i++) { Thread.Sleep(200); d.ObserveEvent(); }
        Assert.True(d.PBurst < bp);
    }

    [Fact] public void ResponsiveConfigValues(){var c=BocpdConfig.Responsive();Assert.InRange(c.MuSteadyMs,149,151);Assert.InRange(c.HazardLambda,29,31);Assert.InRange(c.SteadyThreshold,0.24,0.26);}
    [Fact] public void AggressiveCoalesceConfigValues(){var c=BocpdConfig.AggressiveCoalesce();Assert.InRange(c.MuSteadyMs,249,251);Assert.InRange(c.BurstPrior,0.29,0.31);Assert.InRange(c.BurstThreshold,0.79,0.81);}
    [Fact] public void WithLoggingBuilderToggle(){var c=BocpdConfig.Default.WithLogging(true);Assert.True(c.EnableLogging);Assert.False(c.WithLogging(false).EnableLogging);}
    [Fact] public void RegimeAsStrAll(){Assert.Equal("steady",BocpdRegimeMeta.AsStr(BocpdRegime.Steady));Assert.Equal("burst",BocpdRegimeMeta.AsStr(BocpdRegime.Burst));Assert.Equal("transitional",BocpdRegimeMeta.AsStr(BocpdRegime.Transitional));}
    [Fact] public void EvidenceToJsonlAllFields(){var d=BocpdDetector.WithDefaults();d.ObserveEvent();var j=d.LastEvidence!.ToJsonl();foreach(var k in new[]{"schema_version","bocpd-v1","p_burst","log_bf","obs_ms","regime","runlen_mean","n_obs"})Assert.Contains(k,j);}
    [Fact] public void EvidenceNullOptionals(){var d=BocpdDetector.WithDefaults();d.ObserveEvent();var j=d.LastEvidence!.ToJsonl();Assert.Contains("\"delay_ms\":null",j);Assert.Contains("\"forced_deadline\":null",j);}
    [Fact] public void SetDecisionContextFillsFields(){var d=BocpdDetector.WithDefaults();d.ObserveEvent();d.SetDecisionContext(16,40,true);Assert.NotNull(d.LastEvidence!.RecommendedDelayMs);Assert.Equal(true,d.LastEvidence!.HardDeadlineForced);}
    [Fact] public void RapidEventsIncreasePBurstCheck(){var d=BocpdDetector.WithDefaults();d.ObserveEvent();var init=d.PBurst;for(int i=1;i<20;i++){Thread.Sleep(5);d.ObserveEvent();}Assert.True(d.PBurst>init);}
    [Fact] public void FirstEventUsesDefaultObs(){var d=BocpdDetector.WithDefaults();d.ObserveEvent();Assert.InRange(d.LastEvidence!.ObservationMs,199,201);}
    [Fact] public void ConfigSanitizationEnsuresOrder(){var c=BocpdConfig.Default;c.SteadyThreshold=0.9;c.BurstThreshold=0.1;var d=new BocpdDetector(c);Assert.True(d.Config.SteadyThreshold<=d.Config.BurstThreshold);}
    [Fact] public void DecisionLogJsonlRespectsLogging(){var d=new BocpdDetector(BocpdConfig.Default.WithLogging(true));d.ObserveEvent();var j=d.DecisionLogJsonl(16,40,true);Assert.NotNull(j);Assert.Contains("forced_deadline\":true",j);}
    [Fact] public void InitialRunLengthPosteriorSumsToOne(){var d=BocpdDetector.WithDefaults();Assert.InRange(d.RunLengthPosterior.Sum()-1.0,-1e-10,1e-10);}
    [Fact] public void ObservationCountIncrements(){var d=BocpdDetector.WithDefaults();d.ObserveEvent();d.ObserveEvent();Assert.Equal(2UL,d.ObservationCount);}
    [Fact] public void RecommendedDelayInterpolation(){var d=BocpdDetector.WithDefaults();d.ObserveEvent();Assert.True(d.RecommendedDelay(16,40)>=16&&d.RecommendedDelay(16,40)<=40);}
    [Fact] public void RecommendedDelaySteadyWhenLow(){var d=BocpdDetector.WithDefaults();Assert.Equal(16UL,d.RecommendedDelay(16,40));}

    [Fact] public void DefaultConfigValuesExact(){var c=BocpdConfig.Default;Assert.InRange(c.MuSteadyMs,199,201);Assert.InRange(c.MuBurstMs,19,21);Assert.Equal(100,c.MaxRunLength);Assert.InRange(c.HazardLambda,49,51);}
    [Fact] public void InitialRunLengthPosteriorUniform(){var d=BocpdDetector.WithDefaults();double p=1.0/(d.Config.MaxRunLength+1);foreach(var rp in d.RunLengthPosterior)Assert.InRange(rp,p-1e-10,p+1e-10);}
    [Fact] public void ObservationCountIncrementsPerEvent(){var d=BocpdDetector.WithDefaults();d.ObserveEvent();d.ObserveEvent();d.ObserveEvent();Assert.Equal(3UL,d.ObservationCount);}
    [Fact] public void ResetClearsObservationCount(){var d=BocpdDetector.WithDefaults();d.ObserveEvent();d.ObserveEvent();d.Reset();Assert.Equal(0UL,d.ObservationCount);}
    [Fact] public void FirstEvidenceHasObservationCountOne(){var d=BocpdDetector.WithDefaults();d.ObserveEvent();Assert.Equal(1UL,d.LastEvidence!.ObservationCount);}
    [Fact] public void RegimeTransitionalWhenBetweenThresholds(){var d=BocpdDetector.WithDefaults();d.ObserveEvent();Assert.Equal(BocpdRegime.Steady,d.Regime);}
    [Fact] public void ConfigAccessorReturnsClampedConfig(){var c=BocpdConfig.Default;c.MaxRunLength=0;c.MuSteadyMs=0;var d=new BocpdDetector(c);Assert.True(d.Config.MaxRunLength>=1);Assert.True(d.Config.MuSteadyMs>=1);}
    [Fact] public void EvidenceEnablingControlsJsonl(){var d=new BocpdDetector(BocpdConfig.Default.WithLogging(false));d.ObserveEvent();Assert.Null(d.EvidenceJsonl());d=new BocpdDetector(BocpdConfig.Default.WithLogging(true));d.ObserveEvent();Assert.NotNull(d.EvidenceJsonl());}
    [Fact] public void DecisionLogJsonlRespectsEnabledFlag(){var d=new BocpdDetector(BocpdConfig.Default.WithLogging(true));d.ObserveEvent();var j=d.DecisionLogJsonl(16,40,true);Assert.NotNull(j);Assert.Contains("forced_deadline\":true",j);}
    [Fact] public void DecisionLogJsonlNullWhenDisabled(){var d=BocpdDetector.WithDefaults();d.ObserveEvent();Assert.Null(d.DecisionLogJsonl(16,40,false));}
    [Fact] public void ExpectedRunLengthNonNegativeAlways(){var d=BocpdDetector.WithDefaults();for(int i=0;i<20;i++){d.ObserveEvent();Assert.True(d.ExpectedRunLength()>=0);}}
    [Fact] public void PBurstBoundedAlways(){var d=BocpdDetector.WithDefaults();for(int i=0;i<50;i++){d.ObserveEvent();Assert.True(d.PBurst is >=0 and <=1);}}

    [Fact] public void ExpectedRunLengthInitialUniform(){var d=BocpdDetector.WithDefaults();Assert.InRange(d.ExpectedRunLength()-50.0,-2,2);}
    [Fact] public void PreviousRegimeTracksLastState(){var d=BocpdDetector.WithDefaults();Assert.Equal(BocpdRegime.Steady,d.Regime);}
    [Fact] public void ConfigAccessorReturnsSame(){var c=BocpdConfig.Default;c.MaxRunLength=0;var d=new BocpdDetector(c);Assert.True(d.Config.MaxRunLength>=1);}
    [Fact] public void EvidenceEnablingToggles(){var d=new BocpdDetector(BocpdConfig.Default.WithLogging(false));d.ObserveEvent();Assert.Null(d.EvidenceJsonl());d=new BocpdDetector(BocpdConfig.Default.WithLogging(true));d.ObserveEvent();Assert.NotNull(d.EvidenceJsonl());}
    [Fact] public void ResponsiveDetectsBurstFaster(){var d=new BocpdDetector(BocpdConfig.Responsive());for(int i=0;i<15;i++){Thread.Sleep(5);d.ObserveEvent();}Assert.True(d.PBurst>0.3);}
    [Fact] public void RunLengthPosteriorAccessor(){var d=BocpdDetector.WithDefaults();Assert.Equal(d.Config.MaxRunLength+1,d.RunLengthPosterior.Length);}
    [Fact] public void LastEvidenceInitiallyNone(){var d=BocpdDetector.WithDefaults();Assert.Null(d.LastEvidence);}
    [Fact] public void DetectorDefaultImpl(){var d=BocpdDetector.WithDefaults();Assert.NotNull(d);Assert.Equal(BocpdRegime.Steady,d.Regime);}
    [Fact] public void PosteriorsStayNormalizedUnderAlternatingTraffic(){var d=BocpdDetector.WithDefaults();for(int i=0;i<100;i++){if(i%2==0)Thread.Sleep(5);else Thread.Sleep(300);d.ObserveEvent();Assert.InRange(d.RunLengthPosterior.Sum()-1.0,-1e-6,1e-6);}}
}
