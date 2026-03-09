using FrankenTui.Render;

namespace FrankenTui.Style;

public sealed record UiStyle(PackedRgba Foreground, PackedRgba Background, CellStyleFlags Flags)
{
    public static readonly UiStyle Default = new(PackedRgba.White, PackedRgba.Transparent, CellStyleFlags.None);
    public static readonly UiStyle Accent = new(PackedRgba.Rgb(80, 180, 255), PackedRgba.Transparent, CellStyleFlags.Bold);
    public static readonly UiStyle Muted = new(PackedRgba.Rgb(150, 150, 150), PackedRgba.Transparent, CellStyleFlags.None);
    public static readonly UiStyle Success = new(PackedRgba.Rgb(80, 200, 120), PackedRgba.Transparent, CellStyleFlags.None);
    public static readonly UiStyle Warning = new(PackedRgba.Rgb(230, 190, 60), PackedRgba.Transparent, CellStyleFlags.Bold);
    public static readonly UiStyle Danger = new(PackedRgba.Rgb(220, 90, 90), PackedRgba.Transparent, CellStyleFlags.Bold);

    public UiStyle WithForeground(PackedRgba foreground) => this with { Foreground = foreground };

    public UiStyle WithBackground(PackedRgba background) => this with { Background = background };

    public UiStyle WithFlags(CellStyleFlags flags) => this with { Flags = flags };

    public Cell ToCell(char value = ' ') =>
        new(CellContent.FromChar(value), Foreground, Background, new CellAttributes(Flags, 0));
}
