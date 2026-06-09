// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/rule.rs (770L)
// Horizontal or vertical rule (separator line).

using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

public enum RuleDirection { Horizontal, Vertical }

public sealed class Rule : IWidget
{
    RuleDirection _dir = RuleDirection.Horizontal; WidgetStyle _style;

    public Rule Direction(RuleDirection d) { _dir = d; return this; }
    public Rule Style(WidgetStyle s) { _style = s; return this; }

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0) return;
        var cell = Cell.FromChar(_dir == RuleDirection.Horizontal ? '─' : '│');
        WidgetDrawing.ApplyStyle(ref cell, _style);

        if (_dir == RuleDirection.Horizontal)
        {
            for (ushort x = area.X; x < area.Right; x++) frame.Buffer.SetFast(x, area.Y, cell);
        }
        else
        {
            for (ushort y = area.Y; y < area.Bottom; y++) frame.Buffer.SetFast(area.X, y, cell);
        }
    }
}
