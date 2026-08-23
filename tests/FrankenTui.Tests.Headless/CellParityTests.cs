using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using FrankenTui.Render;

namespace FrankenTui.Tests.Headless;

/// <summary>
/// Closed managed projection of the 145 tests in
/// crates/ftui-render/src/cell.rs at 15cc6543f76b814394c590f9e7719dedd6684e4c.
/// Repetitive source cases and proptests are represented by exhaustive or
/// deterministic loops so the managed corpus remains compact.
/// </summary>
public sealed class CellParityTests
{
    private const ulong FnvOffset = 0xcbf29ce484222325;
    private const ulong FnvPrime = 0x100000001b3;

    [Fact]
    public void PackedValueTypesPreserveSourceSizesAndCellWordOrder()
    {
        Assert.Equal(4, Unsafe.SizeOf<GraphemeId>());
        Assert.Equal(4, Unsafe.SizeOf<CellContent>());
        Assert.Equal(4, Unsafe.SizeOf<PackedRgba>());
        Assert.Equal(4, Unsafe.SizeOf<CellAttributes>());
        Assert.Equal(1, Unsafe.SizeOf<CellStyleFlags>());
        Assert.Equal(16, Unsafe.SizeOf<Cell>());
        Assert.Equal(16, Marshal.SizeOf<Cell>());
        Assert.Equal(32, MemoryMarshal.AsBytes(new Cell[2].AsSpan()).Length);

        var cell = new Cell(
            new CellContent(0x0000_0041),
            new PackedRgba(0x1122_3344),
            new PackedRgba(0x5566_7788),
            new CellAttributes(0x99AA_BBCC));
        var words = MemoryMarshal.Cast<Cell, uint>(
            MemoryMarshal.CreateReadOnlySpan(ref cell, 1));

        Assert.Equal(
            new uint[] { 0x0000_0041, 0x1122_3344, 0x5566_7788, 0x99AA_BBCC },
            words.ToArray());
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 0, 1, 0x0800_0001)]
    [InlineData(0xFFFF, 0, 0, 0x0000_FFFF)]
    [InlineData(0, 0x7FF, 0, 0x07FF_0000)]
    [InlineData(0, 0, 0xF, 0x7800_0000)]
    [InlineData(0xFFFF, 0x7FF, 0xF, 0x7FFF_FFFF)]
    public void GraphemeIdBitLayoutRoundTrips(
        int slot,
        int generation,
        int width,
        int expectedRaw)
    {
        var id = new GraphemeId((uint)slot, (ushort)generation, (byte)width);

        Assert.Equal(unchecked((uint)expectedRaw), id.Raw);
        Assert.Equal(slot, id.Slot);
        Assert.Equal(generation, id.Generation);
        Assert.Equal(width, id.Width);
        Assert.Equal(0U, id.Raw & CellContent.GraphemeFlag);
    }

    [Fact]
    public void GraphemeIdMasksEveryComponentAndProtectsDiscriminatorBit()
    {
        var id = new GraphemeId(0x1_FFFF, ushort.MaxValue, 0x1F);

        Assert.Equal(0x7FFF_FFFFU, id.Raw);
        Assert.Equal(GraphemeId.MaxSlot, (uint)id.Slot);
        Assert.Equal(GraphemeId.MaxGeneration, id.Generation);
        Assert.Equal(GraphemeId.MaxWidth, (byte)id.Width);
        Assert.Equal(0U, id.Raw & CellContent.GraphemeFlag);
    }

    [Fact]
    public void GraphemeIdRawDefaultEqualityHashAndDebugAreStable()
    {
        var id = new GraphemeId(42, 5, 2);
        var restored = GraphemeId.FromRaw(id.Raw);
        var differentWidth = new GraphemeId(42, 5, 3);
        var differentGeneration = new GraphemeId(42, 6, 2);

        Assert.Equal(id, restored);
        Assert.Equal(id.GetHashCode(), restored.GetHashCode());
        Assert.NotEqual(id, differentWidth);
        Assert.NotEqual(id, differentGeneration);
        Assert.Equal(default, new GraphemeId(0, 0, 0));
        Assert.Equal("GraphemeId { slot: 42, gen: 5, width: 2 }", id.ToString());
    }

    [Fact]
    public void CellContentSpecialValuesAndDiscriminatorMatchSource()
    {
        Assert.Equal(0U, CellContent.Empty.Raw);
        Assert.Equal(0x7FFF_FFFFU, CellContent.Continuation.Raw);
        Assert.True(CellContent.Empty.IsEmpty);
        Assert.True(CellContent.Empty.IsDefault);
        Assert.False(CellContent.Empty.IsContinuation);
        Assert.False(CellContent.Empty.IsGrapheme);
        Assert.True(CellContent.Continuation.IsContinuation);
        Assert.False(CellContent.Continuation.IsEmpty);
        Assert.False(CellContent.Continuation.IsGrapheme);
        Assert.Equal(0, CellContent.Empty.WidthHint);
        Assert.Equal(0, CellContent.Continuation.WidthHint);
        Assert.Equal(0, CellContent.Empty.Width());
        Assert.Equal(0, CellContent.Continuation.Width());
        Assert.Null(CellContent.Empty.AsRune());
        Assert.Null(CellContent.Continuation.AsRune());
        Assert.Null(CellContent.Empty.AsChar());
        Assert.Null(CellContent.Continuation.AsChar());
        Assert.Null(CellContent.Empty.GraphemeId);
        Assert.Equal("CellContent::EMPTY", CellContent.Empty.ToString());
        Assert.Equal("CellContent::CONTINUATION", CellContent.Continuation.ToString());
    }

    [Fact]
    public void CellContentScalarRoundTripsCoverBmpAndSupplementaryPlanes()
    {
        Rune[] runes =
        [
            new('A'),
            new('\u65E5'),
            new(0xD7FF),
            new(0xE000),
            new(0x10000),
            new(0x1F389),
            new(0x10FFFF)
        ];

        foreach (var rune in runes)
        {
            var content = CellContent.FromRune(rune);
            Assert.Equal((uint)rune.Value, content.Raw);
            Assert.Equal(rune, content.AsRune());
            Assert.False(content.IsGrapheme);
            Assert.False(content.IsEmpty);
            Assert.False(content.IsContinuation);
        }

        Assert.Equal('A', CellContent.FromChar('A').AsChar());
        Assert.Equal('\u65E5', CellContent.FromChar('\u65E5').AsChar());
        Assert.Null(CellContent.FromRune(new Rune(0x1F389)).AsChar());
        Assert.Equal((uint)' ', CellContent.FromChar('\t').Raw);
        Assert.Equal(CellContent.Empty, CellContent.FromChar('\0'));
        Assert.Equal('\x01', CellContent.FromChar('\x01').AsChar());
    }

    [Theory]
    [InlineData(0xD800)]
    [InlineData(0xDFFF)]
    [InlineData(0x110000)]
    [InlineData(0x7FFF_FFFE)]
    public void InvalidDirectScalarRawValuesBehaveLikeRustFromU32None(int raw)
    {
        var content = new CellContent((uint)raw);

        Assert.False(content.IsGrapheme);
        Assert.False(content.IsEmpty);
        Assert.False(content.IsContinuation);
        Assert.Null(content.AsRune());
        Assert.Null(content.AsChar());
        Assert.Equal(1, content.WidthHint);
        Assert.Equal(1, content.Width());
        Assert.Equal($"CellContent(0x{raw:x8})", content.ToString());
    }

    [Fact]
    public void MalformedUtf16CannotEnterTheScalarContentRoute()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CellContent.FromChar('\uD800'));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cell.FromChar('\uDFFF'));
    }

    [Fact]
    public void CellContentGraphemeRoundTripsAndEmbedsWidth()
    {
        foreach (var width in new byte[] { 0, 1, 2, 3, GraphemeId.MaxWidth })
        {
            var id = new GraphemeId(42, 7, width);
            var content = CellContent.FromGrapheme(id);

            Assert.True(content.IsGrapheme);
            Assert.False(content.IsEmpty);
            Assert.False(content.IsContinuation);
            Assert.Null(content.AsRune());
            Assert.Null(content.AsChar());
            Assert.Equal(id, content.GraphemeId);
            Assert.Equal(width, content.WidthHint);
            Assert.Equal(width, content.Width());
            Assert.NotEqual(0U, content.Raw & CellContent.GraphemeFlag);
            Assert.StartsWith("CellContent::Grapheme(", content.ToString());
        }
    }

    [Fact]
    public void CellContentUnicodeWidthsMatchSourceSemantics()
    {
        Assert.Equal(1, CellContent.FromChar('A').Width());
        Assert.Equal(2, CellContent.FromChar('\u65E5').Width());
        Assert.Equal(2, CellContent.FromRune(new Rune(0x1F389)).Width());
        Assert.Equal(2, CellContent.FromChar('\u26A1').Width());
        Assert.Equal(0, CellContent.FromChar('\x07').Width());
        Assert.Equal(1, CellContent.FromChar('\t').Width());
        Assert.Equal(1, CellContent.FromChar('\u65E5').WidthHint);

        var gearWidth = CellContent.FromChar('\u2699').Width();
        var heartWidth = CellContent.FromChar('\u2764').Width();
        Assert.Contains(gearWidth, new[] { 1, 2 });
        Assert.Equal(gearWidth, heartWidth);
    }

    [Fact]
    public void CellContentEqualityAndHashUseThePackedBits()
    {
        var a = CellContent.FromRune(new Rune(0x1F389));
        var b = new CellContent(0x1F389);
        var c = CellContent.FromGrapheme(new GraphemeId(0, 0, 1));

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.StartsWith("CellContent::Char(", a.ToString());
    }

    [Fact]
    public void StyleFlagBitsAndOperationsCoverTheFullByte()
    {
        var flags = new[]
        {
            CellStyleFlags.Bold,
            CellStyleFlags.Dim,
            CellStyleFlags.Italic,
            CellStyleFlags.Underline,
            CellStyleFlags.Blink,
            CellStyleFlags.Reverse,
            CellStyleFlags.Strikethrough,
            CellStyleFlags.Hidden
        };

        for (var index = 0; index < flags.Length; index++)
        {
            Assert.Equal(1 << index, (byte)flags[index]);
        }

        Assert.Equal(0, (byte)CellStyleFlags.None);
        Assert.Equal(byte.MaxValue, (byte)CellStyleFlags.All);
        Assert.Equal(CellStyleFlags.All, (CellStyleFlags)byte.MaxValue);
        Assert.Equal(
            CellStyleFlags.Italic,
            (CellStyleFlags.Bold | CellStyleFlags.Italic) &
            (CellStyleFlags.Italic | CellStyleFlags.Underline));
    }

    [Fact]
    public void CellAttributesPackFlagsAndTheFullLinkRange()
    {
        var flags = CellStyleFlags.Bold | CellStyleFlags.Italic;
        var attributes = new CellAttributes(flags, 42);

        Assert.Equal(0x0500_002AU, attributes.Raw);
        Assert.Equal(flags, attributes.Flags);
        Assert.Equal(42U, attributes.LinkId);
        Assert.True(attributes.HasFlag(CellStyleFlags.Bold));
        Assert.True(attributes.HasFlag(CellStyleFlags.Italic));
        Assert.False(attributes.HasFlag(CellStyleFlags.Underline));
        Assert.Equal(0U, CellAttributes.LinkIdNone);
        Assert.Equal(0x00FF_FFFFU, CellAttributes.LinkIdMax);

        var all = new CellAttributes(CellStyleFlags.All, CellAttributes.LinkIdMax);
        Assert.Equal(uint.MaxValue, all.Raw);
        Assert.Equal(CellStyleFlags.All, all.Flags);
        Assert.Equal(CellAttributes.LinkIdMax, all.LinkId);
    }

    [Fact]
    public void CellAttributesMutationsPreserveTheOtherPackedField()
    {
        var initial = new CellAttributes(CellStyleFlags.Bold, 123);
        var flagsChanged = initial.WithFlags(CellStyleFlags.Underline);
        var linkChanged = initial.WithLink(999);
        var merged = initial.MergedFlags(CellStyleFlags.Italic);

        Assert.Equal(CellStyleFlags.Underline, flagsChanged.Flags);
        Assert.Equal(123U, flagsChanged.LinkId);
        Assert.Equal(CellStyleFlags.Bold, linkChanged.Flags);
        Assert.Equal(999U, linkChanged.LinkId);
        Assert.Equal(CellStyleFlags.Bold | CellStyleFlags.Italic, merged.Flags);
        Assert.Equal(123U, merged.LinkId);
        Assert.Equal(CellAttributes.None, default);
        Assert.Equal("CellAttrs(0)", CellAttributes.None.ToString());
    }

    [Fact]
    public void CellAttributesAlwaysMaskOverflowLikeSourceReleaseBuilds()
    {
        const uint overflow = 0xABCD_EF12;
        var attributes = new CellAttributes(CellStyleFlags.Hidden, overflow);
        var updated = attributes.WithLink(overflow);

        Assert.Equal(overflow & 0x00FF_FFFF, attributes.LinkId);
        Assert.Equal(overflow & 0x00FF_FFFF, updated.LinkId);
        Assert.Equal(CellStyleFlags.Hidden, updated.Flags);
    }

    [Fact]
    public void PackedRgbaConstantsComponentsDefaultEqualityAndHashMatchSource()
    {
        Assert.Equal(0x0000_0000U, PackedRgba.Transparent.Raw);
        Assert.Equal(0x0000_00FFU, PackedRgba.Black.Raw);
        Assert.Equal(0xFFFF_FFFFU, PackedRgba.White.Raw);
        Assert.Equal(0xFF00_00FFU, PackedRgba.Red.Raw);
        Assert.Equal(0x00FF_00FFU, PackedRgba.Green.Raw);
        Assert.Equal(0x0000_FFFFU, PackedRgba.Blue.Raw);
        Assert.Equal(PackedRgba.Transparent, default);

        var color = PackedRgba.Rgba(10, 20, 30, 40);
        Assert.Equal(10, color.R);
        Assert.Equal(20, color.G);
        Assert.Equal(30, color.B);
        Assert.Equal(40, color.A);
        Assert.Equal(PackedRgba.Rgba(10, 20, 30, 255), PackedRgba.Rgb(10, 20, 30));
        Assert.Equal(color, new PackedRgba(0x0A14_1E28));
        Assert.Equal(color.GetHashCode(), new PackedRgba(0x0A14_1E28).GetHashCode());
        Assert.NotEqual(color, PackedRgba.Red);
        Assert.Equal("PackedRgba(169090600)", color.ToString());
    }

    [Fact]
    public void PackedRgbaSourceOverExactEdgeOutputsMatchRustOracle()
    {
        var cases = new[]
        {
            (PackedRgba.Rgba(255, 0, 0, 128), PackedRgba.Rgba(0, 0, 255, 255), 0x8000_7FFFU),
            (PackedRgba.Rgba(200, 10, 10, 64), PackedRgba.Rgba(10, 200, 10, 128), 0x567C_0AA0U),
            (PackedRgba.Rgba(1, 2, 3, 1), PackedRgba.Rgba(250, 251, 252, 254), 0xF9FA_FBFEU),
            (PackedRgba.Rgba(100, 0, 200, 200), PackedRgba.Rgba(0, 120, 30, 50), 0x5F06_BFD3U),
            (PackedRgba.Rgba(255, 255, 255, 1), PackedRgba.Rgba(0, 0, 0, 1), 0x8080_8002U),
            (PackedRgba.Rgba(255, 0, 0, 254), PackedRgba.Rgba(0, 0, 255, 255), 0xFE00_01FFU),
            (PackedRgba.Rgba(200, 100, 50, 128), PackedRgba.Transparent, 0xC864_3280U)
        };

        foreach (var (source, destination, expectedRaw) in cases)
        {
            Assert.Equal(expectedRaw, source.Over(destination).Raw);
        }

        var opaque = PackedRgba.Rgba(42, 84, 168, 255);
        Assert.Equal(opaque, opaque.Over(PackedRgba.Red));
        Assert.Equal(opaque, PackedRgba.Transparent.Over(opaque));
        Assert.NotEqual(
            PackedRgba.Rgba(255, 0, 0, 128).Over(PackedRgba.Rgba(0, 0, 255, 128)),
            PackedRgba.Rgba(0, 0, 255, 128).Over(PackedRgba.Rgba(255, 0, 0, 128)));
    }

    [Fact]
    public void PackedRgbaSourceOverWholeAlphaDomainMatchesRustFingerprint()
    {
        var hash = FnvOffset;
        for (var sourceAlpha = 0; sourceAlpha <= byte.MaxValue; sourceAlpha++)
        {
            for (var destinationAlpha = 0; destinationAlpha <= byte.MaxValue; destinationAlpha++)
            {
                var result = PackedRgba.Rgba(17, 113, 241, (byte)sourceAlpha).Over(
                    PackedRgba.Rgba(239, 47, 151, (byte)destinationAlpha));
                hash = UpdateFnv(hash, result.Raw);
            }
        }

        Assert.Equal(0xB98A_BE8B_BEC3_AB0EUL, hash);
    }

    [Fact]
    public void PackedRgbaOpacityUsesRustHalfAwayAndSpecialFloatSemantics()
    {
        Assert.Equal(0x0102_0301U, PackedRgba.Rgba(1, 2, 3, 1).WithOpacity(0.5f).Raw);
        Assert.Equal(0x0102_0303U, PackedRgba.Rgba(1, 2, 3, 5).WithOpacity(0.5f).Raw);
        Assert.Equal(0x0102_0380U, PackedRgba.Rgba(1, 2, 3, 255).WithOpacity(0.5f).Raw);
        Assert.Equal(0x0102_0340U, PackedRgba.Rgba(1, 2, 3, 255).WithOpacity(0.25f).Raw);
        Assert.Equal(0x0102_0300U, PackedRgba.Rgba(1, 2, 3, 200).WithOpacity(float.NaN).Raw);
        Assert.Equal(0x0102_0300U, PackedRgba.Rgba(1, 2, 3, 200).WithOpacity(float.NegativeInfinity).Raw);
        Assert.Equal(0x0102_03C8U, PackedRgba.Rgba(1, 2, 3, 200).WithOpacity(float.PositiveInfinity).Raw);
    }

    [Fact]
    public void PackedRgbaOpacityDomainMatchesRustFingerprint()
    {
        float[] opacities =
        [
            float.NegativeInfinity,
            -1f,
            -0f,
            0f,
            BitConverter.Int32BitsToSingle(0x3EAA_AAAB),
            0.5f,
            1f,
            float.PositiveInfinity,
            float.NaN
        ];

        var hash = FnvOffset;
        for (var alpha = 0; alpha <= byte.MaxValue; alpha++)
        {
            foreach (var opacity in opacities)
            {
                var result = PackedRgba.Rgba(1, 2, 3, (byte)alpha).WithOpacity(opacity);
                hash = UpdateFnv(hash, result.Raw);
            }
        }

        Assert.Equal(0x09B4_8A48_060C_846DUL, hash);
    }

    [Fact]
    public void CellDefaultContinuationAndFactoriesMatchSource()
    {
        Assert.Equal(Cell.Empty, new Cell());
        Assert.True(Cell.Empty.IsEmpty);
        Assert.False(Cell.Empty.IsContinuation);
        Assert.Equal(PackedRgba.White, Cell.Empty.Foreground);
        Assert.Equal(PackedRgba.Transparent, Cell.Empty.Background);
        Assert.Equal(CellAttributes.None, Cell.Empty.Attributes);

        Assert.True(Cell.Continuation.IsContinuation);
        Assert.False(Cell.Continuation.IsEmpty);
        Assert.Equal(PackedRgba.Transparent, Cell.Continuation.Foreground);
        Assert.Equal(PackedRgba.Transparent, Cell.Continuation.Background);
        Assert.Equal(CellAttributes.None, Cell.Continuation.Attributes);

        var fromContent = new Cell(CellContent.FromChar('A'));
        Assert.Equal(Cell.FromChar('A'), fromContent);
        Assert.Equal(new Rune('A'), fromContent.Content.AsRune());
        Assert.Equal(PackedRgba.White, fromContent.Foreground);
        Assert.Equal(PackedRgba.Transparent, fromContent.Background);

        var emoji = Cell.FromRune(new Rune(0x1F389));
        Assert.Equal(new Rune(0x1F389), emoji.Content.AsRune());
    }

    [Fact]
    public void CellBuildersPreserveUntouchedFields()
    {
        var attributes = new CellAttributes(CellStyleFlags.Bold, 42);
        var original = Cell.FromChar('A')
            .WithForeground(PackedRgba.Red)
            .WithBackground(PackedRgba.Blue)
            .WithAttributes(attributes);
        var changedCharacter = original.WithChar('Z');
        var changedRune = original.WithRune(new Rune(0x1F389));
        var changedContent = original.WithContent(CellContent.Continuation);

        Assert.Equal('Z', changedCharacter.Content.AsChar());
        Assert.Equal(original.Foreground, changedCharacter.Foreground);
        Assert.Equal(original.Background, changedCharacter.Background);
        Assert.Equal(original.Attributes, changedCharacter.Attributes);
        Assert.Equal(new Rune(0x1F389), changedRune.Content.AsRune());
        Assert.Equal(original.Foreground, changedRune.Foreground);
        Assert.True(changedContent.IsContinuation);
        Assert.Equal(original.Attributes, changedContent.Attributes);

        var overwrittenContinuation = Cell.Continuation.WithChar('A');
        Assert.False(overwrittenContinuation.IsContinuation);
        Assert.Equal(PackedRgba.Transparent, overwrittenContinuation.Foreground);
        Assert.Equal(PackedRgba.Transparent, overwrittenContinuation.Background);
    }

    [Fact]
    public void CellBitsEqualityValueEqualityHashAndWidthAgree()
    {
        var sameA = Cell.FromChar('X').WithForeground(PackedRgba.Rgb(1, 2, 3));
        var sameB = Cell.FromChar('X').WithForeground(PackedRgba.Rgb(1, 2, 3));
        var differentContent = Cell.FromChar('Y').WithForeground(PackedRgba.Rgb(1, 2, 3));
        var differentForeground = sameA.WithForeground(PackedRgba.Red);
        var differentBackground = sameA.WithBackground(PackedRgba.Blue);
        var differentAttributes = sameA.WithAttributes(new CellAttributes(CellStyleFlags.Bold, 0));

        Assert.True(sameA.BitsEqual(sameB));
        Assert.True(sameA == sameB);
        Assert.False(sameA != sameB);
        Assert.Equal(sameA.GetHashCode(), sameB.GetHashCode());
        Assert.False(sameA.BitsEqual(differentContent));
        Assert.False(sameA.BitsEqual(differentForeground));
        Assert.False(sameA.BitsEqual(differentBackground));
        Assert.False(sameA.BitsEqual(differentAttributes));
        Assert.Equal(0, Cell.Empty.WidthHint);
        Assert.Equal(0, Cell.Continuation.WidthHint);
        Assert.Equal(1, Cell.FromChar('A').WidthHint);
        Assert.Equal(3, new Cell(CellContent.FromGrapheme(new GraphemeId(1, 0, 3))).WidthHint);
    }

    [Fact]
    public void CellSignificantEqualityCompatibilityRouteRemainsAvailable()
    {
        var baseline = Cell.FromChar('A')
            .WithForeground(PackedRgba.Red)
            .WithBackground(PackedRgba.Blue)
            .WithAttributes(new CellAttributes(CellStyleFlags.Bold, 42));
        var colorsAndFlagsChanged = Cell.FromChar('A')
            .WithForeground(PackedRgba.Green)
            .WithBackground(PackedRgba.White)
            .WithAttributes(new CellAttributes(CellStyleFlags.Hidden, 42));
        var linkChanged = colorsAndFlagsChanged.WithAttributes(
            colorsAndFlagsChanged.Attributes.WithLink(43));

        Assert.True(baseline.SignificantEqual(colorsAndFlagsChanged));
        Assert.False(baseline.SignificantEqual(linkChanged));
    }

    [Fact]
    public void CellDebugProjectionNamesEverySourceField()
    {
        var text = Cell.FromChar('A').ToString();

        Assert.Contains("Cell", text, StringComparison.Ordinal);
        Assert.Contains("content", text, StringComparison.Ordinal);
        Assert.Contains("fg", text, StringComparison.Ordinal);
        Assert.Contains("bg", text, StringComparison.Ordinal);
        Assert.Contains("attrs", text, StringComparison.Ordinal);
    }

    private static ulong UpdateFnv(ulong hash, uint value)
    {
        for (var shift = 0; shift < 32; shift += 8)
        {
            var octet = (byte)(value >> shift);
            hash ^= octet;
            hash = unchecked(hash * FnvPrime);
        }

        return hash;
    }
}
