// SPDX-License-Identifier: MIT
// Closed source-behavior denominator for ftui-text::justification at 15cc6543.

using FrankenTui.Text;

namespace FrankenTui.Tests.Headless;

public sealed class JustificationTests
{
    [Fact]
    public void LeftDoesNotRequireJustification()
    {
        Assert.False(JustifyMode.Left.RequiresJustification);
        Assert.False(JustifyMode.Left.JustifyLastLine);
    }

    [Fact]
    public void FullRequiresJustificationButLeavesLastLineRagged()
    {
        Assert.True(JustifyMode.Full.RequiresJustification);
        Assert.False(JustifyMode.Full.JustifyLastLine);
    }

    [Fact]
    public void DistributedJustifiesEveryLine()
    {
        Assert.True(JustifyMode.Distributed.RequiresJustification);
        Assert.True(JustifyMode.Distributed.JustifyLastLine);
    }

    [Fact]
    public void JustifyModeDefaultAndDisplayMatchSource()
    {
        Assert.Equal(JustifyMode.Left, default(JustifyMode));
        Assert.Equal("left", JustifyMode.Left.ToString());
        Assert.Equal("right", JustifyMode.Right.ToString());
        Assert.Equal("center", JustifyMode.Center.ToString());
        Assert.Equal("full", JustifyMode.Full.ToString());
        Assert.Equal("distributed", JustifyMode.Distributed.ToString());
    }

    [Fact]
    public void SpaceCategoryDefaultAndDisplayMatchSource()
    {
        Assert.Equal(SpaceCategory.InterWord, default(SpaceCategory));
        Assert.Equal("inter-word", SpaceCategory.InterWord.ToString());
        Assert.Equal("inter-sentence", SpaceCategory.InterSentence.ToString());
        Assert.Equal("inter-character", SpaceCategory.InterCharacter.ToString());
    }

    [Fact]
    public void WordSpaceConstantsUseSourceSubcellScale()
    {
        var glue = GlueSpec.WordSpace;

        Assert.Equal(256u, GlueSpec.SubcellScale);
        Assert.Equal(256u, glue.NaturalSubcell);
        Assert.Equal(128u, glue.StretchSubcell);
        Assert.Equal(85u, glue.ShrinkSubcell);
    }

    [Fact]
    public void SentenceSpaceIsWiderAndMoreStretchable()
    {
        Assert.True(GlueSpec.SentenceSpace.NaturalSubcell > GlueSpec.WordSpace.NaturalSubcell);
        Assert.True(GlueSpec.SentenceSpace.StretchSubcell > GlueSpec.WordSpace.StretchSubcell);
    }

    [Fact]
    public void FrenchSpaceEqualsWordSpace()
    {
        Assert.Equal(GlueSpec.WordSpace, GlueSpec.FrenchSpace);
    }

    [Fact]
    public void InterCharacterGlueHasNoNaturalWidth()
    {
        Assert.Equal(0u, GlueSpec.InterCharacter.NaturalSubcell);
        Assert.Equal(16u, GlueSpec.InterCharacter.StretchSubcell);
        Assert.Equal(8u, GlueSpec.InterCharacter.ShrinkSubcell);
    }

    [Fact]
    public void RigidGlueHasNoElasticity()
    {
        var glue = GlueSpec.Rigid(256);

        Assert.True(glue.IsRigid);
        Assert.Equal(0u, glue.Elasticity);
    }

    [Fact]
    public void WordSpaceIsElastic()
    {
        Assert.False(GlueSpec.WordSpace.IsRigid);
        Assert.Equal(
            GlueSpec.WordSpace.StretchSubcell + GlueSpec.WordSpace.ShrinkSubcell,
            GlueSpec.WordSpace.Elasticity);
    }

    [Theory]
    [InlineData(0, 256)]
    [InlineData(128, 320)]
    [InlineData(256, 384)]
    [InlineData(1024, 384)]
    [InlineData(-128, 214)]
    [InlineData(-256, 171)]
    [InlineData(-1024, 171)]
    public void AdjustedWidthUsesFixedPointAndClamps(int ratio, uint expected)
    {
        Assert.Equal(expected, GlueSpec.WordSpace.AdjustedWidth(ratio));
    }

    [Fact]
    public void RigidAdjustedWidthIgnoresRatio()
    {
        var glue = GlueSpec.Rigid(512);

        Assert.Equal(512u, glue.AdjustedWidth(256));
        Assert.Equal(512u, glue.AdjustedWidth(-256));
        Assert.Equal(512u, glue.AdjustedWidth(int.MinValue));
    }

    [Fact]
    public void AdjustedWidthSaturatesNaturalAddition()
    {
        var glue = new GlueSpec(uint.MaxValue, 100, 0);

        Assert.Equal(uint.MaxValue, glue.AdjustedWidth(256));
    }

    [Fact]
    public void GlueDisplayAndManagedConstructionMapSourceDefault()
    {
        Assert.Equal("1.00 +0.50 -0.33", GlueSpec.WordSpace.ToString());
        Assert.Equal(GlueSpec.WordSpace, new GlueSpec());
        Assert.Equal(GlueSpec.Default, new GlueSpec());
        Assert.Equal(default, default(GlueSpec));
    }

    [Fact]
    public void NoAdjustmentHasNoSpacePenalty()
    {
        Assert.Equal(0ul, SpacePenalty.Default.Evaluate(0, SpaceCategory.InterWord));
        Assert.Equal(0ul, SpacePenalty.Default.Evaluate(0, SpaceCategory.InterCharacter));
    }

    [Theory]
    [InlineData(128, 0)]
    [InlineData(192, 0)]
    [InlineData(193, 50)]
    [InlineData(200, 50)]
    [InlineData(-192, 0)]
    [InlineData(-193, 80)]
    [InlineData(-200, 80)]
    public void WordSpacePenaltyUsesExclusiveSeventyFivePercentThreshold(int ratio, ulong expected)
    {
        Assert.Equal(expected, SpacePenalty.Default.Evaluate(ratio, SpaceCategory.InterWord));
    }

    [Theory]
    [InlineData(1, 200)]
    [InlineData(200, 250)]
    [InlineData(-200, 280)]
    public void TrackingPenaltyCombinesWithExcessiveAdjustment(int ratio, ulong expected)
    {
        Assert.Equal(expected, SpacePenalty.Default.Evaluate(ratio, SpaceCategory.InterCharacter));
    }

    [Fact]
    public void PenaltyPresetsAndConstructionMapSourceDefault()
    {
        Assert.Equal(new SpacePenalty(50, 80, 200), SpacePenalty.Default);
        Assert.Equal(new SpacePenalty(10, 20, 50), SpacePenalty.Permissive);
        Assert.Equal(new SpacePenalty(200, 300, 1000), SpacePenalty.Strict);
        Assert.Equal(SpacePenalty.Default, new SpacePenalty());
    }

    [Fact]
    public void TerminalPresetIsLeftAlignedAndRigid()
    {
        var control = JustificationControl.Terminal;

        Assert.Equal(JustifyMode.Left, control.Mode);
        Assert.True(control.WordSpace.IsRigid);
        Assert.True(control.SentenceSpace.IsRigid);
        Assert.True(control.CharacterSpace.IsRigid);
        Assert.True(control.FrenchSpacing);
        Assert.Equal((byte)0, control.MaxConsecutiveHyphens);
    }

    [Fact]
    public void ReadablePresetIsFullAndElasticWithoutTracking()
    {
        var control = JustificationControl.Readable;

        Assert.Equal(JustifyMode.Full, control.Mode);
        Assert.False(control.WordSpace.IsRigid);
        Assert.True(control.CharacterSpace.IsRigid);
        Assert.True(control.FrenchSpacing);
        Assert.Equal((byte)3, control.MaxConsecutiveHyphens);
    }

    [Fact]
    public void TypographicPresetUsesSentenceSpacingAndTracking()
    {
        var control = JustificationControl.Typographic;

        Assert.Equal(JustifyMode.Full, control.Mode);
        Assert.False(control.FrenchSpacing);
        Assert.False(control.CharacterSpace.IsRigid);
        Assert.Equal(SpacePenalty.Strict, control.Penalties);
        Assert.Equal((byte)2, control.MaxConsecutiveHyphens);
    }

    [Fact]
    public void FrenchSpacingProjectsSentenceSpaceAsWordSpace()
    {
        var control = JustificationControl.Readable;

        Assert.Equal(control.GlueFor(SpaceCategory.InterWord), control.GlueFor(SpaceCategory.InterSentence));
    }

    [Fact]
    public void NonFrenchSpacingUsesDedicatedSentenceGlue()
    {
        var control = JustificationControl.Typographic;

        Assert.Equal(GlueSpec.SentenceSpace, control.GlueFor(SpaceCategory.InterSentence));
        Assert.NotEqual(control.GlueFor(SpaceCategory.InterWord), control.GlueFor(SpaceCategory.InterSentence));
    }

    [Fact]
    public void CategoryTotalsSaturatinglySumGlueFields()
    {
        var control = JustificationControl.Readable;
        var spaces = Enumerable.Repeat(SpaceCategory.InterWord, 5).ToArray();

        Assert.Equal(5 * control.WordSpace.NaturalSubcell, control.TotalNatural(spaces));
        Assert.Equal(5 * control.WordSpace.StretchSubcell, control.TotalStretch(spaces));
        Assert.Equal(5 * control.WordSpace.ShrinkSubcell, control.TotalShrink(spaces));
    }

    [Fact]
    public void CategoryTotalsSaturateAtUintMaximum()
    {
        var control = JustificationControl.Terminal;
        control.WordSpace = new GlueSpec(uint.MaxValue, uint.MaxValue, uint.MaxValue);
        var spaces = new[] { SpaceCategory.InterWord, SpaceCategory.InterWord };

        Assert.Equal(uint.MaxValue, control.TotalNatural(spaces));
        Assert.Equal(uint.MaxValue, control.TotalStretch(spaces));
        Assert.Equal(uint.MaxValue, control.TotalShrink(spaces));
    }

    [Theory]
    [InlineData(0, 100, 100, 0)]
    [InlineData(128, 256, 100, 128)]
    [InlineData(-64, 100, 128, -128)]
    public void AdjustmentRatioMatchesSourceFixedPoint(int slack, uint stretch, uint shrink, int expected)
    {
        Assert.Equal(expected, JustificationControl.Readable.AdjustmentRatio(slack, stretch, shrink));
    }

    [Theory]
    [InlineData(100, 0, 100)]
    [InlineData(-100, 100, 0)]
    [InlineData(-300, 100, 100)]
    public void ImpossibleAdjustmentReturnsTypedAbsence(int slack, uint stretch, uint shrink)
    {
        Assert.Null(JustificationControl.Readable.AdjustmentRatio(slack, stretch, shrink));
    }

    [Fact]
    public void PositiveAdjustmentRatioClampsToIntMaximum()
    {
        Assert.Equal(int.MaxValue, JustificationControl.Readable.AdjustmentRatio(int.MaxValue, 1, 1));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(64, 156)]
    [InlineData(128, 1250)]
    [InlineData(256, 10000)]
    [InlineData(-128, 1250)]
    public void BadnessUsesTexCubicFormula(int ratio, ulong expected)
    {
        Assert.Equal(expected, JustificationControl.Badness(ratio));
    }

    [Fact]
    public void BadnessIsMonotonicAcrossNormalAdjustmentDomain()
    {
        var previous = 0ul;
        for (var ratio = 0; ratio <= 256; ratio++)
        {
            var current = JustificationControl.Badness(ratio);
            Assert.True(current >= previous);
            previous = current;
        }
    }

    [Fact]
    public void BadnessSaturatesIntermediateProducts()
    {
        Assert.True(JustificationControl.Badness(int.MaxValue) > 10_000);
        Assert.Equal(JustificationControl.Badness(int.MinValue), JustificationControl.Badness(int.MinValue));
    }

    [Fact]
    public void ZeroRatioLineDemeritsUseLinePenaltySquare()
    {
        var spaces = Enumerable.Repeat(SpaceCategory.InterWord, 3).ToArray();

        Assert.Equal(100ul, JustificationControl.Readable.LineDemerits(0, spaces, 0));
    }

    [Fact]
    public void LineDemeritsIncreaseWithRatioAndBreakPenalty()
    {
        var spaces = Enumerable.Repeat(SpaceCategory.InterWord, 3).ToArray();
        var low = JustificationControl.Readable.LineDemerits(64, spaces, 0);
        var high = JustificationControl.Readable.LineDemerits(128, spaces, 0);
        var penalized = JustificationControl.Readable.LineDemerits(64, spaces, 50);

        Assert.True(high > low);
        Assert.True(penalized > low);
    }

    [Fact]
    public void LineDemeritsSaturateExtremeBreakPenalty()
    {
        Assert.Equal(
            ulong.MaxValue,
            JustificationControl.Readable.LineDemerits(0, [], long.MinValue));
    }

    [Theory]
    [MemberData(nameof(ValidPresets))]
    public void SourcePresetsValidateCleanly(JustificationControl control)
    {
        Assert.Empty(control.Validate());
    }

    [Fact]
    public void FullModeWithRigidWordSpaceWarns()
    {
        var control = JustificationControl.Terminal;
        control.Mode = JustifyMode.Full;

        Assert.Contains(control.Validate(), warning => warning.Contains("rigid word space", StringComparison.Ordinal));
    }

    [Fact]
    public void ExcessiveWordShrinkWarns()
    {
        var control = JustificationControl.Readable;
        var word = control.WordSpace;
        word.ShrinkSubcell = word.NaturalSubcell + 1;
        control.WordSpace = word;

        Assert.Contains(control.Validate(), warning => warning.Contains("shrink exceeds natural", StringComparison.Ordinal));
    }

    [Fact]
    public void ExcessiveSentenceShrinkWarns()
    {
        var control = JustificationControl.Typographic;
        var sentence = control.SentenceSpace;
        sentence.ShrinkSubcell = sentence.NaturalSubcell + 1;
        control.SentenceSpace = sentence;

        Assert.Contains(control.Validate(), warning => warning.Contains("sentence space", StringComparison.Ordinal));
    }

    [Fact]
    public void ZeroEmergencyFactorWarns()
    {
        var control = JustificationControl.Readable;
        control.EmergencyStretchFactor = 0;

        Assert.Contains(control.Validate(), warning => warning.Contains("emergency", StringComparison.Ordinal));
    }

    [Fact]
    public void ControlDisplayAndConstructionMapSourceDefault()
    {
        Assert.Contains("mode=full", JustificationControl.Readable.ToString());
        Assert.Contains("french=true", JustificationControl.Readable.ToString());
        Assert.Equal(JustificationControl.Terminal, new JustificationControl());
        Assert.Equal(JustificationControl.Default, new JustificationControl());
    }

    [Fact]
    public void SameInputsProduceSameResults()
    {
        var control = JustificationControl.Typographic;
        var spaces = Enumerable.Repeat(SpaceCategory.InterWord, 5).ToArray();

        Assert.Equal(JustificationControl.Badness(200), JustificationControl.Badness(200));
        Assert.Equal(control.LineDemerits(150, spaces, 50), control.LineDemerits(150, spaces, 50));
        Assert.Equal(control.AdjustmentRatio(100, 200, 100), control.AdjustmentRatio(100, 200, 100));
    }

    public static TheoryData<JustificationControl> ValidPresets => new()
    {
        JustificationControl.Terminal,
        JustificationControl.Readable,
        JustificationControl.Typographic
    };
}
