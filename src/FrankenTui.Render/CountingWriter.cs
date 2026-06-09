// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/counting_writer.rs
// Counting writer for tracking bytes emitted.

using System.Diagnostics;

namespace FrankenTui.Render;

public sealed class CountingWriter<T> where T : Stream
{
    readonly T _inner; ulong _bytes;
    public CountingWriter(T inner){_inner=inner;_bytes=0;}
    public ulong BytesWritten=>_bytes;
    public void ResetCounter(){_bytes=0;}
    public T Inner=>_inner;
    public T InnerMut=>_inner;
    public T IntoInner(){var i=_inner;_bytes=0;return i;}

    public void Write(byte[] buf,int offset,int count){_inner.Write(buf,offset,count);_bytes+=(ulong)count;}
    public void WriteAll(byte[] buf){_inner.Write(buf,0,buf.Length);_bytes+=(ulong)buf.Length;}
    public void Flush(){_inner.Flush();}
}

public sealed record PresentStats(ulong BytesEmitted,int CellsChanged,int RunCount,TimeSpan Duration)
{
    public static PresentStats Default=>new(0,0,0,TimeSpan.Zero);
    public double BytesPerCell=>CellsChanged==0?0:(double)BytesEmitted/CellsChanged;
    public double BytesPerRun=>RunCount==0?0:(double)BytesEmitted/RunCount;
    public bool WithinBudget=>BytesEmitted<=PresentBudget.ExpectedMaxBytes(CellsChanged,RunCount);
}

public static class PresentBudget
{
    public const ulong BytesPerCellMax=40,SyncOverhead=20,BytesPerCursorMove=10;
    public static ulong ExpectedMaxBytes(int cellsChanged,int runs)=>(ulong)runs*BytesPerCursorMove+(ulong)cellsChanged*BytesPerCellMax+SyncOverhead;
}

public sealed class StatsCollector
{
    readonly Stopwatch _sw;readonly int _cells,_runs;
    public StatsCollector(int cellsChanged,int runCount){_cells=cellsChanged;_runs=runCount;_sw=Stopwatch.StartNew();}
    public PresentStats Finish(ulong bytesEmitted)=>new(bytesEmitted,_cells,_runs,_sw.Elapsed);
}
