using FrankenTui.Render;
using FrankenTui.Style;

namespace FrankenTui.Text;

public static class MarkupParser
{
    public static TextLine ParseInline(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var spans = new List<TextSpan>();
        var index = 0;
        while (index < text.Length)
        {
            if (text.AsSpan(index).StartsWith("**", StringComparison.Ordinal))
            {
                var end = text.IndexOf("**", index + 2, StringComparison.Ordinal);
                if (end > index)
                {
                    spans.Add(new TextSpan(text[(index + 2)..end], UiStyle.Accent));
                    index = end + 2;
                    continue;
                }
            }

            if (text[index] == '_')
            {
                var end = text.IndexOf('_', index + 1);
                if (end > index)
                {
                    spans.Add(new TextSpan(text[(index + 1)..end], UiStyle.Default.WithFlags(CellStyleFlags.Italic)));
                    index = end + 1;
                    continue;
                }
            }

            spans.Add(new TextSpan(text[index].ToString(), UiStyle.Default));
            index++;
        }

        return new TextLine(spans);
    }
}
