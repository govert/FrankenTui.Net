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
        Assert.Contains("PTY transcript assertions are currently Unix-only.", windows.KnownDivergences);
    }
}
