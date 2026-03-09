using FrankenTui.Extras;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

public static class ShowcaseViewFactory
{
    public static IWidget Build(
        bool inlineMode,
        HostedParityScenarioId scenarioId = HostedParityScenarioId.Overview,
        int frame = 0,
        string language = "en-US",
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight) =>
        HostedParitySurface.Create(HostedParitySession.ForFrame(inlineMode, frame, scenarioId, language, flowDirection));
}
