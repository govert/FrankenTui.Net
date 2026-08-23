// Tests ported from:
//   .external/frankentui/crates/ftui-runtime/src/evidence_bridges.rs
//   .external/frankentui/crates/ftui-runtime/src/diff_evidence.rs
//   .external/frankentui/crates/ftui-runtime/src/policy_config.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

using FrankenTui.Render;
using FrankenTui.Runtime;
using EvidenceEntry = FrankenTui.Runtime.EvidenceEntry;

namespace FrankenTui.Tests.Headless;

public sealed class EvidenceBridgeParityTests
{
    [Fact]
    public void DiffStrategyBridgeUsesPinnedActionAndEvidenceLabels()
    {
        var source = new StrategyEvidence
        {
            Strategy = DiffStrategy.DirtyRows,
            CostFull = 4.0,
            CostDirty = 1.0,
            CostRedraw = 6.0,
            PosteriorMean = 0.25,
            PosteriorVariance = 0.01,
            DirtyRows = 2,
            TotalRows = 10,
            HysteresisApplied = true,
        };

        EvidenceEntry entry = EvidenceBridges.FromDiffStrategy(source, 123);
        Dictionary<string, double> terms = Terms(entry);

        Assert.Equal(DecisionDomain.DiffStrategy, entry.Domain);
        Assert.Equal(123UL, entry.TimestampNs);
        Assert.Equal("dirty_rows", entry.Action);
        Assert.Equal(Math.Log(0.25 / 0.75), entry.LogPosterior, 12);
        Assert.Equal(3.0, entry.LossAvoided, 12);
        Assert.Equal(0.054, entry.ConfidenceInterval.Lower, 12);
        Assert.Equal(0.446, entry.ConfidenceInterval.Upper, 12);
        Assert.Equal(5.0, terms["change_rate"], 12);
        Assert.Equal(2.0, terms["dirty_ratio"], 12);
        Assert.Equal(0.8, terms["hysteresis"], 12);
    }

    [Fact]
    public void DiffStrategyBridgeRejectsTargetOnlyStrategyWithoutInventingLabel()
    {
        var source = new StrategyEvidence { Strategy = DiffStrategy.SignificantDirtyRows };

        NotSupportedException error = Assert.Throws<NotSupportedException>(
            () => EvidenceBridges.FromDiffStrategy(source, 0));

        Assert.Contains("no upstream evidence action", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EProcessBridgePreservesForcedActionAndEvidenceLabels()
    {
        var source = new ThrottleDecision(
            ShouldRecompute: true,
            Wealth: 4.0,
            Lambda: 0.2,
            EmpiricalRate: 0.3,
            ForcedByDeadline: true,
            ObservationsSinceRecompute: 9);

        EvidenceEntry entry = EvidenceBridges.FromEProcess(source, 456);
        Dictionary<string, double> terms = Terms(entry);

        Assert.Equal(DecisionDomain.FrameBudget, entry.Domain);
        Assert.Equal("recompute_forced", entry.Action);
        Assert.Equal(Math.Log(4.0), entry.LogPosterior, 12);
        Assert.Equal(Math.Log(4.0), entry.LossAvoided, 12);
        Assert.Equal(0.3, entry.ConfidenceInterval.Lower, 12);
        Assert.Equal(0.4, entry.ConfidenceInterval.Upper, 12);
        Assert.Equal(4.0, terms["wealth"], 12);
        Assert.Equal(1.2, terms["lambda"], 12);
        Assert.Equal(2.5, terms["empirical_rate"], 12);
    }

    [Fact]
    public void VoiConformalAndBocpdBridgesPreserveDomainLabels()
    {
        EvidenceEntry voi = EvidenceBridges.FromVoi(new VoiDecision
        {
            Reason = "sample_voi",
            VoiGain = 0.7,
            Score = 0.2,
            PosteriorMean = 0.4,
            PosteriorVariance = 0.0025,
            EValue = 5.0,
            BoundaryScore = 0.4,
        }, 1);
        Assert.Equal(DecisionDomain.VoiSampling, voi.Domain);
        Assert.Equal("sample_voi", voi.Action);
        Assert.Equal(new[] { "boundary_score", "e_value", "voi_score" },
            Terms(voi).Keys.Order(StringComparer.Ordinal));

        EvidenceEntry conformal = EvidenceBridges.FromConformal(new ConformalPrediction
        {
            UpperUs = 20.0,
            BudgetUs = 10.0,
            Confidence = 0.9,
            Risk = true,
            Quantile = 100.0,
            SampleCount = 100,
        }, 2);
        Assert.Equal(DecisionDomain.Degradation, conformal.Domain);
        Assert.Equal("degrade", conformal.Action);
        Assert.Equal(Math.Log(2.0), conformal.LogPosterior, 12);
        Assert.Equal(1.0, conformal.LossAvoided, 12);
        Assert.Equal(new[] { "budget_headroom", "quantile", "sample_strength" },
            Terms(conformal).Keys.Order(StringComparer.Ordinal));

        EvidenceEntry bocpd = EvidenceBridges.FromBocpd(new BocpdEvidence
        {
            Regime = BocpdRegime.Burst,
            PBurst = 0.8,
            LogBayesFactor = 3.0,
            LikelihoodSteady = 0.2,
            LikelihoodBurst = 0.8,
            ExpectedRunLength = 10.0,
            RunLengthVariance = 4.0,
            RunLengthTailMass = 0.1,
        }, 3);
        Assert.Equal(DecisionDomain.ResizeCoalescing, bocpd.Domain);
        Assert.Equal("coalesce", bocpd.Action);
        Assert.Equal(0.3, bocpd.LossAvoided, 12);
        Assert.Equal(new[] { "burst_prob", "likelihood_ratio", "tail_mass" },
            Terms(bocpd).Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void PolicyConfigBuildsEveryAvailableManagedControllerConfig()
    {
        var policy = new PolicyConfig
        {
            Conformal = new ConformalPolicyConfig
            {
                Alpha = 0.11,
                MinSamples = 31,
                WindowSize = 401,
                QDefault = 12_345.0,
            },
            FrameGuard = new FrameGuardPolicyConfig
            {
                FallbackBudgetUs = 1_234.0,
                TimeSeriesWindow = 44,
                NonconformityWindow = 55,
            },
            Cascade = new CascadePolicyConfig
            {
                RecoveryThreshold = 17,
                MaxDegradation = DegradationLevel.SkipFrame,
                MinTriggerLevel = DegradationLevel.NoStyling,
                DegradationFloor = DegradationLevel.SimpleBorders,
            },
            Pid = new PidPolicyConfig { Kp = 1.1, Ki = 1.2, Kd = 1.3, IntegralMax = 1.4 },
            EProcessBudget = new EProcessBudgetPolicyConfig
            {
                Lambda = 2.1,
                Alpha = 0.12,
                Beta = 2.3,
                SigmaEmaDecay = 0.87,
                SigmaFloorMs = 2.5,
                WarmupFrames = 26,
            },
            Bocpd = new BocpdPolicyConfig
            {
                MuSteadyMs = 3.1,
                MuBurstMs = 3.2,
                HazardLambda = 3.3,
                MaxRunLength = 34,
                SteadyThreshold = 0.35,
                BurstThreshold = 0.36,
                BurstPrior = 0.37,
                MinObservationMs = 3.8,
                MaxObservationMs = 3.9,
                EnableLogging = true,
            },
            EProcessThrottle = new EProcessThrottlePolicyConfig
            {
                Alpha = 0.14,
                Mu0 = 4.2,
                InitialLambda = 4.3,
                GrapaEta = 4.4,
                HardDeadlineMs = 45,
                MinObservationsBetween = 46,
                RateWindowSize = 47,
                EnableLogging = true,
            },
            Voi = new VoiPolicyConfig
            {
                Alpha = 0.15,
                PriorAlpha = 5.2,
                PriorBeta = 5.3,
                Mu0 = 5.4,
                Lambda = 5.5,
                ValueScale = 5.6,
                BoundaryWeight = 5.7,
                SampleCost = 5.8,
                MinIntervalMs = 59,
                MaxIntervalMs = 60,
                MinIntervalEvents = 61,
                MaxIntervalEvents = 62,
                EnableLogging = true,
                MaxLogEntries = 63,
            },
            Evidence = new EvidencePolicyConfig
            {
                LedgerCapacity = 64,
                SinkEnabled = true,
                SinkFile = "evidence.jsonl",
                FlushOnWrite = false,
            },
        };

        ConformalConfig conformal = policy.ToConformalConfig();
        Assert.Equal(0.11, conformal.Alpha);
        Assert.Equal(31, conformal.MinSamples);
        Assert.Equal(401, conformal.WindowSize);
        Assert.Equal(12_345.0, conformal.QDefault);

        ConformalFrameGuardConfig guard = policy.ToFrameGuardConfig();
        Assert.Equal(1_234.0, guard.FallbackBudget.TotalMicroseconds);
        Assert.Equal(44, guard.TimeSeriesWindow);
        Assert.Equal(55, guard.NonconformityWindow);
        Assert.Equal(12_345.0, guard.EffectiveConformal.DefaultResidualMicroseconds);

        DegradationCascadeConfig cascade = policy.ToCascadeConfig();
        Assert.Equal(17, cascade.RecoveryThreshold);
        Assert.Equal(RuntimeDegradationLevel.SkipFrame, cascade.MaxDegradation);
        Assert.Equal(RuntimeDegradationLevel.NoStyling, cascade.MinTriggerLevel);
        Assert.Equal(RuntimeDegradationLevel.SimpleBorders, cascade.DegradationFloor);

        var pid = policy.ToPidGains();
        Assert.Equal((1.1, 1.2, 1.3, 1.4), (pid.Kp, pid.Ki, pid.Kd, pid.IntegralMax));

        var budget = policy.ToEProcessBudgetConfig();
        Assert.Equal((2.1, 0.12, 2.3, 0.87, 2.5, 26U),
            (budget.Lambda, budget.Alpha, budget.Beta, budget.SigmaEmaDecay,
                budget.SigmaFloorMs, budget.WarmupFrames));

        BocpdConfig bocpd = policy.ToBocpdConfig();
        Assert.Equal(3.1, bocpd.MuSteadyMs);
        Assert.Equal(3.9, bocpd.MaxObservationMs);
        Assert.True(bocpd.EnableLogging);

        ThrottleConfig throttle = policy.ToThrottleConfig();
        Assert.Equal(4.4, throttle.GrapaEta);
        Assert.Equal(47, throttle.RateWindowSize);
        Assert.True(throttle.EnableLogging);

        VoiConfig voi = policy.ToVoiConfig();
        Assert.Equal(5.8, voi.SampleCost);
        Assert.Equal(63, voi.MaxLogEntries);
        Assert.True(voi.EnableLogging);

        EvidenceSinkConfig sink = policy.ToEvidenceSinkConfig();
        Assert.True(sink.Enabled);
        Assert.Equal(EvidenceSinkDestination.File, sink.Destination);
        Assert.Equal("evidence.jsonl", sink.FilePath);
        Assert.False(sink.FlushOnWrite);
        Assert.Equal(50UL * 1024 * 1024, sink.MaxBytes);
    }

    [Fact]
    public void RuntimeDiffLedgerWrapsAndReportsRegimeTransitionOldestFirst()
    {
        var ledger = new RuntimeDiffEvidenceLedger(3);
        ledger.Record(MakeRecord(0, DiffRegime.StableFrame));
        ledger.Record(MakeRecord(1, DiffRegime.StableFrame));
        ledger.Record(MakeRecord(2, DiffRegime.StableFrame));
        ledger.Record(MakeRecord(3, DiffRegime.StableFrame));
        ledger.Record(MakeRecord(4, DiffRegime.BurstyChange));

        Assert.Equal(3, ledger.Count);
        Assert.Equal(new ulong[] { 2, 3, 4 }, ledger.Decisions().Select(item => item.FrameId));
        Assert.Equal(DiffRegime.BurstyChange, ledger.CurrentRegime);
        RegimeTransition transition = Assert.Single(ledger.Transitions());
        Assert.Equal((4UL, DiffRegime.StableFrame, DiffRegime.BurstyChange),
            (transition.FrameId, transition.FromRegime, transition.ToRegime));

        string jsonl = ledger.ExportJsonl();
        Assert.Contains("\"type\":\"diff_decision\"", jsonl, StringComparison.Ordinal);
        Assert.Contains("\"regime\":\"bursty_change\"", jsonl, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"regime_transition\"", jsonl, StringComparison.Ordinal);

        ledger.Clear();
        Assert.True(ledger.IsEmpty);
        Assert.Equal(DiffRegime.StableFrame, ledger.CurrentRegime);
    }

    private static Dictionary<string, double> Terms(EvidenceEntry entry) =>
        entry.TopEvidence
            .OfType<EvidenceTerm>()
            .ToDictionary(term => term.Label, term => term.BayesFactor, StringComparer.Ordinal);

    private static DiffStrategyRecord MakeRecord(ulong frameId, DiffRegime regime) => new(
        frameId,
        regime,
        new[]
        {
            (DiffStrategy.Full, 0.3),
            (DiffStrategy.DirtyRows, 0.6),
            (DiffStrategy.FullRedraw, 0.1),
        },
        DiffStrategy.DirtyRows,
        0.6,
        new StrategyEvidence
        {
            Strategy = DiffStrategy.DirtyRows,
            CostFull = 1.0,
            CostDirty = 0.5,
            CostRedraw = 2.0,
            PosteriorMean = 0.05,
            PosteriorVariance = 0.001,
            Alpha = 2.0,
            Beta = 38.0,
            DirtyRows = 3,
            TotalRows = 24,
            TotalCells = 1920,
        },
        false,
        new[]
        {
            new Observation("change_fraction", 0.05, 0.3),
            new Observation("dirty_rows", 3.0, 0.2),
        });
}
