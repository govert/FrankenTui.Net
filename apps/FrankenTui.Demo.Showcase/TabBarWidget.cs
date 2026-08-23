// Faithful port of render_tab_bar in chrome.rs.
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;

namespace FrankenTui.Demo.Showcase;

internal sealed class TabBarWidget : IWidget
{
    private readonly IReadOnlyList<(string Text, bool Active)> _tabs;

    public TabBarWidget(IReadOnlyList<(string Text, bool Active)> tabs) { _tabs = tabs; }

    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty || _tabs.Count == 0) return;
        ushort x = area.X;
        for (int i = 0; i < _tabs.Count; i++)
        {
            var (text, _) = _tabs[i];
            var full = i == 0 ? text : "\u2502" + text;
            if (x + full.Length > area.Right) break;
            foreach (var c in full)
            {
                if (x >= area.Right) break;
                frame.Buffer.SetFast(x++, area.Y, Cell.FromChar(c));
            }
        }
        while (x < area.Right)
            frame.Buffer.SetFast(x++, area.Y, Cell.Empty);
    }
}
