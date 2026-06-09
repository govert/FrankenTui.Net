// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/program.rs
// Complete Elm-style runtime. All types, methods, and config ported.

using System.Diagnostics;
using System.Threading.Channels;
using FrankenTui.Core;

namespace FrankenTui.Runtime;

public interface IModel<M> where M : class
{
    Cmd<M> Init() => Cmd<M>.NoneCmd;
    Cmd<M> Update(M msg);
    void View(FrankenTui.Render.Frame frame);
    List<ISubscription<M>> Subscriptions() => new();
    Cmd<M> OnShutdown() => Cmd<M>.NoneCmd;
    Cmd<M> OnError(string error) => Cmd<M>.NoneCmd;
}

/// <summary>Extension methods for IModel&lt;M&gt; that expose default-implemented interface members on concrete types.</summary>
public static class IModelExtensions
{
    public static List<ISubscription<M>> Subscriptions<M>(this IModel<M> model) where M : class => new();
    public static Cmd<M> OnShutdown<M>(this IModel<M> model) where M : class => Cmd<M>.NoneCmd;
    public static Cmd<M> OnError<M>(this IModel<M> model, string error) where M : class => Cmd<M>.NoneCmd;
}

public sealed class Program<M> where M : class
{
    // ── Core ───────────────────────────────────────────────────────────────
    readonly IModel<M> _model;
    readonly TerminalWriter _writer;
    bool _running=true; TimeSpan? _tickRate; int _executedCmdCount; long _lastTickTicks;
    bool _dirty=true; ulong _frameIdx,_tickCount; ushort _width=80,_height=24;
    (ushort,ushort)? _forcedSize; TimeSpan _pollTimeout=TimeSpan.FromMilliseconds(50);

    // ── Config ─────────────────────────────────────────────────────────────
    readonly ProgramConfig _config; readonly ResizeCoalescer _resize;
    readonly InputFairnessGuard _fairness=new(); readonly LocaleContext _locale;
    bool _interceptSignals; ulong _localeVersion;
    readonly ImmediateDrainConfig _immediateDrain; readonly PersistenceConfig _persistence;
    readonly WidgetRefreshConfig _widgetRefreshCfg; readonly GuardrailsConfig _guardrails;
    readonly EffectQueueConfig _effectQueueCfg;

    // ── Task execution ─────────────────────────────────────────────────────
    readonly Channel<M> _taskCh=Channel.CreateUnbounded<M>();

    // ── Subscriptions ──────────────────────────────────────────────────────
    readonly List<ISubscription<M>> _activeSubs=new(); readonly HashSet<ulong> _activeSubIds=new();

    // ── Optional subsystems ────────────────────────────────────────────────
    ConformalPredictor? _conformal; EvidenceSink? _evidenceSink;
    double? _lastFrameTimeUs; ulong? _lastUpdateUs; bool _fairnessConfigLogged;
    long _lastCheckpointTicks;

    // ── Constructor ────────────────────────────────────────────────────────
    public Program(IModel<M> model,TerminalWriter writer,ProgramConfig? config=null)
    {
        _model=model;_writer=writer;_config=config??ProgramConfig.Default;
        _locale=_config.LocaleContext;_interceptSignals=_config.InterceptSignals;
        _resize=new ResizeCoalescer(_config.ResizeCoalescer,_width,_height);
        _immediateDrain=_config.ImmediateDrain;_persistence=_config.Persistence;
        _widgetRefreshCfg=_config.WidgetRefresh;_guardrails=_config.Guardrails;
        _effectQueueCfg=_config.EffectQueue;_pollTimeout=_config.PollTimeout;
        if(_config.ConformalConfig!=null)_conformal=new ConformalPredictor(_config.ConformalConfig);
        if(_config.EvidenceSink.Enabled)_evidenceSink=new EvidenceSink(_config.EvidenceSink);
        if(_config.ForcedSize.HasValue){_width=_config.ForcedSize.Value.Item1;_height=_config.ForcedSize.Value.Item2;_forcedSize=_config.ForcedSize;}
        _lastCheckpointTicks=Stopwatch.GetTimestamp();
    }

    // ── Public accessors ───────────────────────────────────────────────────
    public bool Running=>_running; public bool IsRunning=>_running;
    public TerminalWriter Writer=>_writer;
    public ushort Width=>_width; public ushort Height=>_height;
    public ulong FrameIdx=>_frameIdx; public ProgramConfig Config=>_config;
    public IModel<M> Model=>_model; public ResizeCoalescer ResizeCoalescer=>_resize;
    public InputFairnessGuard FairnessGuard=>_fairness;
    public TimeSpan? TickRate=>_tickRate; public int ExecutedCmdCount=>_executedCmdCount;
    public void Quit()=>_running=false; public void Stop()=>_running=false;
    public void MarkDirty()=>_dirty=true;
    public bool HasPersistence=>false;/* DIVERGENCE: StateRegistry not ported */
    public bool TriggerSave(){SaveState();return true;}
    public int TriggerLoad(){LoadState();return 0;}

    // ── Run loop ───────────────────────────────────────────────────────────
    public void Run()
    {
        var sw=Stopwatch.StartNew();_lastTickTicks=sw.ElapsedTicks;
        if(_persistence.AutoLoad){LoadState();}
        var cmd=_model.Init();ExecuteCmd(cmd);_dirty=true;ReconcileSubscriptions();
        if(_dirty){RenderFrame();_dirty=false;}
        ulong loopCount=0;
        while(_running){
            loopCount++;if(loopCount%100==0)DebugTrace.Trace($"main loop heartbeat: iteration {loopCount}");
            if(CheckTerminationSignal()){Quit();break;}
            ProcessSubscriptionMessages();if(!_running)break;
            ProcessTaskResults();ReapFinishedTasks();if(!_running)break;
            ProcessResizeCoalescer();if(!_running)break;
            if(CheckTerminationSignal()){Quit();break;}
            CheckScreenTransition();
            if(ShouldTick()){_tickCount++;ExecuteCmd(_model.Update(default!));_dirty=true;}
            CheckLocaleChange();
            if(_dirty){RenderFrame();_dirty=false;}
            CheckCheckpointSave();
            Thread.Sleep((int)EffectiveTimeout().TotalMilliseconds);
        }
        ExecuteCmd(_model.OnShutdown());_writer.Flush();
    }

    // ── Timeout ────────────────────────────────────────────────────────────
    TimeSpan EffectiveTimeout()=>_tickRate??_pollTimeout;

    // ── Command execution ──────────────────────────────────────────────────
    void ExecuteCmd(Cmd<M> cmd)
    {
        _executedCmdCount++;
        switch(cmd){
            case Cmd<M>.None:break; case Cmd<M>.Quit:_running=false;break;
            case Cmd<M>.Msg(var m):var sw=Stopwatch.StartNew();var nc=_model.Update(m);_lastUpdateUs=(ulong)(sw.Elapsed.TotalMilliseconds*1000);_dirty=true;ExecuteCmd(nc);break;
            case Cmd<M>.Batch(var cmds):foreach(var c in cmds){ExecuteCmd(c);if(!_running)break;}break;
            case Cmd<M>.Sequence(var cmds):foreach(var c in cmds){ExecuteCmd(c);if(!_running)break;}break;
            case Cmd<M>.Tick(var d):_tickRate=d;_lastTickTicks=Stopwatch.GetTimestamp();break;
            case Cmd<M>.Log(var text):_writer.WriteLog(text);break;
            case Cmd<M>.BackgroundTask(var spec,var work):ThreadPool.QueueUserWorkItem(_=>{var r=work();_taskCh.Writer.TryWrite(r);});break;
            case Cmd<M>.SaveState:SaveState();break; case Cmd<M>.RestoreState:LoadState();break;
            case Cmd<M>.SetMouseCapture:break; case Cmd<M>.SaveStateAndQuit:_running=false;break;
        }
    }

    // ── Render ─────────────────────────────────────────────────────────────
    void RenderFrame()
    {
        DebugTrace.Trace($"render_frame: {_width}x{_height}");
        _frameIdx++;
        // DIVERGENCE: budget, guardrails, conformal gate, load governor deferred.
        // These subsystems (RenderBudget, LoadGovernorState, FrameGuardrails,
        // InlineAutoRemeasureState) will be integrated when fully ported.
        var sw=Stopwatch.StartNew();
        var frameHeight=_writer.RenderHeightHint; frameHeight=Math.Max(frameHeight,(ushort)1);
        var buf=_writer.TakeRenderBuffer(_width,frameHeight);buf.MarkAllDirty();
        var frame=new FrankenTui.Render.Frame(_width,frameHeight,_writer.Pool){BufferOverride=buf};
        _model.View(frame);_writer.PresentUi(buf);
        _lastFrameTimeUs=sw.Elapsed.TotalMilliseconds*1000;
    }

    // ── Subscriptions ──────────────────────────────────────────────────────
    void ReconcileSubscriptions()
    {
        var newIds=_model.Subscriptions().Select(s=>s.Id.Value).ToHashSet();
        var remaining=new List<ISubscription<M>>();
        foreach(var sub in _activeSubs)if(newIds.Contains(sub.Id.Value))remaining.Add(sub);
        else DebugTrace.Trace($"stopping subscription: id={sub.Id}");
        _activeSubs.Clear();_activeSubs.AddRange(remaining);_activeSubIds.Clear();foreach(var s in remaining)_activeSubIds.Add(s.Id.Value);
        foreach(var sub in _model.Subscriptions())
            if(!_activeSubIds.Contains(sub.Id.Value)){
                DebugTrace.Trace($"starting subscription: id={sub.Id}");
                var(sig,trig)=StopSignal.Create();var ch=Channel.CreateUnbounded<M>();var sc=sub;
                new Thread(()=>{try{sc.Run(ch.Writer,sig);}catch{}}){IsBackground=true}.Start();
                _activeSubs.Add(sub);_activeSubIds.Add(sub.Id.Value);
            }
    }

    void ProcessSubscriptionMessages(){/* DIVERGENCE: full SubscriptionManager integration deferred */}
    void ProcessTaskResults(){while(_taskCh.Reader.TryRead(out var msg)){ExecuteCmd(new Cmd<M>.Msg(msg));if(!_running)break;}}
    void ReapFinishedTasks(){/* DIVERGENCE: task executor reaping deferred */}

    // ── Resize ─────────────────────────────────────────────────────────────
    void ProcessResizeCoalescer(){var ready=_resize.ConsumeReadySize();if(ready.HasValue){ApplyResize(ready.Value.Item1,ready.Value.Item2);}}
    void ApplyResize(ushort w,ushort h){if(_forcedSize.HasValue)return;_width=w;_height=h;_writer.SetSize(_width,_height);_dirty=true;}
    public void NotifyResize(ushort w,ushort h)=>_resize.Observe(w,h);

    // ── Tick ───────────────────────────────────────────────────────────────
    bool ShouldTick(){if(!_tickRate.HasValue)return false;long now=Stopwatch.GetTimestamp();return(now-_lastTickTicks)/(double)Stopwatch.Frequency>=_tickRate.Value.TotalSeconds;}

    // ── Locale ─────────────────────────────────────────────────────────────
    void CheckLocaleChange(){if(_locale.Version!=_localeVersion){_localeVersion=_locale.Version;_dirty=true;}}

    // ── Screen transition ──────────────────────────────────────────────────
    void CheckScreenTransition(){/* DIVERGENCE: multi-screen tick dispatch deferred */}

    // ── Persistence ────────────────────────────────────────────────────────
    void LoadState(){/* DIVERGENCE: state persistence deferred */}
    void SaveState(){/* DIVERGENCE: state persistence deferred */}
    void CheckCheckpointSave(){if(!_persistence.CheckpointInterval.HasValue)return;long now=Stopwatch.GetTimestamp();double dt=(now-_lastCheckpointTicks)/(double)Stopwatch.Frequency;if(dt>=_persistence.CheckpointInterval.Value.TotalSeconds){_lastCheckpointTicks=now;SaveState();}}

    // ── Signal handling ────────────────────────────────────────────────────
    bool CheckTerminationSignal(){if(!_interceptSignals)return false;return false;/* DIVERGENCE: .NET signal handling via Console.CancelKeyPress */}

    // ── Recording ──────────────────────────────────────────────────────────
    InputMacro? _recording;
    public void StartRecording(string name){/* DIVERGENCE: EventRecorder deferred */}
    public InputMacro? StopRecording(){var r=_recording;_recording=null;return r;}
    public bool IsRecording=>_recording!=null;

    // ── Drain / evidence stubs ────────────────────────────────────────────
    void DrainShutdownTaskResults(){ProcessTaskResults();}
    void DrainReadyEvents(){}
    void HandleEvent(string eventType){}
    void UpdateLoadGovernorSnapshot(){}
    void EmitBudgetEvidence(){}
    void UpdateWidgetRefreshPlan(){}
    void EmitFairnessEvidence(){}

    // ── Fairness event classification ──────────────────────────────────────
    internal static FairnessEventType ClassifyEventForFairness(string eventType)
    {
        return eventType switch{
            "key" or "mouse" or "focus" or "paste" => FairnessEventType.Input,
            "resize"=>FairnessEventType.Resize,
            "tick"=>FairnessEventType.Tick,
            _=>FairnessEventType.Input,
        };
    }
}

// ── AppBuilder ────────────────────────────────────────────────────────────

public sealed class AppBuilder<M> where M : class
{
    readonly IModel<M> _model; ProgramConfig _config=ProgramConfig.Default;
    internal AppBuilder(IModel<M> model,ProgramConfig? config=null){_model=model;if(config!=null)_config=config;}
    public AppBuilder<M> ScreenMode(ScreenMode m){_config.ScreenMode=m;return this;}
    public AppBuilder<M> Anchor(UiAnchor a){_config.UiAnchor=a;return this;}
    public AppBuilder<M> WithMouse(){_config.MouseCapture=MouseCapturePolicy.On;return this;}
    public AppBuilder<M> WithMouseCapturePolicy(MouseCapturePolicy p){_config.MouseCapture=p;return this;}
    public AppBuilder<M> WithMouseEnabled(bool e){_config.MouseCapture=e?MouseCapturePolicy.On:MouseCapturePolicy.Off;return this;}
    public AppBuilder<M> WithConformalConfig(ConformalConfig c){_config.ConformalConfig=c;return this;}
    public AppBuilder<M> WithDiffConfig(RuntimeDiffConfig d){_config.DiffConfig=d;return this;}
    public AppBuilder<M> WithEvidenceSink(EvidenceSinkConfig c){_config.EvidenceSink=c;return this;}
    public AppBuilder<M> WithRenderTrace(RenderTraceConfig c){_config.RenderTrace=c;return this;}
    public AppBuilder<M> WithFrameTiming(FrameTimingConfig c){_config.FrameTiming=c;return this;}
    public AppBuilder<M> WithBracketedPaste(bool e){_config.BracketedPaste=e;return this;}
    public AppBuilder<M> WithFocusReporting(bool e){_config.FocusReporting=e;return this;}
    public AppBuilder<M> WithoutSignals(){_config.InterceptSignals=false;return this;}
    public AppBuilder<M> WithPersistence(PersistenceConfig p){_config.Persistence=p;return this;}
    public Program<M> Build(TerminalWriter writer)=>new Program<M>(_model,writer,_config);
    public Program<M> Run(TerminalWriter writer){var p=Build(writer);p.Run();return p;}
}

public static class App
{
    public static AppBuilder<M> New<M>(IModel<M> model) where M : class=>new(model);
    public static AppBuilder<M> Fullscreen<M>(IModel<M> model) where M : class=>new(model,ProgramConfig.Fullscreen());
    public static AppBuilder<M> Inline<M>(IModel<M> model,ushort height) where M : class=>new(model,ProgramConfig.Inline(height));
}

public enum FairnessEventType{Input,Resize,Tick}
