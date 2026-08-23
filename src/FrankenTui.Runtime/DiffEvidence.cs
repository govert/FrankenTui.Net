// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/diff_evidence.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: DiffRegime is shared from FrankenTui.Render rather than duplicated.
// DIVERGENCE: DiffEvidenceLedger is named RuntimeDiffEvidenceLedger because the
// managed render layer already exposes a distinct DiffEvidenceLedger type.

using System.Globalization;
using System.Text;
using FrankenTui.Render;

namespace FrankenTui.Runtime;

/// <summary>An observation that contributed to a diff strategy decision.</summary>
public sealed record Observation(string MetricName, double Value, double PriorContribution);

/// <summary>A complete, attributable diff-strategy decision record.</summary>
public sealed record DiffStrategyRecord(
    ulong FrameId,
    DiffRegime Regime,
    IReadOnlyList<(DiffStrategy Strategy, double Probability)> Posterior,
    DiffStrategy ChosenStrategy,
    double Confidence,
    StrategyEvidence Evidence,
    bool FallbackTriggered,
    IReadOnlyList<Observation> Observations)
{
    public string ToJsonl()
    {
        var output = new StringBuilder(512);
        output.Append("{\"type\":\"diff_decision\"");
        output.Append(CultureInfo.InvariantCulture, $",\"frame\":{FrameId}");
        output.Append(",\"regime\":\"").Append(DiffRegimeNames.AsStr(Regime)).Append('"');
        output.Append(",\"strategy\":\"").Append(ChosenStrategy).Append('"');
        output.Append(CultureInfo.InvariantCulture, $",\"confidence\":{Confidence:F6}");
        output.Append(",\"fallback\":").Append(FallbackTriggered ? "true" : "false");
        output.Append(
            CultureInfo.InvariantCulture,
            $",\"posterior_mean\":{Evidence.PosteriorMean:F6},\"posterior_var\":{Evidence.PosteriorVariance:F6}");
        output.Append(
            CultureInfo.InvariantCulture,
            $",\"cost_full\":{Evidence.CostFull:F4},\"cost_dirty\":{Evidence.CostDirty:F4},\"cost_redraw\":{Evidence.CostRedraw:F4}");
        output.Append(
            CultureInfo.InvariantCulture,
            $",\"alpha\":{Evidence.Alpha:F4},\"beta\":{Evidence.Beta:F4}");

        output.Append(",\"obs\":[");
        for (int index = 0; index < Observations.Count; index++)
        {
            if (index > 0)
                output.Append(',');
            Observation observation = Observations[index];
            output.Append("{\"m\":\"")
                .Append(observation.MetricName.Replace("\"", "\\\"", StringComparison.Ordinal))
                .Append('"');
            output.Append(
                CultureInfo.InvariantCulture,
                $",\"v\":{observation.Value:F6},\"c\":{observation.PriorContribution:F6}}}");
        }
        output.Append("]}");
        return output.ToString();
    }
}

/// <summary>A regime transition emitted by the diff evidence ledger.</summary>
public sealed record RegimeTransition(
    ulong FrameId,
    DiffRegime FromRegime,
    DiffRegime ToRegime,
    string Trigger,
    double Confidence)
{
    public string ToJsonl() => FormattableString.Invariant(
        $"{{\"type\":\"regime_transition\",\"frame\":{FrameId},\"from\":\"{DiffRegimeNames.AsStr(FromRegime)}\",\"to\":\"{DiffRegimeNames.AsStr(ToRegime)}\",\"trigger\":\"{Trigger.Replace("\"", "\\\"", StringComparison.Ordinal)}\",\"confidence\":{Confidence:F6}}}");
}

/// <summary>
/// Fixed-capacity ring buffer for diff decisions and regime transitions.
/// Items are enumerated oldest-first and overwrite the oldest item when full.
/// </summary>
public sealed class RuntimeDiffEvidenceLedger
{
    private readonly DiffStrategyRecord?[] _decisions;
    private readonly RegimeTransition?[] _transitions;
    private int _decisionHead;
    private int _transitionHead;
    private int _decisionCount;
    private int _transitionCount;

    public RuntimeDiffEvidenceLedger(int decisionCapacity)
    {
        int capacity = Math.Max(decisionCapacity, 1);
        _decisions = new DiffStrategyRecord?[capacity];
        _transitions = new RegimeTransition?[Math.Max(capacity / 10, 16)];
    }

    public int Count => _decisionCount;
    public bool IsEmpty => _decisionCount == 0;
    public int TransitionCount => _transitionCount;
    public DiffRegime CurrentRegime { get; private set; } = DiffRegime.StableFrame;

    public void Record(DiffStrategyRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Regime != CurrentRegime)
        {
            RecordTransition(new RegimeTransition(
                record.FrameId,
                CurrentRegime,
                record.Regime,
                FormattableString.Invariant(
                    $"confidence={record.Confidence:F3} strategy={record.ChosenStrategy}"),
                record.Confidence));
            CurrentRegime = record.Regime;
        }

        _decisions[_decisionHead] = record;
        _decisionHead = (_decisionHead + 1) % _decisions.Length;
        if (_decisionCount < _decisions.Length)
            _decisionCount++;
    }

    public void RecordTransition(RegimeTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        _transitions[_transitionHead] = transition;
        _transitionHead = (_transitionHead + 1) % _transitions.Length;
        if (_transitionCount < _transitions.Length)
            _transitionCount++;
    }

    public IEnumerable<DiffStrategyRecord> Decisions()
    {
        int start = _decisionCount < _decisions.Length ? 0 : _decisionHead;
        for (int index = 0; index < _decisionCount; index++)
        {
            DiffStrategyRecord? decision = _decisions[(start + index) % _decisions.Length];
            if (decision is not null)
                yield return decision;
        }
    }

    public IEnumerable<RegimeTransition> Transitions()
    {
        int start = _transitionCount < _transitions.Length ? 0 : _transitionHead;
        for (int index = 0; index < _transitionCount; index++)
        {
            RegimeTransition? transition = _transitions[(start + index) % _transitions.Length];
            if (transition is not null)
                yield return transition;
        }
    }

    public DiffStrategyRecord? LastDecision()
    {
        if (_decisionCount == 0)
            return null;
        int index = _decisionHead == 0 ? _decisions.Length - 1 : _decisionHead - 1;
        return _decisions[index];
    }

    public string ExportJsonl()
    {
        var output = new StringBuilder();
        foreach (DiffStrategyRecord decision in Decisions())
            output.AppendLine(decision.ToJsonl());
        foreach (RegimeTransition transition in Transitions())
            output.AppendLine(transition.ToJsonl());
        return output.ToString();
    }

    public void FlushToSink(EvidenceSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        foreach (DiffStrategyRecord decision in Decisions())
            sink.WriteJsonl(decision.ToJsonl());
        foreach (RegimeTransition transition in Transitions())
            sink.WriteJsonl(transition.ToJsonl());
    }

    public void Clear()
    {
        Array.Clear(_decisions);
        Array.Clear(_transitions);
        _decisionHead = 0;
        _transitionHead = 0;
        _decisionCount = 0;
        _transitionCount = 0;
        CurrentRegime = DiffRegime.StableFrame;
    }
}

internal static class DiffRegimeNames
{
    public static string AsStr(DiffRegime regime) => regime switch
    {
        DiffRegime.StableFrame => "stable_frame",
        DiffRegime.BurstyChange => "bursty_change",
        DiffRegime.ResizeRegime => "resize",
        DiffRegime.DegradedTerminal => "degraded",
        _ => throw new ArgumentOutOfRangeException(nameof(regime), regime, null),
    };
}

// decision_core.rs remains a compatibility projection while its generic
// State/Action/Outcome/DecisionCore traits are ported as a separate unit.
public abstract record DecisionAction
{
    public sealed record Hold : DecisionAction;
    public sealed record Degrade(int Level) : DecisionAction;
    public sealed record Upgrade(int Level) : DecisionAction;
}

public sealed record Decision(
    DecisionAction Chosen,
    double Confidence,
    List<EvidenceTerm> Evidence,
    string Reason);
