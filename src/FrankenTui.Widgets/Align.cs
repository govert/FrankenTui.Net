// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/align.rs
// Alignment container widget — positions child within area by h/v alignment.

using FrankenTui.Core;
using FrankenTui.Render; using Buffer=FrankenTui.Render.Buffer;

namespace FrankenTui.Widgets;

public enum VerticalAlignment { Top, Middle, Bottom }

// DIVERGENCE: Align<W> only implements IWidget (not IStatefulWidget).
// For stateful children, access .Inner and render through IStatefulWidget directly.
public sealed class Align<W> : IWidget where W : IWidget
{
    W _inner; Alignment _hAlign; VerticalAlignment _vAlign; ushort? _childW, _childH;

    public Align(W inner) { _inner = inner; _hAlign = Alignment.Left; _vAlign = VerticalAlignment.Top; }

    public Align<W> Horizontal(Alignment a) { _hAlign = a; return this; }
    public Align<W> Vertical(VerticalAlignment a) { _vAlign = a; return this; }
    public Align<W> ChildWidth(ushort w) { _childW = w; return this; }
    public Align<W> ChildHeight(ushort h) { _childH = h; return this; }

    public W Inner => _inner;
    public Rect AlignedArea(Rect area)
    {
        ushort w = _childW.HasValue ? Math.Min(_childW.Value, area.Width) : area.Width;
        ushort h = _childH.HasValue ? Math.Min(_childH.Value, area.Height) : area.Height;
        ushort x = _hAlign switch
        {
            Alignment.Center => (ushort)(area.X + (area.Width - w) / 2),
            Alignment.Right => (ushort)(area.X + area.Width - w),
            _ => area.X,
        };
        ushort y = _vAlign switch
        {
            VerticalAlignment.Middle => (ushort)(area.Y + (area.Height - h) / 2),
            VerticalAlignment.Bottom => (ushort)(area.Y + area.Height - h),
            _ => area.Y,
        };
        return new Rect(x, y, w, h);
    }

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0) return;
        // Clear stale child glyphs
        for (ushort y = area.Y; y < area.Bottom; y++)
            for (ushort x = area.X; x < area.Right; x++)
                frame.Buffer.Set(x, y, Cell.Empty);

        var childArea = AlignedArea(area);
        if (childArea.Width == 0 || childArea.Height == 0) return;
        _inner.Render(childArea, frame);
    }

    public bool IsEssential() => _inner.IsEssential();
}
