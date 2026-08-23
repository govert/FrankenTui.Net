// SPDX-License-Identifier: MIT
// Closed source-behavior denominator for ftui-text::layout_policy at 15cc6543.

using FrankenTui.Text;

namespace FrankenTui.Tests.Headless;

public sealed class LayoutPolicyTests
{
    [Fact]
    public void TiersUseSourceQualityOrdering()
    {
        Assert.True(LayoutTier.Emergency < LayoutTier.Fast);
        Assert.True(LayoutTier.Fast < LayoutTier.Balanced);
        Assert.True(LayoutTier.Balanced < LayoutTier.Quality);
    }

    [Theory]
    [MemberData(nameof(DegradationCases))]
    public void TierDegradesOneStep(LayoutTier tier, LayoutTier? expected)
    {
        Assert.Equal(expected, tier.Degrade());
    }

    [Fact]
    public void QualityDegradationChainReachesEmergency()
    {
        Assert.Equal(
            [LayoutTier.Quality, LayoutTier.Balanced, LayoutTier.Fast, LayoutTier.Emergency],
            LayoutTier.Quality.DegradationChain());
    }

    [Fact]
    public void FastDegradationChainIncludesEmergency()
    {
        Assert.Equal([LayoutTier.Fast, LayoutTier.Emergency], LayoutTier.Fast.DegradationChain());
    }

    [Fact]
    public void EmergencyDegradationChainContainsOnlyItself()
    {
        Assert.Equal([LayoutTier.Emergency], LayoutTier.Emergency.DegradationChain());
    }

    [Fact]
    public void TierDefaultAndDisplayMatchSource()
    {
        Assert.Equal(LayoutTier.Balanced, default(LayoutTier));
        Assert.Equal("emergency", LayoutTier.Emergency.ToString());
        Assert.Equal("fast", LayoutTier.Fast.ToString());
        Assert.Equal("balanced", LayoutTier.Balanced.ToString());
        Assert.Equal("quality", LayoutTier.Quality.ToString());
    }

    [Fact]
    public void TerminalCapabilitiesAlwaysSupportEmergencyFastAndBalanced()
    {
        Assert.True(RuntimeCapability.Terminal.SupportsTier(LayoutTier.Emergency));
        Assert.True(RuntimeCapability.Terminal.SupportsTier(LayoutTier.Fast));
        Assert.True(RuntimeCapability.Terminal.SupportsTier(LayoutTier.Balanced));
        Assert.False(RuntimeCapability.Terminal.SupportsTier(LayoutTier.Quality));
    }

    [Fact]
    public void FullCapabilitiesSupportQuality()
    {
        Assert.True(RuntimeCapability.Full.SupportsTier(LayoutTier.Quality));
    }

    [Theory]
    [MemberData(nameof(BestTierCases))]
    public void CapabilityBestTierMatchesSource(RuntimeCapability capability, LayoutTier expected)
    {
        Assert.Equal(expected, capability.BestTier());
    }

    [Fact]
    public void DefaultCapabilitiesAreTerminalLike()
    {
        var capability = default(RuntimeCapability);

        Assert.False(capability.ProportionalFonts);
        Assert.False(capability.SubpixelPositioning);
        Assert.False(capability.HyphenationAvailable);
        Assert.False(capability.TrackingSupport);
        Assert.False(capability.LigatureSupport);
        Assert.Equal((nuint)0, capability.MaxParagraphWords);
    }

    [Fact]
    public void CapabilityDisplayUsesSourceLabelsAndLowercaseBooleans()
    {
        Assert.Equal(
            "proportional=true subpixel=true hyphen=true tracking=true ligature=true",
            RuntimeCapability.Full.ToString());
    }

    [Fact]
    public void EmergencyResolvesWithoutDegradation()
    {
        var resolved = LayoutPolicy.Emergency.Resolve(RuntimeCapability.Terminal).Unwrap();

        Assert.Equal(LayoutTier.Emergency, resolved.EffectiveTier);
        Assert.False(resolved.Degraded);
        Assert.False(resolved.UseOptimalBreaking);
        Assert.False(resolved.UseHyphenation);
        Assert.False(resolved.IsJustified);
    }

    [Fact]
    public void FastResolvesWithTerminalCapabilities()
    {
        var resolved = LayoutPolicy.Fast.Resolve(RuntimeCapability.Terminal).Unwrap();

        Assert.Equal(LayoutTier.Fast, resolved.EffectiveTier);
        Assert.False(resolved.Degraded);
        Assert.False(resolved.UseOptimalBreaking);
    }

    [Fact]
    public void BalancedResolvesWithTerminalCapabilities()
    {
        var resolved = LayoutPolicy.Balanced.Resolve(RuntimeCapability.Terminal).Unwrap();

        Assert.Equal(LayoutTier.Balanced, resolved.EffectiveTier);
        Assert.False(resolved.Degraded);
        Assert.True(resolved.UseOptimalBreaking);
    }

    [Fact]
    public void QualityDegradesToBestTerminalTier()
    {
        var resolved = LayoutPolicy.Quality.Resolve(RuntimeCapability.Terminal).Unwrap();

        Assert.True(resolved.Degraded);
        Assert.Equal(LayoutTier.Quality, resolved.RequestedTier);
        Assert.Equal(LayoutTier.Balanced, resolved.EffectiveTier);
    }

    [Fact]
    public void QualityUsesFullTypographicFeaturesWhenSupported()
    {
        var resolved = LayoutPolicy.Quality.Resolve(RuntimeCapability.Full).Unwrap();

        Assert.Equal(LayoutTier.Quality, resolved.EffectiveTier);
        Assert.False(resolved.Degraded);
        Assert.True(resolved.IsJustified);
        Assert.True(resolved.UseHyphenation);
        Assert.True(resolved.UseOptimalBreaking);
    }

    [Fact]
    public void DisabledDegradationReturnsTypedCapabilityError()
    {
        var policy = LayoutPolicy.Quality;
        policy.AllowDegradation = false;

        var result = policy.Resolve(RuntimeCapability.Terminal);

        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(new PolicyError(LayoutTier.Quality, LayoutTier.Balanced), result.Error);
    }

    [Fact]
    public void JustifyOverrideIsAppliedAfterTierPreset()
    {
        var policy = LayoutPolicy.Balanced;
        policy.JustifyOverride = JustifyMode.Center;

        var resolved = policy.Resolve(RuntimeCapability.Terminal).Unwrap();

        Assert.Equal(JustifyMode.Center, resolved.Justification.Mode);
    }

    [Fact]
    public void VerticalOverrideIsAppliedAfterTierPreset()
    {
        var policy = LayoutPolicy.Fast;
        policy.VerticalOverride = VerticalPolicy.Typographic;

        var resolved = policy.Resolve(RuntimeCapability.Terminal).Unwrap();

        Assert.True(resolved.Vertical.BaselineGrid.IsActive);
    }

    [Fact]
    public void CustomLineHeightFlowsToResolvedMetrics()
    {
        var policy = LayoutPolicy.Balanced;
        policy.LineHeightSubpixels = 20 * 256;

        var resolved = policy.Resolve(RuntimeCapability.Terminal).Unwrap();

        Assert.Equal(20u * 256, policy.EffectiveLineHeight);
        Assert.Equal(20u * 256, resolved.LineHeightSubpixels);
    }

    [Fact]
    public void ZeroLineHeightUsesSixteenPixelDefault()
    {
        Assert.Equal(16u * 256, LayoutPolicy.Balanced.EffectiveLineHeight);
    }

    [Fact]
    public void MissingTrackingSupportMakesCharacterGlueRigid()
    {
        var capability = RuntimeCapability.Full;
        capability.TrackingSupport = false;

        var resolved = LayoutPolicy.Quality.Resolve(capability).Unwrap();

        Assert.True(resolved.Justification.CharacterSpace.IsRigid);
    }

    [Fact]
    public void MonospaceCapabilitiesMakeAllSpacesRigid()
    {
        var resolved = LayoutPolicy.Balanced.Resolve(RuntimeCapability.Terminal).Unwrap();

        Assert.True(resolved.Justification.WordSpace.IsRigid);
        Assert.True(resolved.Justification.SentenceSpace.IsRigid);
        Assert.True(resolved.Justification.CharacterSpace.IsRigid);
        Assert.Equal(GlueSpec.SubcellScale, resolved.Justification.WordSpace.NaturalSubcell);
    }

    [Fact]
    public void MissingDictionaryDisablesHyphenation()
    {
        var capability = RuntimeCapability.Full;
        capability.HyphenationAvailable = false;

        Assert.False(LayoutPolicy.Quality.Resolve(capability).Unwrap().UseHyphenation);
    }

    [Fact]
    public void FastPolicyIsNotJustified()
    {
        Assert.False(LayoutPolicy.Fast.Resolve(RuntimeCapability.Terminal).Unwrap().IsJustified);
    }

    [Fact]
    public void QualityPolicyIsJustified()
    {
        Assert.True(LayoutPolicy.Quality.Resolve(RuntimeCapability.Full).Unwrap().IsJustified);
    }

    [Fact]
    public void FastFeatureSummaryUsesGreedyWrappingOnly()
    {
        var features = LayoutPolicy.Fast.Resolve(RuntimeCapability.Terminal).Unwrap().FeatureSummary();

        Assert.Contains("greedy-wrapping", features);
        Assert.DoesNotContain("justified", features);
        Assert.DoesNotContain("hyphenation", features);
    }

    [Fact]
    public void QualityFeatureSummaryContainsAllTypographicRoutes()
    {
        var features = LayoutPolicy.Quality.Resolve(RuntimeCapability.Full).Unwrap().FeatureSummary();

        Assert.Equal(
            ["optimal-breaking", "justified", "hyphenation", "baseline-grid", "first-line-indent", "tracking"],
            features);
    }

    [Fact]
    public void ResolvedDisplayOmitsDegradedWhenUnchanged()
    {
        var text = LayoutPolicy.Balanced.Resolve(RuntimeCapability.Terminal).Unwrap().ToString();

        Assert.Contains("balanced", text);
        Assert.DoesNotContain("degraded", text);
    }

    [Fact]
    public void ResolvedDisplayIncludesDegradedWhenFallbackOccurred()
    {
        Assert.Contains(
            "degraded",
            LayoutPolicy.Quality.Resolve(RuntimeCapability.Terminal).Unwrap().ToString());
    }

    [Fact]
    public void PolicyErrorDisplayNamesRequestedAndAvailableTiers()
    {
        var error = new PolicyError(LayoutTier.Quality, LayoutTier.Fast);

        Assert.Equal("requested tier 'quality' not supported; best available is 'fast'", error.ToString());
    }

    [Fact]
    public void DefaultPolicyIsBalancedAndAllowsDegradation()
    {
        Assert.Equal(LayoutPolicy.Balanced, default(LayoutPolicy));
        Assert.Equal(LayoutPolicy.Balanced, new LayoutPolicy());
        Assert.True(default(LayoutPolicy).AllowDegradation);
    }

    [Fact]
    public void PolicyDisplayUsesSourceFormat()
    {
        Assert.Equal("tier=quality degrade=true", LayoutPolicy.Quality.ToString());
        Assert.Equal("tier=emergency degrade=false", LayoutPolicy.Emergency.ToString());
    }

    [Fact]
    public void SameInputsProduceValueEqualResolution()
    {
        var first = LayoutPolicy.Quality.Resolve(RuntimeCapability.Full).Unwrap();
        var second = LayoutPolicy.Quality.Resolve(RuntimeCapability.Full).Unwrap();

        Assert.Equal(first, second);
    }

    [Fact]
    public void SameDegradationProducesValueEqualResolution()
    {
        var first = LayoutPolicy.Quality.Resolve(RuntimeCapability.Terminal).Unwrap();
        var second = LayoutPolicy.Quality.Resolve(RuntimeCapability.Terminal).Unwrap();

        Assert.Equal(first, second);
    }

    [Fact]
    public void FastWithFullCapabilitiesStaysFast()
    {
        var resolved = LayoutPolicy.Fast.Resolve(RuntimeCapability.Full).Unwrap();

        Assert.Equal(LayoutTier.Fast, resolved.EffectiveTier);
        Assert.False(resolved.Degraded);
    }

    [Fact]
    public void LeftOverrideDisablesJustificationWithoutChangingQualityTier()
    {
        var policy = LayoutPolicy.Quality;
        policy.JustifyOverride = JustifyMode.Left;

        var resolved = policy.Resolve(RuntimeCapability.Full).Unwrap();

        Assert.False(resolved.IsJustified);
        Assert.Equal(LayoutTier.Quality, resolved.EffectiveTier);
    }

    [Fact]
    public void FailureUnwrapPreservesTypedExplanation()
    {
        var policy = LayoutPolicy.Quality;
        policy.AllowDegradation = false;
        var result = policy.Resolve(RuntimeCapability.Terminal);

        var exception = Assert.Throws<InvalidOperationException>(() => result.Unwrap());
        Assert.Contains("quality", exception.Message);
        Assert.Contains("balanced", exception.Message);
    }

    public static TheoryData<LayoutTier, LayoutTier?> DegradationCases => new()
    {
        { LayoutTier.Quality, LayoutTier.Balanced },
        { LayoutTier.Balanced, LayoutTier.Fast },
        { LayoutTier.Fast, LayoutTier.Emergency },
        { LayoutTier.Emergency, null }
    };

    public static TheoryData<RuntimeCapability, LayoutTier> BestTierCases => new()
    {
        { RuntimeCapability.Terminal, LayoutTier.Balanced },
        { RuntimeCapability.Full, LayoutTier.Quality },
        { RuntimeCapability.Web, LayoutTier.Quality }
    };
}
