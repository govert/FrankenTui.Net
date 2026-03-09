using System.Net;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Web;

public static class WebHost
{
    public static WebFrame Render(IRuntimeView view, Size size, Theme? theme = null)
    {
        ArgumentNullException.ThrowIfNull(view);

        var buffer = new RenderBuffer(size.Width, size.Height);
        view.Render(new RuntimeRenderContext(buffer, Rect.FromSize(size.Width, size.Height), theme ?? Theme.DefaultTheme));
        return Render(buffer);
    }

    public static WebFrame Render(RenderBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        var rows = HeadlessBufferView.ScreenText(buffer);
        var html = $"<pre class=\"frankentui-frame\">{WebUtility.HtmlEncode(string.Join(Environment.NewLine, rows))}</pre>";
        return new WebFrame(rows, html);
    }
}
