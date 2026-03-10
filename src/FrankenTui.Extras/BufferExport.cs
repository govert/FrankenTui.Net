using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Web;
using RenderBuffer = FrankenTui.Render.Buffer;

namespace FrankenTui.Extras;

public sealed record ExportBundle(
    string PlainText,
    string Html,
    IReadOnlyList<string> Rows)
{
    public int TextLength => PlainText.Length;

    public int HtmlLength => Html.Length;
}

public static class BufferExport
{
    public static ExportBundle Capture(
        IRuntimeView view,
        Size size,
        Theme? theme = null,
        WebRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(view);

        var frame = WebHost.Render(view, size, theme, options);
        return new ExportBundle(frame.Text, frame.DocumentHtml, frame.Rows);
    }

    public static ExportBundle Capture(
        RenderBuffer buffer,
        WebRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        var frame = WebHost.Render(buffer, options);
        return new ExportBundle(frame.Text, frame.DocumentHtml, frame.Rows);
    }

    public static string ToPlainText(RenderBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return HeadlessBufferView.ScreenString(buffer);
    }

    public static string ToHtml(RenderBuffer buffer, WebRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return WebHost.Render(buffer, options).DocumentHtml;
    }
}
