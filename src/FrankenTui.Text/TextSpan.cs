using FrankenTui.Style;

namespace FrankenTui.Text;

public sealed record TextSpan(string Text, UiStyle? Style = null);
