// Ui helpers for the Extras layer. Declared in the FrankenTui.Extras namespace to
// avoid colliding with the comprehensive FrankenTui.Ui surface in the FrankenTui assembly
// (both previously lived in the top-level FrankenTui namespace, which produced an
// unresolvable cross-assembly type clash for consumers referencing both assemblies).

using FrankenTui.Widgets;

namespace FrankenTui.Extras;

/// <summary>Ergonomic helpers for the FrankenTui.Extras layer.</summary>
public static class Ui
{
    /// <summary>
    /// Parse a markdown string and return a ParagraphWidget whose Document is cached by content.
    /// Repeated calls with the same markdown string return the same Document instance.
    /// </summary>
    public static ParagraphWidget Markdown(string markdown)
    {
        var document = MarkdownDocumentBuilder.ParseCached(markdown);
        return new ParagraphWidget(string.Empty) { Document = document };
    }
}
