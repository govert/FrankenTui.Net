using FrankenTui.Tty;

namespace FrankenTui.Tests.Headless;

public sealed class HostDivergenceTests
{
    [Fact]
    public void HostMatrixCarriesStructuredValidationMetadata()
    {
        Assert.All(
            TerminalHostMatrix.Profiles,
            profile =>
            {
                Assert.False(string.IsNullOrWhiteSpace(profile.ValidationStatus));
                Assert.NotEmpty(profile.EvidenceSources);
                Assert.NotEmpty(profile.KnownDivergences);
                Assert.NotEmpty(profile.CapabilityOverrides);
            });
    }

    [Fact]
    public void HostLookupIsStableAcrossNamedPlatforms()
    {
        var linux = TerminalHostMatrix.ForPlatform("linux");
        var windows = TerminalHostMatrix.ForPlatform("windows");

        Assert.Equal("unix-tty", linux.Host);
        Assert.Equal("conpty", windows.Host);
        Assert.Equal("validated-external", windows.ValidationStatus);
        Assert.Contains("windows-local-interactive", windows.EvidenceSources);
        Assert.Contains(
            "In-repo PTY transcript assertions remain Unix-only; Windows transcript refreshes currently come from external hosts and CI.",
            windows.KnownDivergences);
        Assert.Contains(
            windows.KnownDivergences,
            static divergence => divergence.Contains("crossterm-compat", StringComparison.Ordinal));
        Assert.Contains(
            windows.CapabilityOverrides,
            static policy => policy.Contains("Unix-native backend assumptions", StringComparison.Ordinal));
    }
}
