using System.Text;
using FrankenTui.Render;

namespace FrankenTui.Backend;

public static class TerminalFeatureControl
{
    public static string Transition(TerminalBackendFeatures current, TerminalBackendFeatures next)
    {
        var builder = new StringBuilder();
        AppendToggle(
            builder,
            current.MouseCapture,
            next.MouseCapture,
            "\u001b[?1003h\u001b[?1006h",
            "\u001b[?1003l\u001b[?1006l");
        AppendToggle(builder, current.BracketedPaste, next.BracketedPaste, "\u001b[?2004h", "\u001b[?2004l");
        AppendToggle(builder, current.FocusEvents, next.FocusEvents, "\u001b[?1004h", "\u001b[?1004l");
        AppendToggle(builder, current.KittyKeyboard, next.KittyKeyboard, AnsiBuilder.KittyKeyboardEnable(), AnsiBuilder.KittyKeyboardDisable());
        return builder.ToString();
    }

    private static void AppendToggle(StringBuilder builder, bool current, bool next, string enable, string disable)
    {
        if (current == next)
        {
            return;
        }

        builder.Append(next ? enable : disable);
    }
}
