namespace FrankenTui.Widgets;

public readonly record struct WidgetModalId(long Value);

public sealed record WidgetModalEntry(
    string Key,
    IReadOnlyList<string>? FocusableIds = null,
    bool AriaModal = true,
    string? PreferredFocusId = null)
{
    public IReadOnlyList<string> FocusableIds { get; init; } = FocusableIds ?? [];
}

public sealed record WidgetModalResult(WidgetModalId Id, WidgetModalEntry Entry);

internal sealed record ActiveWidgetModal(WidgetModalId Id, WidgetModalEntry Entry, string GroupId, string TrapId);

public sealed class WidgetModalStack
{
    private readonly List<ActiveWidgetModal> _modals = [];
    private long _nextId = 1;

    public bool IsEmpty => _modals.Count == 0;

    public int Depth => _modals.Count;

    public WidgetModalId? TopId => _modals.Count == 0 ? null : _modals[^1].Id;

    internal IReadOnlyList<ActiveWidgetModal> ActiveModals => _modals;

    public WidgetModalId Push(WidgetModalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.Key);

        var id = new WidgetModalId(_nextId++);
        _modals.Add(new ActiveWidgetModal(
            id,
            entry,
            $"modal-group:{id.Value}",
            $"modal-trap:{id.Value}"));
        return id;
    }

    public WidgetModalResult? Pop()
    {
        if (_modals.Count == 0)
        {
            return null;
        }

        var modal = _modals[^1];
        _modals.RemoveAt(_modals.Count - 1);
        return new WidgetModalResult(modal.Id, modal.Entry);
    }

    public WidgetModalResult? PopById(WidgetModalId id)
    {
        var index = _modals.FindIndex(modal => modal.Id.Equals(id));
        if (index < 0)
        {
            return null;
        }

        var modal = _modals[index];
        _modals.RemoveAt(index);
        return new WidgetModalResult(modal.Id, modal.Entry);
    }

    public IReadOnlyList<WidgetModalResult> PopAll()
    {
        var results = new List<WidgetModalResult>(_modals.Count);
        while (Pop() is { } result)
        {
            results.Add(result);
        }

        return results;
    }

    public bool Update(WidgetModalId id, WidgetModalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var index = _modals.FindIndex(modal => modal.Id.Equals(id));
        if (index < 0)
        {
            return false;
        }

        var active = _modals[index];
        _modals[index] = active with { Entry = entry };
        return true;
    }
}

public sealed class WidgetFocusAwareModalStack
{
    private readonly HashSet<string> _managedGroupIds = new(StringComparer.Ordinal);

    public WidgetFocusAwareModalStack(WidgetFocusManager? focusManager = null)
    {
        FocusManager = focusManager ?? new WidgetFocusManager();
    }

    public WidgetModalStack Stack { get; } = new();

    public WidgetFocusManager FocusManager { get; }

    public bool IsFocusTrapped => FocusManager.IsFocusTrapped;

    public bool IsEmpty => Stack.IsEmpty;

    public int Depth => Stack.Depth;

    public WidgetModalId Push(WidgetModalEntry entry)
    {
        var id = Stack.Push(entry);
        SyncFocus();
        return id;
    }

    public WidgetModalResult? Pop()
    {
        var preferredBaseFocusId = Stack.Depth == 1 ? FocusManager.RootTrapReturnFocusId : null;
        var result = Stack.Pop();
        if (result is not null)
        {
            SyncFocus(preferredBaseFocusId);
        }

        return result;
    }

    public WidgetModalResult? PopById(WidgetModalId id)
    {
        var preferredBaseFocusId = Stack.Depth == 1 ? FocusManager.RootTrapReturnFocusId : null;
        var result = Stack.PopById(id);
        if (result is not null)
        {
            SyncFocus(preferredBaseFocusId);
        }

        return result;
    }

    public IReadOnlyList<WidgetModalResult> PopAll()
    {
        var preferredBaseFocusId = FocusManager.RootTrapReturnFocusId;
        var results = Stack.PopAll();
        if (results.Count > 0)
        {
            SyncFocus(preferredBaseFocusId);
        }

        return results;
    }

    public bool Update(WidgetModalId id, WidgetModalEntry entry)
    {
        var updated = Stack.Update(id, entry);
        if (updated)
        {
            SyncFocus();
        }

        return updated;
    }

    public bool ApplyHostFocus(bool focused) => FocusManager.ApplyHostFocus(focused);

    private void SyncFocus(string? preferredBaseFocusId = null)
    {
        var activeGroups = new HashSet<string>(StringComparer.Ordinal);
        var descriptors = new List<WidgetFocusTrapDescriptor>(Stack.Depth);

        foreach (var modal in Stack.ActiveModals)
        {
            if (!modal.Entry.AriaModal)
            {
                continue;
            }

            FocusManager.SetGroup(modal.GroupId, modal.Entry.FocusableIds, reconcile: false);
            activeGroups.Add(modal.GroupId);

            if (modal.Entry.FocusableIds.Count > 0)
            {
                descriptors.Add(new WidgetFocusTrapDescriptor(
                    modal.TrapId,
                    modal.GroupId,
                    modal.Entry.PreferredFocusId));
            }
        }

        foreach (var obsolete in _managedGroupIds.Except(activeGroups, StringComparer.Ordinal).ToArray())
        {
            FocusManager.RemoveGroup(obsolete, reconcile: false);
            _managedGroupIds.Remove(obsolete);
        }

        foreach (var groupId in activeGroups)
        {
            _managedGroupIds.Add(groupId);
        }

        FocusManager.SyncTrapStack(descriptors, preferredBaseFocusId);
    }
}
