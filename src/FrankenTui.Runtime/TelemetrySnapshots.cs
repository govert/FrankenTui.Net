// SPDX-License-Identifier: Apache-2.0
// Port of voi_telemetry.rs, evidence_telemetry.rs, transparency.rs
// Global telemetry snapshots for runtime introspection.

using FrankenTui.Render;

namespace FrankenTui.Runtime;

// ── voi_telemetry ─────────────────────────────────────────────────────────
public static class VoiTelemetry
{
    static VoiSamplerSnapshot? _snap; static readonly object _lk=new();
    public static void Store(VoiSamplerSnapshot s){lock(_lk)_snap=s;}
    public static VoiSamplerSnapshot? Load(){lock(_lk)return _snap;}
    public static void Clear(){lock(_lk)_snap=null;}
}

// ── evidence_telemetry ────────────────────────────────────────────────────
// DIVERGENCE: Diff/Budget/Resize evidence types depend on ftui_render::budget
// and resize_coalescer (not yet ported). Stub storage provided.

public static class EvidenceTelemetry
{
    static StrategyEvidence? _diffEvidence; static BocpdEvidence? _bocpdEvidence;
    static readonly object _lk=new();

    public static void StoreDiff(StrategyEvidence e){lock(_lk)_diffEvidence=e;}
    public static StrategyEvidence? LoadDiff(){lock(_lk)return _diffEvidence;}
    public static void StoreBocpd(BocpdEvidence e){lock(_lk)_bocpdEvidence=e;}
    public static BocpdEvidence? LoadBocpd(){lock(_lk)return _bocpdEvidence;}
    public static void Clear(){lock(_lk){_diffEvidence=null;_bocpdEvidence=null;}}
}

// ── transparency ──────────────────────────────────────────────────────────
public sealed class TransparencyReport
{
    public List<Decision> Decisions=new(); public List<EvidenceTerm> Evidence=new();
    public void Record(Decision d){Decisions.Add(d);}
    public void Clear(){Decisions.Clear();Evidence.Clear();}
}
