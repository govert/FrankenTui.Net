// SPDX-License-Identifier: Apache-2.0
// Ported from crates/ftui-render/src/frame.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source SHA-256: FC7E3000C4ABA1B9A335A68A03C940A2EA6CAEFD023B250842B3E724C0C977EB.

using FrankenTui.Core;

namespace FrankenTui.Render;

public readonly record struct HitId(uint Id)
{
    public static HitId New(uint id) => new(id);

    public uint Value => Id;
}

public readonly record struct HitData(ulong Value)
{
    public static readonly HitData Zero = new(0);

    public static implicit operator HitData(ulong value) => new(value);

    public static implicit operator HitData(int value) => value >= 0
        ? new((ulong)value)
        : throw new ArgumentOutOfRangeException(nameof(value));

    public static implicit operator ulong(HitData data) => data.Value;
}

public readonly record struct HitOwner(ulong Value)
{
    public static readonly HitOwner None = new(0);

    public static implicit operator HitOwner(ulong value) => new(value);

    public static implicit operator ulong(HitOwner owner) => owner.Value;
}

/// <summary>
/// Existing target projection of hit-region variants. Named modal variants are retained as
/// compatibility conveniences for source <c>Custom(tag)</c> values.
/// </summary>
public enum HitRegionKind
{
    None,
    Content,
    Border,
    Scrollbar,
    Handle,
    Button,
    Link,
    Custom,
    DialogInput,
    ModalBackdrop,
    ModalContent,
}

/// <summary>Source-shaped hit region, including the payload of <c>Custom(u8)</c>.</summary>
public readonly record struct HitRegion(HitRegionKind Kind, byte CustomTag = 0)
{
    public static readonly HitRegion None = new(HitRegionKind.None);
    public static readonly HitRegion Content = new(HitRegionKind.Content);
    public static readonly HitRegion Border = new(HitRegionKind.Border);
    public static readonly HitRegion Scrollbar = new(HitRegionKind.Scrollbar);
    public static readonly HitRegion Handle = new(HitRegionKind.Handle);
    public static readonly HitRegion Button = new(HitRegionKind.Button);
    public static readonly HitRegion Link = new(HitRegionKind.Link);

    public static HitRegion Custom(byte tag) => new(HitRegionKind.Custom, tag);

    public bool IsCustom => Kind == HitRegionKind.Custom;

    public HitRegionKind ToCompatibilityKind() => Kind;

    public static HitRegion FromCompatibilityKind(HitRegionKind kind) => kind switch
    {
        HitRegionKind.DialogInput => Custom(1),
        HitRegionKind.ModalBackdrop => Custom(1),
        HitRegionKind.ModalContent => Custom(2),
        HitRegionKind.Custom => Custom(0),
        _ => new HitRegion(kind),
    };
}

public readonly record struct HitTestResult
{
    public HitId Id { get; init; }

    /// <summary>Existing target projection, preserving named compatibility variants.</summary>
    public HitRegionKind Region { get; init; }

    public HitRegion SourceRegion { get; init; }

    public HitData Data { get; init; }

    public HitOwner? Owner { get; init; }

    public (HitId, HitRegionKind, HitData) IntoTuple() => (Id, Region, Data);

    public (HitId, HitRegion, HitData) IntoSourceTuple() => (Id, SourceRegion, Data);

    public static HitTestResult New(
        HitId id,
        HitRegion region,
        HitData data,
        HitOwner? owner = null) => new()
    {
        Id = id,
        Region = region.ToCompatibilityKind(),
        SourceRegion = region,
        Data = data,
        Owner = owner,
    };

    public static HitTestResult New(
        HitId id,
        HitRegionKind region,
        HitData data,
        ulong owner) => FromCompatibility(id, region, data, new HitOwner(owner));

    public static HitTestResult New(
        HitId id,
        HitRegionKind region,
        HitData data) => FromCompatibility(id, region, data, null);

    internal static HitTestResult FromCompatibility(
        HitId id,
        HitRegionKind region,
        HitData data,
        HitOwner? owner) => new()
    {
        Id = id,
        Region = region,
        SourceRegion = HitRegion.FromCompatibilityKind(region),
        Data = data,
        Owner = owner,
    };
}

public readonly record struct HitCell
{
    public HitId? WidgetId { get; init; }

    /// <summary>Existing target projection, preserving named compatibility variants.</summary>
    public HitRegionKind Region { get; init; }

    public HitRegion SourceRegion { get; init; }

    public HitData Data { get; init; }

    public HitOwner? Owner { get; init; }

    public bool IsEmpty => WidgetId is null;

    public static HitCell Create(HitId widgetId, HitRegion region, HitData data) =>
        Create(widgetId, region, data, null);

    public static HitCell Create(
        HitId widgetId,
        HitRegion region,
        HitData data,
        HitOwner? owner) => new()
    {
        WidgetId = widgetId,
        Region = region.ToCompatibilityKind(),
        SourceRegion = region,
        Data = data,
        Owner = owner,
    };

    public static HitCell Create(HitId widgetId, HitRegionKind region, HitData data) =>
        Create(widgetId, region, data, null);

    public static HitCell Create(
        HitId widgetId,
        HitRegionKind region,
        HitData data,
        HitOwner? owner) => new()
    {
        WidgetId = widgetId,
        Region = region,
        SourceRegion = HitRegion.FromCompatibilityKind(region),
        Data = data,
        Owner = owner,
    };
}

public sealed class HitGrid
{
    private readonly HitCell[] _cells;

    public HitGrid(ushort width, ushort height)
    {
        Width = width;
        Height = height;
        _cells = new HitCell[checked(width * height)];
    }

    private HitGrid(ushort width, ushort height, HitCell[] cells)
    {
        Width = width;
        Height = height;
        _cells = cells;
    }

    public ushort Width { get; }

    public ushort Height { get; }

    public HitCell? Get(ushort x, ushort y) =>
        TryIndex(x, y, out var index) ? _cells[index] : null;

    /// <summary>Managed mutation projection of Rust's optional mutable reference.</summary>
    public bool TryMutate(ushort x, ushort y, Func<HitCell, HitCell> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        if (!TryIndex(x, y, out var index))
        {
            return false;
        }

        _cells[index] = mutation(_cells[index]);
        return true;
    }

    public void Register(Rect rect, HitId widgetId, HitRegion region, HitData data) =>
        Register(rect, widgetId, region, data, null);

    public void Register(
        Rect rect,
        HitId widgetId,
        HitRegion region,
        HitData data,
        HitOwner? owner) => RegisterCore(
            rect,
            HitCell.Create(widgetId, region, data, owner));

    public void Register(Rect rect, HitId widgetId, HitRegionKind region, HitData data) =>
        Register(rect, widgetId, region, data, null);

    public void Register(
        Rect rect,
        HitId widgetId,
        HitRegionKind region,
        HitData data,
        HitOwner? owner) => RegisterCore(
            rect,
            HitCell.Create(widgetId, region, data, owner));

    public (HitId, HitRegionKind, HitData)? HitTest(ushort x, ushort y) =>
        HitTestDetailed(x, y)?.IntoTuple();

    public (HitId, HitRegion, HitData)? HitTestSource(ushort x, ushort y) =>
        HitTestDetailed(x, y)?.IntoSourceTuple();

    public HitTestResult? HitTestDetailed(ushort x, ushort y)
    {
        var cell = Get(x, y);
        if (cell is not { WidgetId: { } id } populated)
        {
            return null;
        }

        return new HitTestResult
        {
            Id = id,
            Region = populated.Region,
            SourceRegion = populated.SourceRegion,
            Data = populated.Data,
            Owner = populated.Owner,
        };
    }

    public IReadOnlyList<(HitId Id, HitRegionKind Region, HitData Data)> HitsIn(Rect rect)
    {
        var hits = new List<(HitId, HitRegionKind, HitData)>();
        Visit(rect, (x, y) =>
        {
            if (HitTest(x, y) is { } hit)
            {
                hits.Add(hit);
            }
        });
        return hits;
    }

    public IReadOnlyList<(HitId Id, HitRegion Region, HitData Data)> HitsInSource(Rect rect)
    {
        var hits = new List<(HitId, HitRegion, HitData)>();
        Visit(rect, (x, y) =>
        {
            if (HitTestSource(x, y) is { } hit)
            {
                hits.Add(hit);
            }
        });
        return hits;
    }

    public void Clear() => Array.Clear(_cells);

    public HitGrid Clone() => new(Width, Height, [.. _cells]);

    private void RegisterCore(Rect rect, HitCell cell)
    {
        var xEnd = Math.Min((uint)rect.X + rect.Width, Width);
        var yEnd = Math.Min((uint)rect.Y + rect.Height, Height);
        if (rect.X >= xEnd || rect.Y >= yEnd)
        {
            return;
        }

        for (var y = (uint)rect.Y; y < yEnd; y++)
        {
            var rowStart = y * Width;
            var start = rowStart + rect.X;
            var end = rowStart + xEnd;
            Array.Fill(_cells, cell, checked((int)start), checked((int)(end - start)));
        }
    }

    private void Visit(Rect rect, Action<ushort, ushort> visitor)
    {
        var xEnd = Math.Min((uint)rect.X + rect.Width, Width);
        var yEnd = Math.Min((uint)rect.Y + rect.Height, Height);
        for (var y = (uint)rect.Y; y < yEnd; y++)
        {
            for (var x = (uint)rect.X; x < xEnd; x++)
            {
                visitor((ushort)x, (ushort)y);
            }
        }
    }

    private bool TryIndex(ushort x, ushort y, out int index)
    {
        if (x < Width && y < Height)
        {
            index = checked((y * Width) + x);
            return true;
        }

        index = -1;
        return false;
    }
}
