// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/render_trace.rs
// Render trace recorder for deterministic replay and golden testing.

namespace FrankenTui.Runtime;

public sealed class RenderTraceConfig
{
    public bool Enabled; public string? FilePath,RunId,TestModule; public ulong Seed; public bool FlushOnWrite,StartTsMs;
    public static RenderTraceConfig Default=>new();
    public static RenderTraceConfig EnabledFile(string path)=>new(){Enabled=true,FilePath=path};
    public RenderTraceConfig WithRunId(string id){RunId=id;return this;}
    public RenderTraceConfig WithSeed(ulong s){Seed=s;return this;}
}

public enum RenderTracePayloadKind{FullBuffer,DiffRuns,FrameTiming}

public sealed class RenderTraceFrame
{
    public ulong FrameIdx,TimestampUs; public RenderTracePayloadKind Kind; public string Payload="";
    public string ToJsonl()=>$"{{\"frame_idx\":{FrameIdx},\"ts_us\":{TimestampUs},\"kind\":\"{Kind}\",\"payload\":{Payload}}}";
}

public sealed class RenderTraceRecorder
{
    RenderTraceConfig _c; readonly List<RenderTraceFrame> _frames=new(); ulong _idx; System.Diagnostics.Stopwatch _sw=System.Diagnostics.Stopwatch.StartNew();

    public RenderTraceRecorder(RenderTraceConfig? c=null){_c=c??RenderTraceConfig.Default;}
    public void RecordFullBuffer(string payload){if(!_c.Enabled)return;_frames.Add(new RenderTraceFrame{FrameIdx=_idx++,TimestampUs=(ulong)(_sw.Elapsed.TotalMilliseconds*1000),Kind=RenderTracePayloadKind.FullBuffer,Payload=payload});}
    public void RecordDiffRuns(string payload){if(!_c.Enabled)return;_frames.Add(new RenderTraceFrame{FrameIdx=_idx++,TimestampUs=(ulong)(_sw.Elapsed.TotalMilliseconds*1000),Kind=RenderTracePayloadKind.DiffRuns,Payload=payload});}
    public void RecordFrameTiming(string payload){if(!_c.Enabled)return;_frames.Add(new RenderTraceFrame{FrameIdx=_idx++,TimestampUs=(ulong)(_sw.Elapsed.TotalMilliseconds*1000),Kind=RenderTracePayloadKind.FrameTiming,Payload=payload});}
    public string ToJsonl()=>string.Join("\n",_frames.Select(f=>f.ToJsonl()));
    public void Finish(string? path){if(_c.Enabled&&_c.FilePath!=null)System.IO.File.WriteAllText(_c.FilePath,ToJsonl());}
    public IReadOnlyList<RenderTraceFrame> Frames=>_frames;
}
