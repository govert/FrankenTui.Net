using FrankenTui.Core;

namespace FrankenTui.Backend;

public readonly record struct TerminalBackendFeatures(
    bool MouseCapture = false,
    bool BracketedPaste = false,
    bool FocusEvents = false,
    bool KittyKeyboard = false)
{
    public static readonly TerminalBackendFeatures None = default;

    public bool AnyEnabled =>
        MouseCapture ||
        BracketedPaste ||
        FocusEvents ||
        KittyKeyboard;

    public TerminalBackendFeatures Sanitize(TerminalCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        var focusEventsSupported = capabilities.FocusEvents && !capabilities.InAnyMux();
        var kittyKeyboardSupported = capabilities.KittyKeyboard && !capabilities.InAnyMux();

        return this with
        {
            MouseCapture = MouseCapture && capabilities.MouseSgr,
            BracketedPaste = BracketedPaste && capabilities.BracketedPaste,
            FocusEvents = FocusEvents && focusEventsSupported,
            KittyKeyboard = KittyKeyboard && kittyKeyboardSupported
        };
    }
}
