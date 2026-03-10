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
        var style = Style ?? context.Theme.Default;
        var lines = TextRenderer.Layout(
            Document,
            context.Bounds.Width,
            RenderOptions ?? new TextRenderOptions(WrapMode));
        for (var row = 0; row < Math.Min(lines.Count, context.Bounds.Height); row++)
        {
            TextRenderer.Write(context.Buffer, context.Bounds.X, (ushort)(context.Bounds.Y + row), lines[row], style);
        }
    }
}
