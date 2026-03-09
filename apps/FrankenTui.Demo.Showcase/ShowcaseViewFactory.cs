using FrankenTui.Extras;
using FrankenTui.Layout;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

public static class ShowcaseViewFactory
{
    public static IWidget Build(bool inlineMode) =>
        new StackWidget(
            LayoutDirection.Vertical,
            [
                (LayoutConstraint.Fixed(1), new TabsWidget { Tabs = ["Overview", "Render", "Hosts"], SelectedIndex = inlineMode ? 1 : 0 }),
                (LayoutConstraint.Fixed(3), new StatusWidget { Label = "Session", Value = inlineMode ? "inline" : "alternate-screen", IsHealthy = true }),
                (LayoutConstraint.Fixed(8), DashboardSurface.CreateDefault("FrankenTui.Net", ["Core", "Render", "Runtime", "Widgets"])),
                (LayoutConstraint.Fill(), new ParagraphWidget("This showcase is the first integrated .NET baseline for the FrankenTUI port."))
            ]);
}
