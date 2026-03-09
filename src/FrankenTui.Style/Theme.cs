using FrankenTui.Render;

namespace FrankenTui.Style;

public sealed record Theme
{
    public UiStyle Default { get; init; } = UiStyle.Default;
    public UiStyle Border { get; init; } = UiStyle.Muted;
    public UiStyle Muted { get; init; } = UiStyle.Muted;
    public UiStyle Title { get; init; } = UiStyle.Accent;
    public UiStyle Accent { get; init; } = UiStyle.Accent;
    public UiStyle Success { get; init; } = UiStyle.Success;
    public UiStyle Warning { get; init; } = UiStyle.Warning;
    public UiStyle Danger { get; init; } = UiStyle.Danger;
    public UiStyle Selection { get; init; } = UiStyle.Accent with { Background = PackedRgba.Rgb(25, 65, 120) };
    public InteractiveStyleSet Interactive { get; init; } = InteractiveStyleSet.Default;
    public TableTheme Table { get; init; } = TableTheme.Default;

    public static Theme DefaultTheme { get; } = new();
}
