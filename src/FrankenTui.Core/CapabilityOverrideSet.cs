namespace FrankenTui.Core;

public sealed record CapabilityOverrideSet
{
    public TerminalProfile? Profile { get; init; }
    public bool? TrueColor { get; init; }
    public bool? Colors256 { get; init; }
    public bool? UnicodeEmoji { get; init; }
    public bool? SyncOutput { get; init; }
    public bool? Osc8Hyperlinks { get; init; }
    public bool? ScrollRegion { get; init; }
    public bool? InTmux { get; init; }
    public bool? InScreen { get; init; }
    public bool? InZellij { get; init; }
    public bool? InWeztermMux { get; init; }

    public TerminalCapabilities Apply(TerminalCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        return capabilities with
        {
            Profile = Profile ?? capabilities.Profile,
            TrueColor = TrueColor ?? capabilities.TrueColor,
            Colors256 = Colors256 ?? capabilities.Colors256,
            UnicodeEmoji = UnicodeEmoji ?? capabilities.UnicodeEmoji,
            SyncOutput = SyncOutput ?? capabilities.SyncOutput,
            Osc8Hyperlinks = Osc8Hyperlinks ?? capabilities.Osc8Hyperlinks,
            ScrollRegion = ScrollRegion ?? capabilities.ScrollRegion,
            InTmux = InTmux ?? capabilities.InTmux,
            InScreen = InScreen ?? capabilities.InScreen,
            InZellij = InZellij ?? capabilities.InZellij,
            InWeztermMux = InWeztermMux ?? capabilities.InWeztermMux
        };
    }
}
