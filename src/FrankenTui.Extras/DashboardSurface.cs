using FrankenTui.Core;
using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public static class DashboardSurface
{
    public static IWidget CreateDefault(string title, IReadOnlyList<string> items) =>
        new PanelWidget
        {
            Title = title,
            Child = new PaddingWidget(
                new StackWidget(
                    LayoutDirection.Vertical,
                    [
                        (LayoutConstraint.Fixed(1), new StatusWidget { Label = "Mode", Value = "Autorun" }),
                        (LayoutConstraint.Fixed((ushort)Math.Max(items.Count, 1)), new ListWidget { Items = items }),
                        (LayoutConstraint.Fixed(2), new ProgressWidget { Value = 0.6, Label = "Port baseline" })
                    ]),
                Sides.All(1))
        };
}
