// Upstream source: crates/ftui-text/src/shaping.rs
// Source basis: 15cc6543f76b814394c590f9e7719dedd6684e4c
// Direct port of the shaped-output records consumed by ClusterMap. This does
// not claim that the wider shaping engine has been ported.

namespace FrankenTui.Text;

/// <summary>A single positioned glyph emitted by a shaping engine.</summary>
public readonly record struct ShapedGlyph(
    uint GlyphId,
    uint Cluster,
    int XAdvance,
    int YAdvance,
    int XOffset,
    int YOffset);

/// <summary>The positioned glyphs and aggregate advance for one shaped run.</summary>
public sealed class ShapedRun
{
    public ShapedRun(IReadOnlyList<ShapedGlyph> glyphs, int totalAdvance)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        Glyphs = glyphs;
        TotalAdvance = totalAdvance;
    }

    public IReadOnlyList<ShapedGlyph> Glyphs { get; }

    public int TotalAdvance { get; }

    public int Length => Glyphs.Count;

    public bool IsEmpty => Glyphs.Count == 0;
}
