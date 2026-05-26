using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>Help system widget. Matches upstream help.rs + help_index.rs + help_registry.rs.</summary>
public sealed class HelpSystemWidget : IWidget
{
    private readonly List<(string Section, string Content)> _sections = [];
    private int _scrollOffset;
    public int MaxScroll { get; private set; }

    public void Register(string section, string content) => _sections.Add((section, content));
    public void ScrollUp() => _scrollOffset = Math.Max(0, _scrollOffset - 1);
    public void ScrollDown() => _scrollOffset = Math.Min(Math.Max(0, MaxScroll), _scrollOffset + 1);

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty) return;
        var fg = PackedRgba.Rgb(200, 210, 220);
        var titleFg = PackedRgba.Rgb(100, 200, 255);

        var lines = new List<string>();
        foreach (var (section, content) in _sections)
        {
            lines.Add($"── {section} ──");
            lines.AddRange(content.Split('\n'));
        }
        MaxScroll = Math.Max(0, lines.Count - area.Height);

        var visible = lines.Skip(_scrollOffset).Take(area.Height).ToList();
        for (var i = 0; i < visible.Count; i++)
        {
            var y = (ushort)(area.Y + i);
            var color = visible[i].StartsWith("──") ? titleFg : fg;
            for (var x = 0; x < Math.Min(visible[i].Length, area.Width); x++)
                context.Buffer.Set((ushort)(area.X + x), y, Cell.FromChar(visible[i][x]).WithForeground(color));
        }
    }

    public Size Measure(Size available) => available;
}
