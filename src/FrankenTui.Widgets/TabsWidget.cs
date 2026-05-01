using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

public sealed class TabsWidget : IWidget
{
    public IReadOnlyList<string> Tabs { get; init; } = [];

    public int SelectedIndex { get; init; }

    public int FocusedIndex { get; init; } = -1;

    public int HoveredIndex { get; init; } = -1;

    public void Render(RuntimeRenderContext context)
    {
        WidgetRenderHelpers.ClearTextRow(context, 0, context.Theme.Default);
        if (!WidgetRenderHelpers.RenderContent(context))
        {
            return;
        }

        var column = context.Bounds.X;
        for (var index = 0; index < Tabs.Count; index++)
        {
            var label = index == SelectedIndex ? $"[{Tabs[index]}]" : $" {Tabs[index]} ";
            var style = WidgetRenderHelpers.ApplyStyling(context)
                ? index switch
                {
                    _ when index == SelectedIndex && index == FocusedIndex => context.Theme.Selection,
                    _ when index == SelectedIndex => context.Theme.Accent,
                    _ when index == FocusedIndex => context.Theme.Interactive.Focused,
                    _ when index == HoveredIndex => context.Theme.Interactive.Hover,
                    _ => context.Theme.Default
                }
                : context.Theme.Default;
            BufferPainter.WriteText(context.Buffer, column, context.Bounds.Y, label, style.ToCell());
            column += (ushort)Math.Min(ushort.MaxValue, label.Length + 1);
            if (column >= context.Bounds.Right)
            {
                break;
            }
        }
    }
}
