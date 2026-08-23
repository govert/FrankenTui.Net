// SPDX-License-Identifier: Apache-2.0
// Ported from crates/ftui-render/src/link_registry.rs at
// 15cc6543f76b814394c590f9e7719dedd6684e4c.
// Source SHA-256: 61CA85E504DE2BA2F78A7A048EFE40BBE462234939B0CD925016EC027628FF09.

using System.Text;

namespace FrankenTui.Render;

public sealed class LinkRegistry
{
    private const uint MaxLinkId = 0x00FF_FFFF;
    private const int MaxUrlBytes = 4096;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private readonly List<string?> _links;
    private readonly Dictionary<string, uint> _lookup;
    private readonly Stack<uint> _free;

    public LinkRegistry()
    {
        _links = [null];
        _lookup = new Dictionary<string, uint>(StringComparer.Ordinal);
        _free = new Stack<uint>();
    }

    private LinkRegistry(
        List<string?> links,
        Dictionary<string, uint> lookup,
        Stack<uint> free)
    {
        _links = links;
        _lookup = lookup;
        _free = free;
    }

    public uint Register(string url)
    {
        ArgumentNullException.ThrowIfNull(url);
        if (!IsSafeOsc8Url(url))
        {
            return 0;
        }

        if (_lookup.TryGetValue(url, out var existing))
        {
            return existing;
        }

        uint id;
        if (_free.Count > 0)
        {
            id = _free.Pop();
        }
        else
        {
            if ((uint)_links.Count > MaxLinkId)
            {
                return 0;
            }

            id = (uint)_links.Count;
            _links.Add(null);
        }

        if (id == 0 || id > MaxLinkId)
        {
            return 0;
        }

        _links[(int)id] = url;
        _lookup[url] = id;
        return id;
    }

    public string? Get(uint id) => id < (uint)_links.Count ? _links[(int)id] : null;

    public void Unregister(uint id)
    {
        if (id == 0 || id >= (uint)_links.Count || _links[(int)id] is not { } url)
        {
            return;
        }

        _lookup.Remove(url);
        _links[(int)id] = null;
        _free.Push(id);
    }

    /// <summary>Compatibility name retained from the original managed port.</summary>
    public void Remove(uint id) => Unregister(id);

    public int Count => _lookup.Count;

    public bool IsEmpty => Count == 0;

    public bool Contains(uint id) => Get(id) is not null;

    /// <summary>Returns an approximate managed heap footprint in bytes.</summary>
    public ulong EstimateMemory()
    {
        var total = (ulong)_links.Capacity * (ulong)IntPtr.Size;
        total += (ulong)_links
            .Where(static url => url is not null)
            .Sum(static url => checked(url!.Length * sizeof(char)));

        // Dictionary entries contain hash/next integers plus key/value fields.
        total += (ulong)_lookup.EnsureCapacity(0) * (ulong)((2 * sizeof(int)) + IntPtr.Size + sizeof(uint));
        total += (ulong)_free.EnsureCapacity(0) * sizeof(uint);
        return total;
    }

    public LinkRegistry Clone()
    {
        return new LinkRegistry(
            [.. _links],
            new Dictionary<string, uint>(_lookup, StringComparer.Ordinal),
            new Stack<uint>(_free.Reverse()));
    }

    public void Clear()
    {
        _links.Clear();
        _links.Add(null);
        _lookup.Clear();
        _free.Clear();
    }

    private static bool IsSafeOsc8Url(string url)
    {
        // Rust `str` is guaranteed to contain valid UTF-8.  Managed strings can
        // contain unpaired UTF-16 surrogates, so reject values that cannot be
        // represented by the source contract before measuring their UTF-8 size.
        for (var index = 0; index < url.Length; index++)
        {
            var current = url[index];
            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= url.Length || !char.IsLowSurrogate(url[index + 1]))
                {
                    return false;
                }

                index++;
            }
            else if (char.IsLowSurrogate(current))
            {
                return false;
            }
        }

        try
        {
            if (StrictUtf8.GetByteCount(url) > MaxUrlBytes)
            {
                return false;
            }
        }
        catch (EncoderFallbackException)
        {
            return false;
        }

        return !url.EnumerateRunes().Any(Rune.IsControl);
    }
}
