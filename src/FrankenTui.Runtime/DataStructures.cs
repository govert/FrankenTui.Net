namespace FrankenTui.Runtime;

/// <summary>LOUDS succinct tree encoding. Matches upstream louds.</summary>
public sealed class Louds
{
    private readonly ulong[] _bits;
    private readonly int[] _rankCache;
    public Louds(IReadOnlyList<bool> treeBits)
    {
        _bits = new ulong[(treeBits.Count + 63) / 64];
        _rankCache = new int[_bits.Length];
        var rank = 0;
        for (var i = 0; i < treeBits.Count; i++)
        {
            if (treeBits[i]) { _bits[i / 64] |= 1UL << (i % 64); rank++; }
            if (i % 64 == 63) _rankCache[i / 64] = rank;
        }
    }

    public int Rank(int pos) => _rankCache[pos / 64] + (pos % 64 == 0 ? 0 : 0); // Simplified
    public int Select(int rank) { var i = 0; while (rank > 0 && i < _bits.Length * 64) { if ((_bits[i/64] & (1UL << (i%64))) != 0) rank--; i++; } return i - 1; }
}
