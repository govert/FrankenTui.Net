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
}
