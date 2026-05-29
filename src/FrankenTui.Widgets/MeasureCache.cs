// Upstream source: crates/ftui-widgets/src/measure_cache.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Direct 1-1 port of WidgetId, CacheStats, and MeasureCache.

using System.Runtime.CompilerServices;
using FrankenTui.Core;

namespace FrankenTui.Widgets;

/// <summary>
/// Unique identifier for a widget instance.
/// Used as part of the cache key to distinguish between different widgets.
/// </summary>
public readonly record struct WidgetId(ulong Value)
{
    /// <summary>
    /// Create a WidgetId from an object reference.
    /// Stable for the lifetime of the widget.
    /// </summary>
    public static WidgetId FromPtr<T>(T obj) where T : class =>
        new((ulong)RuntimeHelpers.GetHashCode(obj)); // DIVERGENCE: .NET doesn't expose raw ptrs safely; use GetHashCode as stable surrogate

    /// <summary>
    /// Create a WidgetId from a content hash.
    /// Stable across widget recreations as long as content is the same.
    /// </summary>
    public static WidgetId FromHash<T>(ref T value) where T : notnull =>
        new((ulong)value.GetHashCode()); // DIVERGENCE: simpler hash; upstream uses DefaultHasher
}

/// <summary>
/// Statistics about cache performance.
/// </summary>
public readonly record struct CacheStats
{
    /// <summary>Number of entries currently in the cache.</summary>
    public int Entries { get; init; }
    /// <summary>Total cache hits since creation or last reset.</summary>
    public ulong Hits { get; init; }
    /// <summary>Total cache misses since creation or last reset.</summary>
    public ulong Misses { get; init; }
    /// <summary>Hit rate as a fraction (0.0 to 1.0).</summary>
    public double HitRate { get; init; }
}

/// <summary>
/// Cache for widget measure results.
/// Caches <see cref="SizeConstraints"/> returned by <see cref="IMeasurableWidget.MeasureConstraints"/>
/// to avoid redundant computation during layout passes.
/// </summary>
public sealed class MeasureCache
{
    private readonly Dictionary<(WidgetId, ushort, ushort), CacheEntry> _entries = [];
    private ulong _generation;
    private readonly int _maxEntries;
    private ulong _hits;
    private ulong _misses;

    /// <summary>Create a new cache with the specified maximum capacity.</summary>
    public MeasureCache(int maxEntries = 256)
    {
        _maxEntries = maxEntries;
    }

    /// <summary>
    /// Get cached result or compute and cache a new one.
    /// </summary>
    public SizeConstraints GetOrCompute(WidgetId widgetId, Size available, Func<SizeConstraints> compute)
    {
        var key = (widgetId, available.Width, available.Height);

        // Check for existing valid entry
        if (_entries.TryGetValue(key, out var entry) && entry.Generation == _generation)
        {
            _hits++;
            _entries[key] = new CacheEntry(entry.Constraints, entry.Generation, entry.AccessCount + 1);
            return entry.Constraints;
        }

        // Cache miss
        _misses++;
        var constraints = compute();

        // Evict if at capacity
        while (_entries.Count >= _maxEntries && _maxEntries > 0)
        {
            EvictLfu();
        }

        _entries[key] = new CacheEntry(constraints, _generation, 1);
        return constraints;
    }

    /// <summary>Invalidate all entries by bumping the generation.</summary>
    public void InvalidateAll()
    {
        _generation++;
    }

    /// <summary>Invalidate entries for a specific widget.</summary>
    public void InvalidateWidget(WidgetId widgetId)
    {
        var keysToRemove = _entries.Keys
            .Where(k => k.Item1 == widgetId)
            .ToList();
        foreach (var key in keysToRemove)
            _entries.Remove(key);
    }

    /// <summary>Get current cache statistics.</summary>
    public CacheStats Stats() => new()
    {
        Entries = _entries.Count,
        Hits = _hits,
        Misses = _misses,
        HitRate = (_hits + _misses) > 0 ? (double)_hits / (_hits + _misses) : 0.0,
    };

    /// <summary>Reset statistics counters to zero.</summary>
    public void ResetStats()
    {
        _hits = 0;
        _misses = 0;
    }

    /// <summary>Clear all entries from the cache.</summary>
    public void Clear()
    {
        _entries.Clear();
        _generation++;
    }

    /// <summary>Returns the current number of entries.</summary>
    public int Count => _entries.Count;

    /// <summary>Returns true if the cache is empty.</summary>
    public bool IsEmpty => _entries.Count == 0;

    /// <summary>Returns the maximum capacity.</summary>
    public int Capacity => _maxEntries;

    /// <summary>Evict the least frequently used entry.</summary>
    private void EvictLfu()
    {
        if (_entries.Count == 0) return;
        var minKey = _entries.MinBy(kvp => kvp.Value.AccessCount).Key;
        _entries.Remove(minKey);
    }

    private readonly record struct CacheEntry(SizeConstraints Constraints, ulong Generation, uint AccessCount);
}
