using System.Text;

namespace FrankenTui.Text;

public static class TextNormalizer
{
    public static string Normalize(string text, NormalizationForm normalization = NormalizationForm.FormC)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Normalize(normalization);
    }
}
