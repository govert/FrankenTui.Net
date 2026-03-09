using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Text;

namespace FrankenTui.Widgets;

public sealed class TextAreaWidget : IWidget
{
    public TextDocument Document { get; init; } = TextDocument.FromString(string.Empty);

    public TextCursor Cursor { get; init; }

    public void Render(RuntimeRenderContext context)
    {
        var lines = TextWrapper.Wrap(Document, context.Bounds.Width, TextWrapMode.Character);
        for (var row = 0; row < Math.Min(lines.Count, context.Bounds.Height); row++)
        {
            BufferPainter.WriteText(context.Buffer, context.Bounds.X, (ushort)(context.Bounds.Y + row), lines[row], context.Theme.Default.ToCell());
        }

        if (Cursor.Line < context.Bounds.Height && Cursor.Column < context.Bounds.Width)
        {
            context.Buffer.Set(
                (ushort)(context.Bounds.X + Cursor.Column),
                (ushort)(context.Bounds.Y + Cursor.Line),
                context.Theme.Accent.ToCell('▌'));
        }
    }
}
