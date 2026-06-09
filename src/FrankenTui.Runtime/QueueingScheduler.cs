// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/queueing_scheduler.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// Queueing Theory Scheduler with SRPT/Smith-Rule Style Scheduling.

using System.Text;

namespace FrankenTui.Runtime;

// ── Constants & Config ────────────────────────────────────────────────────

static class QSched { public const double DefAging=0.1, DefPMin=0.05, DefPMax=5000, DefEstDef=10, DefEstUnk=1000, DefWMin=1e-6, DefWMax=100, DefWeightDef=1, DefWeightUnk=1, DefStarveMs=500, DefStarveBoost=1.5, DefQuantum=10; public const int MaxQueue=10000; }

public sealed class SchedulerConfig
{
    public double AgingFactor=QSched.DefAging, PMinMs=QSched.DefPMin, PMaxMs=QSched.DefPMax, EstimateDefaultMs=QSched.DefEstDef, EstimateUnknownMs=QSched.DefEstUnk;
    public double WMin=QSched.DefWMin, WMax=QSched.DefWMax, WeightDefault=QSched.DefWeightDef, WeightUnknown=QSched.DefWeightUnk, WaitStarveMs=QSched.DefStarveMs, StarveBoostRatio=QSched.DefStarveBoost, TimeQuantum=QSched.DefQuantum;
    public bool SmithEnabled=true, ForceFifo, Preemptive=true, EnableLogging; public int MaxQueueSize=QSched.MaxQueue;
    public static SchedulerConfig Default=>new();
    public SchedulingMode Mode=>ForceFifo?SchedulingMode.Fifo:SmithEnabled?SchedulingMode.Smith:SchedulingMode.Srpt;
}

// ── Enums ─────────────────────────────────────────────────────────────────

public enum SchedulingMode{Smith,Srpt,Fifo}
public enum SelectionReason{QueueEmpty,ShortestRemaining,HighestWeightedPriority,Fifo,AgingBoost,Continuation}
public enum EstimateSource{Explicit,Historical,Default,Unknown}
public enum WeightSource{Explicit,Default,Unknown}
public enum TieBreakReason{EffectivePriority,BaseRatio,Weight,RemainingTime,ArrivalSeq,JobId,Continuation}

internal static class EnumStr{
    public static string S(SelectionReason r)=>r switch{SelectionReason.QueueEmpty=>"queue_empty",SelectionReason.ShortestRemaining=>"shortest_remaining",SelectionReason.HighestWeightedPriority=>"highest_weighted_priority",SelectionReason.Fifo=>"fifo",SelectionReason.AgingBoost=>"aging_boost",SelectionReason.Continuation=>"continuation",_=>""};
    public static string E(EstimateSource e)=>e switch{EstimateSource.Explicit=>"explicit",EstimateSource.Historical=>"historical",EstimateSource.Default=>"default",EstimateSource.Unknown=>"unknown",_=>""};
    public static string W(WeightSource w)=>w switch{WeightSource.Explicit=>"explicit",WeightSource.Default=>"default",WeightSource.Unknown=>"unknown",_=>""};
    public static string T(TieBreakReason t)=>t switch{TieBreakReason.EffectivePriority=>"effective_priority",TieBreakReason.BaseRatio=>"base_ratio",TieBreakReason.Weight=>"weight",TieBreakReason.RemainingTime=>"remaining_time",TieBreakReason.ArrivalSeq=>"arrival_seq",TieBreakReason.JobId=>"job_id",TieBreakReason.Continuation=>"continuation",_=>""};
}

// ── Job ───────────────────────────────────────────────────────────────────

public sealed class Job
{
    public ulong Id; public double Weight,RemainingTime,TotalTime,ArrivalTime; public ulong ArrivalSeq;
    public EstimateSource EstimateSource; public WeightSource WeightSource; public string? Name;

    public static Job Create(ulong id,double weight,double estimatedTime)
    {
        weight=double.IsNaN(weight)?QSched.DefWMin:double.IsInfinity(weight)?(weight>0?QSched.DefWMax:QSched.DefWMin):Math.Clamp(weight,QSched.DefWMin,QSched.DefWMax);
        estimatedTime=double.IsNaN(estimatedTime)?QSched.DefPMax:double.IsInfinity(estimatedTime)?(estimatedTime>0?QSched.DefPMax:QSched.DefPMin):Math.Clamp(estimatedTime,QSched.DefPMin,QSched.DefPMax);
        return new Job{Id=id,Weight=weight,RemainingTime=estimatedTime,TotalTime=estimatedTime,EstimateSource=EstimateSource.Explicit,WeightSource=WeightSource.Explicit};
    }
    public static Job CreateNamed(ulong id,double w,double t,string name){var j=Create(id,w,t);j.Name=name;return j;}
    public Job WithSources(WeightSource ws,EstimateSource es){WeightSource=ws;EstimateSource=es;return this;}
    public double Progress=>TotalTime<=0?1:1-Math.Clamp(RemainingTime/TotalTime,0,1);
    public bool IsComplete=>RemainingTime<=0;
}

// ── PriorityJob (binary heap wrapper) ─────────────────────────────────────

sealed class PriorityJob : IComparable<PriorityJob>
{
    public double Priority,BaseRatio; public Job Job; public SchedulingMode Mode;
    public int CompareTo(PriorityJob? other)
    {
        if(other==null)return -1;
        // .NET PriorityQueue is min-heap, so negate for max-heap behavior
        if(Mode==SchedulingMode.Fifo||other.Mode==SchedulingMode.Fifo){
            int c=Job.ArrivalSeq.CompareTo(other.Job.ArrivalSeq);if(c!=0)return c;
            return Job.Id.CompareTo(other.Job.Id);}
        int r=Priority.CompareTo(other.Priority); if(r!=0)return -r;
        r=BaseRatio.CompareTo(other.BaseRatio); if(r!=0)return -r;
        r=Job.Weight.CompareTo(other.Job.Weight); if(r!=0)return -r;
        r=other.Job.RemainingTime.CompareTo(Job.RemainingTime); if(r!=0)return -r;
        r=other.Job.ArrivalSeq.CompareTo(Job.ArrivalSeq); if(r!=0)return -r;
        return other.Job.Id.CompareTo(Job.Id);
    }
}

// ── Evidence types ────────────────────────────────────────────────────────

public sealed class SchedulingEvidence
{
    public double CurrentTime; public ulong? SelectedJobId; public int QueueLength; public double MeanWaitTime,MaxWaitTime; public SelectionReason Reason; public TieBreakReason? TieBreakReason; public List<JobEvidence> Jobs=new();
    public string ToJsonl(string evt)
    {
        var sb=new StringBuilder(256+Jobs.Count*64);
        sb.Append("{\"event\":\"").Append(Evt(evt)).Append("\",\"current_time\":").AppendFormat("{0:F6}",CurrentTime);
        sb.Append(",\"selected_job_id\":").Append(SelectedJobId?.ToString()??"null");
        sb.Append(",\"queue_length\":").Append(QueueLength).Append(",\"mean_wait_time\":").AppendFormat("{0:F3}",MeanWaitTime);
        sb.Append(",\"max_wait_time\":").AppendFormat("{0:F3}",MaxWaitTime).Append(",\"reason\":\"").Append(EnumStr.S(Reason)).Append('"');
        sb.Append(",\"tie_break_reason\":").Append(TieBreakReason.HasValue?'"'+EnumStr.T(TieBreakReason.Value)+'"':"null");
        sb.Append(",\"jobs\":["); bool first=true;
        foreach(var j in Jobs){if(!first)sb.Append(',');first=false;sb.Append(j.ToJson());}
        sb.Append("]}"); return sb.ToString();
    }
    static string Evt(string s)=>s.Replace("\\","\\\\").Replace("\"","\\\"");
}
public sealed class JobEvidence
{
    public ulong JobId; public string? Name; public double EstimateMs,Weight,Ratio,AgingReward,StarvationFloor,AgeMs,EffectivePriority,ObjectiveLossProxy; public EstimateSource EstimateSource; public WeightSource WeightSource;
    public string ToJson()=>$"{{\"job_id\":{JobId},\"name\":{JsonStr(Name)},\"estimate_ms\":{EstimateMs:F3},\"weight\":{Weight:F3},\"ratio\":{Ratio:F6},\"aging_reward\":{AgingReward:F6},\"starvation_floor\":{StarvationFloor:F6},\"age_ms\":{AgeMs:F3},\"effective_priority\":{EffectivePriority:F6},\"objective_loss_proxy\":{ObjectiveLossProxy:F6},\"estimate_source\":\"{EnumStr.E(EstimateSource)}\",\"weight_source\":\"{EnumStr.W(WeightSource)}\"}}";
    static string JsonStr(string? s)=>s==null?"null":'"'+s.Replace("\\","\\\\").Replace("\"","\\\"")+'"';
}

// ── SchedulerStats ────────────────────────────────────────────────────────

public sealed class SchedulerStats
{
    public ulong TotalSubmitted,TotalCompleted,TotalRejected,TotalPreemptions; public double TotalProcessingTime,TotalResponseTime,MaxResponseTime; public int QueueLength;
    public static SchedulerStats Default=>new();
    public double MeanResponseTime=>TotalCompleted>0?TotalResponseTime/TotalCompleted:0;
    public double Throughput=>TotalProcessingTime>0?TotalCompleted/TotalProcessingTime:0;
}

// ── QueueingScheduler ─────────────────────────────────────────────────────

public sealed class QueueingScheduler
{
    SchedulerConfig _c; PriorityQueue<PriorityJob,PriorityJob> _q=new(); Job? _cur; double _t; ulong _nid=1,_aseq=1; SchedulerStats _s=SchedulerStats.Default;

    public QueueingScheduler(SchedulerConfig c){_c=c;}

    public ulong? Submit(double weight,double estimatedTime)=>SubmitNamed(weight,estimatedTime,null);
    public ulong? SubmitNamed(double weight,double estimatedTime,string? name)=>SubmitWithSources(weight,estimatedTime,WeightSource.Explicit,EstimateSource.Explicit,name);
    public ulong? SubmitWithSources(double weight,double estimatedTime,WeightSource ws,EstimateSource es,string? name)
    {
        if(_q.Count>=_c.MaxQueueSize){_s.TotalRejected++;return null;}
        var id=_nid++; var j=Job.Create(id,weight,estimatedTime); j.EstimateSource=es;j.WeightSource=ws;
        j.Weight=NormWeightSrc(j.Weight,j.WeightSource); j.RemainingTime=NormTimeSrc(j.RemainingTime,j.EstimateSource); j.TotalTime=j.RemainingTime;
        j.ArrivalTime=_t;j.ArrivalSeq=_aseq++;
        if(name!=null)j.Name=name;
        var pj=MakePJ(j); _q.Enqueue(pj,pj); _s.TotalSubmitted++;_s.QueueLength=_q.Count+(_cur!=null?1:0);
        if(_c.Preemptive)MayPreempt();
        return id;
    }

    public List<ulong> Tick(double delta)
    {
        var done=new List<ulong>();
        if(!double.IsFinite(delta)||delta<=0)return done;
        double rem=delta,now=_t,proc=0;
        while(rem>0){
            var job=_cur??(_q.Count>0?_q.Dequeue().Job:null);
            if(job==null){now+=rem;break;}
            double pt=Math.Min(rem,job.RemainingTime);job.RemainingTime-=pt;rem-=pt;now+=pt;proc+=pt;
            if(job.IsComplete){var rt=now-job.ArrivalTime;_s.TotalResponseTime+=rt;_s.MaxResponseTime=Math.Max(_s.MaxResponseTime,rt);_s.TotalCompleted++;done.Add(job.Id);}
            else _cur=job;
        }
        _s.TotalProcessingTime+=proc;_t=now;RefreshPrios();_s.QueueLength=_q.Count+(_cur!=null?1:0);return done;
    }

    public Job? PeekNext()=>_cur??(_q.Count>0?_q.Peek().Job:null);
    public SchedulingEvidence Evidence()
    {
        var(mw,mx)=WaitStats();
        var cands=new List<PriorityJob>();
        foreach(var (x, _) in _q.UnorderedItems)cands.Add(MakePJ(x.Job));
        if(_cur!=null)cands.Add(MakePJ(_cur));
        cands.Sort((a,b)=>{int c=b.Priority.CompareTo(a.Priority);if(c!=0)return c;c=b.BaseRatio.CompareTo(a.BaseRatio);if(c!=0)return c;c=b.Job.Weight.CompareTo(a.Job.Weight);if(c!=0)return c;c=a.Job.RemainingTime.CompareTo(b.Job.RemainingTime);if(c!=0)return c;c=b.Job.ArrivalSeq.CompareTo(a.Job.ArrivalSeq);if(c!=0)return c;return b.Job.Id.CompareTo(a.Job.Id);});
        ulong? sel=_cur?.Id??(cands.Count>0?cands[0].Job.Id:null);
        TieBreakReason? tbr=_cur!=null?TieBreakReason.Continuation:(cands.Count>1?TieBreak(cands[0],cands[1]):null);
        SelectionReason r=(_q.Count==0&&_cur==null)?SelectionReason.QueueEmpty:(_cur!=null?SelectionReason.Continuation:(_c.Mode==SchedulingMode.Fifo?SelectionReason.Fifo:(cands.Count>0?(WaitAging(cands[0])?SelectionReason.AgingBoost:(_c.SmithEnabled&&cands[0].Job.Weight>1?SelectionReason.HighestWeightedPriority:SelectionReason.ShortestRemaining)):SelectionReason.QueueEmpty)));
        var jobs=cands.Select(pj=>{
            double age=Math.Max(0,_t-pj.Job.ArrivalTime);var terms=CompTerms(pj.Job);
            return new JobEvidence{JobId=pj.Job.Id,Name=pj.Job.Name,EstimateMs=pj.Job.RemainingTime,Weight=pj.Job.Weight,Ratio=pj.BaseRatio,AgingReward=terms.aging,StarvationFloor=terms.starve,AgeMs=age,EffectivePriority=pj.Priority,ObjectiveLossProxy=1/Math.Max(pj.Priority,_c.WMin),EstimateSource=pj.Job.EstimateSource,WeightSource=pj.Job.WeightSource};
        }).ToList();
        return new SchedulingEvidence{CurrentTime=_t,SelectedJobId=sel,QueueLength=_q.Count+(_cur!=null?1:0),MeanWaitTime=mw,MaxWaitTime=mx,Reason=r,TieBreakReason=tbr,Jobs=jobs};
    }

    public SchedulerStats Stats(){var s=_s; s.QueueLength=_q.Count+(_cur!=null?1:0);return s;}
    public int MaxQueueSize=>_c.MaxQueueSize;
    public bool Cancel(ulong jid){if(_cur!=null&&_cur.Id==jid){_cur=null;_s.QueueLength=_q.Count;return true;}int ol=_q.Count;var items=_q.UnorderedItems.Where(x=>x.Item1.Job.Id!=jid).Select(x=>x.Item1).ToList();_q=new PriorityQueue<PriorityJob,PriorityJob>();foreach(var pj in items)_q.Enqueue(pj,pj);_s.QueueLength=_q.Count;return ol!=_q.Count;}
    public void Clear(){_q.Clear();_cur=null;_s.QueueLength=0;}
    public void Reset(){_q.Clear();_cur=null;_t=0;_nid=1;_aseq=1;_s=SchedulerStats.Default;}

    // Internal
    double NormWeight(double w)=>double.IsNaN(w)?_c.WMin:double.IsInfinity(w)?(w>0?_c.WMax:_c.WMin):Math.Clamp(w,_c.WMin,_c.WMax);
    double NormTime(double e)=>double.IsNaN(e)?_c.PMaxMs:double.IsInfinity(e)?(e>0?_c.PMaxMs:_c.PMinMs):Math.Clamp(e,_c.PMinMs,_c.PMaxMs);
    double NormWeightSrc(double w,WeightSource s)=>NormWeight(s switch{WeightSource.Explicit=>w,WeightSource.Default=>_c.WeightDefault,WeightSource.Unknown=>_c.WeightUnknown,_=>w});
    double NormTimeSrc(double e,EstimateSource s)=>NormTime(s switch{EstimateSource.Explicit or EstimateSource.Historical=>e,EstimateSource.Default=>_c.EstimateDefaultMs,EstimateSource.Unknown=>_c.EstimateUnknownMs,_=>e});
    double BaseRatio(Job j){if(_c.Mode==SchedulingMode.Fifo)return 0;double r=Math.Max(j.RemainingTime,_c.PMinMs),w=_c.Mode switch{SchedulingMode.Smith=>j.Weight,SchedulingMode.Srpt=>1,_=>0};return w/r;}
    (double aging,double starve,double eff) CompTerms(Job j)
    {
        if(_c.Mode==SchedulingMode.Fifo)return(0,0,0);
        double br=BaseRatio(j),wt=Math.Max(0,_t-j.ArrivalTime),ar=_c.AgingFactor*wt;
        double sf=(_c.WaitStarveMs>0&&wt>=_c.WaitStarveMs)?br*_c.StarveBoostRatio:0;
        return(ar,sf,Math.Max(br+ar,sf));
    }
    double CompPrio(Job j)=>CompTerms(j).eff;
    PriorityJob MakePJ(Job j){double br=BaseRatio(j);return new PriorityJob{Priority=CompPrio(j),BaseRatio=br,Job=j,Mode=_c.Mode};}
    TieBreakReason TieBreak(PriorityJob a,PriorityJob b)
    {
        if(_c.Mode==SchedulingMode.Fifo)return a.Job.ArrivalSeq!=b.Job.ArrivalSeq?TieBreakReason.ArrivalSeq:TieBreakReason.JobId;
        if(a.Priority!=b.Priority)return TieBreakReason.EffectivePriority;
        if(a.BaseRatio!=b.BaseRatio)return TieBreakReason.BaseRatio;
        if(a.Job.Weight!=b.Job.Weight)return TieBreakReason.Weight;
        if(a.Job.RemainingTime!=b.Job.RemainingTime)return TieBreakReason.RemainingTime;
        if(a.Job.ArrivalSeq!=b.Job.ArrivalSeq)return TieBreakReason.ArrivalSeq;
        return TieBreakReason.JobId;
    }
    bool WaitAging(PriorityJob pj){double wt=Math.Max(0,_t-pj.Job.ArrivalTime),ac=_c.AgingFactor*wt;return(_c.WaitStarveMs>0&&wt>=_c.WaitStarveMs)||ac>pj.BaseRatio*0.5;}
    void MayPreempt()
    {
        if(_c.Mode==SchedulingMode.Fifo||_cur==null||_q.Count==0)return;
        var cpj=MakePJ(_cur); var top=_q.Peek();
        if(top.CompareTo(cpj)<0){var old=_cur!;_cur=null;var pj=MakePJ(old);_q.Enqueue(pj,pj);_s.TotalPreemptions++;}
    }
    void RefreshPrios(){var items=_q.UnorderedItems.Select(x=>x.Item1.Job).ToList();_q.Clear();foreach(var j in items){var pj=MakePJ(j);_q.Enqueue(pj,pj);}}
    (double mean,double max) WaitStats()
    {
        double tot=0,mx=0;int n=0;
        foreach(var (x, _) in _q.UnorderedItems){double w=Math.Max(0,_t-x.Job.ArrivalTime);tot+=w;mx=Math.Max(mx,w);n++;}
        if(_cur!=null){double w=Math.Max(0,_t-_cur.ArrivalTime);tot+=w;mx=Math.Max(mx,w);n++;}
        return(n>0?tot/n:0,mx);
    }
}
