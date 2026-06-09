// SPDX-License-Identifier: Apache-2.0
// Port of diff_evidence.rs and decision_core.rs
// DIVERGENCE: ftui_render::diff_strategy types not ported — stubbed boundary.

namespace FrankenTui.Runtime;

// ── diff_evidence ─────────────────────────────────────────────────────────
// DIVERGENCE: DiffRegime defined in FrankenTui.Render.DiffRegime
public sealed record Observation(string MetricName,double Value,double PriorContribution);
// RegimeTransition deferred — depends on FrankenTui.Render.DiffRegime
// DIVERGENCE: DiffStrategyRecord depends on ftui_render::diff_strategy (Phase I)

// ── decision_core ────────────────────────────────────────────────────────
public abstract record DecisionAction{public sealed record Hold:DecisionAction;public sealed record Degrade(int Level):DecisionAction;public sealed record Upgrade(int Level):DecisionAction;}
public sealed record Decision(DecisionAction Chosen,double Confidence,List<EvidenceTerm> Evidence,string Reason);
// EvidenceTerm already defined in UnifiedEvidence.cs
