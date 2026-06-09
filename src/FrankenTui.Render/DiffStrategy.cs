// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/diff_strategy.rs
// Bayesian diff strategy selector with Beta-Binomial change-rate estimation.
// Replaces the stub in DiffStrategy.cs

namespace FrankenTui.Render;

public enum DiffStrategy{Full,DirtyRows,FullRedraw,SignificantDirtyRows}

// ── Keep existing types for AppRuntime.cs compatibility ──
public enum DiffRegime{StableFrame,BurstyChange,ResizeRegime,DegradedTerminal}
public sealed record DiffStrategySelection(int FrameIndex,DiffRegime Regime,DiffStrategy Strategy,double Confidence,int DirtyRows,int TotalCells,DiffRegime? TransitionFrom,string? TransitionReason);

public sealed record DiffTransitionRecord(DiffRegime From, DiffRegime To, int FrameIndex);
public sealed record DiffDecisionRecord(DiffStrategy Strategy, DiffRegime Regime, int FrameIndex);

public sealed class DiffEvidenceLedger
{
    public List<StrategyEvidence> Entries = new();
    public List<DiffTransitionRecord> Transitions = new();
    public List<DiffDecisionRecord> Decisions = new();
    public void Record(StrategyEvidence e) => Entries.Add(e);
    public void RecordDecision(DiffDecisionRecord d) => Decisions.Add(d);
    public void RecordTransition(DiffTransitionRecord t) => Transitions.Add(t);
}

public sealed class DiffStrategyConfig
{
    public double CScan=1.0,CEmit=6.0,CRow=0.1,PriorAlpha=1.0,PriorBeta=19.0,Decay=0.95,HysteresisRatio=0.05,UncertaintyGuardVariance=0.002,ConservativeQuantile=0.95;
    public bool Conservative; public int MinObservationCells=1;
    public static DiffStrategyConfig Default=>new();
    public DiffStrategyConfig Sanitized()=>new(){CScan=NormCost(CScan,1),CEmit=NormCost(CEmit,6),CRow=NormCost(CRow,0.1),PriorAlpha=NormPos(PriorAlpha,1),PriorBeta=NormPos(PriorBeta,19),Decay=NormDecay(Decay),Conservative=Conservative,ConservativeQuantile=double.IsNaN(ConservativeQuantile)?1e-6:Math.Clamp(ConservativeQuantile,1e-6,1-1e-6),MinObservationCells=MinObservationCells,HysteresisRatio=NormRatio(HysteresisRatio,0.05),UncertaintyGuardVariance=NormCost(UncertaintyGuardVariance,0.002)};
    static double NormPos(double v,double f)=>double.IsFinite(v)&&v>0?v:f;
    static double NormCost(double v,double f)=>double.IsFinite(v)&&v>=0?v:f;
    static double NormDecay(double v)=>double.IsFinite(v)&&v>0?Math.Min(v,1):1;
    static double NormRatio(double v,double f)=>double.IsFinite(v)?Math.Clamp(v,0,1):f;
}

public sealed class ChangeRateEstimator
{
    readonly double _pa,_pb,_decay;readonly int _minObs;double _a,_b;
    public ChangeRateEstimator(double pa,double pb,double decay,int minObs){_pa=pa;_pb=pb;_decay=decay;_minObs=minObs;_a=pa;_b=pb;}
    public void Reset(){_a=_pa;_b=_pb;}
    public (double,double) PosteriorParams=>(_a,_b);
    public double Mean=>_a/(_a+_b);
    public double Variance{get{double s=_a+_b;return(_a*_b)/(s*s*(s+1));}}
    public void Observe(int cellsScanned,int cellsChanged)
    {
        if(cellsScanned<_minObs)return;
        cellsChanged=Math.Min(cellsChanged,cellsScanned);_a*=_decay;_b*=_decay;
        _a+=cellsChanged;_b+=cellsScanned-cellsChanged;
        _a=Math.Clamp(_a,1e-6,1e6);_b=Math.Clamp(_b,1e-6,1e6);
    }
    public double UpperQuantile(double q)
    {
        q=Math.Clamp(q,1e-6,1-1e-6);double m=Mean,s=Math.Sqrt(Variance);
        double t=Math.Sqrt(-2*Math.Log(q>=0.5?1-q:q));
        double z=q>=0.5?t-(2.515517+0.802853*t+0.010328*t*t)/(1+1.432788*t+0.189269*t*t+0.001308*t*t*t):-(t-(2.515517+0.802853*t+0.010328*t*t)/(1+1.432788*t+0.189269*t*t+0.001308*t*t*t));
        return Math.Clamp(m+z*s,0,1);
    }
}

public sealed class StrategyEvidence
{
    public DiffStrategy Strategy; public double CostFull,CostDirty,CostRedraw,PosteriorMean,PosteriorVariance,Alpha,Beta,HysteresisRatio;
    public int DirtyRows,TotalRows,TotalCells; public string GuardReason="none"; public bool HysteresisApplied;
    public string ToJsonl()=>$"{{\"schema\":\"diff-strategy-v1\",\"strategy\":\"{Strategy}\",\"cost_full\":{CostFull:F2},\"cost_dirty\":{CostDirty:F2},\"cost_redraw\":{CostRedraw:F2},\"posterior_mean\":{PosteriorMean:F6},\"posterior_var\":{PosteriorVariance:F8},\"alpha\":{Alpha:F4},\"beta\":{Beta:F4},\"dirty_rows\":{DirtyRows},\"total_rows\":{TotalRows},\"total_cells\":{TotalCells},\"guard\":\"{GuardReason}\",\"hysteresis\":{HysteresisApplied.ToString().ToLowerInvariant()},\"hysteresis_ratio\":{HysteresisRatio:F4}}}";
}

public sealed class DiffStrategySelector
{
    DiffStrategyConfig _c;ChangeRateEstimator _e;ulong _fc;StrategyEvidence? _last;
    public DiffStrategySelector(DiffStrategyConfig? c=null){_c=(c??DiffStrategyConfig.Default).Sanitized();_e=new ChangeRateEstimator(_c.PriorAlpha,_c.PriorBeta,_c.Decay,_c.MinObservationCells);}
    public static DiffStrategySelector WithDefaults()=>new(DiffStrategyConfig.Default);
    public DiffStrategyConfig Config=>_c;
    public (double,double) PosteriorParams=>_e.PosteriorParams;
    public double PosteriorMean=>_e.Mean;
    public double PosteriorVariance=>_e.Variance;
    public StrategyEvidence? LastEvidence=>_last;
    public DiffEvidenceLedger Ledger{get;}=new();
    public ulong FrameCount=>_fc;
    public void OverrideLastStrategy(DiffStrategy s,string reason){if(_last!=null){_last.Strategy=s;_last.GuardReason=reason;_last.HysteresisApplied=false;}}
    public void Observe(int scanned,int changed)=>_e.Observe(scanned,changed);
    public void Observe(DiffStrategySelection sel,int changedCells,TimeSpan writeLatency)=>_e.Observe(sel.TotalCells,changedCells);
    public void Reset(){_e.Reset();_fc=0;_last=null;}
    public DiffStrategySelection Select(int width,int height,int dirtyRows,bool resized,TimeSpan lastWriteLatency)
    {
        var s=Select(width,height,dirtyRows);
        var regime=resized?DiffRegime.ResizeRegime:dirtyRows>0?DiffRegime.BurstyChange:DiffRegime.StableFrame;
        var selection=new DiffStrategySelection((int)_fc,regime,s,1.0,dirtyRows,width*height,null,null);
        Ledger.RecordDecision(new DiffDecisionRecord(s,regime,(int)_fc));
        if(Ledger.Decisions.Count>=2){var prev=Ledger.Decisions[^2];if(prev.Regime!=regime)Ledger.RecordTransition(new DiffTransitionRecord(prev.Regime,regime,(int)_fc));}
        return selection;
    }
    public DiffStrategy Select(int width,int height,int dirtyRows)=>SelectWithScan(width,height,dirtyRows,dirtyRows*width);
    public DiffStrategy SelectWithScan(int width,int height,int dirtyRows,int dirtyScanCells)
    {
        _fc++;
        double w=width,h=height,d=dirtyRows,n=w*h,sc=Math.Min(dirtyScanCells,width*height);
        bool ug=_c.UncertaintyGuardVariance>0&&PosteriorVariance>_c.UncertaintyGuardVariance;
        string gr=dirtyRows==0?"zero_dirty_rows":"none";
        double p=dirtyRows==0?0:(_c.Conservative||ug?_e.UpperQuantile(_c.ConservativeQuantile):PosteriorMean);
        double cf=_c.CRow*h+_c.CScan*d*w+_c.CEmit*p*n,cd=_c.CScan*sc+_c.CEmit*p*n,cr=_c.CEmit*n;
        var s=cd<=cf&&cd<=cr?DiffStrategy.DirtyRows:cf<=cr?DiffStrategy.Full:DiffStrategy.FullRedraw;
        if(ug){if(gr=="none")gr="uncertainty_variance";if(s==DiffStrategy.FullRedraw)s=cd<=cf?DiffStrategy.DirtyRows:DiffStrategy.Full;}
        bool hy=false;
        if(_last is{}prev&&prev.Strategy!=s){
            double pc=CostFor(prev.Strategy,cf,cd,cr),nc=CostFor(s,cf,cd,cr),r=_c.HysteresisRatio;
            if(r>0&&double.IsFinite(pc)&&pc>0&&nc>=pc*(1-r)&&!(ug&&prev.Strategy==DiffStrategy.FullRedraw)){s=prev.Strategy;hy=true;}
        }
        var(a,b)=_e.PosteriorParams;
        _last=new StrategyEvidence{Strategy=s,CostFull=cf,CostDirty=cd,CostRedraw=cr,PosteriorMean=PosteriorMean,PosteriorVariance=PosteriorVariance,Alpha=a,Beta=b,DirtyRows=dirtyRows,TotalRows=height,TotalCells=width*height,GuardReason=gr,HysteresisApplied=hy,HysteresisRatio=_c.HysteresisRatio};
        return s;
    }
    static double CostFor(DiffStrategy s,double cf,double cd,double cr)=>s switch{DiffStrategy.Full=>cf,DiffStrategy.DirtyRows=>cd,DiffStrategy.FullRedraw=>cr,_=>cf};
}
