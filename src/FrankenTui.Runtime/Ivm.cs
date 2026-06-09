// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/ivm.rs
// Incremental View Maintenance — delta-propagation DAG for derived render state.

namespace FrankenTui.Runtime;

/// <summary>
/// A signed update tuple for IVM delta propagation.
/// Key, weight, and logical time uniquely identify a version.
/// </summary>
public readonly record struct IvmDelta(ulong Key,long Weight,ulong LogicalTime)
{
    public static IvmDelta Insert(ulong key,ulong lt)=>new(key,1,lt);
    public static IvmDelta Remove(ulong key,ulong lt)=>new(key,-1,lt);
    public bool IsInsert=>Weight>0; public bool IsRemove=>Weight<0;
}

/// <summary>
/// A view in the IVM DAG. Maintains a materialized index of (key → weight)
/// and propagates net-zero deltas downstream.
/// </summary>
public sealed class IvmView
{
    readonly Dictionary<ulong,long> _index=new();
    readonly List<Action<IvmDelta>> _downstream=new();
    ulong _logicalTime;

    public IvmView(){}
    public int Count=>_index.Count;

    /// <summary>Read current weight for a key.</summary>
    public long this[ulong key]=>_index.GetValueOrDefault(key);

    /// <summary>Subscribe to output deltas.</summary>
    public void Subscribe(Action<IvmDelta> handler)=>_downstream.Add(handler);

    /// <summary>Apply an input delta.</summary>
    public void Apply(IvmDelta delta)
    {
        _logicalTime=Math.Max(_logicalTime,delta.LogicalTime)+1;
        long current=_index.GetValueOrDefault(delta.Key);
        long next=current+delta.Weight;
        if(next==0)_index.Remove(delta.Key);else _index[delta.Key]=next;
        long netWeight=next-current;
        if(netWeight!=0){
            var outDelta=new IvmDelta(delta.Key,netWeight,_logicalTime);
            foreach(var d in _downstream)d(outDelta);
        }
    }

    /// <summary>Apply a batch of deltas.</summary>
    public void ApplyBatch(IEnumerable<IvmDelta> deltas){foreach(var d in deltas)Apply(d);}

    /// <summary>Get all keys with positive weight.</summary>
    public IEnumerable<ulong> Keys=>_index.Where(kv=>kv.Value>0).Select(kv=>kv.Key);

    /// <summary>Snapshot for debug.</summary>
    public Dictionary<ulong,long> Snapshot()=>new(_index);

    /// <summary>Clear all state.</summary>
    public void Clear(){_index.Clear();_logicalTime=0;}
}

/// <summary>
/// A transforming view. Applies a function to each delta before forwarding.
/// </summary>
public sealed class IvmTransform
{
    readonly Func<ulong,ulong> _mapKey;
    readonly List<Action<IvmDelta>> _downstream=new();
    ulong _logicalTime;

    public IvmTransform(Func<ulong,ulong> mapKey){_mapKey=mapKey;}

    public void Subscribe(Action<IvmDelta> handler)=>_downstream.Add(handler);
    public void Apply(IvmDelta delta)
    {
        _logicalTime=Math.Max(_logicalTime,delta.LogicalTime)+1;
        var mapped=new IvmDelta(_mapKey(delta.Key),delta.Weight,_logicalTime);
        foreach(var d in _downstream)d(mapped);
    }
}

/// <summary>
/// A filtering view. Only forwards deltas that pass the predicate.
/// </summary>
public sealed class IvmFilter
{
    readonly Func<ulong,bool> _predicate;
    readonly List<Action<IvmDelta>> _downstream=new();

    public IvmFilter(Func<ulong,bool> predicate){_predicate=predicate;}

    public void Apply(IvmDelta delta)
    {
        if(_predicate(delta.Key))foreach(var d in _downstream)d(delta);
    }
}

/// <summary>
/// A joining view. Joins two input sources on key and forwards result.
/// </summary>
public sealed class IvmJoin
{
    readonly Dictionary<ulong,long> _left=new(),_right=new();
    readonly List<Action<IvmDelta>> _downstream=new();

    public void ApplyLeft(IvmDelta d){Apply(d,_left,_right);}
    public void ApplyRight(IvmDelta d){Apply(d,_right,_left);}

    void Apply(IvmDelta delta,Dictionary<ulong,long> mine,Dictionary<ulong,long> other)
    {
        long cur=mine.GetValueOrDefault(delta.Key);
        long nxt=cur+delta.Weight;
        if(nxt==0)mine.Remove(delta.Key);else mine[delta.Key]=nxt;
        long otherW=other.GetValueOrDefault(delta.Key);
        bool wasActive=cur>0&&otherW>0;
        bool isActive=nxt>0&&otherW>0;
        if(wasActive!=isActive){
            var outD=new IvmDelta(delta.Key,isActive?1:-1,delta.LogicalTime);
            foreach(var d in _downstream)d(outD);
        }
    }
}

/// <summary>
/// IVM DAG builder — chains views together and propagates a batch.
/// </summary>
public sealed class IvmGraph
{
    readonly List<(IvmView view,List<IvmView> inputs)> _nodes=new();

    public IvmView AddNode(params IvmView[] inputs)
    {
        var v=new IvmView();_nodes.Add((v,inputs.ToList()));
        foreach(var inp in inputs)inp.Subscribe(d=>v.Apply(d));
        return v;
    }

    public void Propagate(IvmView source,IEnumerable<IvmDelta> deltas)
    {
        var batch=deltas.ToList();source.ApplyBatch(batch);
    }

    public void Clear(){foreach(var(v,_)in _nodes)v.Clear();}
}

/// <summary>Monotonic logical clock for IVM versioning.</summary>
public sealed class IvmClock
{
    ulong _time;
    public ulong Tick()=>++_time;
    public ulong Now=>_time;
}
