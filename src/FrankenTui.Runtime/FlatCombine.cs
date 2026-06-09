// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/flat_combine.rs
// Flat combining for batched operation dispatch. Reference-type state only.

namespace FrankenTui.Runtime;

public sealed class CombinerStats
{
    public ulong CombinePasses,TotalOps; public int MaxBatchSize; public ulong ContentionEvents;
    public double AvgBatchSize=>CombinePasses==0?0:(double)TotalOps/CombinePasses;
}

public sealed class FlatCombiner<S> where S : class
{
    readonly object _sl=new(); readonly S _s; readonly object _ql=new(); Queue<Action<S>> _q=new(); long _gen; CombinerStats _st=new(); int _owner;

    public FlatCombiner(S state){_s=state;}
    public R Execute<R>(Func<S,R> op){lock(_sl)return op(_s);}
    public R WithState<R>(Func<S,R> f){lock(_sl)return f(_s);}
    public void Submit(Action<S> op){lock(_ql)_q.Enqueue(op);}
    public int PendingCount{get{lock(_ql)return _q.Count;}}

    public int Combine()
    {
        List<Action<S>> ops; lock(_ql){ops=_q.ToList();_q.Clear();}
        if(ops.Count==0)return 0;
        lock(_sl)foreach(var o in ops)o(_s);
        Interlocked.Increment(ref _gen);
        lock(_sl){_st.CombinePasses++;_st.TotalOps+=(ulong)ops.Count;_st.MaxBatchSize=Math.Max(_st.MaxBatchSize,ops.Count);}
        return ops.Count;
    }

    public ulong Generation=>(ulong)Interlocked.Read(ref _gen);
    public CombinerStats Stats{get{lock(_sl)return new CombinerStats{CombinePasses=_st.CombinePasses,TotalOps=_st.TotalOps,MaxBatchSize=_st.MaxBatchSize,ContentionEvents=_st.ContentionEvents};}}
    public void ResetStats(){lock(_sl)_st=new CombinerStats();}
}
