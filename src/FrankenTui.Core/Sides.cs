namespace FrankenTui.Core;

public readonly record struct Sides(ushort Top, ushort Right, ushort Bottom, ushort Left)
{
    public static Sides All(ushort value) => new(value, value, value, value);

    public static Sides Horizontal(ushort value) => new(0, value, 0, value);

    public static Sides Vertical(ushort value) => new(value, 0, value, 0);

    public ushort HorizontalSum => SaturatingAdd(Left, Right);

    public ushort VerticalSum => SaturatingAdd(Top, Bottom);

    public static implicit operator Sides(ushort value) => All(value);

    public static implicit operator Sides((ushort Vertical, ushort Horizontal) value) =>
        new(value.Vertical, value.Horizontal, value.Vertical, value.Horizontal);

    public static implicit operator Sides((ushort Top, ushort Right, ushort Bottom, ushort Left) value) =>
        new(value.Top, value.Right, value.Bottom, value.Left);

    private static ushort SaturatingAdd(ushort left, ushort right)
    {
        var sum = left + right;
        return sum >= ushort.MaxValue ? ushort.MaxValue : (ushort)sum;
    }
}
