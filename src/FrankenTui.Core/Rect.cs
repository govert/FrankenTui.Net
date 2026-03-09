namespace FrankenTui.Core;

public readonly record struct Rect(ushort X, ushort Y, ushort Width, ushort Height)
{
    public static Rect FromSize(ushort width, ushort height) => new(0, 0, width, height);

    public ushort Left => X;

    public ushort Top => Y;

    public ushort Right => SaturatingAdd(X, Width);

    public ushort Bottom => SaturatingAdd(Y, Height);

    public uint Area => (uint)Width * Height;

    public bool IsEmpty => Width == 0 || Height == 0;

    public bool Contains(ushort x, ushort y) =>
        x >= X && x < Right && y >= Y && y < Bottom;

    public Rect Intersection(Rect other) =>
        TryIntersection(other, out var result) ? result : default;

    public bool TryIntersection(Rect other, out Rect result)
    {
        var x = Math.Max(X, other.X);
        var y = Math.Max(Y, other.Y);
        var right = Math.Min(Right, other.Right);
        var bottom = Math.Min(Bottom, other.Bottom);

        if (x < right && y < bottom)
        {
            result = new Rect(x, y, (ushort)(right - x), (ushort)(bottom - y));
            return true;
        }

        result = default;
        return false;
    }

    public Rect Inner(Sides margin)
    {
        var x = SaturatingAdd(X, margin.Left);
        var y = SaturatingAdd(Y, margin.Top);
        var width = SaturatingSubtract(SaturatingSubtract(Width, margin.Left), margin.Right);
        var height = SaturatingSubtract(SaturatingSubtract(Height, margin.Top), margin.Bottom);
        return new Rect(x, y, width, height);
    }

    public Rect Union(Rect other)
    {
        var x = Math.Min(X, other.X);
        var y = Math.Min(Y, other.Y);
        var right = Math.Max(Right, other.Right);
        var bottom = Math.Max(Bottom, other.Bottom);
        return new Rect(x, y, SaturatingSubtract(right, x), SaturatingSubtract(bottom, y));
    }

    public static implicit operator Size(Rect rect) => new(rect.Width, rect.Height);

    private static ushort SaturatingAdd(ushort left, ushort right)
    {
        var sum = left + right;
        return sum >= ushort.MaxValue ? ushort.MaxValue : (ushort)sum;
    }

    private static ushort SaturatingSubtract(ushort left, ushort right) =>
        left >= right ? (ushort)(left - right) : (ushort)0;
}
