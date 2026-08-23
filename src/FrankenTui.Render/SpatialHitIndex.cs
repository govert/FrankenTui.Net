// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/spatial_hit_index.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: current Rust construction computes a cell-size-1 grid for a
// configured zero cell size but retains zero in `config`, so the first routed
// query/register would divide by zero. The managed constructor retains the
// source test/documented intent by normalizing the stored cell size to one.

using FrankenTui.Core;

namespace FrankenTui.Render;

public sealed record SpatialHitConfig
{
    public ushort CellSize { get; set; } = 8;
    public int BucketWarnThreshold { get; set; } = 64;
    public bool TrackCacheStats { get; set; }
}

public struct HitEntry : IEquatable<HitEntry>
{
    public HitEntry(
        HitId id,
        Rect rect,
        HitRegionKind region,
        HitData data,
        ushort zOrder,
        uint order)
    {
        Id = id;
        Rect = rect;
        Region = region;
        Data = data;
        ZOrder = zOrder;
        Order = order;
    }

    public HitId Id { get; set; }
    public Rect Rect { get; set; }
    public HitRegionKind Region { get; set; }
    public HitData Data { get; set; }
    public ushort ZOrder { get; set; }
    internal uint Order { get; }

    public static HitEntry New(
        HitId id,
        Rect rect,
        HitRegionKind region,
        HitData data,
        ushort zOrder,
        uint order) => new(id, rect, region, data, zOrder, order);

    public readonly bool Contains(ushort x, ushort y) => Rect.Contains(x, y);

    internal readonly int CompareZOrder(in HitEntry other)
    {
        var byLayer = ZOrder.CompareTo(other.ZOrder);
        return byLayer != 0 ? byLayer : Order.CompareTo(other.Order);
    }

    public readonly bool Equals(HitEntry other) =>
        Id == other.Id && Rect == other.Rect && Region == other.Region &&
        Data == other.Data && ZOrder == other.ZOrder && Order == other.Order;

    public override readonly bool Equals(object? obj) => obj is HitEntry other && Equals(other);

    public override readonly int GetHashCode() =>
        HashCode.Combine(Id, Rect, Region, Data, ZOrder, Order);

    public static bool operator ==(HitEntry left, HitEntry right) => left.Equals(right);
    public static bool operator !=(HitEntry left, HitEntry right) => !left.Equals(right);
}

public record struct CacheStats(ulong Hits, ulong Misses, ulong Rebuilds)
{
    public float HitRate
    {
        get
        {
            var total = Hits + Misses;
            return total == 0 ? 0f : ((float)Hits / total) * 100f;
        }
    }
}

/// <summary>
/// Uniform-grid spatial hit index with z-order, registration-order tie
/// breaking, a one-position hover cache, and dirty-region invalidation.
/// </summary>
public sealed class SpatialHitIndex
{
    private readonly SpatialHitConfig _config;
    private readonly ushort _width;
    private readonly ushort _height;
    private readonly ushort _gridWidth;
    private readonly ushort _gridHeight;
    private readonly List<HitEntry> _entries = new(256);
    private readonly List<uint>[] _buckets;
    private readonly Dictionary<HitId, uint> _idToEntry = new(256);
    private readonly Dictionary<(ushort X, ushort Y), string> _legacyHits = [];
    private readonly List<Rect> _dirtyRects = [];
    private uint _nextOrder;
    private (ushort X, ushort Y) _cachePosition;
    private uint? _cacheResult;
    private bool _cacheValid;
    private bool _fullRebuildDirty;
    private ulong _cacheHits;
    private ulong _cacheMisses;
    private ulong _rebuilds;

    /// <summary>Compatibility constructor for the former 80x24 point-map placeholder.</summary>
    public SpatialHitIndex() : this(80, 24, new SpatialHitConfig())
    {
    }

    public SpatialHitIndex(ushort width, ushort height, SpatialHitConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config with { CellSize = Math.Max(config.CellSize, (ushort)1) };
        _width = width;
        _height = height;
        var cellSize = _config.CellSize;
        _gridWidth = (ushort)(((uint)width + cellSize - 1) / cellSize);
        _gridHeight = (ushort)(((uint)height + cellSize - 1) / cellSize);
        _buckets = new List<uint>[(int)_gridWidth * _gridHeight];
        for (var index = 0; index < _buckets.Length; index++)
        {
            _buckets[index] = [];
        }
    }

    public static SpatialHitIndex WithDefaults(ushort width, ushort height) =>
        new(width, height, new SpatialHitConfig());

    public int Count => _idToEntry.Count;
    public bool IsEmpty => _idToEntry.Count == 0;
    public CacheStats Stats => new(_cacheHits, _cacheMisses, _rebuilds);

    internal SpatialHitConfig Config => _config;
    internal ushort GridWidth => _gridWidth;
    internal ushort GridHeight => _gridHeight;
    internal bool CacheValid => _cacheValid;

    public void Register(
        HitId id,
        Rect rect,
        HitRegionKind region,
        HitData data,
        ushort zOrder)
    {
        if (id == default)
        {
            return;
        }

        if (_idToEntry.ContainsKey(id))
        {
            Remove(id);
        }

        var entryIndex = (uint)_entries.Count;
        _entries.Add(new HitEntry(id, rect, region, data, zOrder, _nextOrder));
        _nextOrder = unchecked(_nextOrder + 1);
        _idToEntry[id] = entryIndex;
        AddToBuckets(entryIndex, rect, warnOnDensity: true);
        MarkDirty(rect);
        InvalidateCacheIfDirty();
    }

    public void RegisterSimple(HitId id, Rect rect, HitRegionKind region, HitData data) =>
        Register(id, rect, region, data, 0);

    public bool Update(HitId id, Rect newRect)
    {
        if (!_idToEntry.TryGetValue(id, out var entryIndex))
        {
            return false;
        }

        var entry = _entries[(int)entryIndex];
        MarkDirty(entry.Rect);
        MarkDirty(newRect);
        entry.Rect = newRect;
        _entries[(int)entryIndex] = entry;
        RebuildBuckets();
        _cacheValid = false;
        return true;
    }

    public bool Remove(HitId id)
    {
        if (!_idToEntry.TryGetValue(id, out var entryIndex))
        {
            return false;
        }

        var entry = _entries[(int)entryIndex];
        MarkDirty(entry.Rect);
        entry.Id = default;
        _entries[(int)entryIndex] = entry;
        _idToEntry.Remove(id);
        RebuildBuckets();
        _cacheValid = false;
        return true;
    }

    public (HitId Id, HitRegionKind Region, HitData Data)? HitTest(ushort x, ushort y)
    {
        if (x >= _width || y >= _height)
        {
            return null;
        }

        if (_cacheValid && _cachePosition == (x, y))
        {
            if (_config.TrackCacheStats)
            {
                _cacheHits++;
            }

            return _cacheResult is { } cached ? ToResult(_entries[(int)cached]) : null;
        }

        if (_config.TrackCacheStats)
        {
            _cacheMisses++;
        }

        var bestIndex = FindBestEntry(x, y);
        _cachePosition = (x, y);
        _cacheResult = bestIndex;
        _cacheValid = true;
        ClearDirty();
        return bestIndex is { } found ? ToResult(_entries[(int)found]) : null;
    }

    public (HitId Id, HitRegionKind Region, HitData Data)? HitTestReadonly(ushort x, ushort y)
    {
        if (x >= _width || y >= _height)
        {
            return null;
        }

        var bestIndex = FindBestEntry(x, y);
        return bestIndex is { } found ? ToResult(_entries[(int)found]) : null;
    }

    public void Clear()
    {
        _entries.Clear();
        _idToEntry.Clear();
        _legacyHits.Clear();
        foreach (var bucket in _buckets)
        {
            bucket.Clear();
        }

        _nextOrder = 0;
        _cacheValid = false;
        ClearDirty();
    }

    public void ResetStats()
    {
        _cacheHits = 0;
        _cacheMisses = 0;
        _rebuilds = 0;
    }

    public void InvalidateRegion(Rect rect)
    {
        MarkDirty(rect);
        InvalidateCacheIfDirty();
    }

    public void InvalidateAll()
    {
        _cacheValid = false;
        _fullRebuildDirty = true;
        _dirtyRects.Clear();
    }

    /// <summary>Legacy point-map API preserved from the earlier managed placeholder.</summary>
    public void Register(ushort x, ushort y, string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        _legacyHits[(x, y)] = id;
    }

    /// <summary>Legacy point-map API preserved from the earlier managed placeholder.</summary>
    public string? Hit(ushort x, ushort y) =>
        _legacyHits.TryGetValue((x, y), out var id) ? id : null;

    private uint? FindBestEntry(ushort x, ushort y)
    {
        var bucket = _buckets[BucketIndex(x, y)];
        uint? bestIndex = null;
        foreach (var entryIndex in bucket)
        {
            var candidate = _entries[(int)entryIndex];
            if (candidate.Id == default || !candidate.Contains(x, y))
            {
                continue;
            }

            if (bestIndex is null ||
                candidate.CompareZOrder(_entries[(int)bestIndex.Value]) > 0)
            {
                bestIndex = entryIndex;
            }
        }

        return bestIndex;
    }

    private int BucketIndex(ushort x, ushort y)
    {
        var bucketX = Math.Min(x / _config.CellSize, Math.Max(_gridWidth - 1, 0));
        var bucketY = Math.Min(y / _config.CellSize, Math.Max(_gridHeight - 1, 0));
        return (bucketY * _gridWidth) + bucketX;
    }

    private (ushort XStart, ushort YStart, ushort XEnd, ushort YEnd) BucketRange(Rect rect)
    {
        var maxGridX = _gridWidth == 0 ? 0 : _gridWidth - 1;
        var maxGridY = _gridHeight == 0 ? 0 : _gridHeight - 1;
        var xStart = rect.X / _config.CellSize;
        var yStart = rect.Y / _config.CellSize;
        var xEndCoordinate = SaturatingAdd(rect.X, SaturatingSubtract(rect.Width, 1));
        var yEndCoordinate = SaturatingAdd(rect.Y, SaturatingSubtract(rect.Height, 1));
        return (
            (ushort)Math.Min(xStart, maxGridX),
            (ushort)Math.Min(yStart, maxGridY),
            (ushort)Math.Min(xEndCoordinate / _config.CellSize, maxGridX),
            (ushort)Math.Min(yEndCoordinate / _config.CellSize, maxGridY));
    }

    private void AddToBuckets(uint entryIndex, Rect rect, bool warnOnDensity)
    {
        if (rect.Width == 0 || rect.Height == 0)
        {
            return;
        }

        var (xStart, yStart, xEnd, yEnd) = BucketRange(rect);
        for (var y = (int)yStart; y <= yEnd; y++)
        {
            for (var x = (int)xStart; x <= xEnd; x++)
            {
                var bucketIndex = (y * _gridWidth) + x;
                if ((uint)bucketIndex >= (uint)_buckets.Length)
                {
                    continue;
                }

                _buckets[bucketIndex].Add(entryIndex);
                if (warnOnDensity && _buckets[bucketIndex].Count > _config.BucketWarnThreshold)
                {
                    // Upstream reserves this branch for a future diagnostic sink.
                }
            }
        }
    }

    private void RebuildBuckets()
    {
        foreach (var bucket in _buckets)
        {
            bucket.Clear();
        }

        var writeIndex = 0;
        for (var readIndex = 0; readIndex < _entries.Count; readIndex++)
        {
            if (_entries[readIndex].Id == default)
            {
                continue;
            }

            if (writeIndex != readIndex)
            {
                _entries[writeIndex] = _entries[readIndex];
            }

            writeIndex++;
        }

        if (writeIndex < _entries.Count)
        {
            _entries.RemoveRange(writeIndex, _entries.Count - writeIndex);
        }

        _idToEntry.Clear();
        for (var index = 0; index < _entries.Count; index++)
        {
            _idToEntry[_entries[index].Id] = (uint)index;
        }

        for (var index = 0; index < _entries.Count; index++)
        {
            AddToBuckets((uint)index, _entries[index].Rect, warnOnDensity: false);
        }

        ClearDirty();
        _rebuilds++;
    }

    private void MarkDirty(Rect rect)
    {
        if (!_fullRebuildDirty)
        {
            _dirtyRects.Add(rect);
        }
    }

    private void InvalidateCacheIfDirty()
    {
        if (!_cacheValid)
        {
            return;
        }

        if (_fullRebuildDirty || _dirtyRects.Any(rect => rect.Contains(_cachePosition.X, _cachePosition.Y)))
        {
            _cacheValid = false;
        }
    }

    private void ClearDirty()
    {
        _dirtyRects.Clear();
        _fullRebuildDirty = false;
    }

    private static (HitId Id, HitRegionKind Region, HitData Data) ToResult(in HitEntry entry) =>
        (entry.Id, entry.Region, entry.Data);

    private static ushort SaturatingAdd(ushort left, ushort right)
    {
        var sum = (uint)left + right;
        return sum >= ushort.MaxValue ? ushort.MaxValue : (ushort)sum;
    }

    private static ushort SaturatingSubtract(ushort left, ushort right) =>
        left >= right ? (ushort)(left - right) : (ushort)0;
}
