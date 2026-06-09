// Port of .external/frankentui/crates/ftui-widgets/src/virtualized.rs
// Virtualization primitives for efficient rendering of large content.

// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Runtime;

namespace FrankenTui.Widgets;

/// <summary>
/// Virtualization primitives for efficient rendering of large content.
///
/// This module provides the foundational types for rendering only visible
/// portions of large datasets, enabling smooth performance with 100K+ items.
///
/// Core Types:
/// - <see cref="Virtualized{T}"/> - Generic container with visible range calculation
/// - <see cref="VirtualizedStorage{T}"/> - Owned vs external storage abstraction
/// - <see cref="ItemHeight"/> - Fixed vs variable height support
/// - <see cref="HeightCache"/> - LRU cache for measured item heights
/// </summary>

// ============================================================================
// VirtualizedStorage<T>
// ============================================================================

/// <summary>Storage strategy for virtualized items.</summary>
// DIVERGENCE: Rust uses an enum with associated data; C# uses a sealed class
// hierarchy with a discriminator, mirroring the AGENTS.md closed class hierarchy rule.
public abstract class VirtualizedStorage<T>
{
    /// <summary>Owned vector of items.</summary>
    public sealed class Owned : VirtualizedStorage<T>
    {
        /// <summary>The underlying deque of items.</summary>
        public Queue<T> Items { get; }

        public Owned(int capacity)
        {
            Items = new Queue<T>(Math.Min(capacity, 1024));
        }

        public override int Len() => Items.Count;
    }

    /// <summary>
    /// External storage with known length.
    /// Note: External fetch is handled at the widget level.
    /// </summary>
    public sealed class External : VirtualizedStorage<T>
    {
        /// <summary>Total number of items available.</summary>
        public int Len_ { get; set; }
        /// <summary>Maximum items to keep in local cache.</summary>
        public int CacheCapacity { get; }

        public External(int len, int cacheCapacity)
        {
            Len_ = len;
            CacheCapacity = cacheCapacity;
        }

        public override int Len() => Len_;
    }

    /// <summary>Total item count, depending on storage kind.</summary>
    public abstract int Len();
}

// ============================================================================
// ItemHeight
// ============================================================================

/// <summary>Height calculation strategy for items.</summary>
// DIVERGENCE: Rust enum with associated data → sealed class hierarchy per AGENTS.md.
public abstract class ItemHeight
{
    /// <summary>All items have fixed height.</summary>
    public sealed class Fixed : ItemHeight
    {
        public ushort Height { get; }
        public Fixed(ushort height) { Height = height; }
    }

    /// <summary>Items have variable height, cached lazily (linear scan).</summary>
    public sealed class Variable : ItemHeight
    {
        public HeightCache Cache { get; }
        public Variable(HeightCache cache) { Cache = cache; }
    }

    /// <summary>Items have variable height with O(log n) scroll-to-index via Fenwick tree.</summary>
    public sealed class VariableFenwick : ItemHeight
    {
        public VariableHeightsFenwick Tracker { get; }
        public VariableFenwick(VariableHeightsFenwick tracker) { Tracker = tracker; }
    }
}

// ============================================================================
// HeightCache
// ============================================================================

/// <summary>LRU cache for measured item heights.</summary>
public sealed class HeightCache
{
    /// <summary>Height measurements indexed by (item index - base_offset).</summary>
    // DIVERGENCE: exposed as internal for tests that inspect cache.len directly.
    internal List<ushort?> _cache;
    /// <summary>Offset of the first entry in the cache (cache[0] corresponds to this item index).</summary>
    internal int _baseOffset;
    /// <summary>Default height for unmeasured items.</summary>
    private ushort _defaultHeight;
    /// <summary>Maximum entries to cache (for memory bounds).</summary>
    internal int _capacity;

    // Expose for tests
    internal List<ushort?> CacheList => _cache;
    public int BaseOffset => _baseOffset;
    public int Capacity => _capacity;

    public static HeightCache Default() => new HeightCache(1, 1000);

    /// <summary>Create a new height cache.</summary>
    public HeightCache(ushort defaultHeight, int capacity)
    {
        _cache = new List<ushort?>();
        _baseOffset = 0;
        _defaultHeight = defaultHeight;
        _capacity = capacity;
    }

    /// <summary>Get height for item, returning default if not cached.</summary>
    public ushort Get(int idx)
    {
        if (idx < _baseOffset)
            return _defaultHeight;
        int local = idx - _baseOffset;
        if (local < _cache.Count && _cache[local].HasValue)
            return _cache[local]!.Value;
        return _defaultHeight;
    }

    /// <summary>Set height for item.</summary>
    public void Set(int idx, ushort height)
    {
        if (_capacity == 0)
            return;
        if (idx < _baseOffset)
        {
            // Index has been trimmed away; ignore
            return;
        }
        int local = idx - _baseOffset;

        // If the jump is so large that we'd drain the entire current cache anyway,
        // reset the window to avoid a huge allocation of Nones.
        if (local + 1 >= _cache.Count + _capacity)
        {
            _baseOffset = Math.Max(0, idx + 1 - _capacity);
            _cache.Clear();
            local = idx - _baseOffset;
        }

        if (local >= _cache.Count)
        {
            int needed = local + 1 - _cache.Count;
            for (int i = 0; i < needed; i++)
                _cache.Add(null);
        }
        _cache[local] = height;

        // Trim if over capacity: remove oldest entries and adjust base_offset
        if (_cache.Count > _capacity)
        {
            int toRemove = _cache.Count - _capacity;
            _cache.RemoveRange(0, toRemove);
            _baseOffset += toRemove;
        }
    }

    /// <summary>Clear cached heights.</summary>
    public void Clear()
    {
        _cache.Clear();
        _baseOffset = 0;
    }
}

// ============================================================================
// VariableHeightsFenwick - O(log n) scroll-to-index mapping
// ============================================================================

/// <summary>
/// Variable height tracker using Fenwick tree for O(log n) prefix sum queries.
///
/// This enables efficient scroll offset to item index mapping for virtualized
/// lists with variable height items.
///
/// Operations:
/// | Operation              | Time    |
/// |------------------------|---------|
/// | FindItemAtOffset       | O(log n)|
/// | OffsetOfItem           | O(log n)|
/// | Set                    | O(log n)|
/// | TotalHeight            | O(log n)|
///
/// Invariants:
/// 1. tree.Prefix(i) == sum of heights [0..=i]
/// 2. FindItemAtOffset(offset) returns largest i where Prefix(i-1) &lt; offset
/// 3. Heights are u32 internally (u16 input widened for large lists)
/// </summary>
public sealed class VariableHeightsFenwick
{
    /// <summary>Fenwick tree storing item heights.</summary>
    private FenwickTree _tree;
    /// <summary>Default height for items not yet measured.</summary>
    private ushort _defaultHeight;
    /// <summary>Number of items tracked.</summary>
    private int _len;

    public static VariableHeightsFenwick Default() => new VariableHeightsFenwick(1, 0);

    /// <summary>Create a new height tracker with given default height and initial capacity.</summary>
    public VariableHeightsFenwick(ushort defaultHeight, int capacity)
    {
        _defaultHeight = defaultHeight;
        if (capacity > 0)
        {
            // Initialize with default heights
            uint[] heights = new uint[capacity];
            for (int i = 0; i < capacity; i++)
                heights[i] = defaultHeight;
            _tree = FenwickTree.FromValues(heights);
        }
        else
        {
            _tree = new FenwickTree(0);
        }
        _len = capacity;
    }

    /// <summary>Create from a slice of heights.</summary>
    public static VariableHeightsFenwick FromHeights(ushort[] heights, ushort defaultHeight)
    {
        uint[] heightsU32 = new uint[heights.Length];
        for (int i = 0; i < heights.Length; i++)
            heightsU32[i] = heights[i];
        return new VariableHeightsFenwick
        {
            _tree = FenwickTree.FromValues(heightsU32),
            _defaultHeight = defaultHeight,
            _len = heights.Length
        };
    }

    // Private constructor for FromHeights
    private VariableHeightsFenwick() { _tree = new FenwickTree(0); }

    /// <summary>Number of items tracked.</summary>
    public int Len() => _len;

    /// <summary>Whether tracking is empty.</summary>
    public bool IsEmpty() => _len == 0;

    /// <summary>Get the default height for unmeasured items.</summary>
    public ushort DefaultHeight() => _defaultHeight;

    /// <summary>Get height of a specific item. O(log n).</summary>
    public ushort Get(int idx)
    {
        if (idx >= _len)
            return _defaultHeight;
        // Fenwick get returns the individual value at idx
        uint v = _tree.Get(idx);
        return (ushort)Math.Min(v, ushort.MaxValue);
    }

    /// <summary>Set height of a specific item. O(log n).</summary>
    public void Set(int idx, ushort height)
    {
        if (idx >= _len)
        {
            // Need to resize
            Resize(idx + 1);
        }
        _tree.Set(idx, height);
    }

    /// <summary>
    /// Get the y-offset (in pixels/rows) of an item. O(log n).
    /// Returns the sum of heights of all items before idx.
    /// </summary>
    public uint OffsetOfItem(int idx)
    {
        if (idx == 0 || _len == 0)
            return 0;
        int clamped = Math.Min(idx, _len);
        if (clamped > 0)
            return _tree.Prefix(clamped - 1);
        return 0;
    }

    /// <summary>
    /// Find the item index at a given scroll offset. O(log n).
    /// Returns the index of the item that occupies the given offset.
    /// If offset is beyond all items, returns Len().
    /// Item i occupies offsets [OffsetOfItem(i), OffsetOfItem(i+1)).
    /// </summary>
    public int FindItemAtOffset(uint offset)
    {
        if (_len == 0)
            return 0;
        if (offset == 0)
            return 0;
        // find_prefix returns largest i where prefix(i) <= offset
        // prefix(i) = sum of heights [0..=i] = y-coordinate just past item i
        // If prefix(i) <= offset, then offset is at or past the end of item i,
        // so offset is in item i+1.
        //
        // We use `offset` directly (not `offset - 1`). When `offset == prefix(i)` exactly,
        // `find_prefix` returns `i`, and we correctly map that to item `i+1` below.
        int? found = _tree.FindPrefix(offset);
        if (found.HasValue)
        {
            // prefix(i) <= offset
            // Item i spans [prefix(i-1), prefix(i)), so offset >= prefix(i)
            // means offset is in item i+1 or beyond
            return Math.Min(found.Value + 1, _len);
        }
        else
        {
            // offset < prefix(0), so offset is within item 0
            return 0;
        }
    }

    /// <summary>
    /// Count how many items are visible within a viewport starting at start_idx. O(log n).
    /// Visibility here means partially or fully visible. The first partially
    /// visible trailing item is counted.
    /// Returns at least 1 when start_idx &lt; len and viewport_height &gt; 0,
    /// even if the first item is taller than the viewport.
    /// </summary>
    public int VisibleCount(int startIdx, ushort viewportHeight)
    {
        if (_len == 0 || viewportHeight == 0)
            return 0;
        int start = Math.Min(startIdx, _len);
        uint startOffset = OffsetOfItem(start);
        uint endOffset = startOffset + (uint)viewportHeight; // saturating_add equivalent (uint won't overflow for typical TUI)

        // Find last item that fits
        int endIdx = FindItemAtOffset(endOffset);

        // Count items from start to end (including partially visible trailing item)
        if (endIdx > start)
        {
            // find_item_at_offset returns len when end_offset is at/after the end
            // of the list. In that case, everything from start to the end fits.
            if (endIdx >= _len)
                return Math.Max(0, _len - start);
            // Check if end_idx item is visible (partially or fully)
            uint endItemStart = OffsetOfItem(endIdx);
            if (endOffset > endItemStart)
                return endIdx - start + 1;
            else
                return endIdx - start;
        }
        else
        {
            // At least show one item if viewport has space
            if (viewportHeight > 0 && start < _len)
                return 1;
            else
                return 0;
        }
    }

    /// <summary>Get total height of all items. O(log n).</summary>
    public uint TotalHeight() => _tree.Total();

    /// <summary>
    /// Resize the tracker to accommodate new_len items.
    /// New items are initialized with default height.
    /// </summary>
    public void Resize(int newLen)
    {
        if (newLen == _len)
            return;
        _tree.Resize(newLen);
        // Set default heights for new items
        if (newLen > _len)
        {
            for (int i = _len; i < newLen; i++)
                _tree.Set(i, _defaultHeight);
        }
        _len = newLen;
    }

    /// <summary>Clear all height data.</summary>
    public void Clear()
    {
        _tree = new FenwickTree(0);
        _len = 0;
    }

    /// <summary>Rebuild from a fresh set of heights.</summary>
    public void Rebuild(ushort[] heights)
    {
        uint[] heightsU32 = new uint[heights.Length];
        for (int i = 0; i < heights.Length; i++)
            heightsU32[i] = heights[i];
        _tree = FenwickTree.FromValues(heightsU32);
        _len = heights.Length;
    }
}

// ============================================================================
// Virtualized<T>
// ============================================================================

/// <summary>
/// A virtualized content container that tracks scroll state and computes visible ranges.
///
/// Design Rationale:
/// - Generic over item type for flexibility
/// - Supports both owned storage and external data sources
/// - Computes visible ranges for O(visible) rendering
/// - Optional overscan for smooth scrolling
/// - Momentum scrolling support
/// </summary>
public sealed class Virtualized<T>
{
    /// <summary>The stored items (or external storage reference).</summary>
    private VirtualizedStorage<T> _storage;
    /// <summary>Current scroll offset (in items).</summary>
    // DIVERGENCE: exposed as internal for direct field access in tests (e.g. scroll_offset == usize::MAX).
    internal int _scrollOffset;
    /// <summary>Number of visible items (cached from last render).</summary>
    // DIVERGENCE: Rust uses StdCell<usize> for interior mutability; C# uses a plain int field.
    private int _visibleCount;
    /// <summary>Overscan: extra items rendered above/below visible.</summary>
    private int _overscan;
    /// <summary>Height calculation strategy.</summary>
    private ItemHeight _itemHeight;
    /// <summary>Whether to auto-scroll on new items.</summary>
    // DIVERGENCE: internal for test access (upstream uses pub(crate) field access in tests).
    internal bool _followMode;
    /// <summary>Scroll velocity for momentum scrolling.</summary>
    private float _scrollVelocity;

    /// <summary>
    /// Create a new virtualized container with owned storage.
    /// </summary>
    /// <param name="capacity">Maximum items to retain in memory.</param>
    public static Virtualized<T> New(int capacity)
    {
        return new Virtualized<T>
        {
            _storage = new VirtualizedStorage<T>.Owned(capacity),
            _scrollOffset = 0,
            _visibleCount = 0,
            _overscan = 2,
            _itemHeight = new ItemHeight.Fixed(1),
            _followMode = false,
            _scrollVelocity = 0.0f,
        };
    }

    /// <summary>Create with external storage reference.</summary>
    public static Virtualized<T> External(int len, int cacheCapacity)
    {
        return new Virtualized<T>
        {
            _storage = new VirtualizedStorage<T>.External(len, cacheCapacity),
            _scrollOffset = 0,
            _visibleCount = 0,
            _overscan = 2,
            _itemHeight = new ItemHeight.Fixed(1),
            _followMode = false,
            _scrollVelocity = 0.0f,
        };
    }

    private Virtualized() { _storage = null!; _itemHeight = null!; }

    /// <summary>Set item height strategy.</summary>
    public Virtualized<T> WithItemHeight(ItemHeight height)
    {
        _itemHeight = height;
        return this;
    }

    /// <summary>Set fixed item height.</summary>
    public Virtualized<T> WithFixedHeight(ushort height)
    {
        _itemHeight = new ItemHeight.Fixed(height);
        return this;
    }

    /// <summary>
    /// Set variable heights with O(log n) Fenwick tree tracking.
    /// This is more efficient than Variable(HeightCache) for large lists
    /// as scroll-to-index mapping is O(log n) instead of O(visible).
    /// </summary>
    public Virtualized<T> WithVariableHeightsFenwick(ushort defaultHeight, int capacity)
    {
        _itemHeight = new ItemHeight.VariableFenwick(new VariableHeightsFenwick(defaultHeight, capacity));
        return this;
    }

    /// <summary>Set overscan amount.</summary>
    public Virtualized<T> WithOverscan(int overscan)
    {
        _overscan = overscan;
        return this;
    }

    /// <summary>Enable follow mode.</summary>
    public Virtualized<T> WithFollow(bool follow)
    {
        _followMode = follow;
        return this;
    }

    /// <summary>Get total number of items.</summary>
    public int Len() => _storage.Len();

    /// <summary>Check if empty.</summary>
    public bool IsEmpty() => Len() == 0;

    /// <summary>
    /// Get current scroll offset.
    /// The internal sentinel value (int.MaxValue, used for lazy scroll-to-bottom)
    /// is clamped to the actual item count so callers never see it.
    /// </summary>
    public int ScrollOffset() => Math.Min(_scrollOffset, Math.Max(0, Len() - 1));

    /// <summary>Get current visible count (from last render).</summary>
    public int VisibleCount() => _visibleCount;

    /// <summary>Check if follow mode is enabled.</summary>
    public bool FollowMode() => _followMode;

    /// <summary>Calculate visible range for given viewport height.</summary>
    public Range<int> VisibleRange(ushort viewportHeight)
    {
        if (IsEmpty() || viewportHeight == 0)
        {
            _visibleCount = 0;
            return new Range<int>(0, 0);
        }

        int itemsVisible;
        switch (_itemHeight)
        {
            case ItemHeight.Fixed f when f.Height > 0:
                // Use div_ceil to include partially visible items
                itemsVisible = ((int)viewportHeight + f.Height - 1) / f.Height;
                break;
            case ItemHeight.Fixed _:
                itemsVisible = viewportHeight;
                break;
            case ItemHeight.Variable v:
            {
                // Sum heights until we fill or exceed viewport
                int count = 0;
                int totalHeight = 0;
                int start = Math.Min(_scrollOffset, Math.Max(0, Len() - 1));
                while (start + count < Len())
                {
                    int next = v.Cache.Get(start + count);
                    int proposed = totalHeight + next;

                    // Always include the item
                    totalHeight = proposed;
                    count++;

                    // Stop if we've filled the viewport
                    if (totalHeight >= viewportHeight)
                        break;
                }
                itemsVisible = count;
                break;
            }
            case ItemHeight.VariableFenwick vf:
                // O(log n) using Fenwick tree
                itemsVisible = vf.Tracker.VisibleCount(_scrollOffset, viewportHeight);
                break;
            default:
                itemsVisible = viewportHeight;
                break;
        }

        int maxOffset = Math.Max(0, Len() - itemsVisible);
        int rangeStart = Math.Min(_scrollOffset, maxOffset);
        int rangeEnd = Math.Min(rangeStart + itemsVisible, Len());
        _visibleCount = itemsVisible;
        return new Range<int>(rangeStart, rangeEnd);
    }

    /// <summary>Get render range with overscan for smooth scrolling.</summary>
    public Range<int> RenderRange(ushort viewportHeight)
    {
        var visible = VisibleRange(viewportHeight);
        int start = Math.Max(0, visible.Start - _overscan);
        int end = Math.Min(visible.End + _overscan, Len());
        return new Range<int>(start, end);
    }

    /// <summary>Scroll by delta (positive = down/forward).</summary>
    public void Scroll(int delta)
    {
        if (IsEmpty())
            return;
        int maxOffset = _visibleCount > 0
            ? Math.Max(0, Len() - _visibleCount)
            : Math.Max(0, Len() - 1);
        // Clamp current offset BEFORE adding delta, so lazy MAX doesn't swallow negative deltas
        int clampedCurrent = Math.Min(_scrollOffset, maxOffset);
        long newOffsetL = (long)clampedCurrent + delta;
        int newOffset = (int)Math.Clamp(newOffsetL, 0L, (long)maxOffset);
        _scrollOffset = newOffset;

        // Disable follow mode on manual scroll
        if (delta != 0)
            _followMode = false;
    }

    /// <summary>Scroll to specific item index.</summary>
    public void ScrollTo(int idx)
    {
        _scrollOffset = Math.Min(idx, Math.Max(0, Len() - 1));
        _followMode = false;
    }

    /// <summary>Scroll to bottom.</summary>
    public void ScrollToBottom()
    {
        if (IsEmpty())
        {
            _scrollOffset = 0;
            return;
        }

        if (_visibleCount == 0)
        {
            // Viewport unknown; keep a sentinel and let VisibleRange clamp lazily.
            _scrollOffset = int.MaxValue;
        }
        else if (Len() > _visibleCount)
        {
            _scrollOffset = Len() - _visibleCount;
        }
        else
        {
            _scrollOffset = 0;
        }
    }

    /// <summary>Scroll to top.</summary>
    public void ScrollToTop()
    {
        _scrollOffset = 0;
        _followMode = false;
    }

    /// <summary>Alias for scroll_to_top (Home key).</summary>
    public void ScrollToStart() => ScrollToTop();

    /// <summary>Scroll to bottom and enable follow mode (End key).</summary>
    public void ScrollToEnd()
    {
        ScrollToBottom();
        _followMode = true;
    }

    /// <summary>Page up (scroll by visible count - 1).</summary>
    public void PageUp()
    {
        if (_visibleCount > 0)
        {
            int step = _visibleCount > 1 ? _visibleCount - 1 : 1;
            // Clamp to i32::MAX range
            int delta = step > int.MaxValue ? int.MaxValue : (int)step;
            Scroll(-delta);
        }
    }

    /// <summary>Page down (scroll by visible count - 1).</summary>
    public void PageDown()
    {
        if (_visibleCount > 0)
        {
            int step = _visibleCount > 1 ? _visibleCount - 1 : 1;
            int delta = step > int.MaxValue ? int.MaxValue : (int)step;
            Scroll(delta);
        }
    }

    /// <summary>Set follow mode.</summary>
    public void SetFollow(bool follow)
    {
        _followMode = follow;
        if (follow)
            ScrollToBottom();
    }

    /// <summary>Check if scrolled to bottom.</summary>
    public bool IsAtBottom()
    {
        if (Len() <= _visibleCount)
            return true;
        return _scrollOffset >= Math.Max(0, Len() - _visibleCount);
    }

    /// <summary>Start momentum scroll.</summary>
    public void Fling(float velocity)
    {
        _scrollVelocity = velocity;
    }

    /// <summary>Apply momentum scroll tick.</summary>
    public void Tick(TimeSpan dt)
    {
        if (Math.Abs(_scrollVelocity) > 0.1f)
        {
            int delta = (int)(_scrollVelocity * (float)dt.TotalSeconds);
            if (delta != 0)
                Scroll(delta);
            // Apply friction
            _scrollVelocity *= 0.95f;
        }
        else
        {
            _scrollVelocity = 0.0f;
        }
    }

    /// <summary>Update visible count (called during render).</summary>
    public void SetVisibleCount(int count)
    {
        _visibleCount = count;
    }

    /// <summary>Get reference to the item height strategy.</summary>
    public ItemHeight ItemHeight_() => _itemHeight;

    /// <summary>
    /// Get mutable reference to the item height strategy.
    /// Useful for updating variable height trackers (e.g. resizing Fenwick tree)
    /// when items are added.
    /// </summary>
    public ItemHeight ItemHeightMut() => _itemHeight;

    // ── Push / Access ──────────────────────────────────────────────────────

    /// <summary>Push an item (owned storage only).</summary>
    public void Push(T item)
    {
        if (_storage is VirtualizedStorage<T>.Owned owned)
        {
            owned.Items.Enqueue(item);
            if (_followMode)
                ScrollToBottom();
        }
    }

    /// <summary>Get item by index (owned storage only).</summary>
    public T? Get(int idx)
    {
        if (_storage is VirtualizedStorage<T>.Owned owned)
        {
            // DIVERGENCE: Queue<T> doesn't support O(1) index access.
            // We iterate to find the element. For large lists, callers should
            // prefer range-based access via Iter().
            int i = 0;
            foreach (var item in owned.Items)
            {
                if (i == idx) return item;
                i++;
            }
        }
        return default;
    }

    /// <summary>Get mutable item by index (owned storage only).</summary>
    // DIVERGENCE: Queue<T> does not support mutable indexed access.
    // This method returns null/default for all inputs.
    // In practice, Virtualized<T> mutable item access was only used in tests;
    // the test for this is preserved using the nullable return.
    public T? GetMut(int idx)
    {
        // DIVERGENCE: Queue<T> does not expose mutable references by index.
        // Returns default (null for reference types) for all inputs.
        return default;
    }

    /// <summary>Clear all items (owned storage only).</summary>
    public void Clear()
    {
        if (_storage is VirtualizedStorage<T>.Owned owned)
            owned.Items.Clear();
        _scrollOffset = 0;
    }

    /// <summary>
    /// Trim items from the front to keep at most max items (owned storage only).
    /// Returns the number of items removed.
    /// </summary>
    public int TrimFront(int max)
    {
        if (_storage is VirtualizedStorage<T>.Owned owned && owned.Items.Count > max)
        {
            int toRemove = owned.Items.Count - max;
            for (int i = 0; i < toRemove; i++)
                owned.Items.Dequeue();
            // Adjust scroll_offset if it was pointing beyond the new start
            _scrollOffset = Math.Max(0, _scrollOffset - toRemove);
            return toRemove;
        }
        return 0;
    }

    /// <summary>
    /// Iterate over items (owned storage only).
    /// Returns empty iterator for external storage.
    /// </summary>
    public IEnumerable<T> Iter()
    {
        if (_storage is VirtualizedStorage<T>.Owned owned)
            return owned.Items;
        return System.Linq.Enumerable.Empty<T>();
    }

    /// <summary>Update external storage length.</summary>
    public void SetExternalLen(int len)
    {
        if (_storage is VirtualizedStorage<T>.External ext)
        {
            ext.Len_ = len;
            if (_followMode)
                ScrollToBottom();
        }
    }
}

// ============================================================================
// Range<T> helper (mirrors Rust's std::ops::Range)
// ============================================================================

/// <summary>A half-open range [start, end).</summary>
// DIVERGENCE: Rust std::ops::Range<usize> → C# value type with Start/End.
// Used only within this file; callers may also use System.Range.
// The record struct synthesises Equals/GetHashCode/== automatically.
public readonly record struct Range<T>(T Start, T End) where T : IComparable<T>
{
    public bool IsEmpty => Start.CompareTo(End) >= 0;
}

// ============================================================================
// IRenderItem — items that can render themselves
// ============================================================================

/// <summary>
/// Trait for items that can render themselves.
/// Implement this interface for item types that should render in a VirtualizedList.
/// </summary>
public interface IRenderItem
{
    /// <summary>
    /// Render the item into the frame at the given area.
    /// </summary>
    /// <param name="area">The area to render into.</param>
    /// <param name="frame">The frame to render into.</param>
    /// <param name="selected">Whether the item is selected.</param>
    /// <param name="skipRows">
    /// Number of rows to skip from the top of the item content.
    /// This is non-zero when the item partially overlaps the top of the viewport.
    /// </param>
    void Render(Rect area, Frame frame, bool selected, ushort skipRows);

    /// <summary>Height of this item in terminal rows.</summary>
    ushort Height() => 1;
}

// ============================================================================
// Simple IRenderItem implementations for common types
// ============================================================================

/// <summary>IRenderItem implementation wrapping a string.</summary>
public sealed class StringRenderItem : IRenderItem
{
    private readonly string _value;
    public StringRenderItem(string value) { _value = value; }

    public void Render(Rect area, Frame frame, bool selected, ushort skipRows)
    {
        if (area.IsEmpty)
            return;
        // String is assumed to be single-line in this implementation, so skipRows > 0
        // implies the whole item is skipped.
        if (skipRows > 0)
            return;
        ushort x = area.X;
        foreach (char ch in _value)
        {
            if (x >= area.Right)
                break;
            frame.Buffer.Set(x, area.Y, Cell.FromChar(ch));
            x++;
        }
    }

    public ushort Height() => 1;
}

/// <summary>IRenderItem implementation wrapping a read-only string reference.</summary>
// DIVERGENCE: Rust impl RenderItem for &str — C# has no reference-type distinction like &str vs String.
// We reuse StringRenderItem for the same purpose; a separate StrRefRenderItem alias is provided for naming traceability.
public sealed class StrRefRenderItem : IRenderItem
{
    private readonly string _value;
    public StrRefRenderItem(string value) { _value = value; }

    public void Render(Rect area, Frame frame, bool selected, ushort skipRows)
    {
        if (area.IsEmpty)
            return;
        if (skipRows > 0)
            return;
        ushort x = area.X;
        foreach (char ch in _value)
        {
            if (x >= area.Right)
                break;
            frame.Buffer.Set(x, area.Y, Cell.FromChar(ch));
            x++;
        }
    }

    public ushort Height() => 1;
}

// ============================================================================
// VirtualizedListState
// ============================================================================

/// <summary>State for the VirtualizedList widget.</summary>
public sealed class VirtualizedListState
{
    /// <summary>Currently selected index.</summary>
    public int? Selected { get; set; }
    /// <summary>Scroll offset.</summary>
    // DIVERGENCE: exposed as internal for direct field access in tests.
    internal int _scrollOffset;
    /// <summary>Visible count (from last render).</summary>
    // DIVERGENCE: exposed as internal for direct field access in tests.
    internal int _visibleCount;
    /// <summary>Overscan amount.</summary>
    // DIVERGENCE: internal for cross-class access in VirtualizedList<T>.Render.
    internal int _overscan;
    /// <summary>Whether follow mode is enabled.</summary>
    // DIVERGENCE: internal for test access.
    internal bool _followMode;
    /// <summary>Scroll velocity for momentum.</summary>
    // DIVERGENCE: internal for test access.
    internal float _scrollVelocity;
    /// <summary>Drag anchor for scrollbar thumb (offset from thumb top).</summary>
    internal int? _scrollbarDragAnchor;
    /// <summary>Optional persistence ID for state saving/restoration.</summary>
    private string? _persistenceId;

    public static VirtualizedListState Default() => new VirtualizedListState();

    /// <summary>Create a new state.</summary>
    public VirtualizedListState()
    {
        Selected = null;
        _scrollOffset = 0;
        _visibleCount = 0;
        _overscan = 2;
        _followMode = false;
        _scrollVelocity = 0.0f;
        _scrollbarDragAnchor = null;
        _persistenceId = null;
    }

    /// <summary>Create with overscan.</summary>
    public VirtualizedListState WithOverscan(int overscan)
    {
        _overscan = overscan;
        return this;
    }

    /// <summary>Create with follow mode enabled.</summary>
    public VirtualizedListState WithFollow(bool follow)
    {
        _followMode = follow;
        return this;
    }

    /// <summary>Create with a persistence ID for state saving.</summary>
    public VirtualizedListState WithPersistenceId(string id)
    {
        _persistenceId = id;
        return this;
    }

    /// <summary>Get the persistence ID, if set.</summary>
    public string? PersistenceId() => _persistenceId;

    /// <summary>
    /// Get raw scroll offset.
    ///
    /// Warning: After ScrollToBottom, this may return int.MaxValue
    /// (a lazy sentinel that gets clamped during rendering). Use
    /// ScrollOffsetClamped with a known total_items to get a safe value.
    /// </summary>
    public int ScrollOffset() => _scrollOffset;

    /// <summary>
    /// Get scroll offset clamped against a known total item count.
    /// Prefer this over ScrollOffset() when the total is available,
    /// because scroll_to_bottom may store int.MaxValue internally.
    /// </summary>
    public int ScrollOffsetClamped(int totalItems)
    {
        if (totalItems == 0)
            return 0;
        return Math.Min(_scrollOffset, Math.Max(0, totalItems - 1));
    }

    /// <summary>Get visible item count (from last render).</summary>
    public int VisibleCount() => _visibleCount;

    /// <summary>Scroll by delta (positive = down).</summary>
    public void Scroll(int delta, int totalItems)
    {
        if (totalItems == 0)
            return;
        int maxOffset = _visibleCount > 0
            ? Math.Max(0, totalItems - _visibleCount)
            : Math.Max(0, totalItems - 1);
        // Clamp current offset BEFORE adding delta, so lazy MAX doesn't swallow negative deltas
        int clampedCurrent = Math.Min(_scrollOffset, maxOffset);
        long newOffsetL = (long)clampedCurrent + delta;
        int newOffset = (int)Math.Clamp(newOffsetL, 0L, (long)maxOffset);
        _scrollOffset = newOffset;

        if (delta != 0)
            _followMode = false;
    }

    /// <summary>Scroll to specific index.</summary>
    public void ScrollTo(int idx, int totalItems)
    {
        _scrollOffset = Math.Min(idx, Math.Max(0, totalItems - 1));
        _followMode = false;
    }

    /// <summary>Scroll to top.</summary>
    public void ScrollToTop()
    {
        _scrollOffset = 0;
        _followMode = false;
    }

    /// <summary>Scroll to bottom.</summary>
    public void ScrollToBottom(int totalItems)
    {
        if (totalItems == 0)
            _scrollOffset = 0;
        else
            // Set to MAX; Render() will clamp to (total - viewport) once viewport height is known.
            _scrollOffset = int.MaxValue;
    }

    /// <summary>Page up (scroll by visible count - 1).</summary>
    public void PageUp(int totalItems)
    {
        if (_visibleCount > 0)
        {
            int step = _visibleCount > 1 ? _visibleCount - 1 : 1;
            int delta = (int)Math.Min(step, (long)int.MaxValue);
            Scroll(-delta, totalItems);
        }
    }

    /// <summary>Page down (scroll by visible count - 1).</summary>
    public void PageDown(int totalItems)
    {
        if (_visibleCount > 0)
        {
            int step = _visibleCount > 1 ? _visibleCount - 1 : 1;
            int delta = (int)Math.Min(step, (long)int.MaxValue);
            Scroll(delta, totalItems);
        }
    }

    /// <summary>Select an item.</summary>
    public void Select(int? index) { Selected = index; }

    /// <summary>Select previous item.</summary>
    public void SelectPrevious(int totalItems)
    {
        if (totalItems == 0)
        {
            Selected = null;
            return;
        }
        Selected = Selected switch
        {
            int i when i > 0 => i - 1,
            int _ => 0,
            null => 0,
        };
    }

    /// <summary>Select next item.</summary>
    public void SelectNext(int totalItems)
    {
        if (totalItems == 0)
        {
            Selected = null;
            return;
        }
        Selected = Selected switch
        {
            int i when i < totalItems - 1 => i + 1,
            int i => i,
            null => 0,
        };
    }

    /// <summary>Check if at bottom.</summary>
    public bool IsAtBottom(int totalItems)
    {
        if (totalItems <= _visibleCount)
            return true;
        return _scrollOffset >= totalItems - _visibleCount;
    }

    /// <summary>Enable/disable follow mode.</summary>
    public void SetFollow(bool follow, int totalItems)
    {
        _followMode = follow;
        if (follow)
            ScrollToBottom(totalItems);
    }

    /// <summary>Check if follow mode is enabled.</summary>
    public bool FollowMode() => _followMode;

    /// <summary>Start momentum scroll.</summary>
    public void Fling(float velocity) { _scrollVelocity = velocity; }

    /// <summary>Apply momentum scrolling tick.</summary>
    public void Tick(TimeSpan dt, int totalItems)
    {
        if (Math.Abs(_scrollVelocity) > 0.1f)
        {
            int delta = (int)(_scrollVelocity * (float)dt.TotalSeconds);
            if (delta != 0)
                Scroll(delta, totalItems);
            _scrollVelocity *= 0.95f;
        }
        else
        {
            _scrollVelocity = 0.0f;
        }
    }

    /// <summary>
    /// Handle mouse events, including scrollbar interaction.
    ///
    /// The caller must provide the hit test result and the expected hit ID for the scrollbar.
    /// </summary>
    public MouseResult HandleMouse(
        MouseEventProxy @event,
        (HitId, HitRegionKind, HitData)? hit,
        HitId scrollbarHitId,
        int totalItems,
        ushort viewportHeight,
        ushort fixedItemHeight)
    {
        // Construct temporary scrollbar state
        int itemsPerViewport = viewportHeight / Math.Max(1, (int)fixedItemHeight);
        if (viewportHeight % Math.Max(1, (int)fixedItemHeight) != 0)
            itemsPerViewport++; // div_ceil
        var scrollbarState = new ScrollbarStateEx(totalItems, _scrollOffset, itemsPerViewport);

        // Restore drag anchor
        scrollbarState.DragAnchor = _scrollbarDragAnchor;

        var result = scrollbarState.HandleMouse(@event, hit, scrollbarHitId);

        // Sync back
        _scrollOffset = scrollbarState.Position;
        _scrollbarDragAnchor = scrollbarState.DragAnchor;

        if (result == MouseResult.Scrolled)
            _followMode = false;

        return result;
    }
}

// ============================================================================
// VirtualizedListPersistState
// ============================================================================

/// <summary>
/// Persistable state for a VirtualizedListState.
/// Contains the user-facing scroll state that should survive sessions.
/// Transient values like scroll_velocity and visible_count are not persisted.
/// </summary>
public sealed class VirtualizedListPersistState : IEquatable<VirtualizedListPersistState>
{
    /// <summary>Selected item index.</summary>
    public int? Selected { get; set; }
    /// <summary>Scroll offset (first visible item).</summary>
    public int ScrollOffset { get; set; }
    /// <summary>Whether follow mode is enabled.</summary>
    public bool FollowMode { get; set; }

    public VirtualizedListPersistState() { }

    public VirtualizedListPersistState Clone()
        => new VirtualizedListPersistState
        {
            Selected = Selected,
            ScrollOffset = ScrollOffset,
            FollowMode = FollowMode,
        };

    public bool Equals(VirtualizedListPersistState? other)
        => other is not null
            && Selected == other.Selected
            && ScrollOffset == other.ScrollOffset
            && FollowMode == other.FollowMode;

    public override bool Equals(object? obj)
        => obj is VirtualizedListPersistState ps && Equals(ps);

    public override int GetHashCode()
        => HashCode.Combine(Selected, ScrollOffset, FollowMode);

    public static bool operator ==(VirtualizedListPersistState? a, VirtualizedListPersistState? b)
        => a is null ? b is null : a.Equals(b);

    public static bool operator !=(VirtualizedListPersistState? a, VirtualizedListPersistState? b)
        => !(a == b);
}

// ============================================================================
// Stateful impl for VirtualizedListState
// ============================================================================

/// <summary>Stateful persistence implementation for VirtualizedListState.</summary>
public sealed class VirtualizedListStateful : IStateful<VirtualizedListPersistState>
{
    private readonly VirtualizedListState _state;

    public VirtualizedListStateful(VirtualizedListState state) { _state = state; }

    public StateKey StateKey => new StateKey(
        "VirtualizedList",
        _state.PersistenceId() ?? "default");

    public VirtualizedListPersistState SaveState() => new VirtualizedListPersistState
    {
        Selected = _state.Selected,
        ScrollOffset = _state._scrollOffset,
        FollowMode = _state._followMode,
    };

    public void RestoreState(VirtualizedListPersistState state)
    {
        _state.Selected = state.Selected;
        _state._scrollOffset = state.ScrollOffset;
        _state._followMode = state.FollowMode;
        // Reset transient values
        _state._scrollVelocity = 0.0f;
        _state._scrollbarDragAnchor = null;
    }
}

// ============================================================================
// VirtualizedList widget
// ============================================================================

/// <summary>
/// A virtualized list widget that renders only visible items.
///
/// This widget efficiently renders large lists by only drawing items
/// that are currently visible in the viewport, with optional overscan
/// for smooth scrolling.
///
/// Limitations:
/// Currently, VirtualizedList only supports fixed height items.
/// For variable height virtualization, use the Virtualized primitive directly.
/// </summary>
public sealed class VirtualizedList<T> : IStatefulWidget<VirtualizedListState>
    where T : IRenderItem
{
    /// <summary>Items to render.</summary>
    private readonly T[] _items;
    /// <summary>Base style.</summary>
    private WidgetStyle _style;
    /// <summary>Style for selected item.</summary>
    private WidgetStyle _highlightStyle;
    /// <summary>Whether to show scrollbar.</summary>
    // DIVERGENCE: exposed as internal for tests that inspect _showScrollbar.
    internal bool _showScrollbar;
    /// <summary>Fixed item height.</summary>
    // DIVERGENCE: exposed as internal for tests that inspect fixed_height.
    internal ushort _fixedHeight;
    /// <summary>Optional hit ID for scrollbar interaction.</summary>
    private HitId? _hitId;

    /// <summary>Create a new virtualized list.</summary>
    public VirtualizedList(T[] items)
    {
        _items = items;
        _style = default;
        _highlightStyle = default;
        _showScrollbar = true;
        _fixedHeight = 1;
        _hitId = null;
    }

    /// <summary>Set base style.</summary>
    public VirtualizedList<T> Style(WidgetStyle style) { _style = style; return this; }

    /// <summary>Set highlight style for selected item.</summary>
    public VirtualizedList<T> HighlightStyle(WidgetStyle style) { _highlightStyle = style; return this; }

    /// <summary>Enable/disable scrollbar.</summary>
    public VirtualizedList<T> ShowScrollbar(bool show) { _showScrollbar = show; return this; }

    /// <summary>Set fixed item height.</summary>
    public VirtualizedList<T> FixedHeight(ushort height) { _fixedHeight = height; return this; }

    /// <summary>Set hit ID for scrollbar interaction.</summary>
    public VirtualizedList<T> HitId(HitId id) { _hitId = id; return this; }

    public void Render(Rect area, Frame frame, VirtualizedListState state)
    {
        if (area.IsEmpty)
            return;

        // Clear the full owned viewport so empty renders and shorter rows do
        // not leak prior buffer content.
        WidgetDrawing.ClearTextArea(frame, area, _style);

        int totalItems = _items.Length;
        if (totalItems == 0)
            return;

        // Reserve space for scrollbar if needed
        ushort fixedH = Math.Max((ushort)1, _fixedHeight);
        // Use div_ceil to include partially visible items and avoid 0 count for large items
        int itemsPerViewport = ((int)area.Height + fixedH - 1) / fixedH;
        int fullyVisibleItems = (int)area.Height / fixedH;
        bool needsScrollbar = _showScrollbar && totalItems > fullyVisibleItems;
        ushort contentWidth = needsScrollbar
            ? (ushort)Math.Max(0, area.Width - 1)
            : area.Width;

        // Ensure selection is within bounds
        if (state.Selected.HasValue && state.Selected.Value >= totalItems)
        {
            state.Selected = totalItems > 0 ? totalItems - 1 : (int?)null;
        }

        // Ensure visible range includes selected item (it must be fully visible if possible)
        if (state.Selected.HasValue)
        {
            int selected = state.Selected.Value;
            int visCount = Math.Max(1, fullyVisibleItems);
            if (selected >= state._scrollOffset + visCount)
                state._scrollOffset = Math.Max(0, selected - (visCount - 1));
            else if (selected < state._scrollOffset)
                state._scrollOffset = selected;
        }

        // Clamp scroll offset
        int maxOffset = fullyVisibleItems > 0
            ? Math.Max(0, totalItems - fullyVisibleItems)
            : Math.Max(0, totalItems - 1);
        state._scrollOffset = Math.Min(state._scrollOffset, maxOffset);

        // Update visible count — use fully_visible_items (not items_per_viewport
        // which includes partial items via div_ceil) so scroll calculations
        // (page size, max_offset) use a consistent metric.
        state._visibleCount = Math.Min(Math.Max(1, fullyVisibleItems), totalItems);

        // Calculate render range with overscan
        int renderStart = Math.Max(0, state._scrollOffset - state._overscan);
        int renderEnd = Math.Min(totalItems,
            state._scrollOffset + itemsPerViewport + state._overscan);

        // Render visible items
        for (int idx = renderStart; idx < renderEnd; idx++)
        {
            // Calculate Y position relative to viewport
            // Use relative arithmetic to prevent overflow when indices exceed i32::MAX.
            int relativeIdx;
            if (idx >= state._scrollOffset)
                relativeIdx = idx - state._scrollOffset;
            else
                relativeIdx = -(state._scrollOffset - idx);

            int heightI32 = (int)_fixedHeight;
            int yOffset;
            try { yOffset = checked(relativeIdx * heightI32); }
            catch (OverflowException) { yOffset = relativeIdx >= 0 ? int.MaxValue : int.MinValue; }

            // Skip items above viewport
            if (yOffset + heightI32 <= 0)
                continue;

            // Stop if below viewport
            if (yOffset >= (int)area.Height)
                break;

            // Check if item starts off-screen top.
            ushort skipRows = yOffset < 0 ? (ushort)(-(int)yOffset) : (ushort)0;

            // Calculate actual render area
            int yAbs = Math.Clamp((int)area.Y + yOffset, (int)area.Y, (int)ushort.MaxValue);
            ushort y = (ushort)yAbs;

            if (y >= area.Bottom)
                break;

            ushort visibleHeight = (ushort)Math.Min(
                (int)_fixedHeight - skipRows,
                Math.Max(0, (int)area.Bottom - y));

            if (visibleHeight == 0)
                continue;

            var rowArea = new Rect(area.X, y, contentWidth, visibleHeight);

            bool isSelected = state.Selected == idx;

            WidgetStyle rowStyle = isSelected
                ? MergeStyles(_highlightStyle, _style)
                : _style;
            WidgetDrawing.ClearTextArea(frame, rowArea, rowStyle);

            // Render the item
            _items[idx].Render(rowArea, frame, isSelected, skipRows);
        }

        // Render scrollbar
        if (needsScrollbar)
        {
            var scrollbarArea = new Rect(
                (ushort)Math.Max(0, area.Right - 1),
                area.Y,
                1,
                area.Height);

            var scrollbarState = new ScrollbarState(totalItems, state._scrollOffset, itemsPerViewport);

            // Sync drag anchor from persistent state to transient scrollbar state
            // DIVERGENCE: The C# ScrollbarState stub does not carry drag_anchor.
            // The scrollbar render here is a best-effort port. See DIVERGENCE in HandleMouse.

            var scrollbar = new Scrollbar(ScrollbarOrientation.VerticalRight);
            if (_hitId.HasValue)
                scrollbar = scrollbar.HitId_(_hitId.Value);
            scrollbar.Render(scrollbarArea, frame, scrollbarState);
        }
    }

    private static WidgetStyle MergeStyles(WidgetStyle over, WidgetStyle under)
    {
        // Rust: highlight_style.merge(&self.style)
        // Merge = over takes precedence for non-None fields
        return new WidgetStyle(
            over.Fg ?? under.Fg,
            over.Bg ?? under.Bg,
            over.Attrs ?? under.Attrs);
    }
}

// ============================================================================
// ScrollbarStateEx — extended scrollbar state with drag_anchor and HandleMouse
// ============================================================================

/// <summary>
/// Extended scrollbar state that includes drag anchor and mouse handling.
/// This is used internally by VirtualizedListState.HandleMouse.
/// </summary>
// DIVERGENCE: The main ScrollbarState in Scrollbar.cs is a simplified stub.
// The full drag/mouse logic from scrollbar.rs is ported here to support
// VirtualizedListState.HandleMouse, which needs the complete implementation.
public sealed class ScrollbarStateEx
{
    public const ulong ScrollbarPartBegin = 0;
    public const ulong ScrollbarPartEnd = 2;
    public const ulong ScrollbarPartThumb = 1;
    public const ulong ScrollbarPartTrack = 3;

    /// <summary>Total number of scrollable content units.</summary>
    public int ContentLength { get; set; }
    /// <summary>Current scroll position within the content.</summary>
    public int Position { get; set; }
    /// <summary>Number of content units visible in the viewport.</summary>
    public int ViewportLength { get; set; }
    /// <summary>Drag anchor point (offset from thumb top) to prevent jumping.</summary>
    public int? DragAnchor { get; set; }

    public ScrollbarStateEx(int contentLength, int position, int viewportLength)
    {
        ContentLength = contentLength;
        Position = position;
        ViewportLength = viewportLength;
        DragAnchor = null;
    }

    private (int thumbOffset, int thumbSize) CalcThumbGeometry(int trackLen)
    {
        if (trackLen == 0)
            return (0, 0);
        if (ContentLength == 0)
            return (0, trackLen);

        double viewportRatio = (double)ViewportLength / ContentLength;
        int thumbSize = (int)Math.Max(1.0, Math.Round(trackLen * viewportRatio));
        thumbSize = Math.Min(thumbSize, trackLen);

        int maxPos = Math.Max(0, ContentLength - ViewportLength);
        double posRatio = maxPos == 0 ? 0.0 : (double)Math.Min(Position, maxPos) / maxPos;
        int availableTrack = Math.Max(0, trackLen - thumbSize);
        int thumbOffset = (int)Math.Round(availableTrack * posRatio);
        return (thumbOffset, thumbSize);
    }

    /// <summary>Handle a mouse event for this scrollbar.</summary>
    public MouseResult HandleMouse(
        MouseEventProxy @event,
        (HitId, HitRegionKind, HitData)? hit,
        HitId expectedId)
    {
        switch (@event.Kind)
        {
            case MouseEventKindEx.DownLeft:
            {
                if (hit is { } h && h.Item1 == expectedId && h.Item2 == HitRegionKind.Scrollbar)
                {
                    ulong data = h.Item3.Value;
                    ulong part = data >> 56;
                    if (part == ScrollbarPartBegin)
                    {
                        ScrollUp(1);
                        return MouseResult.Scrolled;
                    }
                    if (part == ScrollbarPartEnd)
                    {
                        ScrollDown(1);
                        return MouseResult.Scrolled;
                    }
                    if (part == ScrollbarPartThumb)
                    {
                        int trackLen = (int)((data >> 28) & 0x0FFF_FFFF);
                        int trackPos = (int)(data & 0x0FFF_FFFF);
                        var (thumbOffset, _) = CalcThumbGeometry(trackLen);
                        DragAnchor = Math.Max(0, trackPos - thumbOffset);
                        return MouseResult.Scrolled;
                    }
                    if (part == ScrollbarPartTrack)
                    {
                        int trackLen = (int)((data >> 28) & 0x0FFF_FFFF);
                        int trackPos = (int)(data & 0x0FFF_FFFF);
                        if (trackLen == 0)
                            return MouseResult.Ignored;

                        var (_, thumbSize) = CalcThumbGeometry(trackLen);
                        int available = Math.Max(0, trackLen - thumbSize);
                        int denom = Math.Max(1, available);
                        int targetThumbTop = Math.Max(0, trackPos - thumbSize / 2);
                        int clampedTop = Math.Min(targetThumbTop, denom);
                        int maxPos = Math.Max(0, ContentLength - ViewportLength);
                        Position = maxPos == 0
                            ? 0
                            : (int)((long)clampedTop * maxPos / denom);

                        var (thumbOffset2, _) = CalcThumbGeometry(trackLen);
                        DragAnchor = Math.Max(0, trackPos - thumbOffset2);
                        return MouseResult.Scrolled;
                    }
                }
                return MouseResult.Ignored;
            }
            case MouseEventKindEx.DragLeft:
            {
                (int trackLen, int trackPos)? hitData = null;
                if (hit is { } h && h.Item1 == expectedId && h.Item2 == HitRegionKind.Scrollbar)
                {
                    ulong data = h.Item3.Value;
                    ulong part = data >> 56;
                    if (part == ScrollbarPartTrack || part == ScrollbarPartThumb)
                    {
                        int len = (int)((data >> 28) & 0x0FFF_FFFF);
                        int pos = (int)(data & 0x0FFF_FFFF);
                        hitData = (len, pos);
                    }
                }

                if (hitData is null)
                    return MouseResult.Ignored;

                var (tLen, tPos) = hitData.Value;
                if (tLen == 0)
                    return MouseResult.Ignored;

                var (_, thumbSz) = CalcThumbGeometry(tLen);
                int avail = Math.Max(0, tLen - thumbSz);
                int denom2 = Math.Max(1, avail);
                int anchor = DragAnchor ?? thumbSz / 2;
                int targetTop = Math.Max(0, tPos - anchor);
                int clampedTop2 = Math.Min(targetTop, denom2);
                int maxPos2 = Math.Max(0, ContentLength - ViewportLength);
                Position = maxPos2 == 0
                    ? 0
                    : (int)((long)clampedTop2 * maxPos2 / denom2);
                return MouseResult.Scrolled;
            }
            case MouseEventKindEx.UpLeft:
            {
                bool wasDragging = DragAnchor.HasValue;
                DragAnchor = null;
                return wasDragging ? MouseResult.Scrolled : MouseResult.Ignored;
            }
            case MouseEventKindEx.ScrollUp:
                ScrollUp(3);
                return MouseResult.Scrolled;
            case MouseEventKindEx.ScrollDown:
                ScrollDown(3);
                return MouseResult.Scrolled;
            default:
                return MouseResult.Ignored;
        }
    }

    public void ScrollUp(int lines)
    {
        Position = Math.Max(0, Position - lines);
    }

    public void ScrollDown(int lines)
    {
        int maxPos = Math.Max(0, ContentLength - ViewportLength);
        Position = Math.Min(maxPos, Position + lines);
    }
}

/// <summary>Proxy for mouse events used in HandleMouse to avoid core dependency churn.</summary>
// DIVERGENCE: Rust uses ftui_core::event::MouseEvent. C# uses a minimal proxy record
// so tests can construct events without depending on the full Core event model.
public sealed record MouseEventProxy(MouseEventKindEx Kind, ushort X, ushort Y)
{
    public static MouseEventProxy New(MouseEventKindEx kind, ushort x, ushort y)
        => new MouseEventProxy(kind, x, y);
}

/// <summary>Mouse event kind for scrollbar interaction.</summary>
// DIVERGENCE: Rust MouseEventKind is a rich enum; we port only the variants used by scrollbar/virtualized.
public enum MouseEventKindEx
{
    DownLeft,
    DragLeft,
    UpLeft,
    ScrollUp,
    ScrollDown,
    Other,
}

// ============================================================================
// Scrollbar extension for HitId
// ============================================================================

// DIVERGENCE: The Scrollbar class in Scrollbar.cs is a simplified stub without hit_id.
// We add an extension method to avoid modifying Scrollbar.cs and breaking the 1-1 mapping.
// In the full upstream port, Scrollbar would carry hit_id natively.
public static class ScrollbarExtensions
{
    /// <summary>Set hit ID for scrollbar interaction (extension to match upstream API).</summary>
    public static Scrollbar HitId_(this Scrollbar scrollbar, HitId id)
    {
        // DIVERGENCE: Scrollbar.cs does not store hit_id in the current stub.
        // This method is a no-op to satisfy the API contract. The hit_id would
        // be used during render to register the scrollbar area in the HitGrid.
        return scrollbar;
    }
}
