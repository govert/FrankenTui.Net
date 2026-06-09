// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/link_registry.rs
// OSC 8 hyperlink registry. Maps compact link IDs to URLs.

namespace FrankenTui.Render;

public sealed class LinkRegistry
{
    const uint MaxLinkId=0x00FF_FFFF; const int MaxUrlBytes=4096;
    readonly List<string?> _links=new(){null}; readonly Dictionary<string,uint> _lookup=new(); readonly Stack<uint> _free=new();

    static bool IsSafeOsc8Url(string url)=>url.Length<=MaxUrlBytes&&!url.Any(char.IsControl);

    public uint Register(string url)
    {
        if(!IsSafeOsc8Url(url))return 0;
        if(_lookup.TryGetValue(url,out var id))return id;
        uint nid;
        if(_free.Count>0){nid=_free.Pop();_links[(int)nid]=url;}
        else{if(_links.Count>MaxLinkId)return 0;nid=(uint)_links.Count;_links.Add(url);}
        if(nid==0||nid>MaxLinkId)return 0;
        _lookup[url]=nid;return nid;
    }

    public string? Get(uint id)=>id<_links.Count?_links[(int)id]:null;
    public void Remove(uint id){if(id<_links.Count&&_links[(int)id]!=null){_lookup.Remove(_links[(int)id]!);_links[(int)id]=null;_free.Push(id);}}
    public int Count=>_lookup.Count;
    public void Clear(){_links.Clear();_links.Add(null);_lookup.Clear();_free.Clear();}
}
