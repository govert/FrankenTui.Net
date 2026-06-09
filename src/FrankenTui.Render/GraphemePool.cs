// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/grapheme_pool.rs
// Grapheme interning pool. Uses existing GraphemeId with slot+gen+width.

namespace FrankenTui.Render;

public sealed class GraphemePool
{
    readonly record struct Entry(string Text,string Key,uint RefCount);
    readonly List<Entry?> _entries=new(){null}; readonly Dictionary<string,uint> _lookup=new(); Stack<uint> _free=new();

    public int Count=>_lookup.Count; public bool IsEmpty=>_lookup.Count==0;

    public GraphemeId Intern(string text,byte width)
    {
        string key=$"{text}|{width}";
        if(_lookup.TryGetValue(key,out var slot)){var e=_entries[(int)slot]!.Value;_entries[(int)slot]=new(e.Text,e.Key,e.RefCount+1);return new GraphemeId(slot,0,width);}
        uint nid;
        if(_free.Count>0){nid=_free.Pop();_entries[(int)nid]=new Entry(text,key,1);}
        else{nid=(uint)_entries.Count;if(nid>GraphemeId.MaxSlot)return default;_entries.Add(new Entry(text,key,1));}
        _lookup[key]=nid;return new GraphemeId(nid,0,width);
    }

    public string? Get(GraphemeId id)=>id.Slot<_entries.Count?_entries[id.Slot]?.Text:null;
    public uint RefCount(GraphemeId id)=>id.Slot<_entries.Count?(uint)(_entries[id.Slot]?.RefCount??0):0;
    public void Retain(GraphemeId id){if(id.Slot<_entries.Count&&_entries[id.Slot]is{}e)_entries[id.Slot]=new(e.Text,e.Key,e.RefCount+1);}
    public void Release(GraphemeId id)
    {
        if(id.Slot>=_entries.Count||_entries[id.Slot]is not{}e)return;
        if(e.RefCount<=1){_lookup.Remove(e.Key);_entries[id.Slot]=null;_free.Push((uint)id.Slot);}else _entries[id.Slot]=new(e.Text,e.Key,e.RefCount-1);
    }
    public void Clear(){_entries.Clear();_entries.Add(null);_lookup.Clear();_free.Clear();}
    // DIVERGENCE: gc(Buffer[]) depends on Buffer — deferred
}
