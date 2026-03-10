using FrankenTui.Style;

namespace FrankenTui.Text;

public sealed record TextDocument(IReadOnlyList<TextLine> Lines)
{
    public static TextDocument FromString(string text, UiStyle? style = null) =>
        new(text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(line => new TextLine([new TextSpan(line, style)]))
            .ToArray());

    public static TextDocument FromMarkup(string text) =>
        MarkupParser.ParseDocument(text);

    public string PlainText => string.Join(Environment.NewLine, Lines.Select(static line => line.PlainText));
}
