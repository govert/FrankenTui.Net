using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

public sealed class ListWidget : IWidget
{
    public IReadOnlyList<string> Items { get; init; } = [];

    public int SelectedIndex { get; init; } = -1;

    public int FocusedIndex { get; init; } = -1;

    public int HoveredIndex { get; init; } = -1;

    public void Render(RuntimeRenderContext context)
    {
        for (var row = 0; row < Math.Min(Items.Count, context.Bounds.Height); row++)
        {
            var style = row switch
            {
                _ when row == SelectedIndex && row == FocusedIndex => context.Theme.Selection,
                _ when row == SelectedIndex => context.Theme.Accent,
                _ when row == FocusedIndex => context.Theme.Interactive.Focused,
                _ when row == HoveredIndex => context.Theme.Interactive.Hover,
                _ => context.Theme.Default
            };
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
