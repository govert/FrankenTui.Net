using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Text;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public static class TracebackView
{
    public static TextDocument FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var lines = new List<TextLine>
        {
            new([new TextSpan(exception.GetType().Name, FrankenTui.Style.UiStyle.Danger)]),
            new([new TextSpan(exception.Message, FrankenTui.Style.UiStyle.Warning)])
        };

        var stackTrace = (exception.StackTrace ?? "stack unavailable")
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');
        foreach (var line in stackTrace.Take(6))
        {
            lines.Add(new TextLine([new TextSpan(line.Trim(), FrankenTui.Style.UiStyle.Muted)]));
        }

        return new TextDocument(lines);
    }
}

public sealed class TracebackWidget : IWidget
{
    public Exception Exception { get; init; } = new InvalidOperationException("Unknown failure.");

    public void Render(RuntimeRenderContext context) =>
        new TextAreaWidget
        {
            Document = TracebackView.FromException(Exception),
            RenderOptions = new TextRenderOptions(TextWrapMode.Character),
            StatusText = "Traceback"
        }.Render(context);
}
