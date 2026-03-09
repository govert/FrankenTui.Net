using FrankenTui.Showcase.Wasm;
using FrankenTui.Extras;
using FrankenTui.Testing.Harness;

namespace FrankenTui.Tests.Web;

public sealed class WebDomRunnerTests
{
    [Fact]
    public async Task WebDomRunnerParsesHostedParityDocument()
    {
        var frame = ShowcasePage.RenderScenario(HostedParityScenarioId.Interaction, frame: 3);
        var document = await WebDomRunner.ParseAsync(frame);

        var host = document.QuerySelector(".frankentui-host");
        var rows = document.QuerySelectorAll(".ft-row");
        var accessibility = document.QuerySelectorAll(".ft-a11y li");

        Assert.NotNull(host);
        Assert.Equal("FrankenTui.Net Interaction", document.Title);
        Assert.Equal("interaction", host!.GetAttribute("data-scenario"));
        Assert.Equal(frame.Rows.Count, rows.Length);
        Assert.NotEmpty(accessibility);
        Assert.Contains("Focus", document.DocumentElement.TextContent);
    }
}
