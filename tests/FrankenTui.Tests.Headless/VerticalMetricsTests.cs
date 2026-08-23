// SPDX-License-Identifier: MIT
// Closed source-behavior denominator for ftui-text::vertical_metrics at 15cc6543.

using FrankenTui.Text;

namespace FrankenTui.Tests.Headless;

public sealed class VerticalMetricsTests
{
    private const uint Scale = 256;
    private const uint LineHeight = 16 * Scale;

    [Fact]
    public void LeadingNoneResolvesToZero()
    {
        Assert.Equal(0u, LeadingSpec.None.Resolve(LineHeight));
        Assert.Equal(LeadingSpecKind.None, LeadingSpec.None.Kind);
    }

    [Fact]
    public void FixedLeadingUsesSubpixelValue()
    {
        var leading = LeadingSpec.Fixed(2 * Scale);

        Assert.Equal(2 * Scale, leading.Resolve(LineHeight));
        Assert.Equal(LeadingSpecKind.Fixed, leading.Kind);
        Assert.Equal("fixed(2.0px)", leading.ToString());
    }

    [Theory]
    [InlineData(51, 816)]
    [InlineData(128, 2048)]
    [InlineData(256, 4096)]
    public void ProportionalLeadingUsesFixedPointFloor(uint fraction, uint expected)
    {
        Assert.Equal(expected, LeadingSpec.Proportional(fraction).Resolve(LineHeight));
    }

    [Fact]
    public void LeadingConstantsMatchSourceValues()
    {
        Assert.Equal(LeadingSpec.Proportional(51), LeadingSpec.CssDefault);
        Assert.Equal(LeadingSpec.Proportional(128), LeadingSpec.OneHalf);
        Assert.Equal(LeadingSpec.Proportional(256), LeadingSpec.Double);
        Assert.Equal("none", LeadingSpec.None.ToString());
        Assert.Equal("50%", LeadingSpec.OneHalf.ToString());
    }

    [Fact]
    public void ParagraphSpacingNoneHasNoOverhead()
    {
        Assert.Equal(0u, ParagraphSpacing.None.Total());
        Assert.Equal(default, ParagraphSpacing.None);
    }

    [Fact]
    public void ParagraphSpacingOneLineUsesAfterMargin()
    {
        var spacing = ParagraphSpacing.OneLine(LineHeight);

        Assert.Equal(0u, spacing.BeforeSubpixels);
        Assert.Equal(LineHeight, spacing.AfterSubpixels);
        Assert.Equal(LineHeight, spacing.Total());
    }

    [Fact]
    public void ParagraphSpacingHalfLineFloorsOddValues()
    {
        var spacing = ParagraphSpacing.HalfLine(LineHeight + 1);

        Assert.Equal(LineHeight / 2, spacing.AfterSubpixels);
    }

    [Fact]
    public void ParagraphSpacingCustomAndAliasesRemainMutable()
    {
        var spacing = ParagraphSpacing.Custom(100, 200);

        Assert.Equal(300u, spacing.Total());
        spacing.BeforeSubpx = 300;
        spacing.AfterSubpx = 400;
        Assert.Equal(new ParagraphSpacing(300, 400), spacing);
    }

    [Fact]
    public void ParagraphSpacingTotalSaturates()
    {
        Assert.Equal(uint.MaxValue, ParagraphSpacing.Custom(uint.MaxValue, 1).Total());
    }

    [Fact]
    public void ParagraphSpacingDisplayUsesPixels()
    {
        Assert.Equal("before=1.0px after=2.0px", ParagraphSpacing.Custom(Scale, 2 * Scale).ToString());
    }

    [Fact]
    public void DisabledBaselineGridIsIdentity()
    {
        Assert.False(BaselineGrid.None.IsActive);
        Assert.Equal(42u, BaselineGrid.None.Snap(42));
    }

    [Fact]
    public void BaselineGridCombinesLineHeightAndLeading()
    {
        var grid = BaselineGrid.FromLineHeight(LineHeight, 2 * Scale);

        Assert.True(grid.IsActive);
        Assert.Equal(LineHeight + (2 * Scale), grid.IntervalSubpixels);
        Assert.Equal(0u, grid.OffsetSubpixels);
    }

    [Theory]
    [InlineData(2000, 2000)]
    [InlineData(3000, 3000)]
    [InlineData(1, 1000)]
    [InlineData(999, 1000)]
    [InlineData(1001, 2000)]
    public void BaselineGridSnapsUp(uint input, uint expected)
    {
        var grid = new BaselineGrid(1000, 0);

        Assert.Equal(expected, grid.Snap(input));
    }

    [Theory]
    [InlineData(200, 200)]
    [InlineData(500, 1200)]
    [InlineData(100, 100)]
    public void BaselineGridHonorsSourceOffsetSemantics(uint input, uint expected)
    {
        var grid = new BaselineGrid(1000, 200);

        Assert.Equal(expected, grid.Snap(input));
    }

    [Fact]
    public void BaselineGridConstructionAndSnapSaturate()
    {
        Assert.Equal(uint.MaxValue, BaselineGrid.FromLineHeight(uint.MaxValue, 1).IntervalSubpixels);
        Assert.Equal(uint.MaxValue, new BaselineGrid(1000, 0).Snap(uint.MaxValue - 1));
    }

    [Fact]
    public void CompactPolicyHasNoEnhancements()
    {
        var metrics = VerticalPolicy.Compact.Resolve(LineHeight);

        Assert.Equal(LeadingSpec.None, metrics.Leading);
        Assert.Equal(ParagraphSpacing.None, metrics.ParagraphSpacing);
        Assert.False(metrics.BaselineGrid.IsActive);
        Assert.Equal(0u, metrics.FirstLineIndentSubpixels);
    }

    [Fact]
    public void ReadablePolicyAddsLeadingAndHalfLineSpacing()
    {
        var metrics = VerticalPolicy.Readable.Resolve(LineHeight);

        Assert.Equal(LeadingSpec.CssDefault, metrics.Leading);
        Assert.False(metrics.BaselineGrid.IsActive);
        Assert.Equal((LineHeight + 816) / 2, metrics.ParagraphSpacing.AfterSubpixels);
        Assert.Equal(0u, metrics.FirstLineIndentSubpixels);
    }

    [Fact]
    public void TypographicPolicyAddsGridSpacingAndIndent()
    {
        var metrics = VerticalPolicy.Typographic.Resolve(LineHeight);

        Assert.Equal(LeadingSpec.CssDefault, metrics.Leading);
        Assert.True(metrics.BaselineGrid.IsActive);
        Assert.Equal(LineHeight + 816, metrics.BaselineGrid.IntervalSubpixels);
        Assert.Equal(LineHeight + 816, metrics.ParagraphSpacing.AfterSubpixels);
        Assert.Equal(2 * Scale, metrics.FirstLineIndentSubpixels);
    }

    [Fact]
    public void PolicyDisplayAndDefaultMatchSource()
    {
        Assert.Equal(VerticalPolicy.Compact, default(VerticalPolicy));
        Assert.Equal("compact", VerticalPolicy.Compact.ToString());
        Assert.Equal("readable", VerticalPolicy.Readable.ToString());
        Assert.Equal("typographic", VerticalPolicy.Typographic.ToString());
    }

    [Fact]
    public void EmptyParagraphHasZeroHeightEvenWithSpacing()
    {
        var metrics = new VerticalMetrics(
            LeadingSpec.Fixed(Scale),
            ParagraphSpacing.Custom(Scale, Scale),
            BaselineGrid.None,
            0);

        Assert.Equal(0u, metrics.ParagraphHeight(0, LineHeight));
    }

    [Theory]
    [InlineData(1, 4096)]
    [InlineData(3, 12288)]
    public void CompactParagraphHeightIsLineCountTimesHeight(uint lineCount, uint expected)
    {
        Assert.Equal(expected, VerticalPolicy.Compact.Resolve(LineHeight).ParagraphHeight(lineCount, LineHeight));
    }

    [Fact]
    public void ParagraphHeightIncludesInterlineLeadingOnlyBetweenLines()
    {
        var metrics = VerticalPolicy.Compact.Resolve(LineHeight);
        metrics.Leading = LeadingSpec.Fixed(2 * Scale);

        Assert.Equal((3 * LineHeight) + (4 * Scale), metrics.ParagraphHeight(3, LineHeight));
    }

    [Fact]
    public void ParagraphHeightIncludesBeforeAndAfterSpacing()
    {
        var metrics = VerticalPolicy.Compact.Resolve(LineHeight);
        metrics.ParagraphSpacing = ParagraphSpacing.Custom(Scale, Scale);

        Assert.Equal(LineHeight + (2 * Scale), metrics.ParagraphHeight(1, LineHeight));
    }

    [Fact]
    public void ActiveGridSnapsParagraphHeight()
    {
        var metrics = new VerticalMetrics(
            LeadingSpec.None,
            ParagraphSpacing.None,
            new BaselineGrid(1000, 0),
            0);

        Assert.Equal(5000u, metrics.ParagraphHeight(1, LineHeight));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 4096)]
    [InlineData(2, 8192)]
    public void CompactLinePositionsUseLineHeight(uint lineIndex, uint expected)
    {
        Assert.Equal(expected, VerticalPolicy.Compact.Resolve(LineHeight).LineY(lineIndex, LineHeight));
    }

    [Fact]
    public void LinePositionIncludesLeadingAndBeforeSpacing()
    {
        var metrics = VerticalPolicy.Compact.Resolve(LineHeight);
        metrics.Leading = LeadingSpec.Fixed(Scale);
        metrics.ParagraphSpacing = ParagraphSpacing.Custom(Scale, 0);

        Assert.Equal(Scale, metrics.LineY(0, LineHeight));
        Assert.Equal(Scale + LineHeight + Scale, metrics.LineY(1, LineHeight));
        Assert.Equal(Scale + (2 * (LineHeight + Scale)), metrics.LineY(2, LineHeight));
    }

    [Fact]
    public void ActiveGridSnapsLinePositions()
    {
        var metrics = new VerticalMetrics(
            LeadingSpec.None,
            ParagraphSpacing.Custom(100, 0),
            new BaselineGrid(1000, 0),
            0);

        Assert.Equal(1000u, metrics.LineY(0, LineHeight));
    }

    [Fact]
    public void SingleParagraphDocumentHeightMatchesContent()
    {
        var paragraphs = new nuint[] { 3 };

        Assert.Equal(3 * LineHeight, VerticalPolicy.Compact.Resolve(LineHeight).DocumentHeight(paragraphs, LineHeight));
    }

    [Fact]
    public void MultipleCompactParagraphsConcatenate()
    {
        var paragraphs = new nuint[] { 3, 2 };

        Assert.Equal(5 * LineHeight, VerticalPolicy.Compact.Resolve(LineHeight).DocumentHeight(paragraphs, LineHeight));
    }

    [Fact]
    public void DocumentHeightCollapsesAdjacentParagraphMargins()
    {
        var metrics = VerticalPolicy.Compact.Resolve(LineHeight);
        metrics.ParagraphSpacing = ParagraphSpacing.Custom(0, Scale);
        var paragraphs = new nuint[] { 3, 2 };

        Assert.Equal((5 * LineHeight) + (2 * Scale), metrics.DocumentHeight(paragraphs, LineHeight));
    }

    [Fact]
    public void EmptyDocumentHasZeroHeight()
    {
        Assert.Equal(0u, VerticalPolicy.Compact.Resolve(LineHeight).DocumentHeight([], LineHeight));
    }

    [Fact]
    public void ActiveGridSnapsFinalDocumentHeight()
    {
        var metrics = new VerticalMetrics(
            LeadingSpec.None,
            ParagraphSpacing.None,
            new BaselineGrid(1000, 0),
            0);

        Assert.Equal(5000u, metrics.DocumentHeight(new nuint[] { 1 }, LineHeight));
    }

    [Theory]
    [InlineData(12288, 4096, 3)]
    [InlineData(12289, 4096, 4)]
    [InlineData(0, 4096, 0)]
    [InlineData(4096, 0, 0)]
    public void CellRowConversionRoundsUpAndHandlesZero(uint height, uint cellHeight, ushort expected)
    {
        Assert.Equal(expected, VerticalMetrics.ToCellRows(height, cellHeight));
    }

    [Fact]
    public void CellRowConversionClampsToUshortMaximum()
    {
        Assert.Equal(ushort.MaxValue, VerticalMetrics.ToCellRows(uint.MaxValue, 1));
    }

    [Fact]
    public void SameInputsProduceSameOutputs()
    {
        var first = VerticalPolicy.Typographic.Resolve(LineHeight);
        var second = VerticalPolicy.Typographic.Resolve(LineHeight);

        Assert.Equal(first.ParagraphHeight(5, LineHeight), second.ParagraphHeight(5, LineHeight));
        Assert.Equal(first.LineY(3, LineHeight), second.LineY(3, LineHeight));
        Assert.Equal(first.BaselineGrid.Snap(1234), second.BaselineGrid.Snap(1234));
    }

    [Fact]
    public void PropertyParagraphHeightNeverDropsBelowLineBodies()
    {
        var policies = new[] { VerticalPolicy.Compact, VerticalPolicy.Readable, VerticalPolicy.Typographic };
        foreach (var policy in policies)
        {
            for (nuint lines = 1; lines <= 20; lines++)
            {
                var height = policy.Resolve(LineHeight).ParagraphHeight(lines, LineHeight);
                Assert.True(height >= ((uint)lines * LineHeight));
            }
        }
    }

    [Fact]
    public void PropertyBaselineSnapIsIdempotentAcrossSourceDomain()
    {
        var grid = BaselineGrid.FromLineHeight(16 * Scale, 4 * Scale);
        for (uint position = 0; position <= 100_000; position++)
        {
            var snapped = grid.Snap(position);
            Assert.Equal(snapped, grid.Snap(snapped));
        }
    }

    [Fact]
    public void PropertyProportionalLeadingIsMonotonic()
    {
        for (uint ratio = 1; ratio <= 512; ratio++)
        {
            var spec = LeadingSpec.Proportional(ratio);
            var previous = 0u;
            for (uint height = Scale; height <= 100 * Scale; height += Scale)
            {
                var resolved = spec.Resolve(height);
                Assert.True(resolved >= previous);
                previous = resolved;
            }
        }
    }

    [Fact]
    public void PropertyParagraphSpacingTotalIsTheSumInSourceRange()
    {
        for (uint before = 0; before <= 10_000; before += 97)
        {
            for (uint after = 0; after <= 10_000; after += 101)
            {
                Assert.Equal(before + after, ParagraphSpacing.Custom(before, after).Total());
            }
        }
    }
}
