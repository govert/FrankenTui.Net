// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/render_thread.rs
// Optional dedicated render/output thread (Mode B).

using System.Threading.Channels;

namespace FrankenTui.Runtime;

public abstract record OutMsg
{
    public sealed record Log(byte[] Data) : OutMsg;
    public sealed record Render(FrankenTui.Render.Buffer Buffer, (ushort,ushort)? Cursor, bool CursorVisible) : OutMsg;
    public sealed record Resize(ushort W, ushort H) : OutMsg;
    public sealed record SetMode(ScreenMode Mode) : OutMsg;
    public sealed record Shutdown : OutMsg;
}

public sealed class RenderThread : IDisposable
{
    const int LogChunkLimit=64,ChannelCapacity=256;
    readonly ChannelWriter<OutMsg>? _tx; readonly Thread _thread; readonly ChannelReader<Exception> _errRx;

    RenderThread(ChannelWriter<OutMsg> tx,Thread thread,ChannelReader<Exception> errRx){_tx=tx;_thread=thread;_errRx=errRx;}

    public static RenderThread Start(TerminalWriter writer)
    {
        var ch=Channel.CreateBounded<OutMsg>(ChannelCapacity);
        var errCh=Channel.CreateBounded<Exception>(8);
        var thread=new Thread(()=>RenderLoop(writer,ch.Reader,errCh.Writer)){Name="ftui-render",IsBackground=true};
        thread.Start();
        return new RenderThread(ch.Writer,thread,errCh.Reader);
    }

    public bool Send(OutMsg msg)=>_tx?.TryWrite(msg)??false;
    public Exception? CheckError(){_errRx.TryRead(out var e);return e;}
    public void Shutdown(){_tx?.TryWrite(new OutMsg.Shutdown());_tx?.Complete();_thread.Join(250);}
    public void Dispose(){Shutdown();}

    static void RenderLoop(TerminalWriter writer,ChannelReader<OutMsg> rx,ChannelWriter<Exception> errTx)
    {
        var logs=new List<byte[]>(); OutMsg? latestRender=null; ulong loopCount=0;
        while(true)
        {
            if(!rx.TryRead(out var first))break; loopCount++;
            logs.Clear(); latestRender=null; bool shutdown=false;
            Process(first,logs,ref latestRender,writer,ref shutdown,errTx);
            while(!shutdown&&rx.TryRead(out var msg))Process(msg,logs,ref latestRender,writer,ref shutdown,errTx);
            foreach(var l in logs.Take(LogChunkLimit))writer.WriteLog(System.Text.Encoding.UTF8.GetString(l));
            if(latestRender is OutMsg.Render r)writer.PresentUi(r.Buffer);
            if(shutdown)break;
        }
        writer.Flush();
    }

    static void Process(OutMsg msg,List<byte[]> logs,ref OutMsg? latestRender,TerminalWriter w,ref bool shutdown,ChannelWriter<Exception> errTx)
    {
        try{
            switch(msg){
                case OutMsg.Log l:logs.Add(l.Data);break;
                case OutMsg.Render r:latestRender=r;break;
                case OutMsg.Resize rs:w.SetSize(rs.W,rs.H);break;
                case OutMsg.SetMode sm:/* stubbed */break;
                case OutMsg.Shutdown:shutdown=true;break;
            }
        }catch(Exception ex){errTx.TryWrite(ex);}
    }
}
