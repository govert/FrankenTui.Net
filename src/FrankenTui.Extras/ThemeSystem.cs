using FrankenTui.Render;

namespace FrankenTui.Extras;

/// <summary>Built-in theme identifiers matching upstream ThemeId.</summary>
public enum ThemeId
{
    CyberpunkAurora, Darcula, Solar, Nord, HighContrast,
    Monokai, SolarizedDark, GruvboxDark, OneDark, TokyoNight,
    Dracula, KanagawaWave, EverforestDark, Synthwave84, Palenight
}

/// <summary>Full color palette resolution. Matches upstream ThemePalette.</summary>
public readonly record struct ThemePalette(
    PackedRgba Bg, PackedRgba Fg, PackedRgba Accent, PackedRgba Secondary,
    PackedRgba Muted, PackedRgba Border, PackedRgba Error, PackedRgba Warning,
    PackedRgba Success, PackedRgba Info, PackedRgba Highlight,
    PackedRgba SurfaceAlt, PackedRgba SurfaceHover)
{
    public static ThemePalette ForTheme(ThemeId id) => id switch
    {
        ThemeId.CyberpunkAurora => new(
            Bg: PackedRgba.Rgb(20, 24, 31), Fg: PackedRgba.Rgb(230, 235, 245),
            Accent: PackedRgba.Rgb(100, 200, 255), Secondary: PackedRgba.Rgb(255, 150, 80),
            Muted: PackedRgba.Rgb(100, 110, 120), Border: PackedRgba.Rgb(50, 55, 65),
            Error: PackedRgba.Rgb(255, 80, 80), Warning: PackedRgba.Rgb(255, 200, 50),
            Success: PackedRgba.Rgb(80, 220, 80), Info: PackedRgba.Rgb(80, 180, 255),
            Highlight: PackedRgba.Rgb(255, 255, 100), SurfaceAlt: PackedRgba.Rgb(30, 34, 42),
            SurfaceHover: PackedRgba.Rgb(40, 45, 55)),

        ThemeId.Darcula => new(
            Bg: PackedRgba.Rgb(43, 43, 43), Fg: PackedRgba.Rgb(169, 183, 198),
            Accent: PackedRgba.Rgb(100, 150, 200), Secondary: PackedRgba.Rgb(200, 140, 60),
            Muted: PackedRgba.Rgb(100, 105, 110), Border: PackedRgba.Rgb(60, 63, 65),
            Error: PackedRgba.Rgb(255, 100, 100), Warning: PackedRgba.Rgb(255, 200, 80),
            Success: PackedRgba.Rgb(80, 220, 80), Info: PackedRgba.Rgb(100, 180, 255),
            Highlight: PackedRgba.Rgb(200, 200, 80), SurfaceAlt: PackedRgba.Rgb(50, 50, 50),
            SurfaceHover: PackedRgba.Rgb(60, 60, 60)),

        ThemeId.Solar => new(
            Bg: PackedRgba.Rgb(253, 246, 227), Fg: PackedRgba.Rgb(88, 110, 117),
            Accent: PackedRgba.Rgb(38, 139, 210), Secondary: PackedRgba.Rgb(203, 75, 22),
            Muted: PackedRgba.Rgb(131, 148, 150), Border: PackedRgba.Rgb(238, 232, 213),
            Error: PackedRgba.Rgb(220, 50, 47), Warning: PackedRgba.Rgb(181, 137, 0),
            Success: PackedRgba.Rgb(133, 153, 0), Info: PackedRgba.Rgb(38, 139, 210),
            Highlight: PackedRgba.Rgb(181, 137, 0), SurfaceAlt: PackedRgba.Rgb(238, 232, 213),
            SurfaceHover: PackedRgba.Rgb(220, 215, 195)),

        ThemeId.Nord => new(
            Bg: PackedRgba.Rgb(46, 52, 64), Fg: PackedRgba.Rgb(216, 222, 233),
            Accent: PackedRgba.Rgb(136, 192, 208), Secondary: PackedRgba.Rgb(208, 135, 112),
            Muted: PackedRgba.Rgb(76, 86, 106), Border: PackedRgba.Rgb(59, 66, 82),
            Error: PackedRgba.Rgb(191, 97, 106), Warning: PackedRgba.Rgb(235, 203, 139),
            Success: PackedRgba.Rgb(163, 190, 140), Info: PackedRgba.Rgb(136, 192, 208),
            Highlight: PackedRgba.Rgb(235, 203, 139), SurfaceAlt: PackedRgba.Rgb(59, 66, 82),
            SurfaceHover: PackedRgba.Rgb(67, 76, 94)),

        ThemeId.HighContrast => new(
            Bg: PackedRgba.Rgb(0, 0, 0), Fg: PackedRgba.Rgb(255, 255, 255),
            Accent: PackedRgba.Rgb(0, 200, 255), Secondary: PackedRgba.Rgb(255, 200, 0),
            Muted: PackedRgba.Rgb(180, 180, 180), Border: PackedRgba.Rgb(255, 255, 255),
            Error: PackedRgba.Rgb(255, 60, 60), Warning: PackedRgba.Rgb(255, 255, 0),
            Success: PackedRgba.Rgb(0, 255, 0), Info: PackedRgba.Rgb(0, 200, 255),
            Highlight: PackedRgba.Rgb(255, 255, 0), SurfaceAlt: PackedRgba.Rgb(20, 20, 20),
            SurfaceHover: PackedRgba.Rgb(60, 60, 60)),

        _ => ForTheme(ThemeId.CyberpunkAurora)
    };
}

/// <summary>Theme token resolution. Matches upstream theme color tokens.</summary>
public static class ThemeTokens
{
    public static FxThemeInputs ToFxInputs(this ThemePalette p) =>
        new(p.Bg, p.Accent, p.Secondary, p.Muted);
}

/// <summary>Global theme manager. Matches upstream theme singleton.</summary>
public static class ThemeManager
{
    private static ThemeId _current = ThemeId.CyberpunkAurora;
    public static ThemeId Current => _current;
    public static ThemePalette Palette => ThemePalette.ForTheme(_current);
    public static void SetTheme(ThemeId id) => _current = id;
    public static void Next()
    {
        var values = Enum.GetValues<ThemeId>();
        var idx = Array.IndexOf(values, _current);
        _current = values[(idx + 1) % values.Length];
    }
}
