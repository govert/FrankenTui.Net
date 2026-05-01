namespace FrankenTui.Widgets;

public sealed record WidgetFocusEvent(string Kind, string? FromId, string? ToId);

public sealed record WidgetFocusGroup(string Id, IReadOnlyList<string> Members, bool Wrap = true);

public readonly record struct WidgetFocusTrapDescriptor(string TrapId, string GroupId, string? PreferredFocusId = null);

internal sealed record WidgetFocusTrapFrame(string TrapId, string GroupId, string? ReturnFocusId, string? SelectedFocusId);

public sealed class WidgetFocusManager
{
    private readonly Dictionary<string, WidgetFocusGroup> _groups = new(StringComparer.Ordinal);
    private readonly List<string> _history = [];
    private readonly List<WidgetFocusTrapFrame> _trapStack = [];
    private string? _currentFocusId;
    private string? _deferredFocusId;

    public WidgetFocusGraph Graph { get; } = new();

    public bool HostFocused { get; private set; } = true;

    public string? CurrentFocusId => HostFocused ? _currentFocusId : null;

    public string? DeferredFocusId => HostFocused ? null : _deferredFocusId;

    public string? LogicalFocusId => HostFocused ? _currentFocusId : _deferredFocusId;

    public bool IsFocusTrapped => _trapStack.Count > 0;

    public string? ActiveTrapId => _trapStack.Count == 0 ? null : _trapStack[^1].TrapId;

    public string? ActiveTrapReturnFocusId => _trapStack.Count == 0 ? null : _trapStack[^1].ReturnFocusId;

    public string? RootTrapReturnFocusId => _trapStack.Count == 0 ? null : _trapStack[0].ReturnFocusId;

    public ulong FocusChangeCount { get; private set; }

    public WidgetFocusEvent? LastEvent { get; private set; }

    public string UpsertNode(WidgetFocusNode node)
    {
        var id = Graph.Insert(node);
        ReconcileAfterMutation(LogicalFocusId, RootTrapReturnFocusId);
        return id;
    }

    public WidgetFocusNode? RemoveNode(string id)
    {
        var removed = Graph.Remove(id);
        if (removed is null)
        {
            return null;
        }

        foreach (var group in _groups.Keys.ToArray())
        {
            var nextMembers = _groups[group].Members
                .Where(member => !string.Equals(member, id, StringComparison.Ordinal))
                .ToArray();

            if (nextMembers.Length == 0)
            {
                _groups.Remove(group);
            }
            else
            {
                _groups[group] = _groups[group] with { Members = nextMembers };
            }
        }

        _history.RemoveAll(candidate => string.Equals(candidate, id, StringComparison.Ordinal));
        ReconcileAfterMutation(LogicalFocusId, RootTrapReturnFocusId);
        return removed;
    }

    public void Connect(string fromId, WidgetNavigationDirection direction, string toId) =>
        Graph.Connect(fromId, direction, toId);

    public void Disconnect(string fromId, WidgetNavigationDirection direction) =>
        Graph.Disconnect(fromId, direction);

    public IReadOnlyList<string> BaseTabOrder() => Graph.TabOrder();

    public IReadOnlyList<string> ActiveTabOrder() =>
        _trapStack.Count == 0 ? BaseTabOrder() : GroupTabOrder(_trapStack[^1].GroupId);

    public IReadOnlyList<string> GroupTabOrder(string groupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);

        if (!_groups.TryGetValue(groupId, out var group))
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        return group.Members
            .Where(member => seen.Add(member))
            .Where(CanFocus)
            .OrderBy(member => Graph.Get(member)?.TabIndex ?? int.MaxValue)
            .ThenBy(static member => member, StringComparer.Ordinal)
            .ToArray();
    }

    public void SetGroup(string groupId, IEnumerable<string> members, bool wrap = true, bool reconcile = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentNullException.ThrowIfNull(members);

        var normalized = members
            .Where(CanFocus)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length == 0)
        {
            _groups.Remove(groupId);
        }
        else
        {
            _groups[groupId] = new WidgetFocusGroup(groupId, normalized, wrap);
        }

        if (reconcile)
        {
            ReconcileAfterMutation(LogicalFocusId, RootTrapReturnFocusId);
        }
    }

    public void RemoveGroup(string groupId, bool reconcile = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        if (_groups.Remove(groupId) && reconcile)
        {
            ReconcileAfterMutation(LogicalFocusId, RootTrapReturnFocusId);
        }
    }

    public bool Focus(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!CanFocus(id) || !AllowedByTrap(id))
        {
            return false;
        }

        if (HostFocused && string.Equals(_currentFocusId, id, StringComparison.Ordinal))
        {
            return false;
        }

        if (!HostFocused && string.Equals(_deferredFocusId, id, StringComparison.Ordinal))
        {
            return false;
        }

        if (_trapStack.Count > 0)
        {
            _trapStack[^1] = _trapStack[^1] with { SelectedFocusId = id };
        }

        if (HostFocused)
        {
            var previous = _currentFocusId;
            if (!string.IsNullOrWhiteSpace(previous) && !string.Equals(previous, id, StringComparison.Ordinal))
            {
                PushHistory(previous);
            }

            _currentFocusId = id;
            _deferredFocusId = null;
            RecordEvent(previous, id);
        }
        else
        {
            _deferredFocusId = id;
        }

        return true;
    }

    public bool FocusFirst()
    {
        var target = ActiveTabOrder().FirstOrDefault();
        return target is not null && Focus(target);
    }

    public bool FocusLast()
    {
        var order = ActiveTabOrder();
        var target = order.Count == 0 ? null : order[^1];
        return target is not null && Focus(target);
    }

    public bool FocusNext() => MoveInTabOrder(forward: true);

    public bool FocusPrevious() => MoveInTabOrder(forward: false);

    public bool FocusBack()
    {
        while (_history.Count > 0)
        {
            var previous = _history[^1];
            _history.RemoveAt(_history.Count - 1);
            if (!CanFocus(previous) || !AllowedByTrap(previous))
            {
                continue;
            }

            var old = LogicalFocusId;
            if (HostFocused)
            {
                _currentFocusId = previous;
                _deferredFocusId = null;
                RecordEvent(old, previous);
            }
            else
            {
                _deferredFocusId = previous;
            }

            if (_trapStack.Count > 0)
            {
                _trapStack[^1] = _trapStack[^1] with { SelectedFocusId = previous };
            }

            return true;
        }

        return false;
    }

    public bool Blur()
    {
        if (!HostFocused && _currentFocusId is null)
        {
            return false;
        }

        var previous = _currentFocusId ?? LogicalFocusId;
        HostFocused = false;
        _deferredFocusId = ResolveVisibleTarget();
        _currentFocusId = null;
        if (!string.IsNullOrWhiteSpace(previous))
        {
            LastEvent = new WidgetFocusEvent("lost", previous, null);
            FocusChangeCount++;
        }

        return !string.IsNullOrWhiteSpace(previous);
    }

    public bool ApplyHostFocus(bool focused)
    {
        if (!focused)
        {
            return Blur();
        }

        var target = ResolveVisibleTarget();
        var changed = !HostFocused || !string.Equals(_currentFocusId, target, StringComparison.Ordinal);
        var previous = _currentFocusId;
        HostFocused = true;
        _currentFocusId = target;
        _deferredFocusId = null;

        if (changed && !string.IsNullOrWhiteSpace(target))
        {
            RecordEvent(previous, target);
        }

        return changed;
    }

    public bool PushTrap(string groupId, string? trapId = null, string? preferredFocusId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);

        var descriptors = _trapStack
            .Select(static trap => new WidgetFocusTrapDescriptor(trap.TrapId, trap.GroupId, trap.SelectedFocusId))
            .ToList();
        descriptors.Add(new WidgetFocusTrapDescriptor(
            string.IsNullOrWhiteSpace(trapId) ? groupId : trapId,
            groupId,
            preferredFocusId));

        return SyncTrapStack(descriptors);
    }

    public bool PopTrap()
    {
        if (_trapStack.Count == 0)
        {
            return false;
        }

        var preferredBaseFocusId = _trapStack[^1].ReturnFocusId;
        return SyncTrapStack(_trapStack
            .Take(_trapStack.Count - 1)
            .Select(static trap => new WidgetFocusTrapDescriptor(trap.TrapId, trap.GroupId, trap.SelectedFocusId)),
            preferredBaseFocusId);
    }

    public bool RemoveTrap(string trapId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trapId);

        if (_trapStack.All(trap => !string.Equals(trap.TrapId, trapId, StringComparison.Ordinal)))
        {
            return false;
        }

        var removedTrap = _trapStack.First(trap => string.Equals(trap.TrapId, trapId, StringComparison.Ordinal));
        var preferredBaseFocusId = _trapStack.Count == 1 ? removedTrap.ReturnFocusId : null;
        return SyncTrapStack(_trapStack
            .Where(trap => !string.Equals(trap.TrapId, trapId, StringComparison.Ordinal))
            .Select(static trap => new WidgetFocusTrapDescriptor(trap.TrapId, trap.GroupId, trap.SelectedFocusId)),
            preferredBaseFocusId);
    }

    public bool SyncTrapStack(IEnumerable<WidgetFocusTrapDescriptor> descriptors, string? preferredBaseFocusId = null)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var requested = descriptors.ToArray();
        var previousLogical = LogicalFocusId;
        var baseOrder = BaseTabOrder();
        var baseRestoreTarget = ResolveTarget(preferredBaseFocusId, baseOrder);
        var existing = _trapStack.ToDictionary(static trap => trap.TrapId, StringComparer.Ordinal);
        _trapStack.Clear();

        foreach (var descriptor in requested)
        {
            if (string.IsNullOrWhiteSpace(descriptor.TrapId) || string.IsNullOrWhiteSpace(descriptor.GroupId))
            {
                continue;
            }

            var order = GroupTabOrder(descriptor.GroupId);
            if (order.Count == 0)
            {
                continue;
            }

            existing.TryGetValue(descriptor.TrapId, out var existingTrap);
            var selected = ResolveTarget(existingTrap?.SelectedFocusId, order)
                ?? ResolveTarget(descriptor.PreferredFocusId, order)
                ?? order[0];
            var returnFocus = _trapStack.Count == 0
                ? ResolveTarget(existingTrap?.ReturnFocusId, baseOrder)
                    ?? baseRestoreTarget
                    ?? ResolveTarget(previousLogical, baseOrder)
                    ?? baseOrder.FirstOrDefault()
                : ResolveTarget(_trapStack[^1].SelectedFocusId, GroupTabOrder(_trapStack[^1].GroupId))
                    ?? GroupTabOrder(_trapStack[^1].GroupId).FirstOrDefault();

            _trapStack.Add(new WidgetFocusTrapFrame(descriptor.TrapId, descriptor.GroupId, returnFocus, selected));
        }

        return ReconcileAfterMutation(previousLogical, baseRestoreTarget);
    }

    private bool ReconcileAfterMutation(string? previousLogical, string? preferredBaseFocusId = null)
    {
        var baseOrder = BaseTabOrder();
        var baseRestoreTarget = ResolveTarget(preferredBaseFocusId, baseOrder);
        var normalized = _trapStack
            .Select(static trap => new WidgetFocusTrapDescriptor(trap.TrapId, trap.GroupId, trap.SelectedFocusId))
            .ToArray();

        if (normalized.Length != _trapStack.Count || normalized.Any(descriptor => GroupTabOrder(descriptor.GroupId).Count == 0))
        {
            SyncTrapStack(normalized, baseRestoreTarget ?? RootTrapReturnFocusId);
            return true;
        }

        for (var index = 0; index < _trapStack.Count; index++)
        {
            var order = GroupTabOrder(_trapStack[index].GroupId);
            var selected = ResolveTarget(_trapStack[index].SelectedFocusId, order) ?? order.FirstOrDefault();
            var returnFocus = index == 0
                ? ResolveTarget(_trapStack[index].ReturnFocusId, baseOrder)
                    ?? baseRestoreTarget
                    ?? ResolveTarget(previousLogical, baseOrder)
                    ?? baseOrder.FirstOrDefault()
                : ResolveTarget(_trapStack[index - 1].SelectedFocusId, GroupTabOrder(_trapStack[index - 1].GroupId))
                    ?? GroupTabOrder(_trapStack[index - 1].GroupId).FirstOrDefault();
            _trapStack[index] = _trapStack[index] with
            {
                SelectedFocusId = selected,
                ReturnFocusId = returnFocus
            };
        }

        var previousVisible = LogicalFocusId;
        if (HostFocused)
        {
            _currentFocusId = ResolveVisibleTarget(baseRestoreTarget);
            _deferredFocusId = null;
        }
        else
        {
            _deferredFocusId = ResolveVisibleTarget(baseRestoreTarget);
            _currentFocusId = null;
        }

        if (HostFocused && !string.Equals(previousVisible, _currentFocusId, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(_currentFocusId))
        {
            RecordEvent(previousVisible, _currentFocusId);
            return true;
        }

        return !string.Equals(previousVisible, LogicalFocusId, StringComparison.Ordinal);
    }

    private bool MoveInTabOrder(bool forward)
    {
        var order = ActiveTabOrder();
        if (order.Count == 0)
        {
            return false;
        }

        var wrap = _trapStack.Count == 0
            ? true
            : _groups.TryGetValue(_trapStack[^1].GroupId, out var group) && group.Wrap;
        var current = LogicalFocusId;
        var currentIndex = string.IsNullOrWhiteSpace(current)
            ? -1
            : IndexOf(order, current);

        string next;
        if (currentIndex < 0)
        {
            next = forward ? order[0] : order[^1];
        }
        else if (forward)
        {
            if (currentIndex + 1 < order.Count)
            {
                next = order[currentIndex + 1];
            }
            else if (wrap)
            {
                next = order[0];
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (currentIndex > 0)
            {
                next = order[currentIndex - 1];
            }
            else if (wrap)
            {
                next = order[^1];
            }
            else
            {
                return false;
            }
        }

        return Focus(next);
    }

    private string? ResolveVisibleTarget(string? preferredBaseFocusId = null)
    {
        if (_trapStack.Count > 0)
        {
            var active = _trapStack[^1];
            var order = GroupTabOrder(active.GroupId);
            return ResolveTarget(active.SelectedFocusId, order) ?? order.FirstOrDefault();
        }

        var baseOrder = BaseTabOrder();
        return ResolveTarget(preferredBaseFocusId, baseOrder)
            ?? ResolveTarget(_currentFocusId, baseOrder)
            ?? ResolveTarget(_deferredFocusId, baseOrder)
            ?? baseOrder.FirstOrDefault();
    }

    private bool CanFocus(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var node = Graph.Get(id);
        return node is { IsFocusable: true };
    }

    private bool AllowedByTrap(string id)
    {
        if (_trapStack.Count == 0)
        {
            return true;
        }

        return GroupTabOrder(_trapStack[^1].GroupId).Contains(id, StringComparer.Ordinal);
    }

    private void PushHistory(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        if (_history.Count == 0 || !string.Equals(_history[^1], id, StringComparison.Ordinal))
        {
            _history.Add(id);
        }
    }

    private void RecordEvent(string? fromId, string? toId)
    {
        if (string.IsNullOrWhiteSpace(toId))
        {
            return;
        }

        LastEvent = string.IsNullOrWhiteSpace(fromId)
            ? new WidgetFocusEvent("gained", null, toId)
            : new WidgetFocusEvent("moved", fromId, toId);
        FocusChangeCount++;
    }

    private static string? ResolveTarget(string? candidate, IReadOnlyList<string> order)
    {
        if (string.IsNullOrWhiteSpace(candidate) || order.Count == 0)
        {
            return null;
        }

        return order.Contains(candidate, StringComparer.Ordinal) ? candidate : null;
    }

    private static int IndexOf(IReadOnlyList<string> order, string candidate)
    {
        for (var index = 0; index < order.Count; index++)
        {
            if (string.Equals(order[index], candidate, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }
}
