using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

public static class ShowcaseViewFactory
{
    public static IWidget Build(
        bool inlineMode,
        int screenNumber = 1,
        int frame = 0,
        bool tour = false,
        double tourSpeed = 1.0,
        int tourStartStep = 1,
        string language = "en-US",
        WidgetFlowDirection flowDirection = WidgetFlowDirection.LeftToRight,
        ushort width = 64,
        ushort height = 18,
        bool mouseCaptureEnabled = false,
        string? vfxEffect = null)
    {
        var scripted = ShowcaseDemoState.Create(
            inlineMode,
            new FrankenTui.Core.Size(width, height),
            screenNumber,
            language,
            flowDirection,
            tour,
            tourSpeed,
            tourStartStep,
            paneWorkspace: null,
            mouseCaptureEnabled: mouseCaptureEnabled,
            vfxEffect: vfxEffect).AdvanceScript(frame);
        return (IWidget)ShowcaseSurface.Create(scripted);
    }
}
