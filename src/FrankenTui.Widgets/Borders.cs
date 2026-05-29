// Upstream source: crates/ftui-widgets/src/borders.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Direct 1-1 port of BorderSet, BorderType, and Borders bitflags.

namespace FrankenTui.Widgets;

/// <summary>
/// Border characters for drawing.
/// </summary>
public readonly record struct BorderSet
{
    /// <summary>Vertical border character.</summary>
    public char Vertical { get; init; }
    /// <summary>Horizontal border character.</summary>
    public char Horizontal { get; init; }
    /// <summary>Top-left corner character.</summary>
    public char TopLeft { get; init; }
    /// <summary>Top-right corner character.</summary>
    public char TopRight { get; init; }
    /// <summary>Bottom-left corner character.</summary>
    public char BottomLeft { get; init; }
    /// <summary>Bottom-right corner character.</summary>
    public char BottomRight { get; init; }
    /// <summary>Upward tee junction character.</summary>
    public char TeeUp { get; init; }
    /// <summary>Downward tee junction character.</summary>
    public char TeeDown { get; init; }
    /// <summary>Leftward tee junction character.</summary>
    public char TeeLeft { get; init; }
    /// <summary>Rightward tee junction character.</summary>
    public char TeeRight { get; init; }
    /// <summary>Cross junction character.</summary>
    public char Cross { get; init; }

    /// <summary>ASCII fallback border (+, -, |).</summary>
    public static readonly BorderSet Ascii = new()
    {
        Vertical = '|', Horizontal = '-',
        TopLeft = '+', TopRight = '+', BottomLeft = '+', BottomRight = '+',
        TeeUp = '+', TeeDown = '+', TeeLeft = '+', TeeRight = '+',
        Cross = '+',
    };

    /// <summary>Rounded corners (╭, ╮, ╯, ╰).</summary>
    public static readonly BorderSet Rounded = new()
    {
        Vertical = '│', Horizontal = '─',
        TopLeft = '╭', TopRight = '╮', BottomLeft = '╰', BottomRight = '╯',
        TeeUp = '┴', TeeDown = '┬', TeeLeft = '┤', TeeRight = '├',
        Cross = '┼',
    };

    /// <summary>Square single-line border.</summary>
    public static readonly BorderSet Square = new()
    {
        Vertical = '│', Horizontal = '─',
        TopLeft = '┌', TopRight = '┐', BottomLeft = '└', BottomRight = '┘',
        TeeUp = '┴', TeeDown = '┬', TeeLeft = '┤', TeeRight = '├',
        Cross = '┼',
    };

    /// <summary>Double lines (║, ═).</summary>
    public static readonly BorderSet Double = new()
    {
        Vertical = '║', Horizontal = '═',
        TopLeft = '╔', TopRight = '╗', BottomLeft = '╚', BottomRight = '╝',
        TeeUp = '╩', TeeDown = '╦', TeeLeft = '╣', TeeRight = '╠',
        Cross = '╬',
    };

    /// <summary>Heavy lines (┃, ━).</summary>
    public static readonly BorderSet Heavy = new()
    {
        Vertical = '┃', Horizontal = '━',
        TopLeft = '┏', TopRight = '┓', BottomLeft = '┗', BottomRight = '┛',
        TeeUp = '┻', TeeDown = '┳', TeeLeft = '┫', TeeRight = '┣',
        Cross = '╋',
    };
}

/// <summary>Border style presets.</summary>
public enum BorderType
{
    /// <summary>No border (but space reserved if Borders::ALL is set).</summary>
    Square,
    /// <summary>ASCII fallback border.</summary>
    Ascii,
    /// <summary>Single line border with rounded corners.</summary>
    Rounded,
    /// <summary>Double line border.</summary>
    Double,
    /// <summary>Heavy line border.</summary>
    Heavy,
    /// <summary>Custom border character set.</summary>
    Custom, // BorderSet carried separately by the consumer
}

/// <summary>
/// Extension method to convert BorderType to BorderSet.
/// </summary>
public static class BorderTypeExtensions
{
    /// <summary>Convert this border type to its corresponding border character set.</summary>
    public static BorderSet ToBorderSet(this BorderType borderType) => borderType switch
    {
        BorderType.Square => BorderSet.Square,
        BorderType.Ascii => BorderSet.Ascii,
        BorderType.Rounded => BorderSet.Rounded,
        BorderType.Double => BorderSet.Double,
        BorderType.Heavy => BorderSet.Heavy,
        _ => BorderSet.Square,
    };

    /// <summary>
    /// Convert this BorderSet to a render-level BorderChars (6 basic chars).
    /// The render level does not use tee/cross characters.
    /// </summary>
    public static FrankenTui.Render.BorderChars ToBorderChars(this BorderSet set) =>
        new(set.Vertical, set.Horizontal,
            set.TopLeft, set.TopRight, set.BottomLeft, set.BottomRight);
}

/// <summary>
/// Bitflags for which borders to render.
/// </summary>
[System.Flags]
public enum Borders : byte
{
    /// <summary>No borders.</summary>
    None = 0b0000,
    /// <summary>Top border.</summary>
    Top = 0b0001,
    /// <summary>Right border.</summary>
    Right = 0b0010,
    /// <summary>Bottom border.</summary>
    Bottom = 0b0100,
    /// <summary>Left border.</summary>
    Left = 0b1000,
    /// <summary>All four borders.</summary>
    All = Top | Right | Bottom | Left,
}
