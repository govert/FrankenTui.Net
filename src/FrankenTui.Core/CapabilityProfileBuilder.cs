namespace FrankenTui.Core;

public sealed class CapabilityProfileBuilder
{
    private TerminalCapabilities _capabilities = TerminalCapabilities.Basic() with
    {
        Profile = TerminalProfile.Custom
    };

    public CapabilityProfileBuilder TrueColor(bool enabled)
    {
        _capabilities = _capabilities with { TrueColor = enabled };
        return this;
    }

    public CapabilityProfileBuilder Profile(TerminalProfile profile)
    {
        _capabilities = _capabilities with { Profile = profile };
        return this;
    }

    public CapabilityProfileBuilder Colors256(bool enabled)
    {
        _capabilities = _capabilities with { Colors256 = enabled };
        return this;
    }

    public CapabilityProfileBuilder UnicodeBoxDrawing(bool enabled)
    {
        _capabilities = _capabilities with { UnicodeBoxDrawing = enabled };
        return this;
    }

    public CapabilityProfileBuilder UnicodeEmoji(bool enabled)
    {
        _capabilities = _capabilities with { UnicodeEmoji = enabled };
        return this;
    }

    public CapabilityProfileBuilder DoubleWidth(bool enabled)
    {
        _capabilities = _capabilities with { DoubleWidth = enabled };
        return this;
    }

    public CapabilityProfileBuilder SyncOutput(bool enabled)
    {
        _capabilities = _capabilities with { SyncOutput = enabled };
        return this;
    }

    public CapabilityProfileBuilder Hyperlinks(bool enabled)
    {
        _capabilities = _capabilities with { Osc8Hyperlinks = enabled };
        return this;
    }

    public CapabilityProfileBuilder ScrollRegion(bool enabled)
    {
        _capabilities = _capabilities with { ScrollRegion = enabled };
        return this;
    }

    public CapabilityProfileBuilder KittyKeyboard(bool enabled)
    {
        _capabilities = _capabilities with { KittyKeyboard = enabled };
        return this;
    }

    public CapabilityProfileBuilder FocusEvents(bool enabled)
    {
        _capabilities = _capabilities with { FocusEvents = enabled };
        return this;
    }

    public CapabilityProfileBuilder BracketedPaste(bool enabled)
    {
        _capabilities = _capabilities with { BracketedPaste = enabled };
        return this;
    }

    public CapabilityProfileBuilder MouseSgr(bool enabled)
    {
        _capabilities = _capabilities with { MouseSgr = enabled };
        return this;
    }

    public CapabilityProfileBuilder Osc52Clipboard(bool enabled)
    {
        _capabilities = _capabilities with { Osc52Clipboard = enabled };
        return this;
    }

    public CapabilityProfileBuilder InTmux(bool enabled)
    {
        _capabilities = _capabilities with { InTmux = enabled };
        return this;
    }

    public CapabilityProfileBuilder InScreen(bool enabled)
    {
        _capabilities = _capabilities with { InScreen = enabled };
        return this;
    }

    public CapabilityProfileBuilder InZellij(bool enabled)
    {
        _capabilities = _capabilities with { InZellij = enabled };
        return this;
    }

    public CapabilityProfileBuilder InWeztermMux(bool enabled)
    {
        _capabilities = _capabilities with { InWeztermMux = enabled };
        return this;
    }

    public TerminalCapabilities Build() => _capabilities;
}
