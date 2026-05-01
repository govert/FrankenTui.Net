namespace FrankenTui.Text;

public sealed record TextRenderOptions(
    TextWrapMode WrapMode,
    TextShapingMode ShapingMode = TextShapingMode.NativeAotSafe,
    TextHyphenationMode HyphenationMode = TextHyphenationMode.Disabled,
    int FirstVisualLine = 0,
    int? MaxVisualLines = null)
{
    public static TextRenderOptions Default { get; } = new(TextWrapMode.Word);
}

public enum TextShapingMode
{
    NativeAotSafe,
    EvaluateExternalShaper
}

public enum TextHyphenationMode
{
    Disabled,
    EvaluateSoftHyphenOnly
}
