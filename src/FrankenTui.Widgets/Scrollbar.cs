// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/scrollbar.rs (1086L)
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Scrollbar widget with orientation, thumb/track rendering.

using FrankenTui.Core;
using FrankenTui.Render;
using CanonicalA11y = FrankenTui.A11y;

namespace FrankenTui.Widgets;

public enum ScrollbarOrientation { VerticalRight, VerticalLeft, HorizontalBottom, HorizontalTop }

public sealed class ScrollbarState
{
    public int ContentLength { get; set; }
    public int Position { get; set; }
    public int ViewportLength { get; set; }

    public ScrollbarState(int contentLen, int pos, int viewportLen)
    {
        ContentLength = contentLen; Position = pos; ViewportLength = viewportLen;
    }

    public void ScrollUp(int lines = 1) => Position = Math.Max(0, Position - lines);
    public void ScrollDown(int lines = 1) => Position = Math.Min(Math.Max(0, ContentLength - ViewportLength), Position + lines);
}

public sealed class Scrollbar : IStatefulWidget<ScrollbarState>, IAccessible, CanonicalA11y.IAccessible
{
    ScrollbarOrientation _orient; WidgetStyle _thumbStyle, _trackStyle; string? _beginSym, _endSym;

    public Scrollbar(ScrollbarOrientation orient) => _orient = orient;

    public Scrollbar ThumbStyle(WidgetStyle s) { _thumbStyle = s; return this; }
    public Scrollbar TrackStyle(WidgetStyle s) { _trackStyle = s; return this; }
    public Scrollbar BeginSymbol(string s) { _beginSym = s; return this; }
    public Scrollbar EndSymbol(string s) { _endSym = s; return this; }

    public void Render(Rect area, Frame frame, ScrollbarState state)
    {
        if (area.Width == 0 || area.Height == 0) return;
        int contentLen = Math.Max(state.ContentLength, 1);
        bool isVert = _orient is ScrollbarOrientation.VerticalLeft or ScrollbarOrientation.VerticalRight;
        int trackLen = isVert ? area.Height : area.Width;
        int thumbSize = Math.Max(1, (int)((long)trackLen * state.ViewportLength / contentLen));
        int maxPos = trackLen - thumbSize;
        int thumbPos = contentLen > state.ViewportLength
            ? (int)((long)maxPos * state.Position / (contentLen - state.ViewportLength))
            : 0;

        char trackCh = isVert ? '│' : '─';
        char thumbCh = isVert ? '█' : '█';

        if (isVert)
        {
            for (ushort y = area.Y; y < area.Bottom; y++)
            {
                bool isThumb = (y - area.Y) >= thumbPos && (y - area.Y) < thumbPos + thumbSize;
                var cell = Cell.FromChar(isThumb ? thumbCh : trackCh);
                if (isThumb) WidgetDrawing.ApplyStyle(ref cell, _thumbStyle);
                else WidgetDrawing.ApplyStyle(ref cell, _trackStyle);
                frame.Buffer.SetFast(area.X, y, cell);
            }
        }
        else
        {
            for (ushort x = area.X; x < area.Right; x++)
            {
                bool isThumb = (x - area.X) >= thumbPos && (x - area.X) < thumbPos + thumbSize;
                var cell = Cell.FromChar(isThumb ? thumbCh : trackCh);
                if (isThumb) WidgetDrawing.ApplyStyle(ref cell, _thumbStyle);
                else WidgetDrawing.ApplyStyle(ref cell, _trackStyle);
                frame.Buffer.SetFast(x, area.Y, cell);
            }
        }
        // Arrow symbols at ends
        if (_beginSym != null && trackLen > 1)
        {
            ushort sx = area.X, sy = area.Y;
            if (!isVert) sx = area.X; else sy = area.Y;
            WidgetDrawing.DrawTextSpan(frame, sx, sy, _beginSym, _thumbStyle, isVert ? area.Right : area.Right);
        }
        if (_endSym != null && trackLen > 1)
        {
            if (isVert)
                WidgetDrawing.DrawTextSpan(frame, area.X, (ushort)(area.Bottom - 1), _endSym, _thumbStyle, area.Right);
            else
                WidgetDrawing.DrawTextSpan(frame, (ushort)(area.Right - 1), area.Y, _endSym, _thumbStyle, area.Right);
        }
    }

    // ── Accessibility ─────────────────────────────────────────────────────────

    /// <summary>Get the legacy compatibility projection of this scrollbar's accessibility node.</summary>
    public List<A11yNodeInfo> AccessibilityNodes(Rect area) =>
        LegacyAccessibilityAdapter.FromCanonical(CanonicalAccessibilityNodes(area));

    List<CanonicalA11y.A11yNodeInfo> CanonicalA11y.IAccessible.AccessibilityNodes(Rect area) =>
        CanonicalAccessibilityNodes(area);

    private List<CanonicalA11y.A11yNodeInfo> CanonicalAccessibilityNodes(Rect area)
    {
        string orientation = _orient is ScrollbarOrientation.VerticalRight or ScrollbarOrientation.VerticalLeft
            ? "vertical"
            : "horizontal";
        CanonicalA11y.A11yNodeInfo node = CanonicalA11y.A11yNodeInfo
            .New(WidgetDrawing.A11yNodeId(area), CanonicalA11y.A11yRole.ScrollBar, area)
            .WithName($"{orientation} scrollbar")
            .WithState(new CanonicalA11y.A11yState());
        return [node];
    }
}
