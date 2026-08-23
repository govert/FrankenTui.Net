// SPDX-License-Identifier: Apache-2.0
// Ported from crates/ftui-render/src/frame.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source SHA-256: FC7E3000C4ABA1B9A335A68A03C940A2EA6CAEFD023B250842B3E724C0C977EB.

using FrankenTui.Core;

namespace FrankenTui.Render;

public enum CostEstimateSource
{
    FixedDefault,
    Measured,
    AreaFallback,
}

public sealed class WidgetSignal
{
    public ulong WidgetId { get; set; }
    public bool Essential { get; set; }
    public float Priority { get; set; } = 0.5f;
    public ulong StalenessMs { get; set; }
    public float FocusBoost { get; set; }
    public float InteractionBoost { get; set; }
    public uint AreaCells { get; set; } = 1;
    public float CostEstimateUs { get; set; } = 5.0f;
    public float RecentCostUs { get; set; } = 5.0f;
    public CostEstimateSource EstimateSource { get; set; } = CostEstimateSource.FixedDefault;

    public static WidgetSignal New(ulong widgetId) => new() { WidgetId = widgetId };

    public WidgetSignal Clone() => new()
    {
        WidgetId = WidgetId,
        Essential = Essential,
        Priority = Priority,
        StalenessMs = StalenessMs,
        FocusBoost = FocusBoost,
        InteractionBoost = InteractionBoost,
        AreaCells = AreaCells,
        CostEstimateUs = CostEstimateUs,
        RecentCostUs = RecentCostUs,
        EstimateSource = EstimateSource,
    };
}

public sealed class WidgetBudget
{
    private readonly ulong[]? _allowList;

    private WidgetBudget(ulong[]? allowList)
    {
        _allowList = allowList;
    }

    public WidgetBudget()
        : this(null)
    {
    }

    public static WidgetBudget AllowAll() => new(null);

    public static WidgetBudget AllowOnly(IEnumerable<ulong> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        return new WidgetBudget([.. ids.Distinct().Order()]);
    }

    public bool Allows(ulong widgetId, bool essential) =>
        essential || _allowList is null || Array.BinarySearch(_allowList, widgetId) >= 0;

    public WidgetBudget Clone() => new(_allowList is null ? null : [.. _allowList]);
}

public sealed class Frame
{
    private readonly GraphemePool _pool;
    private readonly Stack<ulong> _hitOwnerStack = new();
    private readonly List<WidgetSignal> _widgetSignals = [];
    private FrameArena? _arena;
    private HitGrid? _hitGrid;
    private LinkRegistry? _links;

    public Frame(ushort width, ushort height, GraphemePool pool)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        Buffer = new Buffer(width, height, pool);
        WidgetBudget = WidgetBudget.AllowAll();
        CursorVisible = true;
    }

    public Buffer Buffer { get; private set; }

    public Buffer BufferOverride
    {
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            value.AttachGraphemePool(_pool);
            Buffer = value;
        }
    }

    public GraphemePool Pool => _pool;

    public LinkRegistry? Links => _links;

    public HitGrid? HitGrid => _hitGrid;

    public WidgetBudget WidgetBudget { get; private set; }

    public IReadOnlyList<WidgetSignal> WidgetSignals => _widgetSignals;

    public (ushort x, ushort y)? CursorPosition { get; private set; }

    public bool CursorVisible { get; private set; }

    public DegradationLevel Degradation { get; private set; } = DegradationLevel.Full;

    public FrameArena? Arena => _arena;

    public ushort Width => Buffer.Width;

    public ushort Height => Buffer.Height;

    public Rect Bounds => Buffer.Bounds;

    public static Frame FromBuffer(Buffer buffer, GraphemePool pool)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var frame = new Frame(buffer.Width, buffer.Height, pool);
        frame.BufferOverride = buffer;
        return frame;
    }

    public static Frame WithLinks(
        ushort width,
        ushort height,
        GraphemePool pool,
        LinkRegistry links)
    {
        var frame = new Frame(width, height, pool);
        frame.SetLinks(links);
        return frame;
    }

    public static Frame WithHitGrid(ushort width, ushort height, GraphemePool pool)
    {
        var frame = new Frame(width, height, pool);
        frame._hitGrid = new HitGrid(frame.Width, frame.Height);
        return frame;
    }

    public void SetLinks(LinkRegistry links) =>
        _links = links ?? throw new ArgumentNullException(nameof(links));

    public uint RegisterLink(string url) => _links?.Register(url) ?? 0;

    public void SetArena(FrameArena arena) =>
        _arena = arena ?? throw new ArgumentNullException(nameof(arena));

    public void ClearArena() => _arena = null;

    public void SetWidgetBudget(WidgetBudget budget) =>
        WidgetBudget = budget ?? throw new ArgumentNullException(nameof(budget));

    public bool ShouldRenderWidget(ulong widgetId, bool essential) =>
        WidgetBudget.Allows(widgetId, essential);

    public void RegisterWidgetSignal(WidgetSignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);
        _widgetSignals.Add(signal);
    }

    public void RegisterWidgetSignal(ulong widgetId, bool essential, uint areaCells) =>
        RegisterWidgetSignal(new WidgetSignal
        {
            WidgetId = widgetId,
            Essential = essential,
            AreaCells = areaCells,
        });

    public IReadOnlyList<WidgetSignal> TakeWidgetSignals()
    {
        var signals = _widgetSignals.ToArray();
        _widgetSignals.Clear();
        return signals;
    }

    /// <summary>Compatibility bridge for the earlier target render-set API.</summary>
    public void SetRenderSet(ulong[] ids) => SetWidgetBudget(WidgetBudget.AllowOnly(ids));

    public GraphemeId Intern(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var width = (byte)Math.Min(TerminalTextWidth.DisplayWidth(text), GraphemeId.MaxWidth);
        return _pool.Intern(text, width);
    }

    public GraphemeId InternWithWidth(string text, byte width)
    {
        ArgumentNullException.ThrowIfNull(text);
        return _pool.Intern(text, width);
    }

    public void EnableHitTesting()
    {
        _hitGrid ??= new HitGrid(Width, Height);
    }

    public void Clear()
    {
        Buffer.Clear();
        _hitGrid?.Clear();
        CursorPosition = null;
        _widgetSignals.Clear();
    }

    public void SetCursor((ushort x, ushort y)? position) => CursorPosition = position;

    public void SetCursorVisible(bool visible) => CursorVisible = visible;

    public void SetDegradation(DegradationLevel level)
    {
        Degradation = level;
        Buffer.Degradation = level;
    }

    public bool RegisterHit(Rect area, HitId id, HitRegion region, HitData data)
    {
        if (_hitGrid is null)
        {
            return false;
        }

        var clipped = area.Intersection(Buffer.CurrentScissor);
        if (!clipped.IsEmpty)
        {
            _hitGrid.Register(clipped, id, region, data, CurrentHitOwner());
        }

        return true;
    }

    public bool RegisterHit(Rect area, HitId id, HitRegionKind region, ulong data)
    {
        if (_hitGrid is null)
        {
            return false;
        }

        var clipped = area.Intersection(Buffer.CurrentScissor);
        if (!clipped.IsEmpty)
        {
            _hitGrid.Register(clipped, id, region, new HitData(data), CurrentHitOwner());
        }

        return true;
    }

    public bool RegisterHitRegion(Rect area, HitId id) =>
        RegisterHit(area, id, HitRegion.Content, HitData.Zero);

    public void WithHitOwner(ulong owner, Action<Frame> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        WithHitOwner<object?>(owner, frame =>
        {
            action(frame);
            return null;
        });
    }

    public TResult WithHitOwner<TResult>(ulong owner, Func<Frame, TResult> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _hitOwnerStack.Push(owner);
        try
        {
            return action(this);
        }
        finally
        {
            _hitOwnerStack.Pop();
        }
    }

    public (HitId, HitRegionKind, ulong)? HitTest(ushort x, ushort y)
    {
        var hit = _hitGrid?.HitTest(x, y);
        return hit is { } value ? (value.Item1, value.Item2, value.Item3.Value) : null;
    }

    public (HitId, HitRegion, HitData)? HitTestSource(ushort x, ushort y) =>
        _hitGrid?.HitTestSource(x, y);

    public HitTestResult? HitTestDetailed(ushort x, ushort y) =>
        _hitGrid?.HitTestDetailed(x, y);

    public void DrawHorizontalLine(ushort x, ushort y, ushort width, Cell cell) =>
        Buffer.DrawHorizontalLine(x, y, width, cell);

    public void DrawVerticalLine(ushort x, ushort y, ushort height, Cell cell) =>
        Buffer.DrawVerticalLine(x, y, height, cell);

    public void DrawRectFilled(Rect rect, Cell cell) => Buffer.DrawRectFilled(rect, cell);

    public void DrawRectOutline(Rect rect, Cell cell) => Buffer.DrawRectOutline(rect, cell);

    public ushort PrintText(ushort x, ushort y, string text, Cell baseCell) =>
        PrintTextClipped(x, y, text, baseCell, Width);

    public ushort PrintTextClipped(
        ushort x,
        ushort y,
        string text,
        Cell baseCell,
        ushort maxX)
    {
        ArgumentNullException.ThrowIfNull(text);
        var column = x;
        foreach (var grapheme in TerminalTextWidth.EnumerateTextElements(text))
        {
            var width = TerminalTextWidth.TextElementWidth(grapheme);
            if (width == 0)
            {
                continue;
            }

            if (column >= maxX || (uint)column + (uint)width > maxX)
            {
                break;
            }

            var runes = grapheme.EnumerateRunes().ToArray();
            var content = width > 1 || runes.Length > 1
                ? CellContent.FromGrapheme(InternWithWidth(grapheme, (byte)width))
                : CellContent.FromRune(runes[0]);
            Buffer.SetFast(column, y, baseCell.WithContent(content));
            column = SaturatingAdd(column, width);
        }

        return column;
    }

    public void DrawBorder(Rect rect, BorderChars border, Cell baseCell) =>
        Buffer.DrawBorder(rect, border, baseCell);

    public void DrawBox(Rect rect, BorderChars border, Cell borderCell, Cell fillCell) =>
        Buffer.DrawBox(rect, border, borderCell, fillCell);

    public void PaintArea(Rect rect, PackedRgba? foreground, PackedRgba? background) =>
        Buffer.PaintArea(rect, foreground, background);

    private HitOwner? CurrentHitOwner() =>
        _hitOwnerStack.TryPeek(out var owner) ? new HitOwner(owner) : null;

    private static ushort SaturatingAdd(ushort value, int increment) =>
        (ushort)Math.Min(ushort.MaxValue, (uint)value + (uint)Math.Max(0, increment));
}
