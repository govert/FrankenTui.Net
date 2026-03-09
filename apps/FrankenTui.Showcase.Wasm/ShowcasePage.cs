using FrankenTui.Core;
using FrankenTui.Demo.Showcase;
using FrankenTui.Web;

namespace FrankenTui.Showcase.Wasm;

public static class ShowcasePage
{
    public static WebFrame RenderDefault() =>
        WebHost.Render(ShowcaseViewFactory.Build(inlineMode: false), new Size(64, 18));
}
