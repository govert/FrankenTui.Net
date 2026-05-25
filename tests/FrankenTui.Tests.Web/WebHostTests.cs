using System.Globalization;
using FrankenTui.Demo.Showcase;
using FrankenTui.Extras;
using FrankenTui.Render;
using FrankenTui.Showcase.Wasm;
using FrankenTui.Testing.Harness;
using FrankenTui.Web;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Tests.Web;

public sealed class WebHostTests
{
    [Fact]
    public void WebHostProducesStyledSemanticHtml()
    {
        var frame = ShowcasePage.RenderScenario(HostedParityScenarioId.Interaction, frame: 2);

        Assert.Contains("frankentui-host", frame.Html);
        Assert.Contains("ft-run", frame.Html);
        Assert.Contains("data-scenario=\"interaction\"", frame.Html);
        Assert.Contains("aria-label", frame.Html);
        Assert.Contains("<!doctype html>", frame.DocumentHtml);
        Assert.NotEmpty(frame.Accessibility.Nodes);
    }

    [Fact]
    public void ShowcasePageRendersSameCoreContent()
    {
        var page = ShowcasePage.RenderScenario(HostedParityScenarioId.Tooling, frame: 2);
        var web = Ui.RenderHostedParity(false, 64, 18, HostedParityScenarioId.Tooling, 2);

        Assert.Contains("Tooling", page.Text);
        Assert.Equal(web.Text, page.Text);
        Assert.Equal(web.Metadata["scenario"], page.Metadata["scenario"]);
    }

    [Fact]
    public void HostedParityEvidenceWritesJsonAndHtmlArtifacts()
    {
        var session = HostedParitySession.ForFrame(false, 2, HostedParityScenarioId.Tooling);
        var evidence = RenderHarness.CaptureHostedParity(
            "hosted-parity-web-test",
            HostedParitySurface.Create(session),
            64,
            18,
            options: HostedParitySurface.CreateWebOptions(session));
        var paths = evidence.WriteArtifacts();

        Assert.Contains("\"scenario\": \"tooling\"", evidence.Json);
        Assert.True(File.Exists(paths["json"]));
        Assert.True(File.Exists(paths["html"]));
    }

    [Fact]
    public void ExtrasScenarioRendersExtrasPanels()
    {
        var page = ShowcasePage.RenderScenario(HostedParityScenarioId.Extras, frame: 2);

        Assert.Contains("data-scenario=\"extras\"", page.Html);
        Assert.Contains("Pane Workspace", page.Text);
        Assert.Contains("Extras", page.Text);
    }

    [Fact]
    public void ShowcaseRunnerCoreStepsSharedShowcaseModel()
    {
        var runner = ShowcasePage.CreateRunner(HostedParityScenarioId.Interaction, width: 40, height: 12);

        var first = runner.Step();
        runner.Resize(0, 0);
        var second = runner.Step();
        var accepted = runner.PushEncodedInput("""{"kind":"scenario","scenario":"Extras"}""");
        runner.Resize(64, 18);
        var third = runner.Step();

        Assert.True(first.Running);
        Assert.True(first.Rendered);
        Assert.Equal((ulong)1, first.FrameIndex);
        Assert.Equal((ulong)2, second.FrameIndex);
        Assert.True(accepted);
        Assert.Equal((ulong)3, third.FrameIndex);
        Assert.Contains("Interaction", first.Frame.Text);
        Assert.Contains("Extras", third.Frame.Text);
        Assert.Equal("extras", third.Frame.Metadata["scenario"]);
    }

    [Fact]
    public void ShowcaseRunnerCoreReleasesPaneCaptureForNativeTouchGesture()
    {
        var runner = ShowcasePage.CreateRunner(HostedParityScenarioId.Extras);

        var down = runner.PaneTouchPointerDownAt(31, activeTouchPoints: 1);
        var acquired = runner.PanePointerCaptureAcquired(31);
        var yielded = runner.PaneTouchPointerDownAt(32, activeTouchPoints: 2);
        var logs = runner.TakeLogs();

        Assert.True(down.Accepted);
        Assert.Equal(ShowcaseRunnerPaneCommand.Acquire, down.Command);
        Assert.True(acquired.Accepted);
        Assert.Equal(ShowcaseRunnerPaneOutcome.CaptureStateUpdated, acquired.Outcome);
        Assert.True(yielded.Accepted);
        Assert.Equal(ShowcaseRunnerPanePhase.NativeTouchGesture, yielded.Phase);
        Assert.Equal(ShowcaseRunnerPaneCommand.Release, yielded.Command);
        Assert.Equal((uint?)31, yielded.PointerId);
        Assert.Null(runner.ActivePointerId);
        Assert.Contains(logs, line => line.Contains("phase=native_touch_gesture", StringComparison.Ordinal) && line.Contains("command=release", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowcaseRunnerCoreReportsContextLossAndRenderStallInterruptions()
    {
        var runner = ShowcasePage.CreateRunner(HostedParityScenarioId.Extras);

        runner.PanePointerDownAt(41);
        runner.PanePointerCaptureAcquired(41);
        var contextLost = runner.PaneContextLost();
        var stallWithoutActivePointer = runner.PaneRenderStalled();
        runner.PanePointerDownAt(42);
        var renderStalled = runner.PaneRenderStalled();
        var logs = runner.TakeLogs();

        Assert.Equal(ShowcaseRunnerPanePhase.ContextLost, contextLost.Phase);
        Assert.Equal(ShowcaseRunnerPaneCommand.Release, contextLost.Command);
        Assert.Null(runner.ActivePointerId);
        Assert.False(stallWithoutActivePointer.Accepted);
        Assert.Equal("no_active_pointer", stallWithoutActivePointer.Reason);
        Assert.True(renderStalled.Accepted);
        Assert.Equal(ShowcaseRunnerPanePhase.RenderStalled, renderStalled.Phase);
        Assert.Equal(ShowcaseRunnerPaneCommand.None, renderStalled.Command);
        Assert.Contains(logs, line => line.Contains("phase=context_lost", StringComparison.Ordinal));
        Assert.Contains(logs, line => line.Contains("phase=render_stalled", StringComparison.Ordinal));
    }

    [Fact]
    public void WebHostRendersResolvedGraphemeText()
    {
        var buffer = new RenderBuffer(6, 1);
        buffer.SetText(0, 0, "e\u0301", Cell.FromChar('x'));
        buffer.SetText(1, 0, "🧑🏽\u200D💻", Cell.FromChar('x'));

        var frame = WebHost.Render(buffer);

        Assert.Equal("e\u0301🧑🏽\u200D💻", frame.Rows[0]);
        Assert.DoesNotContain("\u25A1", frame.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void ShowcaseRunnerCorePaneBlurReleasesActiveCapture()
    {
        var runner = ShowcasePage.CreateRunner(HostedParityScenarioId.Extras);
        runner.PanePointerDownAt(61);
        runner.PanePointerCaptureAcquired(61);
        var blur = runner.PaneBlur();
        Assert.Null(runner.ActivePointerId);
        Assert.True(blur.Accepted);
        Assert.Equal(ShowcaseRunnerPanePhase.Blur, blur.Phase);
        Assert.Equal(ShowcaseRunnerPaneCommand.Release, blur.Command);
    }

    [Fact]
    public void ShowcaseRunnerCorePaneVisibilityHiddenReleasesActiveCapture()
    {
        var runner = ShowcasePage.CreateRunner(HostedParityScenarioId.Extras);
        runner.PanePointerDownAt(62);
        runner.PanePointerCaptureAcquired(62);
        var hidden = runner.PaneVisibilityHidden();
        Assert.Null(runner.ActivePointerId);
        Assert.True(hidden.Accepted);
        Assert.Equal(ShowcaseRunnerPanePhase.VisibilityHidden, hidden.Phase);
        Assert.Equal(ShowcaseRunnerPaneCommand.Release, hidden.Command);
    }

    [Fact]
    public void ShowcaseRunnerCorePaneLostPointerCaptureCancelsWithoutReleaseCommand()
    {
        var runner = ShowcasePage.CreateRunner(HostedParityScenarioId.Extras);
        runner.PanePointerDownAt(63);
        var lost = runner.PaneLostPointerCapture(63);
        Assert.Null(runner.ActivePointerId);
        Assert.True(lost.Accepted);
        Assert.Equal(ShowcaseRunnerPanePhase.LostPointerCapture, lost.Phase);
        Assert.Equal(ShowcaseRunnerPaneCommand.None, lost.Command);
    }

    [Fact]
    public void ShowcaseRunnerCorePaneLeaveBeforeCaptureAckCancelsActivePointer()
    {
        var runner = ShowcasePage.CreateRunner(HostedParityScenarioId.Extras);
        runner.PanePointerDownAt(52);
        var leave = runner.PanePointerLeave(52);
        Assert.Null(runner.ActivePointerId);
        Assert.Equal(ShowcaseRunnerPanePhase.PointerLeave, leave.Phase);
        Assert.True(leave.Accepted);
    }

    [Fact]
    public void ShowcaseRunnerCorePaneLeaveAfterCaptureAckIsIgnored()
    {
        var runner = ShowcasePage.CreateRunner(HostedParityScenarioId.Extras);
        runner.PanePointerDownAt(53);
        runner.PanePointerCaptureAcquired(53);
        var leave = runner.PanePointerLeave(53);
        Assert.True(leave.Accepted);
        Assert.Equal(ShowcaseRunnerPaneCommand.None, leave.Command);
        Assert.Equal("pointer_leave_after_capture", leave.Reason);
    }

    [Fact]
    public void ShowcaseRunnerCorePaneLogsAreDrainedWithTakeLogs()
    {
        var runner = ShowcasePage.CreateRunner(HostedParityScenarioId.Extras);
        runner.PanePointerDownAt(71);
        runner.PanePointerCaptureAcquired(71);
        runner.PaneBlur();
        var logs = runner.TakeLogs();
        Assert.Equal(3, logs.Count);
        Assert.Contains(logs, line => line.Contains("phase=blur", StringComparison.Ordinal) && line.Contains("command=release", StringComparison.Ordinal));
        Assert.Empty(runner.TakeLogs());
    }

    [Fact]
    public void ShowcaseRunnerCoreExposesPatchHashAndTimeControls()
    {
        var runner = ShowcasePage.CreateRunner(screenNumber: 1, width: 80, height: 24);
        var initialHash = runner.PatchHash();
        runner.AdvanceTime(double.NaN);
        runner.AdvanceTime(double.PositiveInfinity);
        runner.AdvanceTime(-1);
        Assert.Equal(initialHash, runner.PatchHash());
        runner.AdvanceTime(16);
        var advanced = runner.Step();
        Assert.Equal(0, advanced.EventsProcessed);
        Assert.StartsWith("fnv1a64:", runner.PatchHash(), StringComparison.Ordinal);
        runner.SetTime(double.NaN);
        runner.SetTime(double.NegativeInfinity);
        runner.SetTime(-123);
        runner.SetTime(16_000_000);
        var setTime = runner.Step();
        Assert.True(setTime.Rendered);
        var stats = runner.PatchStats();
        Assert.NotNull(stats);
        Assert.True(stats.DirtyCells > 0);
        Assert.True(stats.PatchCount > 0);
        Assert.True(stats.BytesUploaded > stats.DirtyCells);
    }

    [Fact]
    public void ShowcaseRunnerCorePreparesAndTakesFlatPatches()
    {
        var runner = ShowcasePage.CreateRunner(screenNumber: 30, width: 10, height: 4);
        runner.PrepareFlatPatches();
        Assert.True(runner.FlatCellsPtr > 0);
        Assert.True(runner.FlatSpansPtr > 0);
        Assert.True(runner.FlatCellsLen > 0);
        Assert.True(runner.FlatSpansLen > 0);
        var batch = runner.TakeFlatPatches();
        Assert.Equal(runner.FlatCellsLen, batch.Cells.Count);
        Assert.Equal(runner.FlatSpansLen, batch.Spans.Count);
        Assert.Equal(0, batch.Cells.Count % 4);
        Assert.Equal(0, batch.Spans.Count % 2);
        Assert.Equal(batch.Cells.Count / 4, runner.PatchStats()!.DirtyCells);
    }

    [Fact]
    public void ShowcaseRunnerCorePaneWorkspaceMethodsFollowUpstreamApiShape()
    {
        var runner = ShowcasePage.CreateRunner();
        Assert.Equal((ulong)0, runner.PaneWorkspaceGeneration());
        Assert.False(runner.PaneWorkspaceDirty());
        Assert.True(runner.PaneImportWorkspaceSnapshot("{\"v\":1}"));
        Assert.Equal((ulong)1, runner.PaneWorkspaceGeneration());
        Assert.True(runner.PaneWorkspaceDirty());
        Assert.True(runner.PaneMarkWorkspaceSaved(1));
        Assert.False(runner.PaneWorkspaceDirty());
        Assert.False(runner.PaneMarkWorkspaceSaved(999));
        Assert.False(runner.PaneApplyLayoutMode(-1, 1));
        Assert.False(runner.PaneApplyLayoutMode(0, 0));
        Assert.True(runner.PaneApplyLayoutMode(1, 42));
        runner.Destroy();
        Assert.True(runner.IsRunning);
    }

    [Fact]
    public void ShowcaseRunnerCoreRoutesEncodedKeyToNumberedScreenState()
    {
        var runner = ShowcasePage.CreateRunner(screenNumber: 44, width: 80, height: 24);
        Assert.True(runner.PushEncodedInput("""{"kind":"key","phase":"down","code":"KeyL","mods":1}"""));
        Assert.Equal(44, runner.ScreenNumber);
        var step = runner.Step();
        Assert.Equal(1, step.EventsProcessed);
        Assert.Equal(45, runner.ScreenNumber);
        Assert.Contains("Quake", runner.RenderCurrent().Text);
    }

    [Fact]
    public void ShowcaseRunnerCoreRoutesEncodedMouseToNumberedScreenState()
    {
        var runner = ShowcasePage.CreateRunner(screenNumber: 30, width: 120, height: 32);
        Assert.True(runner.PushEncodedInput("""{"kind":"mouse","phase":"down","button":0,"x":3,"y":5,"mods":0}"""));
        Assert.DoesNotContain("export=ready", runner.RenderCurrent().Text);
        var step = runner.Step();
        Assert.Equal(1, step.EventsProcessed);
        Assert.Contains("selected=Nord", step.Frame.Text);
    }

    [Fact]
    public void ShowcaseRunnerCoreEncodedHelpOverlayOpensAndClosesViaEsc()
    {
        var runner = ShowcasePage.CreateRunner(screenNumber: 30, width: 120, height: 32);
        Assert.DoesNotContain("Keybindings", runner.RenderCurrent().Text);
        Assert.True(runner.PushEncodedInput("""{"kind":"key","phase":"down","key":"?","code":"Slash","mods":0}"""));
        var help = runner.Step();
        Assert.Contains("Keybindings", help.Frame.Text);
        Assert.True(runner.PushEncodedInput("""{"kind":"key","phase":"down","code":"Escape","mods":0}"""));
        runner.Step();
        Assert.DoesNotContain("Keybindings", runner.RenderCurrent().Text);
    }

    [Fact]
    public void ShowcasePageAndRunnerRenderSharedShowcaseScreens()
    {
        var runner = ShowcasePage.CreateRunner(width: 80, height: 24);
        var accepted = runner.PushEncodedInput("""{"screen":30}""");
        var selected = runner.ScreenNumber;
        var stepped = runner.Step();
        Assert.True(accepted);
        Assert.Equal(30, selected);
        Assert.Equal(1, stepped.EventsProcessed);
        Assert.Contains("Theme Studio", stepped.Frame.Text);
        Assert.Equal("30", stepped.Frame.Metadata["screen-number"]);
    }

    [Fact]
    public void ShowcaseRunnerCoreAppliesScreenNavigationKeysToNumberedScreens()
    {
        var runner = ShowcasePage.CreateRunner(screenNumber: 44, width: 80, height: 24);
        runner.PushEncodedInput("""{"kind":"key","phase":"down","code":"KeyL","mods":1}""");
        Assert.Equal(44, runner.ScreenNumber);
        runner.Step();
        Assert.Equal(45, runner.ScreenNumber);
        runner.PushEncodedInput("""{"kind":"key","phase":"down","code":"Tab","mods":0}""");
        runner.Step();
        Assert.Equal(1, runner.ScreenNumber);
        runner.PushEncodedInput("""{"kind":"key","phase":"down","code":"Digit0","mods":0}""");
        runner.Step();
        Assert.Equal(10, runner.ScreenNumber);
        runner.PushEncodedInput("""{"kind":"key","phase":"up","code":"ArrowLeft","mods":0}""");
        var keyUp = runner.Step();
        Assert.Equal(1, keyUp.EventsProcessed);
        Assert.Equal(10, runner.ScreenNumber);
    }

    [Fact]
    public void ShowcaseRunnerCoreFilesScreenRenderLoopDoesNotPanic()
    {
        var runner = ShowcasePage.CreateRunner(width: 80, height: 24);
        for (var screen = 1; screen <= 45; screen++)
        {
            runner.PushEncodedInput("{\"screen\":" + screen.ToString(CultureInfo.InvariantCulture) + "}");
            var result = runner.Step();
            Assert.True(result.Rendered);
            Assert.NotEmpty(result.Frame.Text);
        }
    }

    [Fact]
    public void ShowcaseRunnerCoreStepNoEventsHasNonzeroFrameIndex()
    {
        var runner = ShowcasePage.CreateRunner(width: 80, height: 24);
        var first = runner.Step();
        Assert.True(first.Rendered);
        Assert.Equal(0, first.EventsProcessed);
        Assert.Equal((ulong)1, first.FrameIndex);
    }

    [Fact]
    public void ShowcaseRunnerCoreResizeClampsZeroDimensionsAndRendersAfterStep()
    {
        var runner = ShowcasePage.CreateRunner(width: 80, height: 24);
        runner.Resize(0, 0);
        var result = runner.Step();
        Assert.True(result.Rendered);
        Assert.Equal(1, result.EventsProcessed);
    }

    [Fact]
    public void ShowcaseRunnerCorePatchHashMatchesAfterPrepareFlatPatches()
    {
        var runner = ShowcasePage.CreateRunner(screenNumber: 30, width: 10, height: 4);
        var liveHash = runner.PatchHash();
        runner.PrepareFlatPatches();
        var preparedHash = runner.PatchHash();
        Assert.Equal(liveHash, preparedHash);
        Assert.StartsWith("fnv1a64:", liveHash, StringComparison.Ordinal);
    }

    [Fact]
    public void ShowcaseRunnerCoreAssertStoppedStepPreservesQuitState()
    {
        var runner = ShowcasePage.CreateRunner(width: 80, height: 24);
        runner.Step();
        Assert.True(runner.IsRunning);
        Assert.Equal((ulong)1, runner.FrameIndex);
        runner.PushEncodedInput("""{"kind":"quit"}""");
        var stopped = runner.Step();
        Assert.False(stopped.Running);
        Assert.False(runner.IsRunning);
        Assert.Equal((ulong)1, runner.FrameIndex);
        var afterStop = runner.Step();
        Assert.False(afterStop.Running);
        Assert.False(afterStop.Rendered);
    }
}
