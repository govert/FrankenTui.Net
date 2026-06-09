// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/timeline_aggregator.rs
// Timeline aggregator with sketch-based frequency estimation + BOCPD.

namespace FrankenTui.Runtime;

public sealed class AggregatorConfig
{
    public SketchConfig Sketch=new(); public AlertConfig Alert=new(); public double HazardRate=0.004; public int WindowSize=100,WarmupObservations=50; public bool EnableLogging;
    public static AggregatorConfig Default=>new();
}

public sealed record AggregationEvidence(ErrorEvidence SketchEvidence,AlertDecision AlertDecision,int RunLength,double ChangeProbability,double RollingMean,double RollingVariance,ulong ObservationCount);

public sealed class AggregatorStats{public ulong TotalEvents,ChangePointsDetected,AlertsTriggered;public int CurrentRunLength,MemoryBytes;}

public sealed class TimelineAggregator
{
    AggregatorConfig _c; CountMinSketch? _sketch; ConformalAlert? _alert; double[] _rl=new double[200]; ulong _n; int _rlIdx; double _sum,_sumSq; Queue<double> _window=new(); double _haz,_prevE;

    public TimelineAggregator(AggregatorConfig c){_c=c;_sketch=CountMinSketch.Create(c.Sketch);_alert=new ConformalAlert(c.Alert);_haz=c.HazardRate;for(int i=0;i<_rl.Length;i++)_rl[i]=1.0/_rl.Length;}

    public AggregationEvidence Observe<T>(T item,double eventRate) where T:notnull
    {
        _n++;_sketch!.Add(item,1);var se=_sketch.GetErrorEvidence();
        _window.Enqueue(eventRate);if(_window.Count>_c.WindowSize)_window.Dequeue();
        double mean=_window.Average(),variance=0;foreach(var x in _window){double d=x-mean;variance+=d*d;}variance/=Math.Max(1,_window.Count-1);

        // BOCPD update
        double predDens=Math.Exp(-0.5*Math.Pow((eventRate-mean)/Math.Max(Math.Sqrt(variance),1e-9),2))/Math.Sqrt(2*Math.PI*Math.Max(variance,1e-9));
        var newRl=new double[_rl.Length]; double total=0;
        for(int r=0;r<_rl.Length-1;r++)newRl[r+1]=_rl[r]*(1-_haz)*predDens;
        double cp=0;for(int r=0;r<_rl.Length;r++)cp+=_rl[r]*_haz*predDens;
        newRl[0]=cp;
        for(int i=0;i<_rl.Length;i++)total+=newRl[i];
        if(total>0)for(int i=0;i<_rl.Length;i++)newRl[i]/=total;else{var u=1.0/_rl.Length;for(int i=0;i<_rl.Length;i++)newRl[i]=u;}
        _rl=newRl;
        int maxIdx=0;double maxP=0;for(int i=0;i<_rl.Length;i++)if(_rl[i]>maxP){maxP=_rl[i];maxIdx=i;}
        double cpProb=_rl[0];

        var ad=_alert!.Observe(eventRate);
        _prevE=ad.Evidence.EValue;
        return new AggregationEvidence(se,ad,maxIdx,cpProb,mean,variance,_n);
    }

    public AggregatorStats Stats()
    {
        int maxIdx=0;double maxP=0;for(int i=0;i<_rl.Length;i++)if(_rl[i]>maxP){maxP=_rl[i];maxIdx=i;}
        return new AggregatorStats{TotalEvents=_n,CurrentRunLength=maxIdx,MemoryBytes=0};
    }
}
