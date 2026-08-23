// SPDX-License-Identifier: Apache-2.0
// Source-equivalent tests for .external/frankentui/crates/ftui-text/src/bidi.rs.
// Upstream basis: 15cc6543f76b814394c590f9e7719dedd6684e4c.

using FrankenTui.Text;

namespace FrankenTui.Tests.Headless;

/// <summary>
/// Closed source denominator: the first 45 facts correspond one-for-one with
/// the 45 tests behind ftui-text's optional <c>bidi</c> Cargo feature.
/// The remaining facts exercise the explicit .NET coordinate adaptation and
/// a pinned differential corpus beyond the source unit assertions.
/// </summary>
public sealed class BidiTests
{
    [Fact]
    public void ReorderEmpty() => Assert.Equal(string.Empty, Bidi.Reorder(string.Empty));

    [Fact]
    public void ReorderPureLeftToRight()
        => Assert.Equal("Hello, world!", Bidi.Reorder("Hello, world!"));

    [Fact]
    public void ReorderPureRightToLeftHebrew()
        => Assert.Equal("םולש", Bidi.Reorder("שלום"));

    [Fact]
    public void ReorderPureRightToLeftArabic()
        => Assert.Equal("ابحرم", Bidi.Reorder("مرحبا"));

    [Fact]
    public void ReorderMixedLeftToRightAndRightToLeft()
        => Assert.Equal("Hello םולש World", Bidi.Reorder("Hello שלום World", TextDirection.LeftToRight));

    [Fact]
    public void ReorderForcedLeftToRight()
        => Assert.Equal("Hello", Bidi.Reorder("Hello", TextDirection.LeftToRight));

    [Fact]
    public void ReorderForcedRightToLeftOnLeftToRightText()
        => Assert.Equal("ABC", Bidi.Reorder("ABC", TextDirection.RightToLeft));

    [Fact]
    public void ReorderWithNumbers()
        => Assert.Contains("123", Bidi.Reorder("שלום 123"));

    [Fact]
    public void ReorderWithLeftToRightMark()
    {
        string result = Bidi.Reorder("A\u200eB");
        Assert.Contains('A', result);
        Assert.Contains('B', result);
    }

    [Fact]
    public void ReorderWithRightToLeftMark()
    {
        string result = Bidi.Reorder("A\u200fB");
        Assert.Contains('A', result);
        Assert.Contains('B', result);
    }

    [Fact]
    public void HasRightToLeftEmpty() => Assert.False(Bidi.HasRightToLeft(string.Empty));

    [Fact]
    public void HasRightToLeftPureLeftToRight() => Assert.False(Bidi.HasRightToLeft("Hello, world!"));

    [Fact]
    public void HasRightToLeftHebrew() => Assert.True(Bidi.HasRightToLeft("שלום"));

    [Fact]
    public void HasRightToLeftArabic() => Assert.True(Bidi.HasRightToLeft("مرحبا"));

    [Fact]
    public void HasRightToLeftMixed() => Assert.True(Bidi.HasRightToLeft("Hello שלום"));

    [Fact]
    public void HasRightToLeftWithRightToLeftMark() => Assert.True(Bidi.HasRightToLeft("A\u200fB"));

    [Fact]
    public void HasRightToLeftNumbersOnly() => Assert.False(Bidi.HasRightToLeft("12345"));

    [Fact]
    public void ResolveLevelsEmpty() => Assert.Empty(Bidi.ResolveLevels(string.Empty));

    [Fact]
    public void ResolveLevelsPureLeftToRight()
    {
        IReadOnlyList<BidiLevel> levels = Bidi.ResolveLevels("ABC");
        Assert.NotEmpty(levels);
        Assert.All(levels, static level => Assert.True(level.IsLeftToRight));
    }

    [Fact]
    public void ResolveLevelsPureRightToLeft()
    {
        IReadOnlyList<BidiLevel> levels = Bidi.ResolveLevels("שלום");
        Assert.NotEmpty(levels);
        Assert.All(levels, static level => Assert.True(level.IsRightToLeft));
    }

    [Fact]
    public void ParagraphLevelEmpty()
        => Assert.Equal(TextDirection.LeftToRight, Bidi.ParagraphLevel(string.Empty));

    [Fact]
    public void ParagraphLevelLeftToRight()
        => Assert.Equal(TextDirection.LeftToRight, Bidi.ParagraphLevel("Hello"));

    [Fact]
    public void ParagraphLevelRightToLeft()
        => Assert.Equal(TextDirection.RightToLeft, Bidi.ParagraphLevel("שלום"));

    [Fact]
    public void ParagraphLevelMixedStartsLeftToRight()
        => Assert.Equal(TextDirection.LeftToRight, Bidi.ParagraphLevel("Hello שלום"));

    [Fact]
    public void ParagraphLevelMixedStartsRightToLeft()
        => Assert.Equal(TextDirection.RightToLeft, Bidi.ParagraphLevel("שלום Hello"));

    [Fact]
    public void RightToLeftDetectionCoversCoreRanges()
    {
        Assert.True(Bidi.HasRightToLeft("א"));
        Assert.True(Bidi.HasRightToLeft("ا"));
        Assert.True(Bidi.HasRightToLeft("\u200f"));
        Assert.False(Bidi.HasRightToLeft("A"));
        Assert.False(Bidi.HasRightToLeft("1"));
        Assert.False(Bidi.HasRightToLeft(" "));
    }

    [Fact]
    public void RightToLeftDetectionCoversAdditionalRanges()
    {
        int[] samples = [0xfb1d, 0xfb50, 0xfe70, 0x10800, 0x10840, 0x10900, 0x10920, 0x10a00, 0x10b00, 0x1ee00];
        Assert.All(samples, static codePoint =>
            Assert.True(Bidi.HasRightToLeft(char.ConvertFromUtf32(codePoint)), $"Expected RTL for U+{codePoint:X}."));
    }

    [Fact]
    public void RunIsEmpty()
    {
        Assert.True(new BidiRun(2, 2, BidiLevel.LeftToRight, TextDirection.LeftToRight).IsEmpty);
        Assert.False(new BidiRun(2, 3, BidiLevel.LeftToRight, TextDirection.LeftToRight).IsEmpty);
    }

    [Fact]
    public void SegmentBaseDirection()
    {
        Assert.Equal(TextDirection.LeftToRight, new BidiSegment("Hello").BaseDirection);
        Assert.Equal(TextDirection.RightToLeft, new BidiSegment("שלום").BaseDirection);
    }

    [Fact]
    public void SegmentComputeHelpersEmpty()
    {
        var segment = new BidiSegment(string.Empty);
        Assert.Empty(segment.Runs);
        Assert.Empty(segment.VisualToLogical);
    }

    [Fact]
    public void SegmentEmpty()
    {
        var segment = new BidiSegment(string.Empty);
        Assert.True(segment.IsEmpty);
        Assert.Equal(0, segment.Length);
        Assert.Empty(segment.Runs);
        Assert.Empty(segment.VisualToLogical);
        Assert.Empty(segment.LogicalToVisual);
    }

    [Fact]
    public void SegmentLeftToRightOnly()
    {
        var segment = new BidiSegment("Hello");
        Assert.Equal(5, segment.Length);
        Assert.Equal(["H", "e", "l", "l", "o"], segment.Chars.Select(static rune => rune.ToString()));
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(i, segment.VisualPosition(i));
            Assert.Equal(i, segment.LogicalPosition(i));
            Assert.False(segment.IsRightToLeft(i));
        }
        BidiRun run = Assert.Single(segment.Runs);
        Assert.Equal(TextDirection.LeftToRight, run.Direction);
        Assert.Equal(0, run.Start);
        Assert.Equal(5, run.End);
        Assert.Equal("Hello", segment.VisualString());
    }

    [Fact]
    public void SegmentRightToLeftOnly()
    {
        var segment = new BidiSegment("שלום");
        Assert.Equal(4, segment.Length);
        Assert.Equal([3, 2, 1, 0], segment.VisualToLogical);
        Assert.Equal([3, 2, 1, 0], segment.LogicalToVisual);
        for (int i = 0; i < 4; i++) Assert.True(segment.IsRightToLeft(i));
        Assert.Equal(TextDirection.RightToLeft, Assert.Single(segment.Runs).Direction);
        Assert.Equal("םולש", segment.VisualString());
    }

    [Fact]
    public void SegmentMixedLeftToRightAndRightToLeft()
    {
        var segment = new BidiSegment("Hello שלום World", TextDirection.LeftToRight);
        Assert.Equal(16, segment.Length);
        Assert.Equal(0, segment.VisualPosition(0));
        Assert.Equal(5, segment.VisualPosition(5));
        Assert.Equal(9, segment.VisualPosition(6));
        Assert.Equal(6, segment.VisualPosition(9));
        Assert.Equal(11, segment.VisualPosition(11));
        Assert.False(segment.IsRightToLeft(0));
        Assert.True(segment.IsRightToLeft(6));
        Assert.True(segment.IsRightToLeft(9));
        Assert.False(segment.IsRightToLeft(11));
        Assert.True(segment.Runs.Count >= 2);
    }

    [Fact]
    public void SegmentNumbersInRightToLeft()
    {
        var segment = new BidiSegment("שלום 123");
        Assert.Contains("123", segment.VisualString());
        Assert.False(segment.IsRightToLeft(5));
    }

    [Fact]
    public void SegmentBracketsPairing()
    {
        const string text = "א(ב)ג";
        var segment = new BidiSegment(text, TextDirection.RightToLeft);
        Assert.Equal(text.EnumerateRunes().Count(), segment.VisualString().EnumerateRunes().Count());
        Assert.Equal(Enumerable.Range(0, segment.Length), segment.VisualToLogical.Order());
    }

    [Fact]
    public void SegmentExplicitMarkers()
    {
        var segment = new BidiSegment("A\u200eB\u200fC");
        Assert.False(segment.IsEmpty);
        Assert.Equal(Enumerable.Range(0, segment.Length), segment.VisualToLogical.Order());
    }

    [Fact]
    public void SegmentCursorMovement()
    {
        var segment = new BidiSegment("Hello שלום", TextDirection.LeftToRight);
        int position = 0;
        for (int i = 0; i < 6; i++) position = segment.MoveRight(position);
        Assert.Equal(6, segment.VisualCursorPosition(position));
        position = segment.MoveLeft(position);
        Assert.Equal(5, segment.VisualCursorPosition(position));
        for (int i = 0; i < 5; i++) position = segment.MoveLeft(position);
        Assert.Equal(0, segment.VisualCursorPosition(position));
        Assert.Equal(0, segment.VisualCursorPosition(segment.MoveLeft(position)));
    }

    [Fact]
    public void SegmentCursorAtBoundary()
    {
        var segment = new BidiSegment("ABC");
        int last = segment.MoveRight(segment.MoveRight(segment.MoveRight(0)));
        Assert.Equal(3, segment.VisualCursorPosition(last));
        Assert.Equal(last, segment.MoveRight(last));
    }

    [Fact]
    public void SegmentDoubleToggle()
    {
        var segment = new BidiSegment("Hello שלום World", TextDirection.LeftToRight);
        for (int start = 0; start < segment.Length; start++)
        {
            int right = segment.MoveRight(start);
            if (right != start) Assert.Equal(start, segment.MoveLeft(right));
        }
    }

    [Fact]
    public void SegmentVisualStringMatchesReorder()
    {
        const string text = "Hello שלום World";
        var segment = new BidiSegment(text, TextDirection.LeftToRight);
        Assert.Equal(Bidi.Reorder(text, TextDirection.LeftToRight), segment.VisualString());
    }

    [Fact]
    public void SegmentRunCoverage()
    {
        var segment = new BidiSegment("Hello שלום World", TextDirection.LeftToRight);
        Assert.Equal(segment.Length, segment.Runs.Sum(static run => run.Length));
        for (int i = 1; i < segment.Runs.Count; i++)
            Assert.Equal(segment.Runs[i - 1].End, segment.Runs[i].Start);
        Assert.Equal(0, segment.Runs[0].Start);
        Assert.Equal(segment.Length, segment.Runs[^1].End);
    }

    [Fact]
    public void SegmentPermutationValidity()
    {
        string[] texts = ["Hello", "שלום", "Hello שלום World", "ABC 123 مرحبا", string.Empty];
        foreach (string text in texts)
        {
            var segment = new BidiSegment(text);
            Assert.Equal(segment.Length, segment.VisualToLogical.Count);
            Assert.Equal(segment.Length, segment.LogicalToVisual.Count);
            for (int i = 0; i < segment.Length; i++)
            {
                Assert.Equal(i, segment.LogicalToVisual[segment.VisualToLogical[i]]);
                Assert.Equal(i, segment.VisualToLogical[segment.LogicalToVisual[i]]);
            }
        }
    }

    [Fact]
    public void SegmentCharacterAtVisual()
    {
        var segment = new BidiSegment("ABC");
        Assert.Equal("A", segment.CharacterAtVisual(0)?.ToString());
        Assert.Equal("B", segment.CharacterAtVisual(1)?.ToString());
        Assert.Equal("C", segment.CharacterAtVisual(2)?.ToString());
        Assert.Null(segment.CharacterAtVisual(3));
    }

    [Fact]
    public void CursorMovementRightToLeftInsertionPoint()
    {
        var segment = new BidiSegment("דהו");
        Assert.Equal(TextDirection.RightToLeft, segment.BaseDirection);
        Assert.Equal(2, segment.MoveRight(3));
        Assert.Equal(3, segment.MoveLeft(3));
    }

    [Fact]
    public void CoordinateSystemsRemainExplicitAcrossUtf8Utf16AndScalars()
    {
        const string text = "A😀א";
        var segment = new BidiSegment(text);
        Assert.Equal(3, segment.Length); // Unicode scalars.
        Assert.Equal(4, text.Length); // UTF-16 code units.
        Assert.Equal(7, Bidi.ResolveLevels(text).Count); // UTF-8 bytes, matching unicode-bidi.
    }

    [Fact]
    public void MalformedUtf16IsRejectedInsteadOfSilentlyReplaced()
    {
        Assert.Throws<ArgumentException>(() => Bidi.Reorder("\ud800"));
        Assert.Throws<ArgumentException>(() => new BidiSegment("\udc00"));
    }

    [Fact]
    public void UnicodePropertyDataIsPinnedToUpstreamVersion()
        => Assert.Equal(new Version(16, 0, 0), Bidi.UnicodeDataVersion);

    [Fact]
    public void NeutralStandaloneDirectionIsTheAutoCompatibilityProjection()
    {
        Assert.Equal(Bidi.Reorder("שלום"), Bidi.Reorder("שלום", TextDirection.Neutral));
        Assert.Equal(Bidi.ResolveLevels("שלום"), Bidi.ResolveLevels("שלום", TextDirection.Neutral));
    }

    [Fact]
    public void SegmentUsesNullForAutoAndRejectsNeutralAsAForcedBase()
    {
        Assert.Equal(TextDirection.RightToLeft, new BidiSegment("שלום", null).BaseDirection);
        Assert.Throws<ArgumentOutOfRangeException>(() => new BidiSegment("שלום", TextDirection.Neutral));
    }

    [Fact]
    public void DifferentialProbeCorpusMatchesPinnedUnicodeBidiOracle()
    {
        var cases = new[]
        {
            new Probe("A\u200eB", "A\u200eB", [0, 0, 0, 0, 0], "A\u200eB", [0, 0, 0], TextDirection.LeftToRight),
            new Probe("A\u200fB", "A\u200fB", [0, 1, 1, 1, 0], "A\u200fB", [0, 1, 0], TextDirection.LeftToRight),
            new Probe("A\u202aB\u202cC", "A\u202aB\u202cC", [0, 0, 0, 0, 2, 2, 2, 2, 0], "A\u202aB\u202cC", [0, 0, 2, 2, 0], TextDirection.LeftToRight),
            new Probe("A\u2067שלום\u2069B", "A\u2067םולש\u2069B", [0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0], "A\u2067םולש\u2069B", [0, 0, 1, 1, 1, 1, 0, 0], TextDirection.LeftToRight),
            new Probe("a\u0301א", "a\u0301א", [0, 0, 0, 1, 1], "a\u0301א", [0, 0, 1], TextDirection.LeftToRight),
            new Probe("A😀א", "A😀א", [0, 0, 0, 0, 0, 1, 1], "A😀א", [0, 0, 1], TextDirection.LeftToRight),
            new Probe("abc\nשלום", "abc\nםולש", [0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1], "abc\nםולש", [0, 0, 0, 0, 1, 1, 1, 1], TextDirection.LeftToRight),
            new Probe("א(123)ב", "ב)123(א", [1, 1, 1, 2, 2, 2, 1, 1, 1], "ב)123(א", [1, 1, 2, 2, 2, 1, 1], TextDirection.RightToLeft),
        };

        foreach (Probe probe in cases)
        {
            Assert.Equal(probe.Reordered, Bidi.Reorder(probe.Text));
            Assert.Equal(probe.ByteLevels, Bidi.ResolveLevels(probe.Text).Select(static level => level.Value).ToArray());
            var segment = new BidiSegment(probe.Text);
            Assert.Equal(probe.Visual, segment.VisualString());
            Assert.Equal(probe.ScalarLevels, segment.Levels.Select(static level => level.Value).ToArray());
            Assert.Equal(probe.BaseDirection, Bidi.ParagraphLevel(probe.Text));
        }
    }

    private sealed record Probe(
        string Text,
        string Reordered,
        byte[] ByteLevels,
        string Visual,
        byte[] ScalarLevels,
        TextDirection BaseDirection);
}
