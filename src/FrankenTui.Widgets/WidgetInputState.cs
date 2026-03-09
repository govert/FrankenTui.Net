using FrankenTui.Core;

namespace FrankenTui.Widgets;

public enum WidgetFlowDirection
{
    LeftToRight,
    RightToLeft
}

public sealed record WidgetInputState
{
    public static WidgetInputState Default { get; } = new();

    public int FocusIndex { get; init; }

    public int PointerColumn { get; init; }

    public int PointerRow { get; init; }

    public bool PointerDown { get; init; }

    public bool KeyboardNavigation { get; init; } = true;

    public IReadOnlyList<string> FocusOrder { get; init; } = [];

    public string? FocusedId { get; init; }

    public string Language { get; init; } = "en-US";

    public WidgetFlowDirection FlowDirection { get; init; } = WidgetFlowDirection.LeftToRight;

    public string LiveRegionText { get; init; } = string.Empty;

    public string? EffectiveFocusId =>
        !string.IsNullOrWhiteSpace(FocusedId)
            ? FocusedId
            : FocusOrder.Count == 0
                ? null
                : FocusOrder[NormalizeIndex(FocusIndex, FocusOrder.Count)];

    public WidgetInputState WithFocusOrder(IEnumerable<string> focusOrder)
    {
        ArgumentNullException.ThrowIfNull(focusOrder);

        var normalized = focusOrder
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalized.Length == 0)
        {
            return this with
            {
                FocusOrder = normalized,
                FocusIndex = 0,
                FocusedId = null
            };
        }

        var effectiveFocus = EffectiveFocusId;
        var focusIndex = !string.IsNullOrWhiteSpace(effectiveFocus)
            ? Array.IndexOf(normalized, effectiveFocus)
            : -1;
        if (focusIndex < 0)
        {
            focusIndex = NormalizeIndex(FocusIndex, normalized.Length);
        }

        return this with
        {
            FocusOrder = normalized,
            FocusIndex = focusIndex,
            FocusedId = normalized[focusIndex]
        };
    }

    public WidgetInputState Focus(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || FocusOrder.Count == 0)
        {
            return this with
            {
                FocusIndex = 0,
                FocusedId = null
            };
        }

        var matchIndex = Array.IndexOf(FocusOrder.ToArray(), id);
        if (matchIndex < 0)
        {
            return this;
        }

        return this with
        {
            FocusIndex = matchIndex,
            FocusedId = FocusOrder[matchIndex],
            KeyboardNavigation = true
        };
    }

    public WidgetInputState MoveFocus(int delta)
    {
        if (FocusOrder.Count == 0 || delta == 0)
        {
            return this;
        }

        var nextIndex = NormalizeIndex(FocusIndex + delta, FocusOrder.Count);
        return this with
        {
            FocusIndex = nextIndex,
            FocusedId = FocusOrder[nextIndex],
            KeyboardNavigation = true
        };
    }

    public WidgetInputState WithPointer(ushort column, ushort row, bool down) =>
        this with
        {
            PointerColumn = column,
            PointerRow = row,
            PointerDown = down,
            KeyboardNavigation = false
        };

    public WidgetInputState Announce(string? liveMessage) =>
        this with { LiveRegionText = liveMessage ?? string.Empty };

    public WidgetInputState Apply(TerminalEvent terminalEvent)
    {
        ArgumentNullException.ThrowIfNull(terminalEvent);

        return terminalEvent switch
        {
            KeyTerminalEvent keyEvent when keyEvent.Gesture.Key == TerminalKey.Tab =>
                MoveFocus(keyEvent.Gesture.Modifiers.HasFlag(TerminalModifiers.Shift) ? -1 : 1),
            KeyTerminalEvent keyEvent when keyEvent.Gesture.Key == TerminalKey.Left =>
                MoveFocus(FlowDirection == WidgetFlowDirection.RightToLeft ? 1 : -1),
            KeyTerminalEvent keyEvent when keyEvent.Gesture.Key == TerminalKey.Right =>
                MoveFocus(FlowDirection == WidgetFlowDirection.RightToLeft ? -1 : 1),
            MouseTerminalEvent mouseEvent =>
                WithPointer(mouseEvent.Gesture.Column, mouseEvent.Gesture.Row, mouseEvent.Gesture.Kind != TerminalMouseKind.Up),
            HoverTerminalEvent hoverEvent =>
                WithPointer(hoverEvent.Column, hoverEvent.Row, PointerDown),
            FocusTerminalEvent focusEvent when !focusEvent.Focused =>
                this with { PointerDown = false },
            PasteTerminalEvent pasteEvent =>
                Announce($"Paste: {TrimPreview(pasteEvent.Text)}"),
            _ => this
        };
    }

    private static int NormalizeIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        var normalized = index % count;
        return normalized < 0 ? normalized + count : normalized;
    }

    private static string TrimPreview(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "(empty)";
        }

        var preview = value.ReplaceLineEndings(" ").Trim();
        return preview.Length <= 24 ? preview : $"{preview[..24]}...";
    }
}
