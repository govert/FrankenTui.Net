using System.Text;
using FrankenTui.Demo.Showcase;
using FrankenTui.Backend;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Simd;
using FrankenTui.Style;
using FrankenTui.Tty;
using FrankenTui.Widgets;
using RenderBuffer = FrankenTui.Render.Buffer;

SimdAccelerators.EnableIfSupported();

if (HasFlag(args, "--help") || HasFlag(args, "-h"))
{
    foreach (var line in ShowcaseCliHelp.Lines)
    {
        Console.WriteLine(line);
    }

    return;
}

if (HasFlag(args, "--version") || HasFlag(args, "-V"))
{
    Console.WriteLine("FrankenTui.Net demo showcase 0.1.0");
    return;
}

var options = ShowcaseCliOptions.Parse(args);
var paneWorkspaceLoad = ShowcasePaneWorkspacePersistence.Load(options.PaneWorkspacePath);
ITerminalBackend backend = OperatingSystem.IsWindows()
    ? new WindowsTerminalBackend()
    : new UnixTerminalBackend();
await using var session = new TerminalSession(
    backend,
    new TerminalSessionOptions
    {
        InlineMode = options.InlineMode,
        UseMouseTracking = options.UseMouseTracking
    });

using var cancellation = new CancellationTokenSource();
if (options.ExitAfterMilliseconds > 0)
{
    cancellation.CancelAfter(TimeSpan.FromMilliseconds(options.ExitAfterMilliseconds));
}

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

await session.EnterAsync(cancellation.Token);
var viewport = ShowcaseViewportResolver.Resolve(options, backend.Size);
var effectiveOptions = options with { Width = viewport.Width, Height = viewport.Height };
using var evidence = ShowcaseEvidenceJsonlWriter.Create(effectiveOptions.EvidenceJsonlPath);
using var vfxHarnessLog = ShowcaseHarnessJsonlWriter.CreateVfx(effectiveOptions);
using var mermaidHarnessLog = ShowcaseHarnessJsonlWriter.CreateMermaid(effectiveOptions);
evidence?.WriteLaunch(effectiveOptions, paneWorkspaceLoad);
evidence?.WriteScreenInit(effectiveOptions);
vfxHarnessLog?.WriteLaunch(effectiveOptions);
mermaidHarnessLog?.WriteLaunch(effectiveOptions);

if (effectiveOptions.InteractiveMode)
{
    var finalPaneWorkspace = await RunInteractiveAsync(
        session,
        backend,
        effectiveOptions.InlineMode,
        effectiveOptions.Width,
        effectiveOptions.Height,
        effectiveOptions.ScreenNumber,
        effectiveOptions.Tour,
        effectiveOptions.TourSpeed,
        effectiveOptions.TourStartStep,
        effectiveOptions.Language,
        effectiveOptions.FlowDirection,
        effectiveOptions.TickIntervalMilliseconds,
        effectiveOptions.ExitAfterTicks,
        evidence,
        effectiveOptions,
        paneWorkspaceLoad.Workspace,
        paneWorkspaceLoad,
        cancellation.Token);
    var saveResult = ShowcasePaneWorkspacePersistence.Save(effectiveOptions.PaneWorkspacePath, finalPaneWorkspace);
    evidence?.WritePaneWorkspaceSaveEvent(effectiveOptions, RuntimeFrameStats.Empty, stepIndex: 0, frame: (int)effectiveOptions.ExitAfterTicks, saveResult);
}
else
{
    var current = new RenderBuffer(effectiveOptions.Width, effectiveOptions.Height);
    var theme = Theme.DefaultTheme;
    for (var frame = 0; frame < Math.Max(effectiveOptions.Frames ?? 1, 1); frame++)
    {
        current.Clear();
        IWidget view = ShowcaseViewFactory.Build(
            effectiveOptions.InlineMode,
            effectiveOptions.ScreenNumber,
            frame,
            effectiveOptions.Tour,
            effectiveOptions.TourSpeed,
            effectiveOptions.TourStartStep,
            effectiveOptions.Language,
            effectiveOptions.FlowDirection,
            effectiveOptions.Width,
            effectiveOptions.Height,
            vfxEffect: effectiveOptions.VfxHarness.Effect);
        view.Render(new RuntimeRenderContext(current, Rect.FromSize(effectiveOptions.Width, effectiveOptions.Height), theme));
        await session.PresentAsync(current, frame == 0 ? BufferDiff.Full(effectiveOptions.Width, effectiveOptions.Height) : null, cancellationToken: cancellation.Token);
        var stats = RuntimeFrameStats.Empty;
        evidence?.WriteFrame("scripted_frame", effectiveOptions, stats, stepIndex: frame, frame: frame);
        vfxHarnessLog?.WriteScriptedVfxInputEvents(effectiveOptions, frame);
        vfxHarnessLog?.WriteFrame(effectiveOptions, frame, stats, current);
        mermaidHarnessLog?.WriteFrame(effectiveOptions, frame, stats, current);
    }

    VerifyOrUpdateVfxGolden(effectiveOptions);

    var saveResult = ShowcasePaneWorkspacePersistence.Save(effectiveOptions.PaneWorkspacePath, paneWorkspaceLoad.Workspace);
    evidence?.WritePaneWorkspaceSaveEvent(effectiveOptions, RuntimeFrameStats.Empty, stepIndex: Math.Max(effectiveOptions.Frames ?? 1, 1), frame: Math.Max(effectiveOptions.Frames ?? 1, 1), saveResult);
}

static bool HasFlag(string[] arguments, string name) =>
    Array.Exists(arguments, argument =>
        argument.Equals(name, StringComparison.OrdinalIgnoreCase) ||
        argument.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase));

static void VerifyOrUpdateVfxGolden(ShowcaseCliOptions options)
{
    if (!options.VfxHarness.Enabled ||
        string.IsNullOrWhiteSpace(options.VfxHarness.JsonlPath) ||
        string.IsNullOrWhiteSpace(options.VfxHarness.GoldenPath))
    {
        return;
    }

    var hashes = ShowcaseVfxGoldenRegistry.ExtractFrameHashesFromJsonl(options.VfxHarness.JsonlPath);
    var result = ShowcaseVfxGoldenRegistry.VerifyOrUpdate(
        options.VfxHarness.GoldenPath,
        hashes,
        options.VfxHarness.UpdateGolden);
    if (result.Outcome == ShowcaseVfxGoldenOutcome.Pass)
    {
        return;
    }

    var detail = result.Outcome == ShowcaseVfxGoldenOutcome.Missing
        ? "missing golden hash vector"
        : $"mismatch at frame {result.MismatchIndex}: expected={result.Expected?.ToString() ?? "none"} actual={result.Actual?.ToString() ?? "none"}";
    throw new InvalidOperationException($"VFX golden verification failed for {options.VfxHarness.GoldenPath}: {detail}");
}

static async Task<FrankenTui.Layout.PaneWorkspaceState> RunInteractiveAsync(
    TerminalSession session,
    ITerminalBackend backend,
    bool inlineMode,
    ushort width,
    ushort height,
    int screenNumber,
    bool tour,
    double tourSpeed,
    int tourStartStep,
    string language,
    WidgetFlowDirection flowDirection,
    uint tickIntervalMilliseconds,
    uint exitAfterTicks,
    ShowcaseEvidenceJsonlWriter? evidence,
    ShowcaseCliOptions options,
    FrankenTui.Layout.PaneWorkspaceState initialPaneWorkspace,
    ShowcasePaneWorkspaceLoadResult paneWorkspaceLoad,
    CancellationToken cancellationToken)
{
    var runtime = new AppRuntime<ShowcaseDemoState, ShowcaseDemoMessage>(
        backend,
        new Size(width, height),
        Theme.DefaultTheme);
    var program = new ShowcaseInteractiveProgram(
        inlineMode,
        screenNumber,
        tour,
        tourSpeed,
        tourStartStep,
        language,
        flowDirection,
        new Size(width, height),
        initialPaneWorkspace,
        paneWorkspaceLoad,
        options.UseMouseTracking);
    var app = new AppSession<ShowcaseDemoState, ShowcaseDemoMessage>(runtime, program);
    var controller = new RuntimeInputController<ShowcaseDemoState, ShowcaseDemoMessage>(
        static model => model.CreateKeybindingState(),
        (_, input) => [new ShowcaseInputMessage(input, runtime.FrameStats)],
        static (_, terminalEvent) => TranslateInput(terminalEvent),
        keybindingConfig: KeybindingConfig.FromEnvironment());
    await runtime.ResizeAsync(new Size(width, height), cancellationToken);
    await app.RenderCurrentAsync(cancellationToken);

    var tickCount = 0u;
    var lastTickAt = DateTimeOffset.UtcNow;
    DateTimeOffset? lastPerfHudStallLogAt = null;
    var pollInterval = TimeSpan.FromMilliseconds(Math.Max(tickIntervalMilliseconds, 1));
    while (!app.Model.QuitRequested && !cancellationToken.IsCancellationRequested)
    {
        if (!await backend.PollEventAsync(pollInterval, cancellationToken))
        {
            var tickStartedAt = DateTimeOffset.UtcNow;
            var elapsedSinceTick = tickStartedAt - lastTickAt;
            var beforeTick = app.Model;
            await controller.TickAsync(app, tickStartedAt, cancellationToken);
            await app.DispatchAsync(new ShowcaseTimerMessage(tickStartedAt, runtime.FrameStats), cancellationToken);
            tickCount++;
            evidence?.WriteTourEvent("tick", options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, beforeTick, app.Model);
            evidence?.WritePaletteEvent("tick", options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, beforeTick, app.Model);
            evidence?.WriteA11yEvent("tick", options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, beforeTick, app.Model);
            evidence?.WritePerfHudEvent("tick", options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, beforeTick, app.Model);
            evidence?.WritePerfHudStatsEvent(options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, app.Model);
            if (ShouldEmitPerfHudStall(app.Model, elapsedSinceTick, tickStartedAt, lastPerfHudStallLogAt))
            {
                evidence?.WritePerfHudStallEvent(
                    options,
                    runtime.FrameStats,
                    runtime.CurrentStepIndex,
                    (int)tickCount,
                    app.Model,
                    (long)elapsedSinceTick.TotalMilliseconds);
                lastPerfHudStallLogAt = tickStartedAt;
            }

            evidence?.WriteFrame("tick", options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, app.Model);
            await ApplyMouseCaptureChangeAsync(session, beforeTick, app.Model, cancellationToken);
            lastTickAt = tickStartedAt;
            if (exitAfterTicks > 0 && tickCount >= exitAfterTicks)
            {
                break;
            }

            continue;
        }

        var terminalEvent = await backend.ReadEventAsync(cancellationToken);
        if (terminalEvent is null)
        {
            continue;
        }

        var beforeInput = app.Model;
        await controller.ProcessAsync(app, terminalEvent, cancellationToken);
        await app.DispatchAsync(new ShowcaseTimerMessage(DateTimeOffset.UtcNow, runtime.FrameStats), cancellationToken);
        tickCount++;
        evidence?.WriteMouseEvent("input", options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, terminalEvent, beforeInput, app.Model);
        evidence?.WriteMouseCaptureToggleEvent("input", options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, terminalEvent, beforeInput, app.Model);
        evidence?.WriteTourEvent("input", options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, beforeInput, app.Model);
        evidence?.WritePaletteEvent("input", options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, beforeInput, app.Model);
        evidence?.WriteA11yEvent("input", options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, beforeInput, app.Model);
        evidence?.WritePerfHudEvent("input", options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, beforeInput, app.Model);
        evidence?.WritePerfHudStatsEvent(options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, app.Model);
        evidence?.WriteFrame("input", options, runtime.FrameStats, runtime.CurrentStepIndex, (int)tickCount, app.Model);
        await ApplyMouseCaptureChangeAsync(session, beforeInput, app.Model, cancellationToken);
        if (exitAfterTicks > 0 && tickCount >= exitAfterTicks)
        {
            break;
        }
    }

    return app.Model.Session.PaneWorkspace;
}

static bool ShouldEmitPerfHudStall(
    ShowcaseDemoState state,
    TimeSpan elapsedSinceTick,
    DateTimeOffset now,
    DateTimeOffset? lastLogAt)
{
    var warnAfter = TimeSpan.FromMilliseconds(1000);
    var logInterval = TimeSpan.FromMilliseconds(1000);
    return state.PerfHudVisible &&
        elapsedSinceTick >= warnAfter &&
        (lastLogAt is null || now - lastLogAt >= logInterval);
}

static ValueTask ApplyMouseCaptureChangeAsync(
    TerminalSession session,
    ShowcaseDemoState before,
    ShowcaseDemoState after,
    CancellationToken cancellationToken)
{
    return before.MouseCaptureEnabled == after.MouseCaptureEnabled
        ? ValueTask.CompletedTask
        : session.SetMouseCaptureAsync(after.MouseCaptureEnabled, cancellationToken);
}

static RuntimeInputTranslation TranslateInput(TerminalEvent terminalEvent)
{
    if (terminalEvent is not KeyTerminalEvent keyEvent ||
        !keyEvent.Gesture.IsCharacter ||
        keyEvent.Gesture.Modifiers != TerminalModifiers.None ||
        keyEvent.Gesture.Character is not { } rune)
    {
        return new RuntimeInputTranslation(terminalEvent, terminalEvent);
    }

    var mapped = char.ToLowerInvariant((char)rune.Value) switch
    {
        'h' => TerminalEvent.Key(new KeyGesture(TerminalKey.Left, TerminalModifiers.None), keyEvent.Timestamp),
        'j' => TerminalEvent.Key(new KeyGesture(TerminalKey.Down, TerminalModifiers.None), keyEvent.Timestamp),
        'k' => TerminalEvent.Key(new KeyGesture(TerminalKey.Up, TerminalModifiers.None), keyEvent.Timestamp),
        'l' => TerminalEvent.Key(new KeyGesture(TerminalKey.Right, TerminalModifiers.None), keyEvent.Timestamp),
        _ => terminalEvent
    };
    return new RuntimeInputTranslation(terminalEvent, mapped);
}
