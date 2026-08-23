// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/quotient_filter.rs
// Upstream commit: 15cc6543f76b814394c590f9e7719dedd6684e4c
// DIVERGENCE: Rust's Hash trait has no closed CLR equivalent. The default
// path reproduces Rust DefaultHasher/SipHash-1-3 for primitive integers,
// Boolean, char, and UTF-8 strings; custom managed types may supply an exact
// full hash through IQuotientFilterHashable. Other types use a documented
// deterministic text/type fallback rather than process-randomized GetHashCode.

using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace FrankenTui.Render;

/// <summary>Supplies the full 64-bit hash consumed by a quotient filter.</summary>
public interface IQuotientFilterHashable
{
    ulong GetQuotientFilterHash();
}

public readonly record struct QuotientFilterConfig(uint Q, uint R)
{
    public QuotientFilterConfig() : this(10, 8)
    {
    }

    public static QuotientFilterConfig Default => new(10, 8);

    public static QuotientFilterConfig ForCapacity(ulong expectedItems, double falsePositiveRate)
    {
        var rate = double.IsFinite(falsePositiveRate) && falsePositiveRate > 0.0
            ? Math.Min(falsePositiveRate, double.BitDecrement(1.0))
            : 0.01;

        var remainderBits = (uint)Math.Ceiling(-Math.Log2(rate));
        remainderBits = Math.Clamp(remainderBits, 2u, 32u);

        var neededDouble = Math.Ceiling(expectedItems / 0.75d);
        var needed = neededDouble >= ulong.MaxValue ? ulong.MaxValue : (ulong)neededDouble;
        var quotientBits = needed == 0 ? 0u : (uint)BitOperations.Log2(needed) + 1;
        quotientBits = Math.Clamp(quotientBits, 4u, 28u);
        return new QuotientFilterConfig(quotientBits, remainderBits);
    }
}

/// <summary>
/// Compact approximate-membership filter with deletion and same-configuration
/// merge. Fingerprints are stored using open addressing and backward-shift
/// deletion, matching the current upstream simplified quotient-filter route.
/// </summary>
public sealed class QuotientFilter
{
    private readonly uint _q;
    private readonly uint _r;
    private readonly Slot?[] _slots;
    private int _count;

    public QuotientFilter() : this(QuotientFilterConfig.Default)
    {
    }

    public QuotientFilter(QuotientFilterConfig config)
    {
        _q = Math.Min(config.Q, 28u);
        _r = Math.Clamp(config.R, 1u, 32u);
        Capacity = 1 << (int)_q;
        _slots = new Slot?[Capacity];
    }

    private QuotientFilter(uint q, uint r, Slot?[] slots, int count)
    {
        _q = q;
        _r = r;
        _slots = slots;
        _count = count;
        Capacity = slots.Length;
    }

    public static QuotientFilter WithDefaults() => new(QuotientFilterConfig.Default);

    public int Count => _count;

    public bool IsEmpty => _count == 0;

    public double LoadFactor => (double)_count / Capacity;

    public int Capacity { get; }

    public double TheoreticalFalsePositiveRate
    {
        get
        {
            var baseRate = 1.0 / (1UL << (int)_r);
            return 1.0 - Math.Pow(1.0 - baseRate, _count);
        }
    }

    public QuotientFilter Clone() =>
        new(_q, _r, (Slot?[])_slots.Clone(), _count);

    /// <summary>Returns false when the fingerprint is already present or the table is full.</summary>
    public bool Insert<T>(T item)
    {
        if (_count >= Capacity)
        {
            return false;
        }

        var fingerprint = Fingerprint(item);
        var position = (int)fingerprint.Quotient;
        for (var probe = 0; probe < Capacity; probe++)
        {
            var slot = _slots[position];
            if (slot is null)
            {
                _slots[position] = fingerprint;
                _count++;
                return true;
            }

            if (slot.Value == fingerprint)
            {
                return false;
            }

            position = (position + 1) % Capacity;
        }

        return false;
    }

    /// <summary>False is definitive; true may be a fingerprint collision.</summary>
    public bool Contains<T>(T item)
    {
        var fingerprint = Fingerprint(item);
        var position = (int)fingerprint.Quotient;
        for (var probe = 0; probe < Capacity; probe++)
        {
            var slot = _slots[position];
            if (slot is null)
            {
                return false;
            }

            if (slot.Value == fingerprint)
            {
                return true;
            }

            position = (position + 1) % Capacity;
        }

        return false;
    }

    /// <summary>Removes a matching fingerprint using cyclic backward-shift deletion.</summary>
    public bool Remove<T>(T item)
    {
        var fingerprint = Fingerprint(item);
        var position = (int)fingerprint.Quotient;
        var found = -1;
        for (var probe = 0; probe < Capacity; probe++)
        {
            var slot = _slots[position];
            if (slot is null)
            {
                break;
            }

            if (slot.Value == fingerprint)
            {
                found = position;
                break;
            }

            position = (position + 1) % Capacity;
        }

        if (found < 0)
        {
            return false;
        }

        position = found;
        _slots[position] = null;
        _count--;

        var current = (position + 1) % Capacity;
        while (_slots[current] is { } currentSlot)
        {
            var canonical = (int)currentSlot.Quotient;
            var canonicalInGapInterval = position <= current
                ? canonical > position && canonical <= current
                : canonical > position || canonical <= current;

            if (!canonicalInGapInterval)
            {
                _slots[position] = currentSlot;
                _slots[current] = null;
                position = current;
            }

            current = (current + 1) % Capacity;
        }

        return true;
    }

    public void Clear()
    {
        Array.Clear(_slots);
        _count = 0;
    }

    /// <summary>Copies fingerprints from an identically configured filter.</summary>
    public int Merge(QuotientFilter other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (_q != other._q || _r != other._r)
        {
            return 0;
        }

        var added = 0;
        foreach (var candidate in other._slots)
        {
            if (candidate is not { } slot)
            {
                continue;
            }

            var position = (int)slot.Quotient;
            var found = false;
            for (var probe = 0; probe < Capacity; probe++)
            {
                var existing = _slots[position];
                if (existing is null)
                {
                    break;
                }

                if (existing.Value == slot)
                {
                    found = true;
                    break;
                }

                position = (position + 1) % Capacity;
            }

            if (found)
            {
                continue;
            }

            position = (int)slot.Quotient;
            for (var probe = 0; probe < Capacity; probe++)
            {
                if (_slots[position] is null)
                {
                    _slots[position] = slot;
                    _count++;
                    added++;
                    break;
                }

                position = (position + 1) % Capacity;
            }
        }

        return added;
    }

    internal (uint Quotient, ulong Remainder) FingerprintForTest<T>(T item)
    {
        var slot = Fingerprint(item);
        return (slot.Quotient, slot.Remainder);
    }

    private Slot Fingerprint<T>(T item)
    {
        var hash = RustHash.Hash(item);
        var quotientMask = (1u << (int)_q) - 1u;
        var remainderMask = (1UL << (int)_r) - 1UL;
        return new Slot((uint)(hash >> (int)_r) & quotientMask, hash & remainderMask);
    }

    private readonly record struct Slot(uint Quotient, ulong Remainder);

    private static class RustHash
    {
        public static ulong Hash<T>(T item)
        {
            if (item is IQuotientFilterHashable exact)
            {
                return exact.GetQuotientFilterHash();
            }

            var bytes = item switch
            {
                null => [0xFF],
                byte value => [value],
                sbyte value => [(byte)value],
                ushort value => LittleEndian(value),
                short value => LittleEndian(unchecked((ushort)value)),
                uint value => LittleEndian(value),
                int value => LittleEndian(unchecked((uint)value)),
                ulong value => LittleEndian(value),
                long value => LittleEndian(unchecked((ulong)value)),
                nuint value => LittleEndian((ulong)value),
                nint value => LittleEndian(unchecked((ulong)value)),
                bool value => [value ? (byte)1 : (byte)0],
                char value => LittleEndian((uint)value),
                string value => Utf8String(value),
                _ => Utf8String($"{typeof(T).AssemblyQualifiedName}\u001F{FormatInvariant(item)}"),
            };

            return SipHash13.Compute(bytes);
        }

        private static string FormatInvariant<T>(T item) => item is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty
            : item?.ToString() ?? string.Empty;

        private static byte[] Utf8String(string value)
        {
            var encoded = Encoding.UTF8.GetBytes(value);
            var result = new byte[encoded.Length + 1];
            encoded.CopyTo(result, 0);
            result[^1] = 0xFF;
            return result;
        }

        private static byte[] LittleEndian(ushort value)
        {
            var result = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(result, value);
            return result;
        }

        private static byte[] LittleEndian(uint value)
        {
            var result = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(result, value);
            return result;
        }

        private static byte[] LittleEndian(ulong value)
        {
            var result = new byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(result, value);
            return result;
        }
    }

    private static class SipHash13
    {
        public static ulong Compute(ReadOnlySpan<byte> input)
        {
            const ulong k0 = 0;
            const ulong k1 = 0;
            var v0 = k0 ^ 0x736F6D6570736575UL;
            var v1 = k1 ^ 0x646F72616E646F6DUL;
            var v2 = k0 ^ 0x6C7967656E657261UL;
            var v3 = k1 ^ 0x7465646279746573UL;

            var offset = 0;
            while (offset + sizeof(ulong) <= input.Length)
            {
                var message = BinaryPrimitives.ReadUInt64LittleEndian(input[offset..]);
                v3 ^= message;
                Round(ref v0, ref v1, ref v2, ref v3);
                v0 ^= message;
                offset += sizeof(ulong);
            }

            var tail = (ulong)input.Length << 56;
            for (var index = 0; offset + index < input.Length; index++)
            {
                tail |= (ulong)input[offset + index] << (index * 8);
            }

            v3 ^= tail;
            Round(ref v0, ref v1, ref v2, ref v3);
            v0 ^= tail;
            v2 ^= 0xFF;
            Round(ref v0, ref v1, ref v2, ref v3);
            Round(ref v0, ref v1, ref v2, ref v3);
            Round(ref v0, ref v1, ref v2, ref v3);
            return v0 ^ v1 ^ v2 ^ v3;
        }

        private static void Round(ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3)
        {
            v0 += v1;
            v1 = BitOperations.RotateLeft(v1, 13);
            v1 ^= v0;
            v0 = BitOperations.RotateLeft(v0, 32);
            v2 += v3;
            v3 = BitOperations.RotateLeft(v3, 16);
            v3 ^= v2;
            v0 += v3;
            v3 = BitOperations.RotateLeft(v3, 21);
            v3 ^= v0;
            v2 += v1;
            v1 = BitOperations.RotateLeft(v1, 17);
            v1 ^= v2;
            v2 = BitOperations.RotateLeft(v2, 32);
        }
    }
}
