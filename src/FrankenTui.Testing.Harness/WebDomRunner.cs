using AngleSharp;
using AngleSharp.Dom;
using FrankenTui.Web;

namespace FrankenTui.Testing.Harness;

public static class WebDomRunner
{
    public static Task<IDocument> ParseAsync(WebFrame frame, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(frame);
        return ParseAsync(frame.DocumentHtml, cancellationToken);
    }

    public static Task<IDocument> ParseAsync(string html, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(html);

        var context = BrowsingContext.New(Configuration.Default);
        return context.OpenAsync(request => request.Content(html), cancellationToken);
    }
}
