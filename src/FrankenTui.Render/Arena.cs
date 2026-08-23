// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/arena.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: .NET cannot expose bump-allocated references whose lifetime is
// statically tied to a frame. This managed realization owns scratch objects
// until Reset, reports retained-capacity/high-water evidence, and lets the GC
// reclaim cleared objects. Destructors/finalizers therefore retain normal CLR
// semantics; callers must still restrict arena values to short-lived scratch data.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace FrankenTui.Render;

/// <summary>Mutable managed analogue of an arena-allocated scalar.</summary>
public sealed class ArenaValue<T>(T value)
{
    public T Value { get; set; } = value;
}

/// <summary>Per-frame owner for temporary render-path allocations.</summary>
public sealed class FrameArena
{
    public const int DefaultArenaCapacity = 256 * 1024;

    private readonly List<object> _allocations = [];
    private long _liveBytes;
    private long _retainedBytes;

    public FrameArena(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        InitialCapacity = capacity;
        _retainedBytes = capacity;
    }

    public FrameArena() : this(DefaultArenaCapacity) { }

    public int InitialCapacity { get; }
    public long Generation { get; private set; }
    public int AllocationCount => _allocations.Count;
    public long LiveBytes => _liveBytes;

    /// <summary>Retained capacity/high-water estimate, not current live usage.</summary>
    public long AllocatedBytes => _retainedBytes;

    public long AllocatedBytesIncludingMetadata =>
        checked(_retainedBytes + (_allocations.Capacity * IntPtr.Size));

    public void Reset()
    {
        _allocations.Clear();
        _liveBytes = 0;
        Generation++;
    }

    public string AllocStr(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var copy = new string(value.AsSpan());
        Track(copy, Encoding.UTF8.GetByteCount(copy));
        return copy;
    }

    public string AllocFmt(FormattableString value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return AllocStr(value.ToString(CultureInfo.InvariantCulture));
    }

    public string AllocFmt(string format, params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(arguments);
        return AllocStr(string.Format(CultureInfo.InvariantCulture, format, arguments));
    }

    public T[] AllocSlice<T>(ReadOnlySpan<T> values)
    {
        var copy = values.ToArray();
        Track(copy, EstimateArrayBytes<T>(copy.Length));
        return copy;
    }

    public ArenaValue<T> AllocWith<T>(Func<T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Alloc(factory());
    }

    public ArenaValue<T> Alloc<T>(T value)
    {
        var box = new ArenaValue<T>(value);
        Track(box, EstimateElementBytes<T>());
        return box;
    }

    public T[] AllocIter<T>(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.ToArray();
        Track(copy, EstimateArrayBytes<T>(copy.Length));
        return copy;
    }

    public List<T> NewVec<T>()
    {
        var list = new List<T>();
        Track(list, 0);
        return list;
    }

    public List<T> NewVecWithCapacity<T>(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        var list = new List<T>(capacity);
        Track(list, EstimateArrayBytes<T>(capacity));
        return list;
    }

    public override string ToString() =>
        $"FrameArena {{ generation = {Generation}, allocations = {AllocationCount}, " +
        $"live_bytes = {LiveBytes}, retained_bytes = {AllocatedBytes} }}";

    private void Track(object value, long bytes)
    {
        _allocations.Add(value);
        _liveBytes = checked(_liveBytes + Math.Max(bytes, 0));
        if (_liveBytes <= _retainedBytes) return;
        var grown = Math.Max(_retainedBytes, 1);
        while (grown < _liveBytes) grown = checked(grown * 2);
        _retainedBytes = grown;
    }

    private static long EstimateArrayBytes<T>(int count) =>
        checked((long)EstimateElementBytes<T>() * count);

    private static int EstimateElementBytes<T>() =>
        RuntimeHelpers.IsReferenceOrContainsReferences<T>() ? IntPtr.Size : Unsafe.SizeOf<T>();
}
