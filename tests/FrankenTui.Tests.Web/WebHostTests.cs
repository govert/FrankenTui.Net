using FrankenTui.Demo.Showcase;
using FrankenTui.Extras;
using FrankenTui.Showcase.Wasm;
using FrankenTui.Testing.Harness;

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
        Assert.Contains("Command Palette", page.Text);
    }
}
