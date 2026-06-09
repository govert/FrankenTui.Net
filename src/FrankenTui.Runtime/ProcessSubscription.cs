// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/process_subscription.rs
// Process subscription for spawning and monitoring external processes.

using System.Diagnostics;
using System.Threading.Channels;

namespace FrankenTui.Runtime;

public abstract record ProcessEvent
{
    public sealed record Stdout(string Line) : ProcessEvent;
    public sealed record Stderr(string Line) : ProcessEvent;
    public sealed record Exited(int Code) : ProcessEvent;
    public sealed record Error(string Message) : ProcessEvent;
}

public sealed class ProcessSubscription<M> : ISubscription<M> where M : class
{
    readonly string _program; readonly List<string> _args=new(); readonly Dictionary<string,string> _env=new(); TimeSpan _timeout=TimeSpan.FromSeconds(30); SubId _id; readonly Func<ProcessEvent,M> _wrap;

    public ProcessSubscription(string program,Func<ProcessEvent,M> wrap){_program=program;_wrap=wrap;_id=new SubId(Hash(program));}
    public ProcessSubscription<M> Arg(string a){_args.Add(a);return this;}
    public ProcessSubscription<M> Env(string k,string v){_env[k]=v;return this;}
    public ProcessSubscription<M> Timeout(TimeSpan t){_timeout=t;return this;}
    public ProcessSubscription<M> WithId(SubId id){_id=id;return this;}
    public SubId Id=>_id;

    public void Run(ChannelWriter<M> writer,StopSignal stop)
    {
        try{
            var psi=new ProcessStartInfo(_program){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true};
            foreach(var a in _args)psi.ArgumentList.Add(a);
            foreach(var(k,v)in _env)psi.Environment[k]=v;
            using var p=Process.Start(psi)!;
            var sw=Stopwatch.StartNew();
            while(!stop.IsStopped&&!p.HasExited&&sw.Elapsed<_timeout){
                var line=p.StandardOutput.ReadLine();
                if(line!=null)writer.TryWrite(_wrap(new ProcessEvent.Stdout(line)));
                else System.Threading.Thread.Sleep(10);
            }
            if(!p.HasExited){try{p.Kill();}catch{}}
            p.WaitForExit();
            writer.TryWrite(_wrap(new ProcessEvent.Exited(p.ExitCode)));
        }catch(Exception ex){writer.TryWrite(_wrap(new ProcessEvent.Error(ex.Message)));}
    }

    static ulong Hash(string s){ulong h=0xcbf29ce484222325;foreach(char c in s){h^=(byte)c;h*=0x100000001b3;}return h;}
}
