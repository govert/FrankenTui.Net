// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/schedule_trace.rs
// Deterministic golden trace for async task manager testing.

namespace FrankenTui.Runtime;

public abstract record WakeupReason{public sealed record Timer:WakeupReason;public sealed record IoReady:WakeupReason;public sealed record Dependency(ulong TaskId):WakeupReason;public sealed record UserAction:WakeupReason;public sealed record Explicit:WakeupReason;public sealed record Other(string Reason):WakeupReason;}
public abstract record CancelReason{public sealed record UserRequest:CancelReason;public sealed record Timeout:CancelReason;public sealed record HazardPolicy(double ExpectedLoss):CancelReason;public sealed record Shutdown:CancelReason;public sealed record Other(string Reason):CancelReason;}

public enum SchedulerPolicy{Fifo,Priority,ShortestFirst,RoundRobin,WeightedFair}

public abstract record TaskEvent
{
    public sealed record Spawn(ulong TaskId,byte Priority,string? Name):TaskEvent;
    public sealed record Start(ulong TaskId):TaskEvent;
    public sealed record Yield(ulong TaskId):TaskEvent;
    public sealed record Wakeup(ulong TaskId,WakeupReason Reason):TaskEvent;
    public sealed record Complete(ulong TaskId):TaskEvent;
    public sealed record Failed(ulong TaskId,string Error):TaskEvent;
    public sealed record Cancelled(ulong TaskId,CancelReason Reason):TaskEvent;
    public sealed record PolicyChange(SchedulerPolicy From,SchedulerPolicy To):TaskEvent;
    public sealed record QueueSnapshot(int Queued,int Running):TaskEvent;
    public sealed record Custom(string Tag,string Data):TaskEvent;
}

public sealed record TraceEntry(ulong Seq,ulong Tick,TaskEvent Event)
{
    public string ToJsonl()
    {
        string et=Event switch{TaskEvent.Spawn=>"spawn",TaskEvent.Start=>"start",TaskEvent.Yield=>"yield",TaskEvent.Wakeup=>"wakeup",TaskEvent.Complete=>"complete",TaskEvent.Failed=>"failed",TaskEvent.Cancelled=>"cancelled",TaskEvent.PolicyChange=>"policy_change",TaskEvent.QueueSnapshot=>"queue_snapshot",TaskEvent.Custom=>"custom",_=>"unknown"};
        string details=Event switch{
            TaskEvent.Spawn s=>$"\"task_id\":{s.TaskId},\"priority\":{s.Priority},\"name\":{(s.Name==null?"null":$"\"{s.Name}\"")}",
            TaskEvent.Start s=>$"\"task_id\":{s.TaskId}",
            TaskEvent.Yield s=>$"\"task_id\":{s.TaskId}",
            TaskEvent.Wakeup w=>$"\"task_id\":{w.TaskId},\"reason\":\"{WReason(w.Reason)}\"",
            TaskEvent.Complete s=>$"\"task_id\":{s.TaskId}",
            TaskEvent.Failed s=>$"\"task_id\":{s.TaskId},\"error\":\"{s.Error}\"",
            TaskEvent.Cancelled c=>$"\"task_id\":{c.TaskId},\"reason\":\"{CReason(c.Reason)}\"",
            TaskEvent.PolicyChange p=>$"\"from\":\"{PolStr(p.From)}\",\"to\":\"{PolStr(p.To)}\"",
            TaskEvent.QueueSnapshot q=>$"\"queued\":{q.Queued},\"running\":{q.Running}",
            TaskEvent.Custom c=>$"\"tag\":\"{c.Tag}\",\"data\":\"{c.Data}\"",
            _=>""
        };
        return $"{{\"seq\":{Seq},\"tick\":{Tick},\"event\":\"{et}\",{details}}}";
    }
    static string WReason(WakeupReason r)=>r switch{WakeupReason.Timer=>"timer",WakeupReason.IoReady=>"io_ready",WakeupReason.Dependency d=>$"dependency:{d.TaskId}",WakeupReason.UserAction=>"user_action",WakeupReason.Explicit=>"explicit",WakeupReason.Other o=>$"other:{o.Reason}",_=>""};
    static string CReason(CancelReason r)=>r switch{CancelReason.UserRequest=>"user_request",CancelReason.Timeout=>"timeout",CancelReason.HazardPolicy h=>$"hazard_policy:{h.ExpectedLoss:F4}",CancelReason.Shutdown=>"shutdown",CancelReason.Other o=>$"other:{o.Reason}",_=>""};
    static string PolStr(SchedulerPolicy p)=>p switch{SchedulerPolicy.Fifo=>"fifo",SchedulerPolicy.Priority=>"priority",SchedulerPolicy.ShortestFirst=>"shortest_first",SchedulerPolicy.RoundRobin=>"round_robin",SchedulerPolicy.WeightedFair=>"weighted_fair",_=>""};
}

public sealed class TraceConfig
{
    public int MaxEntries=10000; public bool AutoSnapshot; public VoiConfig? SnapshotSampling; public int SnapshotChangeThreshold=1; public ulong Seed;
    public static TraceConfig Default=>new();
}

public sealed class ScheduleTrace
{
    TraceConfig _c; Queue<TraceEntry> _entries=new(); ulong _seq,_tick; VoiSampler? _ss;(int,int)? _lastSnap;

    public ScheduleTrace(TraceConfig? c=null){_c=c??TraceConfig.Default;}
    public ulong Tick=>_tick;
    public void AdvanceTick(){_tick++;} public void SetTick(ulong t){_tick=t;}
    public void Record(TaskEvent e)
    {
        _seq++;
        if(_c.MaxEntries>0&&_entries.Count>=_c.MaxEntries)_entries.Dequeue();
        _entries.Enqueue(new TraceEntry(_seq,_tick,e));
    }
    public ulong EntryCount=>(ulong)_entries.Count;
    public ulong TotalSeq=>_seq;
    public List<TraceEntry> Entries=>_entries.ToList();

    public ulong Checksum()
    {
        const ulong OFF=0xcbf29ce484222325,PRIME=0x100000001b3;
        ulong hh=OFF;
        void H(ulong v){hh^=v;hh*=PRIME;}
        void HS(string s){foreach(char c in s){hh^=(byte)c;hh*=PRIME;}}
        H(_seq);H(_tick);
        foreach(var e in _entries){
            H(e.Seq);H(e.Tick);
            switch(e.Event){
                case TaskEvent.Spawn s:H(1);H(s.TaskId);H(s.Priority);if(s.Name!=null)HS(s.Name);break;
                case TaskEvent.Start s:H(2);H(s.TaskId);break;
                case TaskEvent.Yield s:H(3);H(s.TaskId);break;
                case TaskEvent.Wakeup w:H(4);H(w.TaskId);H((ulong)(w.Reason is WakeupReason.Dependency d?d.TaskId.GetHashCode():w.Reason.GetType().Name.GetHashCode()));break;
                case TaskEvent.Complete s:H(5);H(s.TaskId);break;
                case TaskEvent.Failed s:H(6);H(s.TaskId);HS(s.Error);break;
                case TaskEvent.Cancelled c:H(7);H(c.TaskId);H((ulong)(c.Reason is CancelReason.HazardPolicy hp?hp.ExpectedLoss.GetHashCode():c.Reason.GetType().Name.GetHashCode()));break;
                case TaskEvent.PolicyChange p:H(8);H((ulong)p.From);H((ulong)p.To);break;
                case TaskEvent.QueueSnapshot q:H(9);H((ulong)q.Queued);H((ulong)q.Running);break;
                case TaskEvent.Custom c:H(10);HS(c.Tag);HS(c.Data);break;
            }
        }
        return hh;
    }

    public string ToJsonl()=>string.Join("\n",_entries.Select(e=>e.ToJsonl()));
    public void Clear(){_entries.Clear();_seq=0;_tick=0;_lastSnap=null;}
}
