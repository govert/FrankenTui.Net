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

    public bool HostFocused { get; init; } = true;

    public IReadOnlyList<string> FocusOrder { get; init; } = [];

    public IReadOnlyList<WidgetFocusTrap> FocusTrapStack { get; init; } = [];

    public string? FocusedId { get; init; }

    public string? DeferredFocusId { get; init; }

    public string Language { get; init; } = "en-US";

    public WidgetFlowDirection FlowDirection { get; init; } = WidgetFlowDirection.LeftToRight;

    public string LiveRegionText { get; init; } = string.Empty;

    public bool IsFocusTrapped => FocusTrapStack.Count > 0;

    public IReadOnlyList<string> ActiveFocusOrder =>
        FocusTrapStack.Count == 0 ? FocusOrder : FocusTrapStack[^1].FocusOrder;

    public string? ActiveTrapId => FocusTrapStack.Count == 0 ? null : FocusTrapStack[^1].TrapId;

    public string? LogicalFocusId =>
        FocusTrapStack.Count > 0
            ? ResolveTarget(FocusTrapStack[^1].SelectedFocusId, ActiveFocusOrder) ?? ActiveFocusOrder.FirstOrDefault()
            : HostFocused
                ? ResolveTarget(FocusedId, ActiveFocusOrder)
                : ResolveTarget(DeferredFocusId, ActiveFocusOrder);

    public string? EffectiveFocusId => HostFocused ? LogicalFocusId : null;

    public WidgetInputState WithFocusOrder(IEnumerable<string> focusOrder)
    {
        ArgumentNullException.ThrowIfNull(focusOrder);

        var normalized = NormalizeIds(focusOrder);
        var repairedFocused = HostFocused
            ? ResolveTarget(FocusedId, normalized)
            : null;
        var repairedDeferred = HostFocused
            ? null
            : ResolveTarget(DeferredFocusId, normalized);

        if (normalized.Length == 0)
        {
            return this with
            {
                FocusOrder = normalized,
                FocusTrapStack = [],
                FocusIndex = 0,
                FocusedId = null,
                DeferredFocusId = null
            };
        }

        if (repairedFocused is null && repairedDeferred is null)
        {
            var fallback = normalized[NormalizeIndex(FocusIndex, normalized.Length)];
            if (HostFocused)
            {
                repairedFocused = fallback;
            }
            else
            {
                repairedDeferred = fallback;
            }
        }

        return this with
        {
            FocusOrder = normalized,
            FocusTrapStack = [],
            FocusIndex = NormalizeIndex(IndexOf(normalized, repairedFocused ?? repairedDeferred), normalized.Length),
            FocusedId = repairedFocused,
            DeferredFocusId = repairedDeferred
        };
    }

    public WidgetInputState Focus(string? id)
    {
        var activeOrder = ActiveFocusOrder;
        if (activeOrder.Count == 0)
        {
            return this with
            {
                FocusIndex = 0,
                FocusedId = null,
                DeferredFocusId = null
            };
        }

        var target = ResolveTarget(id, activeOrder);
        if (target is null)
        {
            return this;
        }

        var baseIndex = IndexOf(FocusOrder, target);
        return this with
        {
            FocusIndex = baseIndex >= 0 ? baseIndex : FocusIndex,
            FocusTrapStack = UpdateActiveTrapSelection(target),
            FocusedId = HostFocused ? target : null,
            DeferredFocusId = HostFocused ? null : target,
            KeyboardNavigation = true
        };
    }

    public WidgetInputState MoveFocus(int delta)
    {
        var activeOrder = ActiveFocusOrder;
        if (activeOrder.Count == 0 || delta == 0)
        {
            return this;
        }

        var current = LogicalFocusId;
        var currentIndex = current is null
            ? (delta > 0 ? -1 : 0)
            : NormalizeIndex(IndexOf(activeOrder, current), activeOrder.Count);
        var nextIndex = current is null
            ? (delta > 0 ? 0 : activeOrder.Count - 1)
            : NormalizeIndex(currentIndex + delta, activeOrder.Count);
        return Focus(activeOrder[nextIndex]);
    }

    public WidgetInputState PushFocusTrap(IEnumerable<string> focusOrder, string? preferredFocus = null, string? trapId = null)
    {
        ArgumentNullException.ThrowIfNull(focusOrder);

        var normalized = NormalizeIds(focusOrder);
        if (normalized.Length == 0)
        {
            return this;
        }

        var returnFocus = LogicalFocusId;
        var nextTarget = ResolveTarget(preferredFocus, normalized) ?? normalized[0];
        return this with
        {
            FocusTrapStack = FocusTrapStack.Concat([new WidgetFocusTrap(trapId, normalized, returnFocus, nextTarget)]).ToArray(),
            FocusedId = HostFocused ? nextTarget : null,
            DeferredFocusId = HostFocused ? null : nextTarget,
            KeyboardNavigation = true
        };
    }

    public WidgetInputState PopFocusTrap()
    {
        if (FocusTrapStack.Count == 0)
        {
            return this;
        }

        var poppedTrap = FocusTrapStack[^1];
        var nextStack = FocusTrapStack.Take(FocusTrapStack.Count - 1).ToArray();
        var nextActiveOrder = nextStack.Length == 0 ? FocusOrder : nextStack[^1].FocusOrder;
        var nextTarget = ResolveRestoreTarget(nextStack, poppedTrap.ReturnFocusId, nextActiveOrder);
        var baseIndex = IndexOf(FocusOrder, nextTarget);

        return this with
        {
            FocusTrapStack = nextStack,
            FocusIndex = FocusOrder.Count == 0
                ? 0
                : NormalizeIndex(baseIndex >= 0 ? baseIndex : FocusIndex, FocusOrder.Count),
            FocusedId = HostFocused ? nextTarget : null,
            DeferredFocusId = HostFocused ? null : nextTarget,
            KeyboardNavigation = true
        };
    }

    public WidgetInputState RemoveFocusTrap(string trapId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trapId);

        var trapIndex = -1;
        for (var index = 0; index < FocusTrapStack.Count; index++)
        {
            if (string.Equals(FocusTrapStack[index].TrapId, trapId, StringComparison.Ordinal))
            {
                trapIndex = index;
                break;
            }
        }

        if (trapIndex < 0)
        {
            return this;
        }

        if (trapIndex == FocusTrapStack.Count - 1)
        {
            return PopFocusTrap();
        }

        var traps = FocusTrapStack.ToArray();
        var replacementTarget = ResolveReplacementTargetForRemoval(traps, trapIndex);
        var nextStack = traps
            .Where((_, index) => index != trapIndex)
            .ToArray();

        if (trapIndex < nextStack.Length)
        {
            nextStack[trapIndex] = nextStack[trapIndex] with { ReturnFocusId = replacementTarget };
        }

        var activeOrder = nextStack[^1].FocusOrder;
        var activeTarget = ResolveTarget(nextStack[^1].SelectedFocusId, activeOrder) ?? activeOrder.FirstOrDefault();
        var baseIndex = IndexOf(FocusOrder, activeTarget);
        return this with
        {
            FocusTrapStack = nextStack,
            FocusIndex = FocusOrder.Count == 0
                ? 0
                : NormalizeIndex(baseIndex >= 0 ? baseIndex : FocusIndex, FocusOrder.Count),
            FocusedId = HostFocused ? activeTarget : null,
            DeferredFocusId = HostFocused ? null : activeTarget,
            KeyboardNavigation = true
        };
    }

    public WidgetInputState UpdateFocusTrap(string trapId, IEnumerable<string> focusOrder, string? preferredFocus = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trapId);
        ArgumentNullException.ThrowIfNull(focusOrder);

        var trapIndex = -1;
        for (var index = 0; index < FocusTrapStack.Count; index++)
        {
            if (string.Equals(FocusTrapStack[index].TrapId, trapId, StringComparison.Ordinal))
            {
                trapIndex = index;
                break;
            }
        }

        if (trapIndex < 0)
        {
            return this;
        }

        var normalized = NormalizeIds(focusOrder);
        if (normalized.Length == 0)
        {
            return RemoveFocusTrap(trapId);
        }

        var traps = FocusTrapStack.ToArray();
        var selected = ResolveTarget(traps[trapIndex].SelectedFocusId, normalized)
            ?? ResolveTarget(preferredFocus, normalized)
            ?? normalized[0];
        traps[trapIndex] = traps[trapIndex] with
        {
            FocusOrder = normalized,
            SelectedFocusId = selected
        };

        if (trapIndex + 1 < traps.Length && ResolveTarget(traps[trapIndex + 1].ReturnFocusId, normalized) is null)
        {
            traps[trapIndex + 1] = traps[trapIndex + 1] with { ReturnFocusId = selected };
        }

        var activeOrder = traps[^1].FocusOrder;
        var activeTarget = ResolveTarget(traps[^1].SelectedFocusId, activeOrder) ?? activeOrder.FirstOrDefault();
        var baseIndex = IndexOf(FocusOrder, activeTarget);
        return this with
        {
            FocusTrapStack = traps,
            FocusIndex = FocusOrder.Count == 0
                ? 0
                : NormalizeIndex(baseIndex >= 0 ? baseIndex : FocusIndex, FocusOrder.Count),
            FocusedId = HostFocused ? activeTarget : null,
            DeferredFocusId = HostFocused ? null : activeTarget,
            KeyboardNavigation = true
        };
    }

    public WidgetInputState ApplyHostFocus(bool focused)
    {
        if (HostFocused == focused)
        {
            return focused ? this : this with { PointerDown = false };
        }

        if (!focused)
        {
            return this with
            {
                HostFocused = false,
                FocusedId = null,
                DeferredFocusId = LogicalFocusId,
                PointerDown = false
            };
        }

        var restoreTarget =
            ResolveTarget(DeferredFocusId, ActiveFocusOrder) ??
            ResolveTarget(FocusedId, ActiveFocusOrder) ??
            ActiveFocusOrder.FirstOrDefault();
        var baseIndex = IndexOf(FocusOrder, restoreTarget);
        return this with
        {
            HostFocused = true,
            FocusIndex = FocusOrder.Count == 0
                ? 0
                : NormalizeIndex(baseIndex >= 0 ? baseIndex : FocusIndex, FocusOrder.Count),
            FocusedId = restoreTarget,
            DeferredFocusId = null,
            PointerDown = false
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
            FocusTerminalEvent focusEvent =>
                ApplyHostFocus(focusEvent.Focused),
            PasteTerminalEvent pasteEvent =>
                Announce($"Paste: {TrimPreview(pasteEvent.Text)}"),
            _ => this
        };
    }

    private static string[] NormalizeIds(IEnumerable<string> ids) =>
        ids
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private IReadOnlyList<WidgetFocusTrap> UpdateActiveTrapSelection(string target)
    {
        if (FocusTrapStack.Count == 0)
        {
            return FocusTrapStack;
        }

        var traps = FocusTrapStack.ToArray();
        traps[^1] = traps[^1] with { SelectedFocusId = target };
        return traps;
    }

    private static string? ResolveRestoreTarget(
        IReadOnlyList<WidgetFocusTrap> nextStack,
        string? fallbackReturnFocus,
        IReadOnlyList<string> nextActiveOrder)
    {
        if (nextActiveOrder.Count == 0)
        {
            return null;
        }

        if (nextStack.Count > 0)
        {
            return ResolveTarget(nextStack[^1].SelectedFocusId, nextActiveOrder) ?? nextActiveOrder.FirstOrDefault();
        }

        return ResolveTarget(fallbackReturnFocus, nextActiveOrder) ?? nextActiveOrder.FirstOrDefault();
    }

    private string? ResolveReplacementTargetForRemoval(WidgetFocusTrap[] traps, int removedIndex)
    {
        if (removedIndex <= 0)
        {
            return ResolveTarget(traps[removedIndex].ReturnFocusId, FocusOrder) ?? FocusOrder.FirstOrDefault();
        }

        var lower = traps[removedIndex - 1];
        return ResolveTarget(lower.SelectedFocusId, lower.FocusOrder)
            ?? lower.FocusOrder.FirstOrDefault()
            ?? ResolveTarget(traps[removedIndex].ReturnFocusId, FocusOrder)
            ?? FocusOrder.FirstOrDefault();
    }

    private static string? ResolveTarget(string? candidate, IReadOnlyList<string> order)
    {
        if (order.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        return order.Contains(candidate, StringComparer.Ordinal) ? candidate : null;
    }

    private static int IndexOf(IReadOnlyList<string> ids, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return -1;
        }

        for (var index = 0; index < ids.Count; index++)
        {
            if (string.Equals(ids[index], candidate, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
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

public sealed record WidgetFocusTrap(string? TrapId, IReadOnlyList<string> FocusOrder, string? ReturnFocusId, string? SelectedFocusId);
