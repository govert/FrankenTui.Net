namespace FrankenTui.Widgets;

public sealed record WidgetInputState(int FocusIndex = 0, int PointerColumn = 0, int PointerRow = 0, bool PointerDown = false);
