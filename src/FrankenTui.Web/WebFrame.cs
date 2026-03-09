using FrankenTui.A11y;

namespace FrankenTui.Web;

public sealed record WebFrame
{
    public required IReadOnlyList<string> Rows { get; init; }

    public required string Html { get; init; }

    public required string DocumentHtml { get; init; }

    public string Title { get; init; } = WebRenderOptions.Default.Title;

    public string Language { get; init; } = WebRenderOptions.Default.Language;

    public string Direction { get; init; } = WebRenderOptions.Default.Direction;

    public AccessibilitySnapshot Accessibility { get; init; } = new();

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public string Text => string.Join(Environment.NewLine, Rows);
}
