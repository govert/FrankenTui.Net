using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;
using FrankenTui.Widgets;

namespace FrankenTui.Extras;

public sealed record HelpEntry(string Key, string Action, string? Detail = null);

public sealed class HelpWidget : IWidget
{
    public IReadOnlyList<HelpEntry> Entries { get; init; } = [];

    public int HighlightedIndex { get; init; } = -1;

    public void Render(RuntimeRenderContext context)
    {
        for (var row = 0; row < Math.Min(Entries.Count, context.Bounds.Height); row++)
        {
            var entry = Entries[row];
            var style = row == HighlightedIndex ? context.Theme.Selection : context.Theme.Default;
            BufferPainter.WriteText(
                context.Buffer,
                context.Bounds.X,
                (ushort)(context.Bounds.Y + row),
                $"{entry.Key,-8} {entry.Action}",
                style.ToCell());
        }
    }
}

public sealed class SpotlightWidget : IWidget
{
    public string Title { get; init; } = "Spotlight";

    public string Body { get; init; } = string.Empty;

    public void Render(RuntimeRenderContext context) =>
        new PanelWidget
        {
            Title = Title,
            Child = new PaddingWidget(
                new ParagraphWidget(Body),
                Sides.All(1))
        }.Render(context);
}
