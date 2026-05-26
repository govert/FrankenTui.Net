using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>Virtualized list container for efficient large-dataset rendering. Matches upstream Virtualized.</summary>
public sealed class VirtualizedListWidget<T> : IWidget
{
    private readonly IReadOnlyList<T> _items;
    private int _scrollOffset;
    private ushort _itemHeight;
    private int _overscan;
    private bool _followMode;
    private readonly Dictionary<int, ushort> _heightCache = [];

    public required Func<T, int, IWidget> ItemBuilder { get; init; }
    public ushort ItemHeight { get => _itemHeight; init { _itemHeight = Math.Max(value, (ushort)1); _heightCache.Clear(); } }
    public int Overscan { get => _overscan; init => _overscan = Math.Max(value, 0); }

    public int ScrollOffset { get => _scrollOffset; set => _scrollOffset = Math.Clamp(value, 0, Math.Max(0, _items.Count - 1)); }
    public int Count => _items.Count;
    public bool FollowMode { get => _followMode; set => _followMode = value; }

    public VirtualizedListWidget(IReadOnlyList<T> items, ushort itemHeight = 1) { _items = items; _itemHeight = itemHeight; }

    public void ScrollBy(int delta) => ScrollOffset = _scrollOffset + delta;
    public void ScrollToEnd() => _scrollOffset = Math.Max(0, _items.Count - 1);
    public void ScrollToStart() => _scrollOffset = 0;
    public void ScrollPage(int pageSize) => ScrollOffset = _scrollOffset + pageSize;

    void IRuntimeView.Render(RuntimeRenderContext context)
    {
        var area = context.Bounds;
        if (area.IsEmpty || _items.Count == 0) return;

        var visibleItems = (int)Math.Ceiling((double)area.Height / _itemHeight);
        var start = Math.Max(0, _scrollOffset - _overscan);
        var end = Math.Min(_items.Count, start + visibleItems + 2 * _overscan);

        ushort y = (ushort)area.Y;
        for (var i = start; i < end && y < (ushort)area.Bottom; i++)
        {
            var h = _heightCache.TryGetValue(i, out var cached) ? cached : _itemHeight;
            var itemArea = new Rect(area.X, y, area.Width, h);
            if (!itemArea.IsEmpty)
            {
                var widget = ItemBuilder(_items[i], i);
                widget.Render(new RuntimeRenderContext(context.Buffer, itemArea, context.Theme));
            }
            y += h;
        }
    }

    public Size Measure(Size available) => available;
}
