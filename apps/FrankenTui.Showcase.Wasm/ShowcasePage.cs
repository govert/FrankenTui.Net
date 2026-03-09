using FrankenTui;
using FrankenTui.Web;
using FrankenTui.Extras;
using FrankenTui.Widgets;

namespace FrankenTui.Showcase.Wasm;

public static class ShowcasePage
{
    public static WebFrame RenderDefault() =>
        RenderScenario();

    public static WebFrame RenderScenario(
        HostedParityScenarioId scenarioId = HostedParityScenarioId.Overview,
        int frame = 4,
        bool inlineMode = false,
        ushort width = 64,
        ushort height = 18,
        string language = "en-US",
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight) =>
        Ui.RenderHostedParity(inlineMode, width, height, scenarioId, frame, language, flowDirection);
}
