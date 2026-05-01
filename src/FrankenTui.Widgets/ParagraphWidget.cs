using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Style;
using FrankenTui.Text;

namespace FrankenTui.Widgets;

public sealed class ParagraphWidget : IWidget
{
    public ParagraphWidget(string text)
    {
        Document = TextDocument.FromString(text);
    }

    public TextDocument Document { get; init; }

    public TextWrapMode WrapMode { get; init; } = TextWrapMode.Word;

    public UiStyle? Style { get; init; }

    public TextRenderOptions? RenderOptions { get; init; }

    public void Render(RuntimeRenderContext context)
    {
        var style = WidgetRenderHelpers.ApplyStyling(context)
            ? Style ?? context.Theme.Default
            : context.Theme.Default;
        WidgetRenderHelpers.ClearTextArea(context, style);
        if (!WidgetRenderHelpers.RenderContent(context))
        {
            return;
        }

        var options = RenderOptions ?? new TextRenderOptions(WrapMode);
        var lines = options.FirstVisualLine > 0 || options.MaxVisualLines is not null
            ? TextRenderer.LayoutViewport(
                Document,
                context.Bounds.Width,
                options.FirstVisualLine,
                Math.Min(options.MaxVisualLines ?? context.Bounds.Height, context.Bounds.Height),
                options)
            : TextRenderer.Layout(
                Document,
                context.Bounds.Width,
                options);
        for (var row = 0; row < Math.Min(lines.Count, context.Bounds.Height); row++)
        {
            TextRenderer.Write(context.Buffer, context.Bounds.X, (ushort)(context.Bounds.Y + row), lines[row], style);
        }
    }
}
