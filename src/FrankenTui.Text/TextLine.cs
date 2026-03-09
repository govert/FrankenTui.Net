namespace FrankenTui.Text;

public sealed record TextLine(IReadOnlyList<TextSpan> Spans)
{
    public string PlainText => string.Concat(Spans.Select(static span => span.Text));

    public static TextLine FromText(string text) => new([new TextSpan(text)]);
}
