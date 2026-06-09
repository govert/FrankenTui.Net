// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/program.rs (§Cmd, §TaskSpec)
// Upstream commit: f958e59e1406a90fdb92512103e3591911a9d68c
// Full Cmd<T> enum with all 12 variants.

namespace FrankenTui.Runtime;

public sealed class TaskSpec
{
    public double Weight{get;init;}=1.0; public double EstimateMs{get;init;}=0.0; public string? Name{get;init;}
    public static TaskSpec Create(double w,double e)=>new(){Weight=w,EstimateMs=e};
    public TaskSpec WithName(string n)=>new(){Weight=Weight,EstimateMs=EstimateMs,Name=n};
}

public abstract record Cmd<T>
{
    public sealed record None:Cmd<T>;
    public sealed record Quit:Cmd<T>;
    public sealed record Batch(List<Cmd<T>> Items):Cmd<T>;
    public sealed record Sequence(List<Cmd<T>> Items):Cmd<T>;
    public sealed record Msg(T Message):Cmd<T>;
    public sealed record Tick(TimeSpan Duration):Cmd<T>;
    public sealed record Log(string Text):Cmd<T>;
    public sealed record BackgroundTask(TaskSpec Spec,Func<T> Work):Cmd<T>;
    public sealed record SaveState:Cmd<T>;
    public sealed record RestoreState:Cmd<T>;
    public sealed record SetMouseCapture(bool Enabled):Cmd<T>;
    public sealed record SaveStateAndQuit:Cmd<T>; // DIVERGENCE: upstream uses separate variants

    public string TypeName=>this switch{None=>"None",Quit=>"Quit",Batch=>"Batch",Sequence=>"Sequence",Msg=>"Msg",Tick=>"Tick",Log=>"Log",BackgroundTask=>"Task",SaveState=>"SaveState",RestoreState=>"RestoreState",SetMouseCapture=>"SetMouseCapture",SaveStateAndQuit=>"SaveStateAndQuit",_=>"Unknown"};

    public static Cmd<T> NoneCmd=>new None();
    public static Cmd<T> QuitCmd=>new Quit();
    public static Cmd<T> BatchCmd(List<Cmd<T>> cmds)=>cmds.Count==0?new None():cmds.Count==1?cmds[0]:new Batch(cmds);
    public static Cmd<T> SequenceCmd(List<Cmd<T>> cmds)=>cmds.Count==0?new None():cmds.Count==1?cmds[0]:new Sequence(cmds);
    public static Cmd<T> MsgCmd(T m)=>new Msg(m);
    public static Cmd<T> TickCmd(TimeSpan d)=>new Tick(d);
    public static Cmd<T> LogCmd(string t)=>new Log(t);
    public static Cmd<T> TaskCmd(Func<T> f)=>new BackgroundTask(new TaskSpec(),f);
    public static Cmd<T> TaskWithSpec(TaskSpec s,Func<T> f)=>new BackgroundTask(s,f);
    public static Cmd<T> TaskNamed(string n,Func<T> f)=>new BackgroundTask(new TaskSpec().WithName(n),f);
    public static Cmd<T> SaveStateCmd=>new SaveState();
    public static Cmd<T> RestoreStateCmd=>new RestoreState();
    public static Cmd<T> SetMouseCaptureCmd(bool e)=>new SetMouseCapture(e);
    public static Cmd<T> SaveStateAndQuitCmd=>new SaveStateAndQuit();
    public static Cmd<T> Task(Func<T> f)=>new BackgroundTask(new TaskSpec(),f);
    public int Count=>this is Batch b?b.Items.Count:this is Sequence s?s.Items.Count:this is BackgroundTask?1:0;
}
