// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/bocpd.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// Bayesian Online Change-Point Detection. Uses Stopwatch.GetTimestamp() ticks.
// DIVERGENCE: Tracing spans/logs skipped.

using System.Diagnostics;

namespace FrankenTui.Runtime;

static class BocpdCounters { private static long _cp; public static ulong ChangePointsDetectedTotal => (ulong)Interlocked.Read(ref _cp); public static void IncChangePoints() => Interlocked.Increment(ref _cp); }

public sealed class BocpdConfig
{
    public double MuSteadyMs { get; set; } = 200.0; public double MuBurstMs { get; set; } = 20.0;
    public double HazardLambda { get; set; } = 50.0; public int MaxRunLength { get; set; } = 100;
    public double SteadyThreshold { get; set; } = 0.3; public double BurstThreshold { get; set; } = 0.7;
    public double BurstPrior { get; set; } = 0.2; public double MinObservationMs { get; set; } = 1.0;
    public double MaxObservationMs { get; set; } = 10000.0; public bool EnableLogging { get; set; }
    public static BocpdConfig Default => new();
    public static BocpdConfig Responsive() => new() { MuSteadyMs=150, MuBurstMs=15, HazardLambda=30, SteadyThreshold=0.25, BurstThreshold=0.6 };
    public static BocpdConfig AggressiveCoalesce() => new() { MuSteadyMs=250, MuBurstMs=25, HazardLambda=80, SteadyThreshold=0.4, BurstThreshold=0.8, BurstPrior=0.3 };
    public BocpdConfig WithLogging(bool e) { EnableLogging=e; return this; }
}

public enum BocpdRegime { Steady, Burst, Transitional }
internal static class BocpdRegimeMeta { public static string AsStr(BocpdRegime r) => r switch { BocpdRegime.Steady=>"steady", BocpdRegime.Burst=>"burst", BocpdRegime.Transitional=>"transitional", _=>"" }; }

public sealed class BocpdEvidence
{
    public double PBurst, LogBayesFactor, ObservationMs, LikelihoodSteady, LikelihoodBurst, ExpectedRunLength, RunLengthVariance, RunLengthTailMass;
    public BocpdRegime Regime; public int RunLengthMode, RunLengthP95; public ulong? RecommendedDelayMs; public bool? HardDeadlineForced; public ulong ObservationCount;
    public string ToJsonl() => $$"""{"schema_version":"bocpd-v1","event":"bocpd","p_burst":{{PBurst:F4}},"log_bf":{{LogBayesFactor:F3}},"obs_ms":{{ObservationMs:F1}},"regime":"{{BocpdRegimeMeta.AsStr(Regime)}}","ll_steady":{{LikelihoodSteady:F6}},"ll_burst":{{LikelihoodBurst:F6}},"runlen_mean":{{ExpectedRunLength:F1}},"runlen_var":{{RunLengthVariance:F3}},"runlen_mode":{{RunLengthMode}},"runlen_p95":{{RunLengthP95}},"runlen_tail":{{RunLengthTailMass:F4}},"delay_ms":{{RecommendedDelayMs?.ToString()??"null"}},"forced_deadline":{{(HardDeadlineForced?.ToString().ToLowerInvariant()??"null")}},"n_obs":{{ObservationCount}}}""";
}

public sealed class BocpdDetector
{
    private BocpdConfig _c; private double[] _rl; private double _pB; private long _lastTicks; private bool _hasLast;
    private ulong _n; private BocpdEvidence? _ev; private BocpdRegime _pr = BocpdRegime.Steady;
    private double _ls, _lb, _h;

    public BocpdDetector(BocpdConfig c)
    {
        c.MaxRunLength=Math.Max(1,c.MaxRunLength); c.MuSteadyMs=Math.Max(1,c.MuSteadyMs); c.MuBurstMs=Math.Max(1,c.MuBurstMs);
        c.HazardLambda=Math.Max(1,c.HazardLambda); c.MinObservationMs=double.IsNaN(c.MinObservationMs)?0.1:Math.Max(0.1,c.MinObservationMs);
        c.MaxObservationMs=double.IsNaN(c.MaxObservationMs)?c.MinObservationMs:Math.Max(c.MaxObservationMs,c.MinObservationMs);
        c.SteadyThreshold=double.IsNaN(c.SteadyThreshold)?0.3:Math.Clamp(c.SteadyThreshold,0,1);
        c.BurstThreshold=double.IsNaN(c.BurstThreshold)?0.7:Math.Clamp(c.BurstThreshold,0,1);
        if(c.BurstThreshold<c.SteadyThreshold)(c.SteadyThreshold,c.BurstThreshold)=(c.BurstThreshold,c.SteadyThreshold);
        c.BurstPrior=double.IsNaN(c.BurstPrior)?0.1:Math.Clamp(c.BurstPrior,0.001,0.999);
        _c=c; int k=c.MaxRunLength; double p=1.0/(k+1); _rl=new double[k+1]; Array.Fill(_rl,p);
        _pB=c.BurstPrior; _ls=1.0/c.MuSteadyMs; _lb=1.0/c.MuBurstMs; _h=1.0/c.HazardLambda;
    }

    public static BocpdDetector WithDefaults() => new(BocpdConfig.Default);
    public double PBurst => _pB;
    public double[] RunLengthPosterior => _rl;
    public BocpdRegime Regime => _pB<_c.SteadyThreshold?BocpdRegime.Steady:_pB>_c.BurstThreshold?BocpdRegime.Burst:BocpdRegime.Transitional;
    public double ExpectedRunLength() { double s=0; for(int i=0;i<_rl.Length;i++)s+=i*_rl[i]; return s; }
    public BocpdEvidence? LastEvidence => _ev;
    public ulong ObservationCount => _n;
    public BocpdConfig Config => _c;

    public BocpdRegime ObserveEvent() => ObserveEvent(Stopwatch.GetTimestamp());
    public BocpdRegime ObserveEvent(long nowTicks)
    {
        double obsMs = _hasLast ? Math.Max(0,(nowTicks-_lastTicks)/(double)Stopwatch.Frequency*1000.0) : _c.MuSteadyMs;
        double x = Math.Clamp(Math.Max(obsMs,_c.MinObservationMs),0,_c.MaxObservationMs);
        UpdatePosterior(x); _lastTicks=nowTicks; _hasLast=true;
        var r=Regime; if(r!=_pr){BocpdCounters.IncChangePoints();_pr=r;} return r;
    }

    void UpdatePosterior(double x)
    {
        _n++; double ps=ExpPdf(x,_ls), pb=ExpPdf(x,_lb);
        double logLr=Math.Log(_lb)-_lb*x-(Math.Log(_ls)-_ls*x), logBf=logLr/Math.Log(10);
        double priorB=_pB*(1-_h)+_c.BurstPrior*_h, priorOdds=priorB/Math.Max(1e-10,1-priorB);
        double lr=Math.Exp(logLr), postOdds=priorOdds*lr, pRaw=postOdds/(1+postOdds);
        if(double.IsNaN(pRaw))pRaw=double.IsInfinity(postOdds)?1:0.5;
        _pB=Math.Clamp(pRaw,0.001,0.999);
        double mix=_pB*pb+(1-_pB)*ps; int k=_c.MaxRunLength;
        var np=new double[k+1];
        for(int r=0;r<k;r++)np[r+1]+=_rl[r]*(1-_h)*mix; np[k]+=_rl[k]*(1-_h)*mix;
        double cp=0; for(int r=0;r<=k;r++)cp+=_rl[r]*_h*mix; np[0]=cp;
        double tot=0; for(int i=0;i<=k;i++)tot+=np[i];
        if(tot>0)for(int i=0;i<=k;i++)np[i]/=tot; else Array.Fill(np,1.0/(k+1));
        _rl=np;
        var(mean,var,mode,p95,tail)=Summary();
        _ev=new BocpdEvidence{PBurst=_pB,LogBayesFactor=logBf,ObservationMs=x,Regime=Regime,LikelihoodSteady=ps,LikelihoodBurst=pb,ExpectedRunLength=mean,RunLengthVariance=var,RunLengthMode=mode,RunLengthP95=p95,RunLengthTailMass=tail,ObservationCount=_n};
    }

    (double mean,double var,int mode,int p95,double tail) Summary()
    {
        double m=ExpectedRunLength(),v=0; int md=0,p95=_c.MaxRunLength; double mp=-1,cum=0;
        for(int r=0;r<_rl.Length;r++){ double p=_rl[r]; if(p>mp){mp=p;md=r;} v+=p*(r-m)*(r-m); if(cum<0.95){cum+=p;if(cum>=0.95)p95=r;} }
        return (m,v,md,p95,_rl[_c.MaxRunLength]);
    }

    double ExpPdf(double x,double l)=>l*Math.Exp(-l*x);

    public void Reset()
    {
        int k=_c.MaxRunLength; double p=1.0/(k+1); _rl=new double[k+1]; Array.Fill(_rl,p);
        _pB=_c.BurstPrior; _hasLast=false; _n=0; _ev=null; _pr=BocpdRegime.Steady;
    }

    public ulong RecommendedDelay(ulong s,ulong b)
    {
        if(_pB<_c.SteadyThreshold)return s; if(_pB>_c.BurstThreshold)return b;
        double d=Math.Max(1e-6,_c.BurstThreshold-_c.SteadyThreshold),t=Math.Clamp((_pB-_c.SteadyThreshold)/d,0,1);
        return (ulong)Math.Round(s*(1-t)+b*t);
    }

    public void SetDecisionContext(ulong s,ulong b,bool f){if(_ev!=null){_ev.RecommendedDelayMs=RecommendedDelay(s,b);_ev.HardDeadlineForced=f;}}
    public string? EvidenceJsonl()=>!_c.EnableLogging?null:_ev?.ToJsonl();
    public string? DecisionLogJsonl(ulong s,ulong b,bool forced){if(!_c.EnableLogging||_ev==null)return null;var e=new BocpdEvidence{PBurst=_ev.PBurst,LogBayesFactor=_ev.LogBayesFactor,ObservationMs=_ev.ObservationMs,Regime=_ev.Regime,LikelihoodSteady=_ev.LikelihoodSteady,LikelihoodBurst=_ev.LikelihoodBurst,ExpectedRunLength=_ev.ExpectedRunLength,RunLengthVariance=_ev.RunLengthVariance,RunLengthMode=_ev.RunLengthMode,RunLengthP95=_ev.RunLengthP95,RunLengthTailMass=_ev.RunLengthTailMass,ObservationCount=_ev.ObservationCount,RecommendedDelayMs=RecommendedDelay(s,b),HardDeadlineForced=forced};return e.ToJsonl();}
}
