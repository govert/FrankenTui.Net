namespace FrankenTui.Widgets;

/// <summary>Border type presets. Matches upstream borders.rs.</summary>
public enum BorderType { Plain, Rounded, Double, Heavy, Thick, Dashed }

/// <summary>Border type to BorderChars mapping.</summary>
public static class BorderPresets
{
    public static BorderChars For(BorderType type) => type switch
    {
        BorderType.Plain => BorderChars.Square,
        BorderType.Rounded => BorderChars.Rounded,
        BorderType.Double => BorderChars.Double,
        BorderType.Heavy => BorderChars.Heavy,
        BorderType.Thick => new('█', '█', '█', '█', '█', '█'),
        BorderType.Dashed => new('┄', '┄', '┄', '┄', '┄', '┆'),
        _ => BorderChars.Square
    };

    public static BorderChars FocusBorder => BorderChars.Heavy;
    public static BorderChars NormalBorder => BorderChars.Rounded;
}
