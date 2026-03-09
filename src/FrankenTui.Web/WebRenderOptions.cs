using FrankenTui.A11y;

namespace FrankenTui.Web;

public sealed record WebRenderOptions(
    string Title,
    string Language,
    string Direction,
    string AriaLabel,
    AccessibilitySnapshot? Accessibility = null,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    public static WebRenderOptions Default { get; } =
        new("FrankenTui.Net", "en-US", "ltr", "FrankenTui terminal frame");
}
