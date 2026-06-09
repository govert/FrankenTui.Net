// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/terminal_writer.rs
// Terminal output coordinator with inline mode, diff strategy, and one-writer rule.

using System.Text;
using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Runtime;

public enum UiAnchor{Bottom,Top}
public abstract record ScreenMode{public sealed record Inline(ushort UiHeight):ScreenMode;public sealed record AltScreen:ScreenMode;}

public sealed class PresentTimings{public double DiffUs,CloneUs,PresentUs,TotalUs;}

public sealed class RuntimeDiffConfig
{
    public bool BayesianEnabled=true,DirtyRowsEnabled=true,DirtySpansEnabled=true,TileSkipEnabled,ResetOnResize=true,ResetOnInvalidation=true;
    public DirtySpanConfig? DirtySpanCfg; public TileDiffConfig? TileDiffCfg; public DiffStrategyConfig? StrategyCfg;
    public static RuntimeDiffConfig Default=>new();
    public RuntimeDiffConfig WithBayesianEnabled(bool e){BayesianEnabled=e;return this;}
    public RuntimeDiffConfig WithDirtyRowsEnabled(bool e){DirtyRowsEnabled=e;return this;}
    public RuntimeDiffConfig WithDirtySpansEnabled(bool e){DirtySpansEnabled=e;return this;}
    public RuntimeDiffConfig WithDirtySpanConfig(DirtySpanConfig? c){DirtySpanCfg=c;return this;}
    public RuntimeDiffConfig WithTileSkipEnabled(bool e){TileSkipEnabled=e;return this;}
    public RuntimeDiffConfig WithTileDiffConfig(TileDiffConfig? c){TileDiffCfg=c;return this;}
    public RuntimeDiffConfig WithResetOnResize(bool e){ResetOnResize=e;return this;}
    public RuntimeDiffConfig WithStrategyConfig(DiffStrategyConfig c){StrategyCfg=c;return this;}
}

public sealed class InlineRegion(ushort Top,ushort Bottom,ushort Height){public ushort Top=Top,Bottom=Bottom,Height=Height;}

public sealed class TerminalWriter : IDisposable
{
    static int _inlineCount;
    public static int InlineActiveWidgets=>Volatile.Read(ref _inlineCount);

    readonly TextWriter _inner; ScreenMode _screenMode; UiAnchor _uiAnchor; TerminalCapabilities _caps;
    ushort _termWidth=80,_termHeight=24; bool _inSync,_cursorSaved,_cursorVisible=true,_scrollActive;
    InlineStrategy _inlineStrategy; InlineRegion? _lastInlineRegion; FrankenTui.Render.Buffer? _prevBuffer,_spareBuffer,_cloneBuf;
    GraphemePool _pool=new(); LinkRegistry _links=new(); DiffStrategySelector _diffStrat=DiffStrategySelector.WithDefaults();
    BufferDiff _diffScratch=new(); RuntimeDiffConfig _diffCfg=RuntimeDiffConfig.Default;
    EvidenceSink? _evSink; DiffStrategy? _lastDiffStrat; ulong _fullRedrawProbe; PresentTimings? _lastTimings;
    bool _disposed;

    public TerminalWriter(TextWriter writer,ScreenMode mode,UiAnchor anchor,TerminalCapabilities caps,RuntimeDiffConfig? diffCfg=null)
    {
        _inner=writer;_screenMode=mode;_uiAnchor=anchor;_caps=caps;_diffCfg=diffCfg??RuntimeDiffConfig.Default;
        _diffStrat=new DiffStrategySelector(_diffCfg.StrategyCfg??DiffStrategyConfig.Default);
        _inlineStrategy=InlineMode.Select(caps);
        if(mode is ScreenMode.Inline or ScreenMode.AltScreen)Interlocked.Increment(ref _inlineCount);
    }

    public ushort Width=>_termWidth; public ushort Height=>_termHeight; public ScreenMode ScreenMode=>_screenMode;
    public RuntimeDiffConfig DiffConfig=>_diffCfg; public DiffStrategySelector DiffStrategy=>_diffStrat;
    public DiffStrategy? LastDiffStrategy=>_lastDiffStrat; public GraphemePool Pool=>_pool; public LinkRegistry Links=>_links;
    public TerminalCapabilities Capabilities=>_caps;

    public void SetSize(ushort w,ushort h){
        _termWidth=(ushort)Math.Max(1,(int)w);_termHeight=(ushort)Math.Max(1,(int)h);
        if(_diffCfg.ResetOnResize){_prevBuffer=null;_lastInlineRegion=null;ResetDiffStrategy();}
    }

    public FrankenTui.Render.Buffer TakeRenderBuffer(ushort w,ushort h)
    {
        if(_spareBuffer is{}b&&b.Width==w&&b.Height==h){_spareBuffer=null;return b;}
        return new FrankenTui.Render.Buffer(w,h);
    }

    public ushort RenderHeightHint=>_screenMode switch{ScreenMode.Inline i=>Math.Min(i.UiHeight,_termHeight),ScreenMode.AltScreen=>_termHeight,_=>_termHeight};
    public ushort UiHeight=>_screenMode switch{ScreenMode.Inline i=>i.UiHeight,_=>_termHeight};
    public InlineStrategy InlineStrategy=>_inlineStrategy; public bool ScrollRegionActive=>_scrollActive;

    public TerminalWriter WithDiffConfig(RuntimeDiffConfig c){_diffCfg=c;return this;}
    public TerminalWriter WithEvidenceSink(EvidenceSink s){_evSink=s;return this;}
    public void SetEvidenceSink(EvidenceSink? s){_evSink=s;}

    void ResetDiffStrategy(){_diffStrat.Reset();_fullRedrawProbe=0;_lastDiffStrat=null;}

    public void WriteLog(string text)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        if(_screenMode is ScreenMode.AltScreen)return;
        string safe=OutputSanitizer.Sanitize(text);
        if(_screenMode is ScreenMode.Inline i){
            if(!PositionCursorForLog(i.UiHeight))return;
            if(!_scrollActive){_prevBuffer=null;_lastInlineRegion=null;ResetDiffStrategy();}
        }
        _inner.Write(safe);
    }

    bool PositionCursorForLog(ushort uiH)
    {
        ushort vis=Math.Min(uiH,_termHeight); if(vis>=_termHeight)return false;
        ushort row=_uiAnchor==UiAnchor.Bottom?(ushort)(_termHeight-vis):_termHeight;
        _inner.Write($"\x1b[{row};1H"); return true;
    }

    public void ClearScreen()
    {
        if(_inSync&&_caps.UseSyncOutput())_inner.Write("\x1b[?2026l");_inSync=false;
        if(_cursorSaved)_inner.Write("\x1b8");_cursorSaved=false;
        if(_scrollActive)_inner.Write("\x1b[r");_scrollActive=false;
        _inner.Write("\x1b[2J\x1b[1;1H");_inner.Flush();
        _prevBuffer=null;_lastInlineRegion=null;ResetDiffStrategy();
    }

    public void HideCursor(){if(_cursorVisible){_cursorVisible=false;_inner.Write("\x1b[?25l");_inner.Flush();}}
    public void ShowCursor(){if(!_cursorVisible){_cursorVisible=true;_inner.Write("\x1b[?25h");_inner.Flush();}}
    public void Flush(){_inner.Flush();}

    public void PresentUi(FrankenTui.Render.Buffer buffer,ushort rowOffset=0,ushort colOffset=0,bool wrapSync=false)
    {
        var diff=BufferDiff.Compute(_prevBuffer??buffer,buffer);
        var presenter=new Presenter(_caps);
        var result=presenter.Present(buffer,diff,null,rowOffset,colOffset,wrapSync);
        _inner.Write(result.Output);_inner.Flush();
        int cells=diff.Changes.Count,scanned=diff.ScanStats?.CellsScanned??buffer.Width*buffer.Height;
        _diffStrat.Observe(scanned,cells);
        _lastDiffStrat=_diffStrat.Select(buffer.Width,buffer.Height,buffer.DirtyRowCount);
        if(_evSink!=null){var ev=_diffStrat.LastEvidence;if(ev!=null)_evSink.WriteJsonl(ev.ToJsonl());}
        _prevBuffer=TakeRenderBuffer(buffer.Width,buffer.Height);
        _prevBuffer.CopyFrom(buffer);
        buffer.ClearDirty();
        if(buffer.Width!=_termWidth||buffer.Height!=RenderHeightHint)SetSize(buffer.Width,buffer.Height);
    }

    public TextWriter? IntoInner()
    {
        if(_disposed)return null;_disposed=true;
        try{_inner.Flush();}catch{}return _inner;
    }

    public void Dispose()
    {
        if(_disposed)return;_disposed=true;
        if(_screenMode is ScreenMode.Inline or ScreenMode.AltScreen)Interlocked.Decrement(ref _inlineCount);
        if(_inSync&&_caps.UseSyncOutput())_inner.Write("\x1b[?2026l");_inSync=false;
        if(_cursorSaved)_inner.Write("\x1b8");_cursorSaved=false;
        if(_scrollActive)_inner.Write("\x1b[r");_scrollActive=false;
        _inner.Write("\x1b[?25h");_cursorVisible=true;
        try{_inner.Flush();}catch{}
    }
}
