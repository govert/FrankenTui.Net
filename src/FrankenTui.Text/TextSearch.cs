namespace FrankenTui.Text;

public static class TextSearch
{
    public static IReadOnlyList<TextCursor> FindAll(TextDocument document, string needle, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(needle);

        if (needle.Length == 0)
        {
            return [];
        }

        var result = new List<TextCursor>();
        for (var lineIndex = 0; lineIndex < document.Lines.Count; lineIndex++)
        {
            var line = document.Lines[lineIndex].PlainText;
            var offset = 0;
            while (true)
            {
                var found = line.IndexOf(needle, offset, comparison);
                if (found < 0)
                {
                    break;
                }

                result.Add(new TextCursor(lineIndex, found));
                offset = found + needle.Length;
            }
        }

        return result;
    }
}
