// Upstream source: crates/ftui-render/src/frame.rs (HitId, HitRegion, HitCell, HitGrid, HitData, HitOwner, HitTestResult)
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Direct 1-1 port of frame hit-testing infrastructure.

using FrankenTui.Core;

namespace FrankenTui.Render;

/// <summary>Identifier for a clickable region in the hit grid.</summary>
public readonly record struct HitId(ulong Id)
{
    public static HitId New(uint id) => new(id);
    public ulong Value => Id;
}

/// <summary>Opaque user data for hit callbacks.</summary>
public readonly record struct HitData(ulong Value)
{
    public static readonly HitData Zero = new(0);
}

/// <summary>Optional ownership tag attached to a hit region.</summary>
public readonly record struct HitOwner(ulong Value)
{
    public static readonly HitOwner None = new(0);
}

/// <summary>Regions within a widget for mouse interaction.</summary>
public enum HitRegionKind
{
    None,
    Content,
    Border,
    Scrollbar,
    Handle,
    Button,
    Link,
    Custom, // carries u8 upstream; simplified here
    // DIVERGENCE: upstream models these as HitRegion::Custom(n). Modeled as named
    // variants here so the modal/dialog widgets can reference them by name.
    DialogInput,
    ModalBackdrop,
    ModalContent,
}

/// <summary>Full hit-test metadata, including optional ownership provenance.</summary>
public readonly record struct HitTestResult
{
    public HitId Id { get; init; }
    public HitRegionKind Region { get; init; }
    public HitData Data { get; init; }
    public HitOwner? Owner { get; init; }

    public (HitId, HitRegionKind, HitData) IntoTuple() => (Id, Region, Data);
}

/// <summary>A single hit cell in the grid.</summary>
public readonly record struct HitCell
{
    /// <summary>Widget that registered this cell, if any.</summary>
    public HitId? WidgetId { get; init; }
    /// <summary>Region tag for the hit area.</summary>
    public HitRegionKind Region { get; init; }
    /// <summary>Extra data attached to this hit cell.</summary>
    public HitData Data { get; init; }
    /// <summary>Optional owner tag for higher-level hit routing.</summary>
    public HitOwner? Owner { get; init; }

    public static HitCell Create(HitId widgetId, HitRegionKind region, HitData data) => new()
    {
        WidgetId = widgetId,
        Region = region,
        Data = data,
        Owner = null,
    };

    public bool IsEmpty => WidgetId is null;
}

/// <summary>Hit testing grid for mouse interaction.</summary>
public sealed class HitGrid
{
    private readonly ushort _width;
    private readonly ushort _height;
    private readonly HitCell[] _cells;

    public ushort Width => _width;
    public ushort Height => _height;

    public HitGrid(ushort width, ushort height)
    {
        _width = width;
        _height = height;
        _cells = new HitCell[width * height];
    }

    private int Index(ushort x, ushort y) =>
        x < _width && y < _height ? y * _width + x : -1;

    /// <summary>Get the hit cell at (x, y).</summary>
    public HitCell? Get(ushort x, ushort y)
    {
        var idx = Index(x, y);
        return idx >= 0 ? _cells[idx] : null;
    }

    /// <summary>Register a clickable region.</summary>
    public void Register(Rect rect, HitId widgetId, HitRegionKind region, HitData data)
    {
        var xEnd = (ushort)Math.Min(rect.Right, _width);
        var yEnd = (ushort)Math.Min(rect.Bottom, _height);
        var cell = HitCell.Create(widgetId, region, data);

        for (var y = rect.Y; y < yEnd; y++)
        {
            var rowStart = y * _width;
            var start = rowStart + rect.X;
            var end = rowStart + xEnd;
            for (var i = start; i < end; i++)
                _cells[i] = cell;
        }
    }

    /// <summary>Hit test at the given position.</summary>
    public (HitId, HitRegionKind, HitData)? HitTest(ushort x, ushort y)
    {
        var idx = Index(x, y);
        if (idx < 0) return null;
        var cell = _cells[idx];
        return cell.WidgetId is not null ? (cell.WidgetId.Value, cell.Region, cell.Data) : null;
    }

    /// <summary>Clear all hit regions.</summary>
    public void Clear()
    {
        System.Array.Clear(_cells, 0, _cells.Length);
    }
}
