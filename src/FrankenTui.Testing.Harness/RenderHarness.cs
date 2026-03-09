using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Web;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Testing.Harness;

public static class RenderHarness
{
    public static RenderSnapshot Render(IRuntimeView view, ushort width, ushort height, Theme? theme = null)
    {
        var buffer = new RenderBuffer(width, height);
        view.Render(new RuntimeRenderContext(buffer, Rect.FromSize(width, height), theme ?? Theme.DefaultTheme));
        var rows = HeadlessBufferView.ScreenText(buffer);
        return new RenderSnapshot(rows, string.Join(Environment.NewLine, rows));
    }

    public static WebFrame RenderWeb(IRuntimeView view, ushort width, ushort height, Theme? theme = null) =>
        WebHost.Render(view, new Size(width, height), theme);
}
