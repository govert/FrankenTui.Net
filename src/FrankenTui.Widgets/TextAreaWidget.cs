using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Text;

namespace FrankenTui.Widgets;

public sealed class TextAreaWidget : IWidget
{
    public TextDocument Document { get; init; } = TextDocument.FromString(string.Empty);

    public TextCursor Cursor { get; init; }

    public bool HasFocus { get; init; }

    public string? PlaceholderText { get; init; }

    public string? StatusText { get; init; }

    public TextRenderOptions? RenderOptions { get; init; }

    public void Render(RuntimeRenderContext context)
    {
        var content = Document.Lines.Count == 0 && !string.IsNullOrWhiteSpace(PlaceholderText)
            ? TextDocument.FromString(PlaceholderText)
            : Document;
        var lines = TextRenderer.Layout(
            content,
            context.Bounds.Width,
            RenderOptions ?? new TextRenderOptions(TextWrapMode.Character));
        var contentRows = StatusText is null ? context.Bounds.Height : Math.Max(context.Bounds.Height - 1, 0);
        for (var row = 0; row < Math.Min(lines.Count, contentRows); row++)
        {
            var style = Document.Lines.Count == 0 && !string.IsNullOrWhiteSpace(PlaceholderText)
                ? context.Theme.Muted
                : context.Theme.Default;
            TextRenderer.Write(context.Buffer, context.Bounds.X, (ushort)(context.Bounds.Y + row), lines[row], style);
        }

        if (!string.IsNullOrWhiteSpace(StatusText) && context.Bounds.Height > 0)
        {
            BufferPainter.WriteText(
                context.Buffer,
                context.Bounds.X,
                (ushort)(context.Bounds.Bottom - 1),
                StatusText,
                context.Theme.Muted.ToCell());
        }

        if (HasFocus && Cursor.Line < contentRows && Cursor.Column < context.Bounds.Width)
        {
            context.Buffer.Set(
                (ushort)(context.Bounds.X + Cursor.Column),
                (ushort)(context.Bounds.Y + Cursor.Line),
                context.Theme.Accent.ToCell('▌'));
        }
    }
}
