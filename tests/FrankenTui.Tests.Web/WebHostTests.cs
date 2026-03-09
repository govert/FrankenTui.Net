using FrankenTui.Demo.Showcase;
using FrankenTui.Showcase.Wasm;
using FrankenTui.Testing.Harness;
using FrankenTui.Web;

namespace FrankenTui.Tests.Web;

public sealed class WebHostTests
{
    [Fact]
    public void WebHostProducesPreformattedHtml()
    {
        var frame = WebHost.Render(ShowcaseViewFactory.Build(inlineMode: false), new FrankenTui.Core.Size(50, 12));

        Assert.Contains("<pre", frame.Html);
        Assert.Contains("FrankenTui.Net", frame.Text);
    }

    [Fact]
    public void ShowcasePageRendersSameCoreContent()
    {
        var page = ShowcasePage.RenderDefault();
        var web = RenderHarness.RenderWeb(ShowcaseViewFactory.Build(inlineMode: false), 64, 18);

        Assert.Contains("FrankenTui.Net", page.Text);
        Assert.Equal(web.Text, page.Text);
    }
}
