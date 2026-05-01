using FrankenTui.Core;

namespace FrankenTui.Demo.Showcase;

internal static class ShowcaseViewportResolver
{
    public static Size Resolve(ShowcaseCliOptions options, Size hostSize)
    {
        var safeHost = hostSize.IsEmpty ? new Size(1, 1) : hostSize;
        if (options.HasExplicitViewport || options.VfxHarness.Enabled || options.MermaidHarness.Enabled)
        {
            return new Size(options.Width, options.Height);
        }

        return options.ScreenMode switch
        {
            ShowcaseScreenMode.Alt => safeHost,
            ShowcaseScreenMode.Inline => new Size(safeHost.Width, ClampHeight(options.UiHeight, safeHost.Height)),
            ShowcaseScreenMode.InlineAuto => new Size(
                safeHost.Width,
                ClampHeight(ClampHeight(options.Height, options.UiMaxHeight), safeHost.Height, options.UiMinHeight)),
            _ => safeHost
        };
    }

    private static ushort ClampHeight(ushort height, ushort max, ushort min = 1) =>
        ClampHeightCore(height, min, max);

    private static ushort ClampHeightCore(ushort height, ushort min, ushort max)
    {
        var upper = Math.Max(max, (ushort)1);
        var lower = Math.Min(Math.Max(min, (ushort)1), upper);
        return (ushort)Math.Clamp(height, lower, upper);
    }
}
