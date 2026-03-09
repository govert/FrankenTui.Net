using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

public sealed class ListWidget : IWidget
{
    public IReadOnlyList<string> Items { get; init; } = [];

    public int SelectedIndex { get; init; } = -1;

    public void Render(RuntimeRenderContext context)
    {
        for (var row = 0; row < Math.Min(Items.Count, context.Bounds.Height); row++)
        {
            var style = row == SelectedIndex ? context.Theme.Selection : context.Theme.Default;
            var prefix = row == SelectedIndex ? "› " : "• ";
            BufferPainter.WriteText(
                context.Buffer,
                context.Bounds.X,
                (ushort)(context.Bounds.Y + row),
                $"{prefix}{Items[row]}",
                style.ToCell());
        }
    }
}
