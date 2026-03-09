using FrankenTui.Core;

namespace FrankenTui.Tests.Headless;

public sealed class TerminalCapabilitiesTests
{
    [Fact]
    public void DetectRecognizesTmuxAndDisablesMuxUnsafeFeatures()
    {
        var capabilities = TerminalCapabilities.Detect(new Dictionary<string, string?>
        {
            ["TERM"] = "screen-256color",
            ["TMUX"] = "/tmp/tmux-1000/default,123,0"
        });

        Assert.Equal(TerminalProfile.Tmux, capabilities.Profile);
        Assert.True(capabilities.InTmux);
        Assert.False(capabilities.UseSyncOutput());
        Assert.False(capabilities.UseHyperlinks());
        Assert.True(capabilities.NeedsPassthroughWrap());
    }

    [Fact]
    public void DetectRecognizesKittyAndRetainsRichCapabilities()
    {
        var capabilities = TerminalCapabilities.Detect(new Dictionary<string, string?>
        {
            ["TERM"] = "xterm-kitty",
            ["KITTY_WINDOW_ID"] = "17"
        });

        Assert.Equal(TerminalProfile.Kitty, capabilities.Profile);
        Assert.True(capabilities.TrueColor);
        Assert.True(capabilities.UseHyperlinks());
        Assert.True(capabilities.UseSyncOutput());
    }

    [Fact]
    public void CapabilityProfileBuilderBuildsCustomProfiles()
    {
        var capabilities = new CapabilityProfileBuilder()
            .Profile(TerminalProfile.Custom)
            .TrueColor(true)
            .Colors256(true)
            .Hyperlinks(true)
            .InTmux(true)
            .Build();

        Assert.Equal(TerminalProfile.Custom, capabilities.Profile);
        Assert.True(capabilities.TrueColor);
        Assert.True(capabilities.Colors256);
        Assert.True(capabilities.InTmux);
        Assert.False(capabilities.UseHyperlinks());
    }
}
