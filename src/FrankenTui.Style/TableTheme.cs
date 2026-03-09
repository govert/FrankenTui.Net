using FrankenTui.Render;

namespace FrankenTui.Style;

public sealed record TableTheme(
    UiStyle Header,
    UiStyle Cell,
    UiStyle Selected,
    UiStyle Border)
{
    public static readonly TableTheme Default = new(
        UiStyle.Accent,
        UiStyle.Default,
        UiStyle.Accent with { Background = PackedRgba.Rgb(20, 50, 80) },
        UiStyle.Muted);
}
