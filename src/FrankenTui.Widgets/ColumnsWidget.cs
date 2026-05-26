using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>
/// Column layout widget. Distributes children horizontally with proportional widths.
/// Matches upstream ftui-widgets Columns.
/// </summary>
public sealed class ColumnsWidget : IWidget
{
    private readonly IReadOnlyList<ColumnChild> _children;

    public ColumnsWidget(IReadOnlyList<ColumnChild> children) => _children = children;
    public ColumnsWidget(params ColumnChild[] children) => _children = children;

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || _children.Count == 0) return;

        var totalWidth = (int)area.Width;
        var x = (ushort)area.X;
        for (var i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            var isLast = i == _children.Count - 1;
            var childWidth = isLast
                ? (ushort)(area.Right - x)
                : (ushort)(totalWidth * child.WidthFraction / 100);
            if (childWidth == 0) continue;

            var childArea = new Rect(x, area.Y, childWidth, area.Height);
            child.Widget.Render(new RuntimeRenderContext(context.Buffer, childArea, context.Theme));
            x += childWidth;
        }
    }

    public Size Measure(Size available) => available;
}

public sealed record ColumnChild(IWidget Widget, int WidthFraction = 50);
