// SPDX-License-Identifier: Apache-2.0
// Source-equivalent tests for .external/frankentui/crates/ftui-text/src/rope.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using System.Text;
using FrankenTui.Widgets.TextAreaInternals;

namespace FrankenTui.Tests.Headless;

public sealed class RopeTests
{
    [Fact]
    public void RopeBasicCounts()
    {
        var rope = Rope.FromText("Hello, world!");
        Assert.Equal(13, rope.LenChars());
        Assert.Equal(1, rope.LenLines());
    }

    [Fact]
    public void RopeMultilineLines()
    {
        var rope = Rope.FromText("Line 1\nLine 2\nLine 3");
        Assert.Equal(3, rope.LenLines());
        Assert.Equal("Line 1\n", rope.LineWithTerminator(0));
        Assert.Equal("Line 3", rope.LineWithTerminator(2));
    }

    [Fact]
    public void RopeInsertRemoveReplace()
    {
        var rope = Rope.FromText("Hello!");
        rope.Insert(5, ", world");
        Assert.Equal("Hello, world!", rope.ToString());

        rope.Remove(5..12);
        Assert.Equal("Hello!", rope.ToString());

        rope.Replace("Replaced");
        Assert.Equal("Replaced", rope.ToString());
    }

    [Fact]
    public void RopeAppendClear()
    {
        var rope = Rope.FromText("Hi");
        rope.Append(" there");
        Assert.Equal("Hi there", rope.ToString());
        rope.Clear();
        Assert.True(rope.IsEmpty());
        Assert.Equal(1, rope.LenLines());
    }

    [Fact]
    public void RopeCharByteConversions()
    {
        var rope = Rope.FromText("a😀b");
        Assert.Equal(3, rope.LenChars());
        Assert.Equal(0, rope.CharToByte(0));
        Assert.Equal(1, rope.CharToByte(1));
        Assert.Equal(3, rope.ByteToChar(rope.LenBytes()));
    }

    [Fact]
    public void RopeLineColConversions()
    {
        var rope = Rope.FromText("ab\ncde\n");
        Assert.Equal((1, 1), rope.ByteToScalarLineCol(4));
        Assert.Equal(5, rope.LineColToByte(1, 2));
    }

    [Fact]
    public void RopeGraphemeOps()
    {
        var rope = Rope.FromText("e\u0301");
        Assert.Equal(1, rope.GraphemeCount());
        rope.InsertGrapheme(1, "!");
        Assert.Equal("e\u0301!", rope.ToString());

        rope = Rope.FromText("a😀b");
        rope.RemoveGraphemeRange(1..2);
        Assert.Equal("ab", rope.ToString());
    }

    [Fact]
    public void InsertRemoveRoundtrip()
    {
        var random = new Random(0x5eed);
        for (int iteration = 0; iteration < 200; iteration++)
        {
            string original = RandomText(random, 24);
            string inserted = RandomText(random, 12);
            var rope = Rope.FromText(original);
            int position = random.Next(201);
            position = Math.Min(position, rope.LenChars());
            int insertedLength = inserted.EnumerateRunes().Count();

            rope.Insert(position, inserted);
            rope.Remove(position, position + insertedLength);

            Assert.Equal(original, rope.ToString());
        }
    }

    [Fact]
    public void LineCountMatchesNewlines()
    {
        var random = new Random(0x1a1e);
        for (int iteration = 0; iteration < 200; iteration++)
        {
            string text = RandomLfText(random, 40);
            Assert.Equal(text.Count(character => character == '\n') + 1, Rope.FromText(text).LenLines());
        }
    }

    [Fact]
    public void EmptyRopeProperties()
    {
        var rope = new Rope();
        Assert.True(rope.IsEmpty());
        Assert.Equal(0, rope.LenBytes());
        Assert.Equal(0, rope.LenChars());
        Assert.Equal(1, rope.LenLines());
        Assert.Equal(0, rope.GraphemeCount());
        Assert.Equal(string.Empty, rope.ToString());
    }

    [Fact]
    public void EmptyRopeLineAccess()
    {
        var rope = new Rope();
        Assert.NotNull(rope.LineWithTerminator(0));
        Assert.Null(rope.LineWithTerminator(1));
    }

    [Fact]
    public void EmptyRopeSlice()
    {
        var rope = new Rope();
        Assert.Equal(string.Empty, rope.Slice(0..0));
        Assert.Equal(string.Empty, rope.Slice(..));
    }

    [Fact]
    public void EmptyRopeConversions()
    {
        var rope = new Rope();
        Assert.Equal(0, rope.CharToByte(0));
        Assert.Equal(0, rope.ByteToChar(0));
        Assert.Equal(0, rope.CharToLine(0));
        Assert.Equal(0, rope.LineToChar(0));
    }

    [Fact]
    public void FromStrImpl()
    {
        Rope rope = "hello";
        Assert.Equal("hello", rope.ToString());
    }

    [Fact]
    public void FromStringImpl()
    {
        string text = string.Concat("hel", "lo");
        Rope rope = text;
        Assert.Equal("hello", rope.ToString());
    }

    [Fact]
    public void FromStrParse()
    {
        Rope rope = Rope.Parse("hello");
        Assert.Equal("hello", rope.ToString());
    }

    [Fact]
    public void DisplayImpl()
    {
        var rope = Rope.FromText("hello world");
        Assert.Equal("hello world", $"{rope}");
    }

    [Fact]
    public void LineOutOfBounds()
    {
        var rope = Rope.FromText("a\nb");
        Assert.NotNull(rope.LineWithTerminator(0));
        Assert.NotNull(rope.LineWithTerminator(1));
        Assert.Null(rope.LineWithTerminator(2));
        Assert.Null(rope.LineWithTerminator(100));
    }

    [Fact]
    public void TrailingNewlineCreatesEmptyLastLine()
    {
        var rope = Rope.FromText("a\n");
        Assert.Equal(2, rope.LenLines());
        Assert.Equal("a\n", rope.LineWithTerminator(0));
        Assert.Equal(string.Empty, rope.LineWithTerminator(1));
    }

    [Fact]
    public void MultipleNewlines()
    {
        Assert.Equal(4, Rope.FromText("\n\n\n").LenLines());
    }

    [Fact]
    public void LinesIterator()
    {
        string[] lines = Rope.FromText("a\nb\nc").Lines().ToArray();
        Assert.Equal(3, lines.Length);
        Assert.Equal("a\n", lines[0]);
        Assert.Equal("b\n", lines[1]);
        Assert.Equal("c", lines[2]);
    }

    [Fact]
    public void SliceBasic()
    {
        var rope = Rope.FromText("hello world");
        Assert.Equal("hello", rope.Slice(0..5));
        Assert.Equal("world", rope.Slice(6..11));
        Assert.Equal("world", rope.Slice(6..));
        Assert.Equal("hello", rope.Slice(..5));
    }

    [Fact]
    public void SliceOutOfBoundsReturnsEmpty()
    {
        Assert.Equal(string.Empty, Rope.FromText("hi").Slice(100..200));
    }

    [Fact]
    public void InsertAtBeginning()
    {
        var rope = Rope.FromText("world");
        rope.Insert(0, "hello ");
        Assert.Equal("hello world", rope.ToString());
    }

    [Fact]
    public void InsertAtEnd()
    {
        var rope = Rope.FromText("hello");
        rope.Insert(5, " world");
        Assert.Equal("hello world", rope.ToString());
    }

    [Fact]
    public void InsertBeyondLengthClamps()
    {
        var rope = Rope.FromText("hi");
        rope.Insert(100, "!");
        Assert.Equal("hi!", rope.ToString());
    }

    [Fact]
    public void InsertEmptyString()
    {
        var rope = Rope.FromText("hello");
        rope.Insert(2, string.Empty);
        Assert.Equal("hello", rope.ToString());
    }

    [Fact]
    public void RemoveEmptyRange()
    {
        var rope = Rope.FromText("hello");
        rope.Remove(2..2);
        Assert.Equal("hello", rope.ToString());
    }

    [Fact]
    public void RemoveEntireContent()
    {
        var rope = Rope.FromText("hello");
        rope.Remove(..);
        Assert.True(rope.IsEmpty());
    }

    [Fact]
    public void RemoveInvertedRangeIsNoop()
    {
        var rope = Rope.FromText("hello");
        rope.Remove(3, 1);
        Assert.Equal("hello", rope.ToString());
    }

    [Fact]
    public void GraphemeInsertAtBeginning()
    {
        var rope = Rope.FromText("bc");
        rope.InsertGrapheme(0, "a");
        Assert.Equal("abc", rope.ToString());
    }

    [Fact]
    public void GraphemeInsertWithCombining()
    {
        var rope = Rope.FromText("e\u0301x");
        Assert.Equal(2, rope.GraphemeCount());
        rope.InsertGrapheme(1, "y");
        Assert.Equal("e\u0301yx", rope.ToString());
    }

    [Fact]
    public void GraphemeRemoveRange()
    {
        var rope = Rope.FromText("abcd");
        rope.RemoveGraphemeRange(1..3);
        Assert.Equal("ad", rope.ToString());
    }

    [Fact]
    public void GraphemeRemoveEmptyRange()
    {
        var rope = Rope.FromText("abc");
        rope.RemoveGraphemeRange(1..1);
        Assert.Equal("abc", rope.ToString());
    }

    [Fact]
    public void GraphemesReturnsCorrectList()
    {
        IReadOnlyList<string> graphemes = Rope.FromText("ae\u0301b").Graphemes();
        Assert.Equal(3, graphemes.Count);
        Assert.Equal("a", graphemes[0]);
        Assert.Equal("e\u0301", graphemes[1]);
        Assert.Equal("b", graphemes[2]);
    }

    [Fact]
    public void CharToByteWithMultibyte()
    {
        var rope = Rope.FromText("a😀b");
        Assert.Equal(0, rope.CharToByte(0));
        Assert.Equal(1, rope.CharToByte(1));
        Assert.Equal(5, rope.CharToByte(2));
    }

    [Fact]
    public void ByteToCharClamps()
    {
        Assert.Equal(2, Rope.FromText("hi").ByteToChar(100));
    }

    [Fact]
    public void CharToByteClamps()
    {
        Assert.Equal(2, Rope.FromText("hi").CharToByte(100));
    }

    [Fact]
    public void LineToCharOutOfBounds()
    {
        var rope = Rope.FromText("a\nb");
        Assert.Equal(0, rope.LineToChar(0));
        Assert.Equal(2, rope.LineToChar(1));
        Assert.Equal(3, rope.LineToChar(100));
    }

    [Fact]
    public void ByteToLineColBasic()
    {
        Assert.Equal((1, 1), Rope.FromText("abc\ndef").ByteToScalarLineCol(5));
    }

    [Fact]
    public void LineColToByteBasic()
    {
        Assert.Equal(5, Rope.FromText("abc\ndef").LineColToByte(1, 1));
    }

    [Fact]
    public void CharsIterator()
    {
        int[] values = Rope.FromText("ab").Chars().Select(rune => rune.Value).ToArray();
        Assert.Equal(new[] { (int)'a', (int)'b' }, values);
    }

    [Fact]
    public void NormalizeRangeBasic()
    {
        var rope = Rope.FromText("0123456789");
        rope.Remove(2, 5);
        Assert.Equal("0156789", rope.ToString());

        rope = Rope.FromText("0123456789");
        rope.Remove(0, 10);
        Assert.Empty(rope.ToString());

        rope = Rope.FromText("0123456789");
        rope.Remove(..);
        Assert.Empty(rope.ToString());
    }

    [Fact]
    public void NormalizeRangeClampsToMax()
    {
        var rope = Rope.FromText("hello");
        rope.Remove(0, 100);
        Assert.True(rope.IsEmpty());

        rope = Rope.FromText("hello");
        rope.Remove(50, 100);
        Assert.Equal("hello", rope.ToString());
    }

    [Fact]
    public void NormalizeRangeInvertedBecomesEmpty()
    {
        var rope = Rope.FromText("0123456789");
        rope.Remove(5, 2);
        Assert.Equal("0123456789", rope.ToString());
    }

    [Fact]
    public void NormalizeRangeInclusive()
    {
        var rope = Rope.FromText("0123456789");
        rope.Remove(1, 3, endInclusive: true);
        Assert.Equal("0456789", rope.ToString());
    }

    [Fact]
    public void AppendThenLenGrows()
    {
        var random = new Random(0xa99e);
        for (int iteration = 0; iteration < 128; iteration++)
        {
            string text = RandomText(random, 50);
            string suffix = RandomText(random, 50);
            var rope = Rope.FromText(text);
            int before = rope.LenChars();
            int suffixLength = suffix.EnumerateRunes().Count();
            rope.Append(suffix);
            Assert.Equal(before + suffixLength, rope.LenChars());
        }
    }

    [Fact]
    public void ReplaceYieldsNewContent()
    {
        var random = new Random(0x2e91);
        for (int iteration = 0; iteration < 128; iteration++)
        {
            var rope = Rope.FromText(RandomText(random, 50));
            string replacement = RandomText(random, 50);
            rope.Replace(replacement);
            Assert.Equal(replacement, rope.ToString());
        }
    }

    [Fact]
    public void ClearAlwaysEmpty()
    {
        var random = new Random(0xc1ea);
        for (int iteration = 0; iteration < 128; iteration++)
        {
            var rope = Rope.FromText(RandomText(random, 100));
            rope.Clear();
            Assert.True(rope.IsEmpty());
            Assert.Equal(0, rope.LenBytes());
            Assert.Equal(0, rope.LenChars());
        }
    }

    [Fact]
    public void DisplayMatchesToString()
    {
        var random = new Random(0xd15a);
        for (int iteration = 0; iteration < 128; iteration++)
        {
            var rope = Rope.FromText(RandomText(random, 100));
            Assert.Equal(rope.ToString(), $"{rope}");
        }
    }

    [Fact]
    public void CharByteRoundtrip()
    {
        var random = new Random(0xc4ab);
        for (int iteration = 0; iteration < 200; iteration++)
        {
            var rope = Rope.FromText(RandomText(random, 50, minimumLength: 1));
            int charIndex = Math.Min(random.Next(50), rope.LenChars());
            Assert.Equal(charIndex, rope.ByteToChar(rope.CharToByte(charIndex)));
        }
    }

    [Fact]
    public void GraphemeCountLeqCharCount()
    {
        var random = new Random(0x6a9e);
        for (int iteration = 0; iteration < 200; iteration++)
        {
            var rope = Rope.FromText(RandomText(random, 100));
            Assert.True(rope.GraphemeCount() <= rope.LenChars());
        }
    }

    // Managed adaptation evidence beyond the 52-test upstream denominator.

    [Fact]
    public void CompatibilityLineStripsTerminatorWhileSourceProjectionPreservesIt()
    {
        var rope = Rope.FromText("a\r\nb");
        Assert.Equal("a", rope.Line(0));
        Assert.Equal("a\r\n", rope.LineWithTerminator(0));
    }

    [Fact]
    public void CompatibilityByteColumnIsDistinctFromSourceScalarColumn()
    {
        var rope = Rope.FromText("éx\n");
        Assert.Equal((0, 2), rope.ByteToLineCol(2));
        Assert.Equal((0, 1), rope.ByteToScalarLineCol(2));
    }

    [Fact]
    public void Utf16AdaptersSnapToUnicodeScalarBoundary()
    {
        const string text = "a😀b";
        Assert.Equal(1, Rope.ByteOffsetToCharOffset(text, 2));
        Assert.Equal(1, Rope.CharOffsetToByteOffset(text, 2));
        Assert.Equal(5, Rope.CharOffsetToByteOffset(text, 3));
    }

    [Fact]
    public void UnicodeLineTerminatorsArePreservedAndCounted()
    {
        const string text = "a\r\nb\rc\vd\fe\u0085f\u2028g\u2029h";
        var rope = Rope.FromText(text);

        Assert.Equal(8, rope.LenLines());
        Assert.Equal(new[] { "a\r\n", "b\r", "c\v", "d\f", "e\u0085", "f\u2028", "g\u2029", "h" },
            rope.Lines());
        Assert.Equal(text, rope.ToString());
    }

    [Fact]
    public void InvalidUtf16IsRejectedInsteadOfSilentlyReplacingScalars()
    {
        Assert.Throws<ArgumentException>(() => new Rope("\uD800"));
        Assert.False(Rope.TryParse("\uDC00", out _));
        Assert.Throws<ArgumentException>(() => Rope.FromText("ok").Insert(1, "\uD800"));
    }

    [Fact]
    public void ByteMutationHelpersRequireUtf8ScalarBoundaries()
    {
        var rope = Rope.FromText("a😀b");
        Assert.Throws<ArgumentException>(() => rope.InsertAtByte(2, "!"));
        Assert.Throws<ArgumentException>(() => rope.RemoveBytes(1, 2));
        Assert.Throws<ArgumentException>(() => rope.SliceBytes(2, 5));

        rope.InsertAtByte(5, "!");
        Assert.Equal("a😀!b", rope.ToString());
        rope.RemoveBytes(1, 5);
        Assert.Equal("a!b", rope.ToString());
    }

    [Fact]
    public void CloneAndTryParseProduceIndependentRopes()
    {
        Assert.True(Rope.TryParse("hello", out Rope? parsed));
        Rope clone = parsed!.Clone();
        clone.Append("!");
        Assert.Equal("hello", parsed.ToString());
        Assert.Equal("hello!", clone.ToString());
    }

    [Fact]
    public void InclusiveSliceAndRemovalMapRangeBounds()
    {
        var rope = Rope.FromText("abcdef");
        Assert.Equal("bcd", rope.SliceInclusive(1, 3));
        rope.RemoveGraphemeRange(1, 3, endInclusive: true);
        Assert.Equal("aef", rope.ToString());
    }

    [Fact]
    public void CrLfIsOneLineBreakButTwoUnicodeScalars()
    {
        var rope = Rope.FromText("a\r\nb");
        Assert.Equal(4, rope.LenChars());
        Assert.Equal(3, rope.LineToChar(1));
        Assert.Equal((0, 2), rope.ByteToScalarLineCol(2));
        Assert.Equal((1, 0), rope.ByteToScalarLineCol(3));
        Assert.Equal(2, rope.LineColToByte(0, 2));
        Assert.Equal(3, rope.LineColToByte(0, 3));
    }

    [Fact]
    public void InteriorUtf8ByteMapsToContainingUnicodeScalar()
    {
        var rope = Rope.FromText("a😀b");
        Assert.Equal(1, rope.ByteToChar(1));
        Assert.Equal(1, rope.ByteToChar(2));
        Assert.Equal(1, rope.ByteToChar(4));
        Assert.Equal(2, rope.ByteToChar(5));
    }

    private static string RandomText(Random random, int maximumLength, int minimumLength = 0)
    {
        Rune[] alphabet =
        [
            new('a'), new('Z'), new(' '), new('\n'), new('\r'), new('\t'), new('\0'),
            new(0x00e9), new(0x0301), new(0x03bb), new(0x0416), new(0x4e2d),
            new(0x1f600), new(0x1f469), new(0x200d), new(0x1f4bb), new(0x2028),
        ];

        int length = random.Next(minimumLength, maximumLength + 1);
        var builder = new StringBuilder();
        for (int i = 0; i < length; i++)
            builder.Append(alphabet[random.Next(alphabet.Length)].ToString());
        return builder.ToString();
    }

    private static string RandomLfText(Random random, int maximumLength)
    {
        Rune[] alphabet =
        [
            new('a'), new('Z'), new(' '), new('\n'), new('\t'), new(0x00e9),
            new(0x0301), new(0x03bb), new(0x4e2d), new(0x1f600),
        ];

        int length = random.Next(maximumLength + 1);
        var builder = new StringBuilder();
        for (int i = 0; i < length; i++)
            builder.Append(alphabet[random.Next(alphabet.Length)].ToString());
        return builder.ToString();
    }
}
