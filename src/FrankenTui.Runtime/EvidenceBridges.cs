// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/evidence_bridges.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
//
// Evidence bridges: convert domain-specific decision types into unified
// EvidenceEntry records.
//
// DIVERGENCE: Bridge functions are stubbed — they depend on domain-specific
// types from Phases G/H/I:
//   from_diff_strategy → ftui_render::diff_strategy::StrategyEvidence (Phase I)
//   from_eprocess     → crate::eprocess_throttle::ThrottleDecision (Phase G)
//   from_voi          → crate::voi_sampling::VoiDecision (Phase H)
//   from_conformal    → crate::conformal_predictor::ConformalPrediction (Phase H)
//   from_bocpd        → crate::bocpd::BocpdEvidence (Phase H)
//
// Each bridge follows the same pattern: action string, log_posterior,
// loss_avoided, confidence_interval, evidence terms. These will be filled
// when the source types are ported.

namespace FrankenTui.Runtime;

/// <summary>Evidence bridge functions for each decision domain.</summary>
public static class EvidenceBridges
{
    // DIVERGENCE: All bridge functions deferred — source types not yet ported.
    // See header for the upstream type mapping.

    // public static EvidenceEntry FromDiffStrategy(StrategyEvidence e, ulong tsNs) => ...
    // public static EvidenceEntry FromEProcess(ThrottleDecision d, ulong tsNs) => ...
    // public static EvidenceEntry FromVoi(VoiDecision d, ulong tsNs) => ...
    // public static EvidenceEntry FromConformal(ConformalPrediction p, ulong tsNs) => ...
    // public static EvidenceEntry FromBocpd(BocpdEvidence e, ulong tsNs) => ...
}
