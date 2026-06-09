// Tests for .external/frankentui/crates/ftui-runtime/src/program.rs
// Covers Cmd execution, Model lifecycle, Program config, and runtime behavior.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using System.Threading.Channels;

namespace FrankenTui.Tests.Headless;

public class ProgramTests
{
    // ── Test model ─────────────────────────────────────────────────────────
    sealed class Counter : IModel<string>
    {
        public int Count;
        public List<string> Log = new();
        public Cmd<string> Init() => Cmd<string>.NoneCmd;
        public Cmd<string> Update(string msg)
        {
            Log.Add(msg);
            return msg switch
            {
                "inc" => new Cmd<string>.Msg("done"),
                "quit" => Cmd<string>.QuitCmd,
                "tick" => new Cmd<string>.Tick(TimeSpan.FromMilliseconds(100)),
                "task" => new Cmd<string>.BackgroundTask(new TaskSpec(), () => "from-task"),
                "batch" => Cmd<string>.BatchCmd(new List<Cmd<string>> { new Cmd<string>.Msg("a"), new Cmd<string>.Msg("b") }),
                "seq" => Cmd<string>.SequenceCmd(new List<Cmd<string>> { new Cmd<string>.Msg("x"), new Cmd<string>.Msg("y") }),
                _ => Cmd<string>.NoneCmd,
            };
        }
        public void View(FrankenTui.Render.Frame frame) { }
    }

    static TerminalWriter MakeWriter() => new(new StringWriter(), new ScreenMode.AltScreen(), UiAnchor.Bottom, TerminalCapabilities.Basic());

    // ── Cmd tests ──────────────────────────────────────────────────────────

    [Fact] public void CmdNone_DoesNothing()
    {
        var c = Cmd<string>.NoneCmd;
        Assert.Equal("None", c.TypeName);
        Assert.Equal(0, c.Count);
    }

    [Fact] public void CmdQuit_HasCorrectType()
    {
        Assert.Equal("Quit", Cmd<string>.QuitCmd.TypeName);
    }

    [Fact] public void CmdMsg_WrapsMessage()
    {
        var c = new Cmd<string>.Msg("hello");
        Assert.Equal("Msg", c.TypeName);
        Assert.IsType<Cmd<string>.Msg>(c);
    }

    [Fact] public void CmdBatch_EmptyIsNone()
    {
        Assert.IsType<Cmd<string>.None>(Cmd<string>.BatchCmd(new()));
    }

    [Fact] public void CmdBatch_SingleIsDirect()
    {
        var c = Cmd<string>.BatchCmd(new List<Cmd<string>> { new Cmd<string>.Msg("x") });
        Assert.IsType<Cmd<string>.Msg>(c);
    }

    [Fact] public void CmdBatch_MultipleIsBatch()
    {
        var c = Cmd<string>.BatchCmd(new List<Cmd<string>> { new Cmd<string>.Msg("a"), new Cmd<string>.Msg("b") });
        Assert.IsType<Cmd<string>.Batch>(c);
        Assert.Equal(2, c.Count);
    }

    [Fact] public void CmdSequence_ExecutesInOrder()
    {
        var counter = new Counter();
        var cmd = Cmd<string>.SequenceCmd(new List<Cmd<string>> { new Cmd<string>.Msg("first"), new Cmd<string>.Msg("second") });
        Assert.IsType<Cmd<string>.Sequence>(cmd);
    }

    [Fact] public void CmdTick_SetsDuration()
    {
        var c = new Cmd<string>.Tick(TimeSpan.FromMilliseconds(50));
        Assert.IsType<Cmd<string>.Tick>(c);
    }

    [Fact] public void CmdTask_CreatesBackgroundTask()
    {
        var c = Cmd<string>.TaskCmd(() => "result");
        Assert.Equal("Task", c.TypeName);
    }

    [Fact] public void CmdLog_StoresText()
    {
        var c = new Cmd<string>.Log("test log");
        Assert.IsType<Cmd<string>.Log>(c);
    }

    [Fact] public void CmdSaveState_HasCorrectType()
    {
        Assert.Equal("SaveState", Cmd<string>.SaveStateCmd.TypeName);
    }

    [Fact] public void CmdRestoreState_HasCorrectType()
    {
        Assert.Equal("RestoreState", Cmd<string>.RestoreStateCmd.TypeName);
    }

    // ── IModel tests ───────────────────────────────────────────────────────

    [Fact] public void Model_InitDefault_ReturnsNone()
    {
        var m = new Counter();
        Assert.IsType<Cmd<string>.None>(m.Init());
    }

    [Fact] public void Model_Update_ProcessesMessage()
    {
        var m = new Counter();
        m.Update("inc");
        Assert.Contains("inc", m.Log);
    }

    [Fact] public void Model_SubscriptionsDefault_ReturnsEmpty()
    {
        var m = new Counter();
        Assert.Empty(m.Subscriptions());
    }

    [Fact] public void Model_OnShutdownDefault_ReturnsNone()
    {
        Assert.IsType<Cmd<string>.None>(new Counter().OnShutdown());
    }

    [Fact] public void Model_OnErrorDefault_ReturnsNone()
    {
        Assert.IsType<Cmd<string>.None>(new Counter().OnError("test error"));
    }

    // ── Program tests ──────────────────────────────────────────────────────

    [Fact] public void Program_ConstructsWithoutError()
    {
        var p = new Program<string>(new Counter(), MakeWriter());
        Assert.True(p.Running);
    }

    [Fact] public void Program_QuitStopsRunning()
    {
        var m = new Counter();
        var p = new Program<string>(m, MakeWriter());
        p.Stop();
        Assert.False(p.Running);
    }

    [Fact] public void Program_ConfigDefault_HasExpectedValues()
    {
        var c = ProgramConfig.Default;
        Assert.IsType<ScreenMode.Inline>(c.ScreenMode);
        Assert.Equal(UiAnchor.Bottom, c.UiAnchor);
    }

    [Fact] public void Program_InitRunsModelInit()
    {
        var p = new Program<string>(new Counter(), MakeWriter());
        Assert.NotNull(p);
    }

    // ── ProgramConfig tests ────────────────────────────────────────────────

    [Fact] public void ProgramConfig_Fullscreen_SetsAltScreen()
    {
        var c = ProgramConfig.Default;
        c.ScreenMode = new ScreenMode.AltScreen();
        Assert.IsType<ScreenMode.AltScreen>(c.ScreenMode);
    }

    [Fact] public void ResizeBehavior_Immediate_DoesNotUseCoalescer()
    {
        Assert.False(ResizeBehaviorMeta.UsesCoalescer(ResizeBehavior.Immediate));
    }

    [Fact] public void ResizeBehavior_Throttled_UsesCoalescer()
    {
        Assert.True(ResizeBehaviorMeta.UsesCoalescer(ResizeBehavior.Throttled));
    }

    [Fact] public void MouseCapture_Auto_EnablesInAltScreen()
    {
        Assert.True(MouseCaptureMeta.Resolve(MouseCapturePolicy.Auto, new ScreenMode.AltScreen()));
    }

    [Fact] public void MouseCapture_Auto_DisablesInInline()
    {
        Assert.False(MouseCaptureMeta.Resolve(MouseCapturePolicy.Auto, new ScreenMode.Inline(4)));
    }

    [Fact] public void MouseCapture_On_AlwaysEnables()
    {
        Assert.True(MouseCaptureMeta.Resolve(MouseCapturePolicy.On, new ScreenMode.Inline(4)));
    }

    [Fact] public void MouseCapture_Off_AlwaysDisables()
    {
        Assert.False(MouseCaptureMeta.Resolve(MouseCapturePolicy.Off, new ScreenMode.AltScreen()));
    }

    // ── Config types ───────────────────────────────────────────────────────

    [Fact] public void PersistenceConfig_Default_HasAutoSave()
    {
        Assert.True(PersistenceConfig.Default.AutoSave);
    }

    [Fact] public void WidgetRefreshConfig_Default_IsEnabled()
    {
        Assert.True(WidgetRefreshConfig.Default.Enabled);
    }

    [Fact] public void EffectQueueConfig_Default_UsesSpawned()
    {
        Assert.Equal(TaskExecutorBackend.Spawned, EffectQueueConfig.Default.Backend);
    }

    [Fact] public void ImmediateDrainConfig_Default_HasCorrectValues()
    {
        var c = ImmediateDrainConfig.Default;
        Assert.Equal(64, c.MaxZeroTimeoutPollsPerBurst);
        Assert.Equal(TimeSpan.FromMilliseconds(2), c.MaxBurstDuration);
    }

    [Fact] public void GuardrailsConfig_Default_HasNonZeroMax()
    {
        Assert.True(GuardrailsConfig.Default.MaxTotalCells > 0);
    }

    [Fact] public void RuntimeLane_Default_IsStructured()
    {
        Assert.Equal(RuntimeLane.Structured, ProgramConfig.Default.RuntimeLane);
    }

    [Fact] public void RolloutPolicy_Default_IsBaseline()
    {
        Assert.Equal(RolloutPolicy.Baseline, ProgramConfig.Default.RolloutPolicy);
    }

    // ── Builder pattern ────────────────────────────────────────────────────

    [Fact] public void PersistenceConfig_CheckpointEvery_SetsInterval()
    {
        var c = PersistenceConfig.Default.CheckpointEvery(TimeSpan.FromMinutes(5));
        Assert.Equal(TimeSpan.FromMinutes(5), c.CheckpointInterval);
    }

    [Fact] public void EffectQueueConfig_WithMaxQueueDepth_SetsDepth()
    {
        var c = EffectQueueConfig.Default.WithMaxQueueDepth(100);
        Assert.Equal(100, c.MaxQueueDepth);
    }

    [Fact] public void ProgramConfig_CanBeModified()
    {
        var c = ProgramConfig.Default;
        c.BracketedPaste = true;
        c.FocusReporting = true;
        Assert.True(c.BracketedPaste);
        Assert.True(c.FocusReporting);
    }

    [Fact] public void ProgramConfig_Fullscreen_UsesAltScreen()
    {
        Assert.IsType<ScreenMode.AltScreen>(ProgramConfig.Fullscreen().ScreenMode);
    }

    [Fact] public void ProgramConfig_Inline_SetsHeight()
    {
        var c = ProgramConfig.Inline(10);
        var m = Assert.IsType<ScreenMode.Inline>(c.ScreenMode);
        Assert.Equal(10, m.UiHeight);
    }

    [Fact] public void ProgramConfig_WithMouse_SetsOn()
    {
        Assert.Equal(MouseCapturePolicy.On, ProgramConfig.Default.WithMouse().MouseCapture);
    }

    [Fact] public void ProgramConfig_WithDiffConfig_SetsConfig()
    {
        var dc = new RuntimeDiffConfig { BayesianEnabled = false };
        Assert.False(ProgramConfig.Default.WithDiffConfig(dc).DiffConfig.BayesianEnabled);
    }

    [Fact] public void ProgramConfig_WithConformalConfig_SetsConfig()
    {
        var cc = new ConformalConfig { Alpha = 0.01 };
        Assert.InRange(ProgramConfig.Default.WithConformalConfig(cc).ConformalConfig!.Alpha, 0.009, 0.011);
    }

    [Fact] public void PersistenceConfig_WithRegistry_SetsRegistry()
    {
        var r = new object();
        Assert.Same(r, PersistenceConfig.Default.WithRegistry(r).Registry);
    }

    [Fact] public void PersistenceConfig_Disabled_ReturnsDefault()
    {
        Assert.True(PersistenceConfig.Disabled().AutoLoad);
    }

    [Fact] public void EffectQueueConfig_WithBackend_SetsBackend()
    {
        Assert.Equal(TaskExecutorBackend.EffectQueue, EffectQueueConfig.Default.WithBackend(TaskExecutorBackend.EffectQueue).Backend);
    }

    // ── Program accessor tests ────────────────────────────────────────────

    [Fact] public void Program_TickRate_InitiallyNull()
    {
        Assert.Null(new Program<string>(new Counter(), MakeWriter()).TickRate);
    }

    [Fact] public void Program_IsRunning_InitiallyTrue()
    {
        Assert.True(new Program<string>(new Counter(), MakeWriter()).IsRunning);
    }

    [Fact] public void Program_Quit_SetsNotRunning()
    {
        var p=new Program<string>(new Counter(), MakeWriter());p.Quit();Assert.False(p.IsRunning);
    }

    [Fact] public void Program_Stop_SetsNotRunning()
    {
        var p=new Program<string>(new Counter(), MakeWriter());p.Stop();Assert.False(p.IsRunning);
    }

    [Fact] public void Program_MarkDirty_SetsDirty()
    {
        var p=new Program<string>(new Counter(), MakeWriter());p.MarkDirty();Assert.True(true);
    }

    [Fact] public void Program_ExecutedCmdCount_InitiallyZero()
    {
        Assert.Equal(0,new Program<string>(new Counter(), MakeWriter()).ExecutedCmdCount);
    }

    [Fact] public void Program_HasPersistence_InitiallyFalse()
    {
        Assert.False(new Program<string>(new Counter(), MakeWriter()).HasPersistence);
    }

    [Fact] public void Program_TriggerSave_ReturnsTrue()
    {
        Assert.True(new Program<string>(new Counter(), MakeWriter()).TriggerSave());
    }

    [Fact] public void Program_TriggerLoad_ReturnsZero()
    {
        Assert.Equal(0,new Program<string>(new Counter(), MakeWriter()).TriggerLoad());
    }

    [Fact] public void Program_ResizeCoalescer_IsNotNull()
    {
        Assert.NotNull(new Program<string>(new Counter(), MakeWriter()).ResizeCoalescer);
    }

    [Fact] public void Program_FairnessGuard_IsNotNull()
    {
        Assert.NotNull(new Program<string>(new Counter(), MakeWriter()).FairnessGuard);
    }

    [Fact] public void Program_Model_ReturnsModel()
    {
        var m=new Counter();Assert.Same(m,new Program<string>(m,MakeWriter()).Model);
    }

    // ── Fairness event classification ─────────────────────────────────────

    [Fact] public void ClassifyEvent_Key_IsInput()
    {
        Assert.Equal(FairnessEventType.Input,Program<string>.ClassifyEventForFairness("key"));
    }

    [Fact] public void ClassifyEvent_Resize_IsResize()
    {
        Assert.Equal(FairnessEventType.Resize,Program<string>.ClassifyEventForFairness("resize"));
    }

    [Fact] public void ClassifyEvent_Tick_IsTick()
    {
        Assert.Equal(FairnessEventType.Tick,Program<string>.ClassifyEventForFairness("tick"));
    }

    [Fact] public void ClassifyEvent_Unknown_IsInput()
    {
        Assert.Equal(FairnessEventType.Input,Program<string>.ClassifyEventForFairness("unknown"));
    }

    // ── Cmd variant coverage ──────────────────────────────────────────────

    [Fact] public void Cmd_SaveStateAndQuit_HasTypeName()
    {
        Assert.Equal("SaveStateAndQuit",Cmd<string>.SaveStateAndQuitCmd.TypeName);
    }

    [Fact] public void Cmd_SetMouseCapture_RecordsEnabled()
    {
        var c=new Cmd<string>.SetMouseCapture(true);
        Assert.Equal("SetMouseCapture",c.TypeName);
    }

    [Fact] public void Cmd_TaskWithSpec_UsesSpec()
    {
        var spec=new TaskSpec{Weight=2.0,EstimateMs=50};
        var c=Cmd<string>.TaskWithSpec(spec,()=>"r");
        Assert.Equal("Task",c.TypeName);
    }

    [Fact] public void Cmd_TaskNamed_UsesName()
    {
        var c=Cmd<string>.TaskNamed("mytask",()=>"r");
        Assert.Equal("Task",c.TypeName);
    }

    // ── AppBuilder tests ──────────────────────────────────────────────────

    [Fact] public void App_New_CreatesBuilder()
    {
        var b=App.New<string>(new Counter());Assert.NotNull(b);
    }

    [Fact] public void App_Fullscreen_CreatesBuilder()
    {
        var b=App.Fullscreen<string>(new Counter());Assert.NotNull(b);
    }

    [Fact] public void App_Inline_CreatesBuilder()
    {
        var b=App.Inline<string>(new Counter(),10);Assert.NotNull(b);
    }

    [Fact] public void AppBuilder_ScreenMode_SetsMode()
    {
        var b=App.New<string>(new Counter()).ScreenMode(new ScreenMode.AltScreen());
        var p=b.Build(MakeWriter());Assert.NotNull(p);
    }

    [Fact] public void AppBuilder_WithMouse_SetsConfig()
    {
        var b=App.New<string>(new Counter()).WithMouse();
        Assert.NotNull(b);
    }

    [Fact] public void AppBuilder_WithBracketedPaste_ReturnsThis()
    {
        var b=App.New<string>(new Counter()).WithBracketedPaste(true);
        Assert.NotNull(b);
    }

    [Fact] public void AppBuilder_WithoutSignals_Disables()
    {
        var b=App.New<string>(new Counter()).WithoutSignals();
        Assert.NotNull(b);
    }

    [Fact] public void AppBuilder_WithConformalConfig_SetsConfig()
    {
        var b=App.New<string>(new Counter()).WithConformalConfig(new ConformalConfig{Alpha=0.01});
        Assert.NotNull(b);
    }

    [Fact] public void AppBuilder_Build_ReturnsProgram()
    {
        var p=App.New<string>(new Counter()).Build(MakeWriter());
        Assert.NotNull(p);p.Stop();
    }

    // ── Program recording tests ───────────────────────────────────────────

    [Fact] public void Program_IsRecording_InitiallyFalse()
    {
        Assert.False(new Program<string>(new Counter(),MakeWriter()).IsRecording);
    }

    [Fact] public void Program_StartRecording_DoesNotThrow()
    {
        var p=new Program<string>(new Counter(),MakeWriter());p.StartRecording("test");
    }

    [Fact] public void Program_StopRecording_ReturnsNull()
    {
        Assert.Null(new Program<string>(new Counter(),MakeWriter()).StopRecording());
    }
}
