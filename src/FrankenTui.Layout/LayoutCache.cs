// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-layout/src/cache.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Rust usize capacities/counts map to non-negative Int32 values.
// Owned Rust Rect/size vectors map to defensive managed arrays. The pre-existing
// LayoutTrace cache API remains available as a separate compatibility store; Count,
// TryGet, Set, the legacy LayoutCacheKey constructor/properties/Deconstruct, and its
// string form retain their prior behavior. Because that surface already owns a
// LayoutCacheKey.Direction property of type LayoutDirection, the canonical upstream
// Direction field is exposed as AxisDirection. S3FifoLayoutCache uses an internal,
// algorithm-faithful specialization of upstream ftui-core/src/s3_fifo.rs rather than
// widening this layout slice into a new public generic core-cache API.
// Rust Debug derives map to concise deterministic ToString summaries; the key keeps
// its legacy managed string form because compatibility takes precedence there.

using System.Numerics;
using System.Runtime.InteropServices;
using FrankenTui.Core;

namespace FrankenTui.Layout;

/// <summary>Statistics about layout-cache performance.</summary>
public record struct LayoutCacheStats
{
    /// <summary>Current number of memoized layout entries.</summary>
    public int Entries { get; set; }

    /// <summary>Cache hits since creation or the last statistics reset.</summary>
    public ulong Hits { get; set; }

    /// <summary>Cache misses since creation or the last statistics reset.</summary>
    public ulong Misses { get; set; }

    /// <summary>Hits divided by all lookups, or zero when no lookups occurred.</summary>
    public double HitRate { get; set; }
}

/// <summary>
/// Key for a layout computation. The canonical fields reproduce upstream cache-key
/// identity while the legacy fields preserve the earlier managed trace-cache contract.
/// </summary>
public readonly record struct LayoutCacheKey
{
    /// <summary>Create a key for the earlier managed LayoutTrace cache.</summary>
    public LayoutCacheKey(Rect bounds, LayoutDirection direction, string constraintFingerprint)
    {
        ArgumentNullException.ThrowIfNull(constraintFingerprint);
        Bounds = bounds;
        Direction = direction;
        ConstraintFingerprint = constraintFingerprint;
        AreaX = bounds.X;
        AreaY = bounds.Y;
        AreaWidth = bounds.Width;
        AreaHeight = bounds.Height;
        AxisDirection = direction == LayoutDirection.Horizontal
            ? FrankenTui.Layout.Direction.Horizontal
            : FrankenTui.Layout.Direction.Vertical;
    }

    /// <summary>Create a key from the complete canonical upstream field set.</summary>
    public LayoutCacheKey(
        ushort areaX,
        ushort areaY,
        ushort areaWidth,
        ushort areaHeight,
        ulong constraintsHash,
        ulong constraintsHashFx,
        ushort constraintsLen,
        Direction axisDirection,
        ulong? intrinsicsHash,
        ulong? intrinsicsHashFx)
    {
        AreaX = areaX;
        AreaY = areaY;
        AreaWidth = areaWidth;
        AreaHeight = areaHeight;
        ConstraintsHash = constraintsHash;
        ConstraintsHashFx = constraintsHashFx;
        ConstraintsLen = constraintsLen;
        AxisDirection = axisDirection;
        IntrinsicsHash = intrinsicsHash;
        IntrinsicsHashFx = intrinsicsHashFx;

        Bounds = new Rect(areaX, areaY, areaWidth, areaHeight);
        Direction = axisDirection == FrankenTui.Layout.Direction.Horizontal
            ? LayoutDirection.Horizontal
            : LayoutDirection.Vertical;
        ConstraintFingerprint = CanonicalFingerprint(
            constraintsHash,
            constraintsHashFx,
            constraintsLen,
            intrinsicsHash,
            intrinsicsHashFx);
    }

    /// <summary>Area x-coordinate.</summary>
    public ushort AreaX { get; init; }

    /// <summary>Area y-coordinate.</summary>
    public ushort AreaY { get; init; }

    /// <summary>Area width.</summary>
    public ushort AreaWidth { get; init; }

    /// <summary>Area height.</summary>
    public ushort AreaHeight { get; init; }

    /// <summary>Rust DefaultHasher/SipHash-1-3 constraint fingerprint.</summary>
    public ulong ConstraintsHash { get; init; }

    /// <summary>rustc_hash 2.1 FxHasher constraint fingerprint.</summary>
    public ulong ConstraintsHashFx { get; init; }

    /// <summary>Constraint count, using upstream's wrapping u16 representation.</summary>
    public ushort ConstraintsLen { get; init; }

    /// <summary>Canonical layout direction; renamed because the legacy API owns Direction.</summary>
    public FrankenTui.Layout.Direction AxisDirection { get; init; }

    /// <summary>Optional Rust DefaultHasher intrinsic-size fingerprint.</summary>
    public ulong? IntrinsicsHash { get; init; }

    /// <summary>Optional rustc_hash 2.1 intrinsic-size fingerprint.</summary>
    public ulong? IntrinsicsHashFx { get; init; }

    /// <summary>Legacy managed bounds property.</summary>
    public Rect Bounds { get; init; }

    /// <summary>Legacy managed direction property.</summary>
    public LayoutDirection Direction { get; init; }

    /// <summary>Legacy managed constraint fingerprint property.</summary>
    public string ConstraintFingerprint { get; init; } = string.Empty;

    /// <summary>Create a canonical upstream cache key.</summary>
    public static LayoutCacheKey New(
        Rect area,
        IReadOnlyList<Constraint> constraints,
        Direction direction,
        IReadOnlyList<LayoutSizeHint>? intrinsics = null)
    {
        ArgumentNullException.ThrowIfNull(constraints);
        (ulong primary, ulong secondary) = RustCacheHash.HashConstraints(constraints);
        (ulong Primary, ulong Secondary)? intrinsicHashes = intrinsics is null
            ? null
            : RustCacheHash.HashIntrinsics(intrinsics);

        return new LayoutCacheKey(
            area.X,
            area.Y,
            area.Width,
            area.Height,
            primary,
            secondary,
            unchecked((ushort)constraints.Count),
            direction,
            intrinsicHashes?.Primary,
            intrinsicHashes?.Secondary);
    }

    /// <summary>Reconstruct the canonical area rectangle.</summary>
    public Rect Area() => new(AreaX, AreaY, AreaWidth, AreaHeight);

    /// <summary>Create a key for the earlier managed LayoutTrace cache.</summary>
    public static LayoutCacheKey Create(
        Rect bounds,
        LayoutDirection direction,
        IReadOnlyList<LayoutConstraint> constraints)
    {
        ArgumentNullException.ThrowIfNull(constraints);

        string fingerprint = string.Join(
            "|",
            constraints.Select(static constraint => $"{constraint.Kind}:{constraint.Value}"));
        return new LayoutCacheKey(bounds, direction, fingerprint);
    }

    /// <summary>Deconstruct using the earlier managed key contract.</summary>
    public void Deconstruct(
        out Rect bounds,
        out LayoutDirection direction,
        out string constraintFingerprint)
    {
        bounds = Bounds;
        direction = Direction;
        constraintFingerprint = ConstraintFingerprint;
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"{Direction}:{Bounds.X},{Bounds.Y},{Bounds.Width}x{Bounds.Height}:{ConstraintFingerprint}";

    private static string CanonicalFingerprint(
        ulong primary,
        ulong secondary,
        ushort count,
        ulong? intrinsicPrimary,
        ulong? intrinsicSecondary) =>
        $"canonical:{primary:x16}:{secondary:x16}:{count}:{intrinsicPrimary?.ToString("x16") ?? "-"}:{intrinsicSecondary?.ToString("x16") ?? "-"}";
}

/// <summary>Fixed-capacity memoization cache for layout rectangles.</summary>
public sealed class LayoutCache
{
    private const int DefaultCapacity = 64;

    private readonly Dictionary<LayoutCacheKey, LayoutTrace> _legacyEntries = [];
    private readonly Dictionary<LayoutCacheKey, CachedLayoutEntry> _entries;
    private readonly int _maxEntries;
    private ulong _generation;
    private ulong _hits;
    private ulong _misses;

    /// <summary>Create a cache with upstream's default capacity of 64.</summary>
    public LayoutCache()
        : this(DefaultCapacity)
    {
    }

    /// <summary>Create a cache with a fixed maximum capacity.</summary>
    public LayoutCache(int maxEntries)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxEntries);
        _maxEntries = maxEntries;
        _entries = new Dictionary<LayoutCacheKey, CachedLayoutEntry>(maxEntries);
    }

    /// <summary>Create a cache with a fixed maximum capacity.</summary>
    public static LayoutCache New(int maxEntries) => new(maxEntries);

    /// <summary>Number of entries in the earlier managed LayoutTrace store.</summary>
    public int Count => _legacyEntries.Count;

    /// <summary>Look up an entry in the earlier managed LayoutTrace store.</summary>
    public bool TryGet(
        Rect bounds,
        LayoutDirection direction,
        IReadOnlyList<LayoutConstraint> constraints,
        out LayoutTrace trace) =>
        _legacyEntries.TryGetValue(LayoutCacheKey.Create(bounds, direction, constraints), out trace!);

    /// <summary>Store an entry in the earlier managed LayoutTrace store.</summary>
    public void Set(LayoutTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
        _legacyEntries[LayoutCacheKey.Create(trace.Bounds, trace.Direction, trace.Constraints)] = trace;
    }

    /// <summary>Return a cached result or compute, cache, and return a defensive copy.</summary>
    public IReadOnlyList<Rect> GetOrCompute(
        LayoutCacheKey key,
        Func<IReadOnlyList<Rect>> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);

        if (_entries.TryGetValue(key, out CachedLayoutEntry? entry) &&
            entry.Generation == _generation)
        {
            _hits = unchecked(_hits + 1);
            entry.AccessCount = entry.AccessCount == uint.MaxValue
                ? uint.MaxValue
                : entry.AccessCount + 1;
            return entry.Chunks.ToArray();
        }

        _misses = unchecked(_misses + 1);
        IReadOnlyList<Rect> computed = compute()
            ?? throw new InvalidOperationException("The layout-cache computation returned null.");
        Rect[] chunks = computed.ToArray();

        if (_entries.Count >= _maxEntries)
            EvictLeastAccessed();

        _entries[key] = new CachedLayoutEntry(chunks.ToArray(), _generation, 1);
        return chunks;
    }

    /// <summary>Invalidate canonical entries in O(1) by advancing the generation.</summary>
    public void InvalidateAll() => _generation = unchecked(_generation + 1);

    /// <summary>Return a value snapshot of current canonical cache statistics.</summary>
    public LayoutCacheStats Stats()
    {
        ulong total = unchecked(_hits + _misses);
        return new LayoutCacheStats
        {
            Entries = _entries.Count,
            Hits = _hits,
            Misses = _misses,
            HitRate = total == 0 ? 0.0 : (double)_hits / total,
        };
    }

    /// <summary>Reset canonical hit and miss counters.</summary>
    public void ResetStats()
    {
        _hits = 0;
        _misses = 0;
    }

    /// <summary>Clear both canonical and earlier managed compatibility stores.</summary>
    public void Clear()
    {
        _entries.Clear();
        _legacyEntries.Clear();
        _generation = unchecked(_generation + 1);
    }

    /// <summary>Current number of canonical memoized layouts.</summary>
    public int Len() => _entries.Count;

    /// <summary>Whether the canonical memoization store is empty.</summary>
    public bool IsEmpty() => _entries.Count == 0;

    /// <summary>Maximum canonical cache capacity.</summary>
    public int Capacity() => _maxEntries;

    /// <inheritdoc />
    public override string ToString() =>
        $"LayoutCache {{ entries: {_entries.Count}, generation: {_generation}, max_entries: {_maxEntries}, hits: {_hits}, misses: {_misses} }}";

    // Upstream's same-module wraparound test can set the private generation directly;
    // the managed test assembly receives the equivalent internal seam via IVT.
    internal ulong Generation
    {
        get => _generation;
        set => _generation = value;
    }

    private void EvictLeastAccessed()
    {
        LayoutCacheKey? victim = null;
        uint minimum = uint.MaxValue;
        foreach ((LayoutCacheKey key, CachedLayoutEntry value) in _entries)
        {
            if (victim is null || value.AccessCount < minimum)
            {
                victim = key;
                minimum = value.AccessCount;
            }
        }

        if (victim is { } keyToRemove)
            _entries.Remove(keyToRemove);
    }
}

/// <summary>Layout cache backed by the scan-resistant S3-FIFO eviction policy.</summary>
public sealed class S3FifoLayoutCache
{
    private const int DefaultCapacity = 64;

    private readonly S3FifoCache<LayoutCacheKey, CachedLayoutEntry> _cache;
    private readonly int _maxEntries;
    private ulong _generation;
    private ulong _hits;
    private ulong _misses;

    /// <summary>Create a cache with upstream's default capacity of 64.</summary>
    public S3FifoLayoutCache()
        : this(DefaultCapacity)
    {
    }

    /// <summary>Create an S3-FIFO cache; upstream clamps capacity to at least two.</summary>
    public S3FifoLayoutCache(int maxEntries)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxEntries);
        _maxEntries = Math.Max(maxEntries, 2);
        _cache = new S3FifoCache<LayoutCacheKey, CachedLayoutEntry>(_maxEntries);
    }

    /// <summary>Create an S3-FIFO cache with a fixed maximum capacity.</summary>
    public static S3FifoLayoutCache New(int maxEntries) => new(maxEntries);

    /// <summary>Return a cached result or compute, cache, and return a defensive copy.</summary>
    public IReadOnlyList<Rect> GetOrCompute(
        LayoutCacheKey key,
        Func<IReadOnlyList<Rect>> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);

        if (_cache.TryGetValue(key, out CachedLayoutEntry? entry) && entry is not null)
        {
            if (entry.Generation == _generation)
            {
                _hits = unchecked(_hits + 1);
                return entry.Chunks.ToArray();
            }

            _cache.Remove(key);
        }

        _misses = unchecked(_misses + 1);
        IReadOnlyList<Rect> computed = compute()
            ?? throw new InvalidOperationException("The layout-cache computation returned null.");
        Rect[] chunks = computed.ToArray();
        _cache.Insert(key, new CachedLayoutEntry(chunks.ToArray(), _generation, 1));
        return chunks;
    }

    /// <summary>Invalidate all entries in O(1) by advancing the generation.</summary>
    public void InvalidateAll() => _generation = unchecked(_generation + 1);

    /// <summary>Return a value snapshot of current cache statistics.</summary>
    public LayoutCacheStats Stats()
    {
        ulong total = unchecked(_hits + _misses);
        return new LayoutCacheStats
        {
            Entries = _cache.Count,
            Hits = _hits,
            Misses = _misses,
            HitRate = total == 0 ? 0.0 : (double)_hits / total,
        };
    }

    /// <summary>Reset wrapper hit and miss counters.</summary>
    public void ResetStats()
    {
        _hits = 0;
        _misses = 0;
    }

    /// <summary>Clear all entries and advance the generation.</summary>
    public void Clear()
    {
        _cache.Clear();
        _generation = unchecked(_generation + 1);
    }

    /// <summary>Current entry count.</summary>
    public int Len() => _cache.Count;

    /// <summary>Whether the cache is empty.</summary>
    public bool IsEmpty() => _cache.Count == 0;

    /// <summary>Maximum capacity, clamped to at least two.</summary>
    public int Capacity() => _maxEntries;

    /// <inheritdoc />
    public override string ToString() =>
        $"S3FifoLayoutCache {{ entries: {_cache.Count}, generation: {_generation}, max_entries: {_maxEntries}, hits: {_hits}, misses: {_misses} }}";
}

/// <summary>Identity for a logical layout slot independent of its current area.</summary>
public readonly record struct CoherenceId(ulong ConstraintsHash, Direction Direction)
{
    /// <summary>Create an identity from constraints and direction.</summary>
    public static CoherenceId New(IReadOnlyList<Constraint> constraints, Direction direction)
    {
        ArgumentNullException.ThrowIfNull(constraints);
        return new CoherenceId(RustCacheHash.HashConstraints(constraints).Primary, direction);
    }

    /// <summary>Create an identity from an existing canonical cache key.</summary>
    public static CoherenceId FromCacheKey(LayoutCacheKey key) =>
        new(key.ConstraintsHash, key.AxisDirection);
}

/// <summary>Previous allocations used to make layout rounding temporally coherent.</summary>
public sealed class CoherenceCache : ICloneable
{
    private readonly Dictionary<CoherenceId, CoherenceEntry> _entries;
    private readonly int _maxEntries;
    private ulong _tick;

    /// <summary>Create a coherence cache with upstream's default capacity of 64.</summary>
    public CoherenceCache()
        : this(64)
    {
    }

    /// <summary>Create a coherence cache with a fixed maximum capacity.</summary>
    public CoherenceCache(int maxEntries)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxEntries);
        _maxEntries = maxEntries;
        _entries = new Dictionary<CoherenceId, CoherenceEntry>(Math.Min(maxEntries, 256));
    }

    private CoherenceCache(int maxEntries, ulong tick, Dictionary<CoherenceId, CoherenceEntry> entries)
    {
        _maxEntries = maxEntries;
        _tick = tick;
        _entries = entries;
    }

    /// <summary>Create a coherence cache with a fixed maximum capacity.</summary>
    public static CoherenceCache New(int maxEntries) => new(maxEntries);

    /// <summary>Retrieve a defensive copy of the previous allocation.</summary>
    public IReadOnlyList<ushort>? Get(CoherenceId id) =>
        _entries.TryGetValue(id, out CoherenceEntry? entry)
            ? entry.Allocation.ToArray()
            : null;

    /// <summary>Store a defensive allocation snapshot, evicting the oldest slot if needed.</summary>
    public void Store(CoherenceId id, IReadOnlyList<ushort> allocation)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        _tick = unchecked(_tick + 1);

        if (_entries.Count >= _maxEntries && !_entries.ContainsKey(id))
            EvictOldest();

        _entries[id] = new CoherenceEntry(allocation.ToArray(), _tick);
    }

    /// <summary>Clear all stored allocations.</summary>
    public void Clear() => _entries.Clear();

    /// <summary>Number of stored logical layout slots.</summary>
    public int Len() => _entries.Count;

    /// <summary>Whether no prior allocations are stored.</summary>
    public bool IsEmpty() => _entries.Count == 0;

    /// <summary>Configured maximum entry count.</summary>
    public int Capacity() => _maxEntries;

    /// <summary>Compute total and maximum cell displacement from a prior allocation.</summary>
    public (ulong SumDisplacement, uint MaxDisplacement) Displacement(
        CoherenceId id,
        IReadOnlyList<ushort> newAllocation)
    {
        ArgumentNullException.ThrowIfNull(newAllocation);
        if (!_entries.TryGetValue(id, out CoherenceEntry? entry))
            return (0, 0);

        ushort[] previous = entry.Allocation;
        int commonLength = Math.Min(previous.Length, newAllocation.Count);
        ulong sum = 0;
        uint maximum = 0;

        for (int index = 0; index < commonLength; index++)
        {
            uint displacement = (uint)Math.Abs((int)newAllocation[index] - previous[index]);
            sum = unchecked(sum + displacement);
            maximum = Math.Max(maximum, displacement);
        }

        for (int index = commonLength; index < previous.Length; index++)
        {
            sum = unchecked(sum + previous[index]);
            maximum = Math.Max(maximum, previous[index]);
        }

        for (int index = commonLength; index < newAllocation.Count; index++)
        {
            sum = unchecked(sum + newAllocation[index]);
            maximum = Math.Max(maximum, newAllocation[index]);
        }

        return (sum, maximum);
    }

    /// <summary>Create an independent deep clone, matching Rust's Clone behavior.</summary>
    public CoherenceCache Clone()
    {
        Dictionary<CoherenceId, CoherenceEntry> entries = _entries.ToDictionary(
            static pair => pair.Key,
            static pair => new CoherenceEntry(pair.Value.Allocation.ToArray(), pair.Value.LastStored));
        return new CoherenceCache(_maxEntries, _tick, entries);
    }

    object ICloneable.Clone() => Clone();

    /// <inheritdoc />
    public override string ToString() =>
        $"CoherenceCache {{ entries: {_entries.Count}, max_entries: {_maxEntries}, tick: {_tick} }}";

    private void EvictOldest()
    {
        CoherenceId? victim = null;
        ulong oldest = ulong.MaxValue;
        foreach ((CoherenceId id, CoherenceEntry entry) in _entries)
        {
            if (victim is null || entry.LastStored < oldest)
            {
                victim = id;
                oldest = entry.LastStored;
            }
        }

        if (victim is { } idToRemove)
            _entries.Remove(idToRemove);
    }
}

internal sealed class CachedLayoutEntry(Rect[] chunks, ulong generation, uint accessCount)
{
    public Rect[] Chunks { get; } = chunks;

    public ulong Generation { get; } = generation;

    public uint AccessCount { get; set; } = accessCount;
}

internal sealed class CoherenceEntry(ushort[] allocation, ulong lastStored)
{
    public ushort[] Allocation { get; } = allocation;

    public ulong LastStored { get; } = lastStored;
}

internal static class RustCacheHash
{
    private const ulong FxMultiplier = 0xf1357aea2e62a9c5UL;

    public static (ulong Primary, ulong Secondary) HashConstraints(
        IReadOnlyList<Constraint> constraints)
    {
        var sipBytes = new List<byte>(constraints.Count * 16);
        var fx = new FxWriter();

        foreach (Constraint constraint in constraints)
        {
            long discriminant = constraint switch
            {
                Constraint.FixedValue => 0,
                Constraint.PercentageValue => 1,
                Constraint.MinValue => 2,
                Constraint.MaxValue => 3,
                Constraint.RatioValue => 4,
                Constraint.FillValue => 5,
                Constraint.FitContentValue => 6,
                Constraint.FitContentBoundedValue => 7,
                Constraint.FitMinValue => 8,
                _ => throw new ArgumentOutOfRangeException(nameof(constraints), constraint, "Unknown constraint kind."),
            };
            WriteInt64(sipBytes, discriminant);
            fx.WriteIsize(discriminant);

            switch (constraint)
            {
                case Constraint.FixedValue fixedValue:
                    WriteUInt16(sipBytes, fixedValue.Value);
                    fx.WriteUInt16(fixedValue.Value);
                    break;
                case Constraint.PercentageValue percentage:
                    uint bits = BitConverter.SingleToUInt32Bits(percentage.Value);
                    WriteUInt32(sipBytes, bits);
                    fx.WriteUInt32(bits);
                    break;
                case Constraint.MinValue minimum:
                    WriteUInt16(sipBytes, minimum.Value);
                    fx.WriteUInt16(minimum.Value);
                    break;
                case Constraint.MaxValue maximum:
                    WriteUInt16(sipBytes, maximum.Value);
                    fx.WriteUInt16(maximum.Value);
                    break;
                case Constraint.RatioValue ratio:
                    uint divisor = GreatestCommonDivisor(ratio.Numerator, ratio.Denominator);
                    uint numerator = divisor == 0 ? ratio.Numerator : ratio.Numerator / divisor;
                    uint denominator = divisor == 0 ? ratio.Denominator : ratio.Denominator / divisor;
                    WriteUInt32(sipBytes, numerator);
                    WriteUInt32(sipBytes, denominator);
                    fx.WriteUInt32(numerator);
                    fx.WriteUInt32(denominator);
                    break;
                case Constraint.FitContentBoundedValue bounded:
                    WriteUInt16(sipBytes, bounded.Minimum);
                    WriteUInt16(sipBytes, bounded.Maximum);
                    fx.WriteUInt16(bounded.Minimum);
                    fx.WriteUInt16(bounded.Maximum);
                    break;
            }
        }

        return (SipHash13.Compute(CollectionsMarshal.AsSpan(sipBytes)), fx.Finish());
    }

    public static (ulong Primary, ulong Secondary) HashIntrinsics(
        IReadOnlyList<LayoutSizeHint> intrinsics)
    {
        var sipBytes = new List<byte>(intrinsics.Count * 20);
        var fx = new FxWriter();

        foreach (LayoutSizeHint hint in intrinsics)
        {
            WriteUInt16(sipBytes, hint.Min);
            WriteUInt16(sipBytes, hint.Preferred);
            fx.WriteUInt16(hint.Min);
            fx.WriteUInt16(hint.Preferred);

            long optionDiscriminant = hint.Max.HasValue ? 1 : 0;
            WriteInt64(sipBytes, optionDiscriminant);
            fx.WriteIsize(optionDiscriminant);
            if (hint.Max is { } maximum)
            {
                WriteUInt16(sipBytes, maximum);
                fx.WriteUInt16(maximum);
            }
        }

        return (SipHash13.Compute(CollectionsMarshal.AsSpan(sipBytes)), fx.Finish());
    }

    private static uint GreatestCommonDivisor(uint left, uint right)
    {
        while (right != 0)
            (left, right) = (right, left % right);
        return left;
    }

    private static void WriteUInt16(List<byte> bytes, ushort value)
    {
        bytes.Add((byte)value);
        bytes.Add((byte)(value >> 8));
    }

    private static void WriteUInt32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)value);
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 24));
    }

    private static void WriteInt64(List<byte> bytes, long value)
    {
        ulong unsigned = unchecked((ulong)value);
        for (int shift = 0; shift < 64; shift += 8)
            bytes.Add((byte)(unsigned >> shift));
    }

    private struct FxWriter
    {
        private ulong _hash;

        public void WriteIsize(long value) => Add(unchecked((ulong)value));

        public void WriteUInt16(ushort value) => Add(value);

        public void WriteUInt32(uint value) => Add(value);

        public readonly ulong Finish() => BitOperations.RotateLeft(_hash, 26);

        private void Add(ulong value) => _hash = unchecked((_hash + value) * FxMultiplier);

    }
}

internal static class SipHash13
{
    public static ulong Compute(ReadOnlySpan<byte> input)
    {
        ulong v0 = 0x736f6d6570736575UL;
        ulong v1 = 0x646f72616e646f6dUL;
        ulong v2 = 0x6c7967656e657261UL;
        ulong v3 = 0x7465646279746573UL;

        int offset = 0;
        while (offset + sizeof(ulong) <= input.Length)
        {
            ulong message = ReadUInt64LittleEndian(input[offset..]);
            v3 ^= message;
            Round(ref v0, ref v1, ref v2, ref v3);
            v0 ^= message;
            offset += sizeof(ulong);
        }

        ulong tail = (ulong)input.Length << 56;
        for (int index = 0; offset + index < input.Length; index++)
            tail |= (ulong)input[offset + index] << (index * 8);

        v3 ^= tail;
        Round(ref v0, ref v1, ref v2, ref v3);
        v0 ^= tail;
        v2 ^= 0xff;
        Round(ref v0, ref v1, ref v2, ref v3);
        Round(ref v0, ref v1, ref v2, ref v3);
        Round(ref v0, ref v1, ref v2, ref v3);
        return v0 ^ v1 ^ v2 ^ v3;
    }

    private static ulong ReadUInt64LittleEndian(ReadOnlySpan<byte> bytes)
    {
        ulong value = 0;
        for (int index = 0; index < sizeof(ulong); index++)
            value |= (ulong)bytes[index] << (index * 8);
        return value;
    }

    private static void Round(ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3)
    {
        v0 = unchecked(v0 + v1);
        v1 = BitOperations.RotateLeft(v1, 13);
        v1 ^= v0;
        v0 = BitOperations.RotateLeft(v0, 32);
        v2 = unchecked(v2 + v3);
        v3 = BitOperations.RotateLeft(v3, 16);
        v3 ^= v2;
        v0 = unchecked(v0 + v3);
        v3 = BitOperations.RotateLeft(v3, 21);
        v3 ^= v0;
        v2 = unchecked(v2 + v1);
        v1 = BitOperations.RotateLeft(v1, 17);
        v1 ^= v2;
        v2 = BitOperations.RotateLeft(v2, 32);
    }
}
