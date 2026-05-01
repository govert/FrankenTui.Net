using FrankenTui;
using FrankenTui.Web;
using FrankenTui.Extras;
using FrankenTui.Simd;
using FrankenTui.Widgets;

namespace FrankenTui.Showcase.Wasm;

public static class ShowcasePage
{
    public static WebFrame RenderDefault() =>
        RenderScenario();

    public static ShowcaseRunnerCore CreateRunner(
        HostedParityScenarioId scenarioId = HostedParityScenarioId.Overview,
        ushort width = 64,
        ushort height = 18,
        bool inlineMode = false,
        string language = "en-US",
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight) =>
        new(scenarioId, width, height, inlineMode, language, flowDirection);

    public static WebFrame RenderScenario(
        HostedParityScenarioId scenarioId = HostedParityScenarioId.Overview,
        int frame = 4,
        bool inlineMode = false,
        ushort width = 64,
        ushort height = 18,
        string language = "en-US",
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight)
    {
        SimdAccelerators.EnableIfSupported();
        return Ui.RenderHostedParity(inlineMode, width, height, scenarioId, frame, language, flowDirection);
    }
}
