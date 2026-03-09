namespace FrankenTui.Render;

public readonly record struct PackedRgba(uint Raw)
{
    public static readonly PackedRgba Transparent = new(0);
    public static readonly PackedRgba Black = Rgb(0, 0, 0);
    public static readonly PackedRgba White = Rgb(255, 255, 255);
    public static readonly PackedRgba Red = Rgb(255, 0, 0);
    public static readonly PackedRgba Green = Rgb(0, 255, 0);
    public static readonly PackedRgba Blue = Rgb(0, 0, 255);

    public byte R => (byte)(Raw >> 24);
    public byte G => (byte)(Raw >> 16);
    public byte B => (byte)(Raw >> 8);
    public byte A => (byte)Raw;

    public static PackedRgba Rgb(byte r, byte g, byte b) => Rgba(r, g, b, 255);

    public static PackedRgba Rgba(byte r, byte g, byte b, byte a) =>
        new(((uint)r << 24) | ((uint)g << 16) | ((uint)b << 8) | a);

    public PackedRgba Over(PackedRgba destination)
    {
        var sourceAlpha = (ulong)A;
        if (sourceAlpha == 255)
        {
            return this;
        }

        if (sourceAlpha == 0)
        {
            return destination;
        }

        var destinationAlpha = (ulong)destination.A;
        var inverseSourceAlpha = 255UL - sourceAlpha;
        var numeratorAlpha = 255UL * sourceAlpha + destinationAlpha * inverseSourceAlpha;
        if (numeratorAlpha == 0)
        {
            return Transparent;
        }

        var outputAlpha = DivideRoundedToByte(numeratorAlpha, 255UL);
        var red = DivideRoundedToByte(
            (ulong)R * sourceAlpha * 255UL +
            (ulong)destination.R * destinationAlpha * inverseSourceAlpha,
            numeratorAlpha);
        var green = DivideRoundedToByte(
            (ulong)G * sourceAlpha * 255UL +
            (ulong)destination.G * destinationAlpha * inverseSourceAlpha,
            numeratorAlpha);
        var blue = DivideRoundedToByte(
            (ulong)B * sourceAlpha * 255UL +
            (ulong)destination.B * destinationAlpha * inverseSourceAlpha,
            numeratorAlpha);

        return Rgba(red, green, blue, outputAlpha);
    }

    public PackedRgba WithOpacity(float opacity)
    {
        if (float.IsNaN(opacity) || opacity <= 0f)
        {
            return Rgba(R, G, B, 0);
        }

        if (float.IsPositiveInfinity(opacity) || opacity >= 1f)
        {
            return this;
        }

        var alpha = (byte)Math.Clamp((int)MathF.Round(A * opacity), 0, 255);
        return Rgba(R, G, B, alpha);
    }

    private static byte DivideRoundedToByte(ulong numerator, ulong denominator)
    {
        var value = (numerator + (denominator / 2)) / denominator;
        return value > 255 ? (byte)255 : (byte)value;
    }
}
