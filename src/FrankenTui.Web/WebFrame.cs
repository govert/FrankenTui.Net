namespace FrankenTui.Web;

public sealed record WebFrame(IReadOnlyList<string> Rows, string Html)
{
    public string Text => string.Join(Environment.NewLine, Rows);
}
