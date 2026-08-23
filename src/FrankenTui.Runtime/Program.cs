// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-runtime/src/program.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: The managed runtime uses .NET tasks/channels and exceptions in
// place of Rust threads/mpsc and io::Error while preserving lifecycle order.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
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
    readonly FrankenTui.Render.FrameArena _frameArena=new();
    bool _running=true; bool _shutdownComplete; TimeSpan? _tickRate; int _executedCmdCount; long _lastTickTicks;
    bool _dirty=true; ulong _frameIdx,_tickCount; ushort _width=80,_height=24;
    (ushort,ushort)? _forcedSize; TimeSpan _pollTimeout=TimeSpan.FromMilliseconds(50);

    // ── Config ─────────────────────────────────────────────────────────────
    readonly ProgramConfig _config; readonly ResizeCoalescer _resize;
    readonly InputFairnessGuard _fairness=new(); readonly LocaleContext _locale;
    bool _interceptSignals; ulong _localeVersion;
    readonly ImmediateDrainConfig _immediateDrain; readonly PersistenceConfig _persistence;
    readonly StateRegistry? _stateRegistry;
    readonly WidgetRefreshConfig _widgetRefreshCfg; readonly GuardrailsConfig _guardrails;
    readonly EffectQueueConfig _effectQueueCfg;

    // ── Task execution ─────────────────────────────────────────────────────
    readonly Channel<M> _taskCh=Channel.CreateUnbounded<M>();
    readonly Channel<Exception> _taskFailureCh=Channel.CreateUnbounded<Exception>();
    readonly List<Task> _activeTasks=new(); readonly object _activeTasksGate=new();

    // ── Subscriptions ──────────────────────────────────────────────────────
    readonly SubscriptionManager<M> _subscriptions=new();

    // ── Optional subsystems ────────────────────────────────────────────────
    ConformalPredictor? _conformal; EvidenceSink? _evidenceSink;
    double? _lastFrameTimeUs; ulong? _lastUpdateUs; bool _fairnessConfigLogged;
    long _lastCheckpointTicks;

    // ── Constructor ────────────────────────────────────────────────────────
    public Program(IModel<M> model,TerminalWriter writer,ProgramConfig? config=null)
    {
        _model=model;_writer=writer;_config=config??ProgramConfig.Default;
        _locale=_config.LocaleContext;_interceptSignals=_config.InterceptSignals;
        _resize=new ResizeCoalescer(_config.ResizeCoalescer);
        _immediateDrain=_config.ImmediateDrain;_persistence=_config.Persistence;
        _stateRegistry=_persistence.StateRegistry;
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
    public StateRegistry? StateRegistry=>_stateRegistry;
    public bool HasPersistence=>_stateRegistry is not null;
    public bool TriggerSave()=>_stateRegistry?.Flush()??false;
    public int TriggerLoad()=>_stateRegistry?.Load()??0;

    // ── Run loop ───────────────────────────────────────────────────────────
    public void Run()
    {
        if(_shutdownComplete)
            throw new InvalidOperationException("program lifecycle has already completed");

        Exception? primaryError=null;
        try{RunEventLoop();}
        catch(Exception ex){primaryError=ex;}

        CompleteLifecycle(primaryError);
    }

    void RunEventLoop()
    {
        var sw=Stopwatch.StartNew();_lastTickTicks=sw.ElapsedTicks;
        if(_persistence.AutoLoad){LoadState();}
        var cmd=_model.Init();ExecuteCmd(cmd);_dirty=true;
        if(_running)
        {
            ReconcileSubscriptions();
            ProcessSubscriptionFailures(duringLifecycle:false);
            if(_running&&_dirty){RenderFrame();_dirty=false;}
        }
        ulong loopCount=0;
        while(_running){
            loopCount++;if(loopCount%100==0)DebugTrace.Trace($"main loop heartbeat: iteration {loopCount}");
            if(CheckTerminationSignal()){Quit();break;}
            ProcessSubscriptionMessages();
            ProcessSubscriptionFailures(duringLifecycle:false);if(!_running)break;
            ProcessTaskResults();ReapFinishedTasks();if(!_running)break;
            ProcessResizeCoalescer();if(!_running)break;
            if(CheckTerminationSignal()){Quit();break;}
            CheckScreenTransition();
            if(ShouldTick()){
                _tickCount++;ExecuteCmd(_model.Update(default!));_dirty=true;
                if(_running)ReconcileSubscriptions();
            }
            CheckLocaleChange();
            if(_dirty){RenderFrame();_dirty=false;}
            CheckCheckpointSave();
            Thread.Sleep((int)EffectiveTimeout().TotalMilliseconds);
        }
    }

    void CompleteLifecycle(Exception? primaryError)
    {
        _running=false;

        Exception? hookError=null;
        if(primaryError is not null)
        {
            try{InvokeErrorHook(primaryError.Message,duringLifecycle:true);}
            catch(Exception ex){hookError=ex;}
        }

        try{ProcessSubscriptionFailures(duringLifecycle:true);}
        catch(Exception ex){hookError??=ex;}

        Exception? shutdownError=null;
        try{ShutdownOnce();}
        catch(Exception ex){shutdownError=ex;}

        if(primaryError is not null)
            ExceptionDispatchInfo.Capture(primaryError).Throw();
        if(hookError is not null)
            ExceptionDispatchInfo.Capture(hookError).Throw();
        if(shutdownError is not null)
        {
            try{InvokeErrorHook(shutdownError.Message,duringLifecycle:true);}
            catch{ /* The original shutdown error retains precedence. */ }
            ExceptionDispatchInfo.Capture(shutdownError).Throw();
        }
    }

    void ShutdownOnce()
    {
        if(_shutdownComplete)return;
        _shutdownComplete=true;

        Exception? shutdownError=null;
        try{ExecuteLifecycleCmd(_model.OnShutdown());}
        catch(Exception ex){shutdownError=ex;}

        if(_persistence.AutoSave)
        {
            try{SaveState();}
            catch(Exception ex){shutdownError??=ex;}
        }

        try{_subscriptions.StopAll();}
        catch(Exception ex){shutdownError??=ex;}
        try{ProcessSubscriptionFailures(duringLifecycle:true);}
        catch(Exception ex){shutdownError??=ex;}

        try{ShutdownBackgroundTasks();}
        catch(Exception ex){shutdownError??=ex;}
        try{DrainShutdownTaskResults();}
        catch(Exception ex){shutdownError??=ex;}
        try{_writer.Flush();}
        catch(Exception ex){shutdownError??=ex;}

        if(shutdownError is not null)
            ExceptionDispatchInfo.Capture(shutdownError).Throw();
    }

    void InvokeErrorHook(string error,bool duringLifecycle)
    {
        var cmd=_model.OnError(error);
        if(duringLifecycle)ExecuteLifecycleCmd(cmd);
        else ExecuteCmd(cmd);
    }

    void ExecuteLifecycleCmd(Cmd<M> cmd)
    {
        var wasRunning=_running;
        _running=true;
        try{ExecuteCmd(cmd);}
        finally{_running=wasRunning&&_running;}
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
            case Cmd<M>.BackgroundTask(var spec,var work):ScheduleBackgroundTask(work);break;
            case Cmd<M>.SaveState:SaveState();break; case Cmd<M>.RestoreState:LoadState();break;
            case Cmd<M>.SetMouseCapture:break; case Cmd<M>.SaveStateAndQuit:SaveState();_running=false;break;
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
        _frameArena.Reset();
        var frameHeight=_writer.RenderHeightHint; frameHeight=Math.Max(frameHeight,(ushort)1);
        var buf=_writer.TakeRenderBuffer(_width,frameHeight);buf.MarkAllDirty();
        var frame=new FrankenTui.Render.Frame(_width,frameHeight,_writer.Pool){BufferOverride=buf};
        frame.SetLinks(_writer.Links);
        frame.SetArena(_frameArena);
        _model.View(frame);_writer.PresentUi(buf);
        _lastFrameTimeUs=sw.Elapsed.TotalMilliseconds*1000;
    }

    // ── Subscriptions ──────────────────────────────────────────────────────
    void ReconcileSubscriptions()
    {
        _subscriptions.Reconcile(_model.Subscriptions());
    }

    void ProcessSubscriptionMessages()
    {
        foreach(var msg in _subscriptions.DrainMessages())
        {
            ExecuteCmd(new Cmd<M>.Msg(msg));
            if(!_running)break;
            ReconcileSubscriptions();
        }
    }

    void ProcessSubscriptionFailures(bool duringLifecycle)
    {
        Exception? firstError=null;
        foreach(var failure in _subscriptions.DrainFailures())
        {
            var error=$"subscription {failure.Id} failed: {failure.Error}";
            try{InvokeErrorHook(error,duringLifecycle);}
            catch(Exception ex){firstError??=ex;}
        }
        if(firstError is not null)ExceptionDispatchInfo.Capture(firstError).Throw();
    }

    void ScheduleBackgroundTask(Func<M> work)
    {
        var task=Task.Run(()=>
        {
            try{_taskCh.Writer.TryWrite(work());}
            catch(Exception ex){_taskFailureCh.Writer.TryWrite(ex);}
        });
        lock(_activeTasksGate)_activeTasks.Add(task);
    }

    void ProcessTaskResults()
    {
        if(_taskFailureCh.Reader.TryRead(out var failure))
            throw new InvalidOperationException($"background task failed: {failure.Message}",failure);
        while(_taskCh.Reader.TryRead(out var msg))
        {
            ExecuteCmd(new Cmd<M>.Msg(msg));
            if(!_running)break;
            ReconcileSubscriptions();
        }
    }

    void ReapFinishedTasks()
    {
        lock(_activeTasksGate)_activeTasks.RemoveAll(task=>task.IsCompleted);
    }

    void ShutdownBackgroundTasks()
    {
        Task[] tasks;
        lock(_activeTasksGate)tasks=_activeTasks.ToArray();
        if(tasks.Length>0)Task.WaitAll(tasks,TimeSpan.FromMilliseconds(250));
        ReapFinishedTasks();
    }

    // ── Resize ─────────────────────────────────────────────────────────────
    void ProcessResizeCoalescer(){var decision=_resize.Evaluate(DateTimeOffset.UtcNow);if(decision is null)return;var ready=_resize.ConsumeReadySize(decision.Action);if(ready.HasValue){ApplyResize(ready.Value.Width,ready.Value.Height);}}
    void ApplyResize(ushort w,ushort h){if(_forcedSize.HasValue)return;_width=w;_height=h;_writer.SetSize(_width,_height);_dirty=true;}
    public void NotifyResize(ushort w,ushort h){var decision=_resize.Observe(new FrankenTui.Core.Size(w,h),DateTimeOffset.UtcNow);var ready=_resize.ConsumeReadySize(decision.Action);if(ready.HasValue){ApplyResize(ready.Value.Width,ready.Value.Height);}}

    // ── Tick ───────────────────────────────────────────────────────────────
    bool ShouldTick(){if(!_tickRate.HasValue)return false;long now=Stopwatch.GetTimestamp();return(now-_lastTickTicks)/(double)Stopwatch.Frequency>=_tickRate.Value.TotalSeconds;}

    // ── Locale ─────────────────────────────────────────────────────────────
    void CheckLocaleChange(){if(_locale.Version!=_localeVersion){_localeVersion=_locale.Version;_dirty=true;}}

    // ── Screen transition ──────────────────────────────────────────────────
    void CheckScreenTransition(){/* DIVERGENCE: multi-screen tick dispatch deferred */}

    // ── Persistence ────────────────────────────────────────────────────────
    void LoadState()
    {
        if(_stateRegistry is null)return;
        try{_stateRegistry.Load();}
        catch(StorageException ex){DebugTrace.Trace($"failed to load widget state: {ex.Message}");}
    }
    void SaveState()
    {
        if(_stateRegistry is null)return;
        try{_stateRegistry.Flush();}
        catch(StorageException ex){DebugTrace.Trace($"failed to save widget state: {ex.Message}");}
    }
    void CheckCheckpointSave(){if(!_persistence.CheckpointInterval.HasValue)return;long now=Stopwatch.GetTimestamp();double dt=(now-_lastCheckpointTicks)/(double)Stopwatch.Frequency;if(dt>=_persistence.CheckpointInterval.Value.TotalSeconds){_lastCheckpointTicks=now;SaveState();}}

    // ── Signal handling ────────────────────────────────────────────────────
    bool CheckTerminationSignal(){if(!_interceptSignals)return false;return false;/* DIVERGENCE: .NET signal handling via Console.CancelKeyPress */}

    // ── Recording ──────────────────────────────────────────────────────────
    EventRecorder? _recording;

    /// <summary>Start a new input recording, discarding any active recording.</summary>
    public void StartRecording(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        _recording = new EventRecorder(name);
        if (_width > 0 && _height > 0)
        {
            _recording.WithTerminalSize(_width, _height);
        }

        _recording.Start();
    }

    /// <summary>
    /// Feed a decoded terminal event through the recording hook. The target's terminal
    /// host calls this bridge before normal event dispatch, matching upstream ordering.
    /// </summary>
    public bool RecordInput(TerminalEvent terminalEvent)
    {
        ArgumentNullException.ThrowIfNull(terminalEvent);
        return _recording?.Record(terminalEvent) ?? false;
    }

    public InputMacro? StopRecording()
    {
        var recorder = _recording;
        _recording = null;
        return recorder?.Finish();
    }

    public bool IsRecording => _recording?.IsRecording ?? false;

    // ── Drain / evidence stubs ────────────────────────────────────────────
    void DrainShutdownTaskResults()
    {
        Exception? firstError=null;
        while(_taskFailureCh.Reader.TryRead(out var failure))firstError??=failure;
        while(_taskCh.Reader.TryRead(out var msg))
        {
            try{ExecuteLifecycleCmd(new Cmd<M>.Msg(msg));}
            catch(Exception ex){firstError??=ex;}
        }
        if(firstError is not null)ExceptionDispatchInfo.Capture(firstError).Throw();
    }
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
