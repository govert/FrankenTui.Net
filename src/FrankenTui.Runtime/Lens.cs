// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/lens.rs
// Bidirectional lenses for state-widget binding.

namespace FrankenTui.Runtime;

public sealed class Lens<S,A>
{
    readonly Func<S,A> _get; readonly Func<S,A,S> _set;
    public Lens(Func<S,A> get,Func<S,A,S> set){_get=get;_set=set;}
    public A Get(S s)=>_get(s);
    public S Set(S s,A a)=>_set(s,a);
    public S Update(S s,Func<A,A> f)=>_set(s,f(_get(s)));
    public Lens<S,B> Compose<B>(Lens<A,B> inner)=>new(s=>inner.Get(_get(s)),(s,b)=>_set(s,inner.Set(_get(s),b)));
    public static Lens<S,A> Identity(Func<S,A> get,Func<S,A,S> set)=>new(get,set);
}

public sealed class LensList
{
    public static Lens<List<T>,T> At<T>(int index)=>new(l=>l[index],(l,v)=>{l[index]=v;return l;});
    public static Lens<Dictionary<K,V>,V> AtKey<K,V>(K key)where K:notnull=>new(d=>d[key],(d,v)=>{d[key]=v;return d;});
}
