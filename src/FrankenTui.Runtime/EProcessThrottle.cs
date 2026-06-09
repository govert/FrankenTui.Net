// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/eprocess_throttle.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// Anytime-valid throttle using e-process (test martingale) control.
// DIVERGENCE: Tracing spans skipped.

using System.Diagnostics;

namespace FrankenTui.Runtime;

static class EProcessConstants { public const double WMin=1e-12, Mu0Min=1e-6, Mu0Max=1.0-1e-6; }
static class EProcessCounters { static long _rej; public static ulong RejectionsTotal=>(ulong)Interlocked.Read(ref _rej); public static void IncRej()=>Interlocked.Increment(ref _rej); }

public sealed class ThrottleConfig
{
    public double Alpha=0.05,Mu0=0.1,InitialLambda=0.5,GrapaEta=0.1; public ulong HardDeadlineMs=500,MinObservationsBetween=8; public int RateWindowSize=64; public bool EnableLogging;
    public static ThrottleConfig Default=>new();
}

public readonly record struct ThrottleDecision(bool ShouldRecompute,double Wealth,double Lambda,double EmpiricalRate,bool ForcedByDeadline,ulong ObservationsSinceRecompute)
{
    public string ToJsonl()=>$$"""{"schema":"eprocess-throttle-v1","should_recompute":{{ShouldRecompute.ToString().ToLowerInvariant()}},"wealth":{{Wealth:F6}},"lambda":{{Lambda:F6}},"empirical_rate":{{EmpiricalRate:F6}},"forced_by_deadline":{{ForcedByDeadline.ToString().ToLowerInvariant()}},"obs_since_recompute":{{ObservationsSinceRecompute}}}""";
}

public sealed record ThrottleLog(ulong ObservationIdx,bool Matched,double WealthBefore,double WealthAfter,double Lambda,double EmpiricalRate,string Action,double TimeSinceRecomputeMs)
{
    public string ToJsonl()=>$$"""{"schema":"eprocess-log-v1","obs_idx":{{ObservationIdx}},"matched":{{Matched.ToString().ToLowerInvariant()}},"wealth_before":{{WealthBefore:F6}},"wealth_after":{{WealthAfter:F6}},"lambda":{{Lambda:F6}},"empirical_rate":{{EmpiricalRate:F6}},"action":"{{Action}}","time_since_recompute_ms":{{TimeSinceRecomputeMs:F3}}}""";
}

public sealed record ThrottleStats(ulong TotalObservations,ulong TotalRecomputes,ulong ForcedRecomputes,ulong EProcessRecomputes,double CurrentWealth,double CurrentLambda,double EmpiricalRate,double AvgObsBetweenRecomputes);

public sealed class EProcessThrottle
{
    ThrottleConfig _c; double _w=1,_l,_m,_lm,_th; Queue<bool> _rm=new(); ulong _n,_since,_tr,_fr,_er,_cum; long _lrTicks; bool _hasLr; List<ThrottleLog> _logs=new();

    public EProcessThrottle(ThrottleConfig c)
    {
        _c=c; _m=Math.Clamp(c.Mu0,EProcessConstants.Mu0Min,EProcessConstants.Mu0Max); _lm=1.0/_m-1e-6; _l=Math.Clamp(c.InitialLambda,1e-6,_lm); _th=1.0/Math.Max(c.Alpha,1e-12);
    }

    public ThrottleDecision Observe(bool matched)=>ObserveAt(matched,Stopwatch.GetTimestamp());
    public ThrottleDecision ObserveAt(bool matched,long nowTicks)
    {
        _n++;_since++;_rm.Enqueue(matched); while(_rm.Count>_c.RateWindowSize)_rm.Dequeue();
        double er=EmpiricalMatchRate(),xb=_w;
        double x=matched?1:0, mult=1+_l*(x-_m); _w=Math.Max(_w*mult,EProcessConstants.WMin);
        double denom=1+_l*(x-_m); if(Math.Abs(denom)>1e-12)_l=Math.Clamp(_l+_c.GrapaEta*(x-_m)/denom,1e-6,_lm);
        bool hd=_hasLr&&(nowTicks-_lrTicks)/(double)Stopwatch.Frequency>=_c.HardDeadlineMs/1000.0;
        bool mo=_since>=_c.MinObservationsBetween, we=_w>=_th, ep=we&&mo, sr=hd||ep, fd=hd&&!ep;
        if(sr) { _tr++; _cum+=_since; if(fd)_fr++;else _er++; _w=1;_since=0;_lrTicks=nowTicks;_hasLr=true; }
        if(!_hasLr){_lrTicks=nowTicks;_hasLr=true;}
        if(_c.EnableLogging)_logs.Add(new ThrottleLog(_n,matched,xb,_w,_l,er,sr?(fd?"recompute_forced":"recompute_eprocess"):"observe",_hasLr?(nowTicks-_lrTicks)/(double)Stopwatch.Frequency*1000:0));
        if(ep)EProcessCounters.IncRej();
        return new ThrottleDecision(sr,_w,_l,er,sr&&fd,_since);
    }

    public double EmpiricalMatchRate(){if(_rm.Count==0)return 0; int m=0;foreach(var b in _rm)if(b)m++;return(double)m/_rm.Count;}
    public void Reset()=>ResetAt(Stopwatch.GetTimestamp());
    public void ResetAt(long nowTicks){_w=1;_since=0;_lrTicks=nowTicks;_hasLr=true;_rm.Clear();}
    public void SetMu0(double m){_m=Math.Clamp(m,EProcessConstants.Mu0Min,EProcessConstants.Mu0Max);_lm=1.0/_m-1e-6;_l=Math.Clamp(_l,1e-6,_lm);Reset();}
    public double Wealth=>_w; public double Lambda=>_l; public double Threshold=>_th; public ulong ObservationCount=>_n;
    public ThrottleStats Stats()
    {
        double avg=_tr>0?(double)_cum/_tr:0;
        return new ThrottleStats(_n,_tr,_fr,_er,_w,_l,EmpiricalMatchRate(),avg);
    }
    public IReadOnlyList<ThrottleLog> Logs=>_logs; public void ClearLogs()=>_logs.Clear();
}
