using FrankenTui;
using FrankenTui.Core;
using FrankenTui.Demo.Showcase;
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
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight,
        int? screenNumber = null) =>
        new(scenarioId, width, height, inlineMode, language, flowDirection, screenNumber);

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

    public static WebFrame RenderScreen(
        int screenNumber,
        int frame = 0,
        bool inlineMode = false,
        ushort width = 80,
        ushort height = 24,
        string language = "en-US",
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight)
    {
        SimdAccelerators.EnableIfSupported();
        var resolvedScreen = Math.Clamp(screenNumber, 1, 45);
        var view = ShowcaseViewFactory.Build(
            inlineMode, resolvedScreen, frame,
            language: language, flowDirection: flowDirection,
            width: width, height: height);
        var options = new WebRenderOptions(
            "FrankenTui Showcase",
            string.IsNullOrWhiteSpace(language) ? "en-US" : language,
            flowDirection == WidgetFlowDirection.RightToLeft ? "rtl" : "ltr",
            $"FrankenTui showcase screen {resolvedScreen}",
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["showcase"] = "screen",
                ["screen-number"] = resolvedScreen.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["screen-mode"] = inlineMode ? "inline" : "alt"
            });
        return WebHost.Render(view, new Size(width, height), options: options);
    }
}
