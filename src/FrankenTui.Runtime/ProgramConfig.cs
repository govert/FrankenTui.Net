// SPDX-License-Identifier: Apache-2.0
// Port of types from .external/frankentui/crates/ftui-runtime/src/program.rs
// Systematic addition of all config/enum types from the Elm-style runtime.

namespace FrankenTui.Runtime;

// ── ResizeBehavior ────────────────────────────────────────────────────────
public enum ResizeBehavior{Immediate,Throttled}
internal static class ResizeBehaviorMeta{public static bool UsesCoalescer(ResizeBehavior r)=>r==ResizeBehavior.Throttled;}

// ── MouseCapturePolicy ────────────────────────────────────────────────────
public enum MouseCapturePolicy{Auto,On,Off}
internal static class MouseCaptureMeta{public static bool Resolve(MouseCapturePolicy p,ScreenMode m)=>p switch{MouseCapturePolicy.Auto=>m is ScreenMode.AltScreen,MouseCapturePolicy.On=>true,MouseCapturePolicy.Off=>false,_=>false};}

// ── FrameTiming / FrameTimingSink / FrameTimingConfig ─────────────────────
public sealed record FrameTiming(ulong FrameIdx,ulong UpdateUs,ulong RenderUs,ulong DiffUs,ulong PresentUs,ulong TotalUs);
public interface IFrameTimingSink{void RecordFrame(FrameTiming timing);}
public sealed class FrameTimingConfig{public IFrameTimingSink? Sink;public FrameTimingConfig(IFrameTimingSink? s=null){Sink=s;}}

// ── PersistenceConfig ─────────────────────────────────────────────────────
public sealed class PersistenceConfig
{
    public bool AutoLoad=true,AutoSave=true; public TimeSpan? CheckpointInterval; public object? Registry;
    public static PersistenceConfig Default=>new();
    public static PersistenceConfig Disabled()=>new();
    public PersistenceConfig WithRegistry(object r){Registry=r;return this;}
    public PersistenceConfig WithRegistry(StateRegistry r){Registry=r;return this;}
    public StateRegistry? StateRegistry=>Registry as StateRegistry;
    public PersistenceConfig CheckpointEvery(TimeSpan t){CheckpointInterval=t;return this;}
    public PersistenceConfig WithAutoLoad(bool e){AutoLoad=e;return this;}
    public PersistenceConfig WithAutoSave(bool e){AutoSave=e;return this;}
}

// ── WidgetRefreshConfig ───────────────────────────────────────────────────
public sealed class WidgetRefreshConfig
{
    public bool Enabled=true; public ulong StalenessWindowMs=1000,StarveMs=3000; public int MaxStarvedPerFrame=3;
    public float MaxDropFraction=1.0f,WeightPriority=1.0f,WeightStaleness=0.5f,WeightFocus=0.3f,WeightInteraction=0.2f;
    public static WidgetRefreshConfig Default=>new();
}

// ── TaskExecutorBackend / EffectQueueConfig ───────────────────────────────
public enum TaskExecutorBackend{Spawned,EffectQueue}
public sealed class EffectQueueConfig
{
    public bool Enabled; public TaskExecutorBackend Backend=TaskExecutorBackend.Spawned;
    public SchedulerConfig Scheduler=SchedulerConfig.Default; public int MaxQueueDepth;
    public static EffectQueueConfig Default=>new();
    public EffectQueueConfig WithEnabled(bool e){Enabled=e;return this;}
    public EffectQueueConfig WithBackend(TaskExecutorBackend b){Backend=b;return this;}
    public EffectQueueConfig WithMaxQueueDepth(int d){MaxQueueDepth=d;return this;}
}

// ── ImmediateDrainConfig ──────────────────────────────────────────────────
public sealed class ImmediateDrainConfig
{
    public int MaxZeroTimeoutPollsPerBurst=64; public TimeSpan MaxBurstDuration=TimeSpan.FromMilliseconds(2);
    public TimeSpan BackoffTimeout=TimeSpan.FromMilliseconds(1);
    public static ImmediateDrainConfig Default=>new();
}

// ── GuardrailsConfig ──────────────────────────────────────────────────────
public sealed class GuardrailsConfig
{
    public ulong MaxTotalCells=1_000_000; public int MaxQueueDepth=10_000;
    public static GuardrailsConfig Default=>new();
}

// ── InlineAutoRemeasureConfig ─────────────────────────────────────────────
public sealed class InlineAutoRemeasureConfig
{
    public TimeSpan RemeasureInterval=TimeSpan.FromMilliseconds(500);
    public static InlineAutoRemeasureConfig Default=>new();
}

// ── RuntimeLane / RolloutPolicy ───────────────────────────────────────────
public enum RuntimeLane{Legacy,Structured,Asupersync}
public enum RolloutPolicy{Baseline,Shadow,Canary}

// ── ProgramConfig (full, matching upstream) ────────────────────────────────
public sealed class ProgramConfig
{
    public ScreenMode ScreenMode=new ScreenMode.Inline(4); public UiAnchor UiAnchor=UiAnchor.Bottom;
    public RuntimeDiffConfig DiffConfig=RuntimeDiffConfig.Default;
    public EvidenceSinkConfig EvidenceSink=new(); public RenderTraceConfig RenderTrace=new();
    public FrameTimingConfig? FrameTiming; public ConformalConfig? ConformalConfig;
    public LocaleContext LocaleContext=LocaleContext.System(); public TimeSpan PollTimeout=TimeSpan.FromMilliseconds(50);
    public ImmediateDrainConfig ImmediateDrain=ImmediateDrainConfig.Default;
    public CoalescerConfig ResizeCoalescer=CoalescerConfig.Default;
    public ResizeBehavior ResizeBehavior=ResizeBehavior.Throttled;
    public (ushort,ushort)? ForcedSize; public MouseCapturePolicy MouseCapture=MouseCapturePolicy.Auto;
    public bool BracketedPaste,FocusReporting,KittyKeyboard,InterceptSignals=true;
    public PersistenceConfig Persistence=PersistenceConfig.Default;
    public InlineAutoRemeasureConfig? InlineAutoRemeasure;
    public WidgetRefreshConfig WidgetRefresh=WidgetRefreshConfig.Default;
    public EffectQueueConfig EffectQueue=EffectQueueConfig.Default;
    public GuardrailsConfig Guardrails=GuardrailsConfig.Default;
    public RuntimeLane RuntimeLane=RuntimeLane.Structured; public RolloutPolicy RolloutPolicy=RolloutPolicy.Baseline;
    public static ProgramConfig Default=>new();
    public static ProgramConfig Fullscreen()=>new(){ScreenMode=new ScreenMode.AltScreen()};
    public static ProgramConfig Inline(ushort h)=>new(){ScreenMode=new ScreenMode.Inline(h)};
    public ProgramConfig WithMouse(){MouseCapture=MouseCapturePolicy.On;return this;}
    public ProgramConfig WithMouseCapturePolicy(MouseCapturePolicy p){MouseCapture=p;return this;}
    public ProgramConfig WithBudget(object b){/* DIVERGENCE: FrameBudgetConfig deferred */return this;}
    public ProgramConfig WithDiffConfig(RuntimeDiffConfig d){DiffConfig=d;return this;}
    public ProgramConfig WithEvidenceSink(EvidenceSinkConfig c){EvidenceSink=c;return this;}
    public ProgramConfig WithRenderTrace(RenderTraceConfig c){RenderTrace=c;return this;}
    public ProgramConfig WithFrameTiming(FrameTimingConfig c){FrameTiming=c;return this;}
    public ProgramConfig WithConformalConfig(ConformalConfig c){ConformalConfig=c;return this;}
}
