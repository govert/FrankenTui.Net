using FrankenTui.Render;

namespace FrankenTui.Style;

public sealed record InteractiveStyleSet(
    UiStyle Normal,
    UiStyle Hover,
    UiStyle Focused,
    UiStyle Disabled)
{
    public static readonly InteractiveStyleSet Default = new(
        UiStyle.Default,
        UiStyle.Accent with { Flags = CellStyleFlags.Underline },
        UiStyle.Accent,
        UiStyle.Muted);
}
