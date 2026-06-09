// SPDX-License-Identifier: Apache-2.0
// Port of panel.rs (933L), paginator.rs (497L), popover.rs (662L)
// Panel: bordered container with child. Paginator: paging control. Popover: positioning overlay.

using FrankenTui.Core;
using FrankenTui.Render; using Buffer=FrankenTui.Render.Buffer;

namespace FrankenTui.Widgets;

// ── Panel ─────────────────────────────────────────────────────────────────

public sealed class Panel<W> : IWidget where W : IWidget
{
    W _child; Borders _borders = Borders.All; WidgetStyle _borderStyle, _style, _titleStyle, _subtitleStyle;
    BorderType _borderType = BorderType.Square; string? _title, _subtitle; Alignment _titleAlign, _subtitleAlign;
    Sides _padding;

    public Panel(W child) => _child = child;
    public Panel<W> WithBorders(Borders b) { _borders = b; return this; }
    public Panel<W> WithBorderStyle(WidgetStyle s) { _borderStyle = s; return this; }
    public Panel<W> WithBorderType(BorderType t) { _borderType = t; return this; }
    public Panel<W> Title(string? t) { _title = t; return this; }
    public Panel<W> TitleAlignment(Alignment a) { _titleAlign = a; return this; }
    public Panel<W> TitleStyle(WidgetStyle s) { _titleStyle = s; return this; }
    public Panel<W> Subtitle(string? t) { _subtitle = t; return this; }
    public Panel<W> SubtitleAlignment(Alignment a) { _subtitleAlign = a; return this; }
    public Panel<W> SubtitleStyle(WidgetStyle s) { _subtitleStyle = s; return this; }
    public Panel<W> Style(WidgetStyle s) { _style = s; return this; }
    public Panel<W> Padding(Sides p) { _padding = p; return this; }

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0) return;

        // Draw the block-like border around the area, then render child inside
        var block = Block.Bordered()
            .Borders_(_borders)
            .BorderType(_borderType)
            .BorderStyle(_borderStyle)
            .Padding_(_padding);
        if (_title != null) block = block.Title(_title).TitleAlignment(_titleAlign);
        block.Style(_style);

        block.Render(area, frame);
        var inner = new Block().Borders_(_borders).Padding_(_padding).Inner(area);
        if (inner.Width > 0 && inner.Height > 0)
        {
            // Draw subtitle on bottom border if present
            if (_subtitle != null && _borders.HasFlag(Borders.Bottom) && area.Width >= 3)
            {
                WidgetDrawing.DrawTextSpan(frame, (ushort)(area.X + 1),
                    (ushort)(area.Bottom - 1), _subtitle, _subtitleStyle, (ushort)(area.Right - 1));
            }
            _child.Render(inner, frame);
        }
    }

    public bool IsEssential() => _child.IsEssential();
}

// ── Paginator ─────────────────────────────────────────────────────────────

public enum PaginatorMode { Dots, Numbers, DotsWithNumbers }

public sealed class Paginator : IWidget
{
    int _current, _total; PaginatorMode _mode; WidgetStyle _style, _activeStyle; string _sep = " ";

    public Paginator(int total) { _total = total; _mode = PaginatorMode.Dots; _style = WidgetStyle.Default; _activeStyle = WidgetStyle.Default; }
    public Paginator Current(int c) { _current = Math.Clamp(c, 0, _total - 1); return this; }
    public Paginator Mode(PaginatorMode m) { _mode = m; return this; }
    public Paginator Style(WidgetStyle s) { _style = s; return this; }
    public Paginator ActiveStyle(WidgetStyle s) { _activeStyle = s; return this; }
    public Paginator Separator(string s) { _sep = s; return this; }

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0 || _total == 0) return;
        var parts = new List<string>();
        for (int i = 0; i < Math.Min(_total, area.Width / 2); i++)
        {
            bool active = i == _current;
            parts.Add(_mode switch
            {
                PaginatorMode.Numbers => (i + 1).ToString(),
                PaginatorMode.DotsWithNumbers => active ? $"({i + 1})" : (i + 1).ToString(),
                _ => active ? "●" : "○",
            });
        }
        string text = string.Join(_sep, parts);
        ushort cx = (ushort)(area.X + (area.Width - text.Length) / 2);
        WidgetDrawing.DrawTextSpan(frame, cx, area.Y, text, _style, area.Right);
    }
}

// ── Popover ───────────────────────────────────────────────────────────────

public enum Placement { Above, Below, Left, Right, Center }

public sealed class Popover : IWidget
{
    string _text; Placement _placement; WidgetStyle _style; ushort _width, _height;

    public Popover(string text) { _text = text; _placement = Placement.Below; _width = (ushort)text.Length; _height = 1; }
    public Popover At(Placement p) { _placement = p; return this; }
    public Popover Style(WidgetStyle s) { _style = s; return this; }
    public Popover Size(ushort w, ushort h) { _width = w; _height = h; return this; }

    public void Render(Rect area, Frame frame)
    {
        if (area.Width == 0 || area.Height == 0 || _width == 0 || _height == 0) return;
        // Position relative to area
        var popRect = _placement switch
        {
            Placement.Above => new Rect(area.X, (ushort)Math.Max(0, area.Y - _height), _width, Math.Min(_height, area.Y)),
            Placement.Below => new Rect(area.X, (ushort)(area.Bottom), _width, Math.Min(_height, (ushort)(frame.Buffer.Height - area.Bottom))),
            Placement.Left => new Rect((ushort)Math.Max(0, area.X - _width), area.Y, Math.Min(_width, area.X), _height),
            Placement.Right => new Rect(area.Right, area.Y, Math.Min(_width, (ushort)(frame.Buffer.Width - area.Right)), _height),
            _ => new Rect((ushort)(area.X + (area.Width - _width) / 2), (ushort)(area.Y + (area.Height - _height) / 2), _width, _height),
        };
        WidgetDrawing.ClearTextArea(frame, popRect, _style);
        WidgetDrawing.DrawTextSpan(frame, popRect.X, popRect.Y, _text, _style, popRect.Right);
    }
}
