// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/input_fairness.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// Input Fairness Guard — prevents resize from starving keyboard input.

using System.Diagnostics;

namespace FrankenTui.Runtime;

public enum EventType{Input,Resize,Tick}
public enum InterventionReason{None,InputLatency,ResizeDominance,FairnessIndex}

internal static class InterventionMeta{
    public static bool RequiresIntervention(InterventionReason r)=>r!=InterventionReason.None;
    public static string AsStr(InterventionReason r)=>r switch{InterventionReason.None=>"none",InterventionReason.InputLatency=>"input_latency",InterventionReason.ResizeDominance=>"resize_dominance",InterventionReason.FairnessIndex=>"fairness_index",_=>""};
}

public sealed class FairnessConfig
{
    public TimeSpan InputPriorityThreshold=TimeSpan.FromMilliseconds(50); public bool Enabled=true; public uint DominanceThreshold=3; public double FairnessThreshold=0.8;
    public static FairnessConfig Default=>new();
    public static FairnessConfig Disabled()=>new(){Enabled=false};
    public FairnessConfig WithMaxLatency(TimeSpan l){InputPriorityThreshold=l;return this;}
    public FairnessConfig WithDominanceThreshold(uint t){DominanceThreshold=t;return this;}
}

public sealed class FairnessDecision
{
    public bool ShouldProcess=true; public TimeSpan? PendingInputLatency; public InterventionReason Reason; public bool YieldToInput; public double JainIndex=1.0;
    public static FairnessDecision Default=>new();
}

public sealed class FairnessStats
{
    public ulong EventsProcessed,InputEvents,ResizeEvents,TickEvents,TotalChecks,TotalInterventions; public TimeSpan MaxInputLatency;
}

public sealed class InterventionCounts{public ulong InputLatency,ResizeDominance,FairnessIndex;}

public sealed class InputFairnessGuard
{
    const int FAIRNESS_WINDOW_SIZE=16; const double FAIRNESS_THRESHOLD_EPSILON=1e-9;
    FairnessConfig _c; FairnessStats _s=new(); InterventionCounts _ic=new();
    long? _pendingTicks,_recentTicks; uint _dom; Queue<(EventType,long)> _win=new(); long _inUs,_reUs;

    public InputFairnessGuard(FairnessConfig? c=null){_c=c??FairnessConfig.Default;}

    public void InputArrived(long nowTicks){_pendingTicks??=nowTicks;_recentTicks=nowTicks;}
    public FairnessDecision CheckFairness(long nowTicks)
    {
        _s.TotalChecks++;
        if(!_c.Enabled){_recentTicks=null;return FairnessDecision.Default;}
        double jain=JainIndex();
        var hasPending=_pendingTicks.HasValue;
        TimeSpan? lat=null;
        if(hasPending){lat=TicksToSpan(nowTicks-(_pendingTicks??nowTicks));if(lat>_s.MaxInputLatency)_s.MaxInputLatency=lat.Value;}
        var reason=DetermineReason(lat,jain,hasPending); bool yield=InterventionMeta.RequiresIntervention(reason);
        if(yield){_s.TotalInterventions++;switch(reason){case InterventionReason.InputLatency:_ic.InputLatency++;break;case InterventionReason.ResizeDominance:_ic.ResizeDominance++;break;case InterventionReason.FairnessIndex:_ic.FairnessIndex++;break;}_dom=0;}
        _recentTicks=null;
        return new FairnessDecision{ShouldProcess=!yield,PendingInputLatency=hasPending?lat:null,Reason=reason,YieldToInput=yield,JainIndex=jain};
    }

    public void EventProcessed(EventType et,TimeSpan dur,long _)
    {
        _s.EventsProcessed++;switch(et){case EventType.Input:_s.InputEvents++;break;case EventType.Resize:_s.ResizeEvents++;break;case EventType.Tick:_s.TickEvents++;break;}
        if(!_c.Enabled)return;
        long dus=(long)(dur.TotalMilliseconds*1000); // microseconds
        if(_win.Count>=FAIRNESS_WINDOW_SIZE&&_win.TryDequeue(out var old)){switch(old.Item1){case EventType.Input:_inUs-=old.Item2;break;case EventType.Resize:_reUs-=old.Item2;break;}}
        switch(et){case EventType.Input:_inUs+=dus;_pendingTicks=null;_dom=0;break;case EventType.Resize:_reUs+=dus;_dom++;break;}
        _win.Enqueue((et,dus));
    }

    double JainIndex()
    {
        double x=_inUs,y=_reUs; if(x==0&&y==0)return 1.0;
        double sum=x+y,sumSq=x*x+y*y; if(sumSq==0)return 1.0;
        return sum*sum/(2.0*sumSq);
    }

    InterventionReason DetermineReason(TimeSpan? lat,double jain,bool hasPending)
    {
        if(hasPending&&lat.HasValue&&lat.Value>=_c.InputPriorityThreshold)return InterventionReason.InputLatency;
        if(hasPending&&_dom>=_c.DominanceThreshold)return InterventionReason.ResizeDominance;
        if(hasPending&&jain+FAIRNESS_THRESHOLD_EPSILON<_c.FairnessThreshold&&_reUs>_inUs)return InterventionReason.FairnessIndex;
        return InterventionReason.None;
    }

    static TimeSpan TicksToSpan(long ticks)=>TimeSpan.FromMilliseconds(ticks*1000.0/Stopwatch.Frequency);

    public FairnessStats Stats=>_s; public InterventionCounts InterventionCounts=>_ic; public FairnessConfig Config=>_c; public uint ResizeDominanceCount=>_dom; public bool IsEnabled=>_c.Enabled; public double JainIdx=>JainIndex(); public bool HasPendingInput=>_pendingTicks.HasValue;
    public void Reset(){_pendingTicks=_recentTicks=null;_dom=0;_win.Clear();_inUs=_reUs=0;_s=new();_ic=new();}
}
