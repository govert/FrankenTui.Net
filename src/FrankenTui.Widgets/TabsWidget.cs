using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

public sealed class TabsWidget : IWidget
{
    public IReadOnlyList<string> Tabs { get; init; } = [];

    public int SelectedIndex { get; init; }

    public void Render(RuntimeRenderContext context)
    {
        var column = context.Bounds.X;
        for (var index = 0; index < Tabs.Count; index++)
        {
            var label = index == SelectedIndex ? $"[{Tabs[index]}]" : $" {Tabs[index]} ";
            var style = index == SelectedIndex ? context.Theme.Accent : context.Theme.Default;
            BufferPainter.WriteText(context.Buffer, column, context.Bounds.Y, label, style.ToCell());
            column += (ushort)Math.Min(ushort.MaxValue, label.Length + 1);
            if (column >= context.Bounds.Right)
            {
                break;
            }
        }
    }
}
