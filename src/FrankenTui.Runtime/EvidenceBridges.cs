// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/evidence_bridges.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c

using FrankenTui.Render;

namespace FrankenTui.Runtime;

/// <summary>
/// Converts domain-specific decisions into the common evidence-ledger schema.
/// Labels and numerical transforms intentionally mirror the Rust bridges.
/// </summary>
public static class EvidenceBridges
{
    public static EvidenceEntry FromDiffStrategy(StrategyEvidence evidence, ulong timestampNs)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        string action = evidence.Strategy switch
        {
            DiffStrategy.Full => "full",
            DiffStrategy.DirtyRows => "dirty_rows",
            DiffStrategy.FullRedraw => "full_redraw",
            // DIVERGENCE: SignificantDirtyRows is not present in upstream at the pinned basis.
            DiffStrategy.SignificantDirtyRows => throw new NotSupportedException(
                "DiffStrategy.SignificantDirtyRows has no upstream evidence action at 15cc6543."),
            _ => throw new ArgumentOutOfRangeException(nameof(evidence), evidence.Strategy, null),
        };

        double chosenCost = evidence.Strategy switch
        {
            DiffStrategy.Full => evidence.CostFull,
            DiffStrategy.DirtyRows => evidence.CostDirty,
            DiffStrategy.FullRedraw => evidence.CostRedraw,
            _ => throw new ArgumentOutOfRangeException(nameof(evidence), evidence.Strategy, null),
        };

        double minOtherCost = double.MaxValue;
        foreach (double cost in new[] { evidence.CostFull, evidence.CostDirty, evidence.CostRedraw })
        {
            if (Math.Abs(cost - chosenCost) > 1e-12)
                minOtherCost = Math.Min(minOtherCost, cost);
        }

        double lossAvoided = minOtherCost < double.MaxValue
            ? Math.Max(minOtherCost - chosenCost, 0.0)
            : 0.0;
        double p = Math.Clamp(evidence.PosteriorMean, 1e-6, 1.0 - 1e-6);
        double logPosterior = Math.Log(p / (1.0 - p));
        double standardDeviation = Math.Sqrt(evidence.PosteriorVariance);
        double lower = Math.Clamp(p - 1.96 * standardDeviation, 0.0, 1.0);
        double upper = Math.Clamp(p + 1.96 * standardDeviation, 0.0, 1.0);

        var builder = new EvidenceEntryBuilder(DecisionDomain.DiffStrategy, 0, timestampNs)
            .LogPosterior(logPosterior)
            .Action(action)
            .LossAvoided(lossAvoided)
            .ConfidenceInterval(lower, upper);

        if (evidence.PosteriorMean > 0.0)
            builder.Evidence("change_rate", evidence.PosteriorMean * 20.0);
        if (evidence.TotalRows > 0)
        {
            double dirtyRatio = (double)evidence.DirtyRows / evidence.TotalRows;
            builder.Evidence("dirty_ratio", 1.0 + dirtyRatio * 5.0);
        }
        if (evidence.HysteresisApplied)
            builder.Evidence("hysteresis", 0.8);

        return builder.Build();
    }

    public static EvidenceEntry FromEProcess(ThrottleDecision decision, ulong timestampNs)
    {
        string action = decision.ForcedByDeadline
            ? "recompute_forced"
            : decision.ShouldRecompute ? "recompute" : "hold";
        double logPosterior = Math.Log(Math.Max(decision.Wealth, 1e-12));

        var builder = new EvidenceEntryBuilder(DecisionDomain.FrameBudget, 0, timestampNs)
            .LogPosterior(logPosterior)
            .Action(action)
            .LossAvoided(decision.ShouldRecompute ? Math.Max(Math.Log(decision.Wealth), 0.0) : 0.0)
            .ConfidenceInterval(
                Math.Max(decision.EmpiricalRate, 0.0),
                Math.Min(decision.EmpiricalRate + 0.1, 1.0))
            .Evidence("wealth", decision.Wealth);

        if (Math.Abs(decision.Lambda) > 1e-12)
            builder.Evidence("lambda", Math.Max(1.0 + Math.Abs(decision.Lambda), 0.01));
        builder.Evidence("empirical_rate", 1.0 + decision.EmpiricalRate * 5.0);

        return builder.Build();
    }

    public static EvidenceEntry FromVoi(VoiDecision decision, ulong timestampNs)
    {
        ArgumentNullException.ThrowIfNull(decision);

        double p = Math.Clamp(decision.PosteriorMean, 1e-6, 1.0 - 1e-6);
        double logPosterior = Math.Log(p / (1.0 - p));
        double standardDeviation = Math.Sqrt(decision.PosteriorVariance);
        double lower = Math.Clamp(p - 1.96 * standardDeviation, 0.0, 1.0);
        double upper = Math.Clamp(p + 1.96 * standardDeviation, 0.0, 1.0);

        var builder = new EvidenceEntryBuilder(DecisionDomain.VoiSampling, 0, timestampNs)
            .LogPosterior(logPosterior)
            .Action(decision.Reason)
            .LossAvoided(decision.VoiGain)
            .ConfidenceInterval(lower, upper);

        if (decision.Score > 0.0)
            builder.Evidence("voi_score", 1.0 + decision.Score * 10.0);
        if (decision.EValue > 0.0)
            builder.Evidence("e_value", decision.EValue);
        if (decision.BoundaryScore > 0.0)
            builder.Evidence("boundary_score", 1.0 + decision.BoundaryScore * 3.0);

        return builder.Build();
    }

    public static EvidenceEntry FromConformal(ConformalPrediction prediction, ulong timestampNs)
    {
        ArgumentNullException.ThrowIfNull(prediction);

        string action = prediction.Risk ? "degrade" : "hold";
        double riskRatio = prediction.BudgetUs > 0.0
            ? prediction.UpperUs / prediction.BudgetUs
            : 1.0;
        double logPosterior = Math.Log(Math.Clamp(riskRatio, 0.01, 100.0));

        var builder = new EvidenceEntryBuilder(DecisionDomain.Degradation, 0, timestampNs)
            .LogPosterior(logPosterior)
            .Action(action)
            .LossAvoided(prediction.Risk
                ? Math.Max(prediction.UpperUs - prediction.BudgetUs, 0.0) /
                  Math.Max(prediction.BudgetUs, 1.0)
                : 0.0)
            .ConfidenceInterval(prediction.Confidence - 0.05, prediction.Confidence);

        if (prediction.BudgetUs > 0.0)
        {
            builder.Evidence(
                "budget_headroom",
                Math.Max(prediction.BudgetUs / Math.Max(prediction.UpperUs, 1.0), 0.01));
        }
        if (prediction.Quantile > 0.0)
            builder.Evidence("quantile", 1.0 + prediction.Quantile / 1000.0);
        if (prediction.SampleCount > 0)
            builder.Evidence("sample_strength", 1.0 + Math.Log(prediction.SampleCount) / 5.0);

        return builder.Build();
    }

    public static EvidenceEntry FromBocpd(BocpdEvidence evidence, ulong timestampNs)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        string action = evidence.Regime switch
        {
            BocpdRegime.Steady => "apply",
            BocpdRegime.Burst => "coalesce",
            BocpdRegime.Transitional => "placeholder",
            _ => throw new ArgumentOutOfRangeException(nameof(evidence), evidence.Regime, null),
        };

        double p = Math.Clamp(evidence.PBurst, 1e-6, 1.0 - 1e-6);
        double logPosterior = Math.Log(p / (1.0 - p));
        double runLengthStandardDeviation = Math.Sqrt(evidence.RunLengthVariance);
        double denominator = evidence.ExpectedRunLength + 1.96 * runLengthStandardDeviation + 1.0;
        double lower = Math.Clamp(
            (evidence.ExpectedRunLength - 1.96 * runLengthStandardDeviation) / denominator,
            0.0,
            1.0);
        double upper = Math.Clamp(
            (evidence.ExpectedRunLength + 1.96 * runLengthStandardDeviation) / denominator,
            0.0,
            1.0);

        var builder = new EvidenceEntryBuilder(DecisionDomain.ResizeCoalescing, 0, timestampNs)
            .LogPosterior(logPosterior)
            .Action(action)
            .LossAvoided(Math.Abs(evidence.LogBayesFactor) * 0.1)
            .ConfidenceInterval(lower, upper)
            .Evidence("burst_prob", evidence.PBurst / (1.0 - evidence.PBurst + 1e-12));

        if (evidence.LikelihoodSteady > 0.0)
        {
            builder.Evidence(
                "likelihood_ratio",
                evidence.LikelihoodBurst / Math.Max(evidence.LikelihoodSteady, 1e-12));
        }
        if (evidence.RunLengthTailMass > 0.0)
            builder.Evidence("tail_mass", 1.0 / (evidence.RunLengthTailMass + 0.01));

        return builder.Build();
    }
}
