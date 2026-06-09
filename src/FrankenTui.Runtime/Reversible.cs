// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/reversible.rs
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// Reversible computing primitives for undo.

namespace FrankenTui.Runtime;

public interface IReversible<S>{void Forward(ref S state);void Backward(ref S state);string Description=>"reversible op";}

public sealed class AddOp : IReversible<ulong>{readonly ulong _d;public AddOp(ulong d){_d=d;}public void Forward(ref ulong s){s+=_d;}public void Backward(ref ulong s){s-=_d;}public string Description=>"add";}
public sealed class XorOp : IReversible<ulong>{readonly ulong _m;public XorOp(ulong m){_m=m;}public void Forward(ref ulong s){s^=_m;}public void Backward(ref ulong s){s^=_m;}public string Description=>"xor";}

public sealed class MulOp : IReversible<double>
{
    readonly double _f; public MulOp(double factor){_f=factor;} public void Forward(ref double s){s*=_f;} public void Backward(ref double s){s/=_f;} public string Description=>"mul_f64";
}

public sealed class SwapOp<T> : IReversible<List<T>>
{
    readonly int _i,_j; public SwapOp(int i,int j){_i=i;_j=j;}
    public void Forward(ref List<T> s){var tmp=s[_i];s[_i]=s[_j];s[_j]=tmp;}
    public void Backward(ref List<T> s){var tmp=s[_i];s[_i]=s[_j];s[_j]=tmp;} public string Description=>"swap";
}

public sealed class SetOp<T> : IReversible<T>
{
    readonly T _n,_o; public SetOp(T old,T @new){_o=old;_n=@new;} public void Forward(ref T s){s=_n;} public void Backward(ref T s){s=_o;} public string Description=>"set";
}

public sealed class PushOp<T> : IReversible<List<T>>
{
    readonly T _v; public PushOp(T v){_v=v;} public void Forward(ref List<T> s){s.Add(_v);} public void Backward(ref List<T> s){s.RemoveAt(s.Count-1);} public string Description=>"push";
}

public sealed class InsertOp<T> : IReversible<List<T>>
{
    readonly int _i;readonly T _v; public InsertOp(int i,T v){_i=i;_v=v;} public void Forward(ref List<T> s){s.Insert(_i,_v);} public void Backward(ref List<T> s){s.RemoveAt(_i);} public string Description=>"insert";
}

public sealed class RemoveOp<T> : IReversible<List<T>>
{
    readonly int _i;readonly T? _r; public RemoveOp(int i,T? removed){_i=i;_r=removed;} public void Forward(ref List<T> s){s.RemoveAt(_i);} public void Backward(ref List<T> s){if(_r!=null)s.Insert(_i,_r);} public string Description=>"remove";
    public static RemoveOp<T> Capturing(List<T> s,int i)=>new(i,i<s.Count?s[i]:default);
}

public sealed class Sequence<S> : IReversible<S>
{
    readonly List<IReversible<S>> _ops;readonly string _label;
    public Sequence(List<IReversible<S>> ops,string label="sequence"){_ops=ops;_label=label;}
    public static Sequence<S> Empty=>new(new());
    public Sequence<S> WithLabel(string l)=>new(_ops,l);
    public void Push(IReversible<S> op)=>_ops.Add(op);
    public int Count=>_ops.Count; public bool IsEmpty=>_ops.Count==0;
    public void Forward(ref S s){foreach(var o in _ops)o.Forward(ref s);}
    public void Backward(ref S s){for(int i=_ops.Count-1;i>=0;i--)_ops[i].Backward(ref s);}
    public string Description=>_label;
}

public sealed class Journal<S>
{
    readonly List<IReversible<S>> _applied=new(),_undone=new();
    public void Apply(IReversible<S> op,ref S s){op.Forward(ref s);_applied.Add(op);_undone.Clear();}
    public bool Undo(ref S s){if(_applied.Count==0)return false;var op=_applied[^1];_applied.RemoveAt(_applied.Count-1);op.Backward(ref s);_undone.Add(op);return true;}
    public bool Redo(ref S s){if(_undone.Count==0)return false;var op=_undone[^1];_undone.RemoveAt(_undone.Count-1);op.Forward(ref s);_applied.Add(op);return true;}
    public int UndoCount=>_applied.Count; public int RedoCount=>_undone.Count;
    public void Clear(){_applied.Clear();_undone.Clear();}
}
