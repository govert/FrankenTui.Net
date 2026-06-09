// Port of .external/frankentui/crates/ftui-widgets/src/modal/focus_integration.rs
// Focus-aware modal integration for automatic focus trap management.

// DIVERGENCE: The upstream Rust FocusManager (focus/manager.rs) and ModalStack (modal/stack.rs)
// expose rich internal APIs (e.g. current(), is_trapped(), push_trap(), pop_trap(),
// focus_without_history(), group_members(), create_group_preserving_members(),
// deferred_focus_target(), host_focused(), logical_focus_target(),
// push_trap_with_return_focus(), clear_traps(), replace_deferred_focus_target(),
// apply_host_focus(), blur(), repair_focus_after_excluding_ids(),
// focus_first_without_history_for_restore(), restore_focus_after_invalid_current(),
// graph()/graph_mut(), has_group(), remove_group(), remove_group_without_repair(),
// group_primary_focus_target(), clear_deferred_focus_if_excluded(), group_count(),
// focus_change_count(), take_focus_event(), focus_back()) that are not yet present in the
// existing C# FocusManager stub (FrankenTui.Widgets.Focus.FocusManager) or ModalStack stub
// (FrankenTui.Widgets.Modal.ModalStack).  This file introduces self-contained
// UpstreamFocusManager and UpstreamModalStack types that carry the full upstream contract
// so the coordinator and public API can be ported faithfully.  When the stub types are upgraded
// to full ports those two types should be merged with or replaced by the stub types.

using System.Threading;
using FrankenTui.Core;

namespace FrankenTui.Widgets.Modal;

// ── FocusId alias ─────────────────────────────────────────────────────────

// Rust: type FocusId = u64  (crates/ftui-widgets/src/focus/graph.rs)
// In C# we use ulong directly everywhere.

// ── OptionOption<T> – representing Rust Option<Option<T>> ─────────────────

/// <summary>
/// Represents Rust <c>Option&lt;Option&lt;T&gt;&gt;</c> — distinguishes None, Some(None),
/// and Some(Some(value)).
/// </summary>
internal readonly struct OptionOption<T> where T : struct
{
    private readonly bool _outer;   // false → None
    private readonly bool _inner;   // false → Some(None)
    private readonly T _value;

    private OptionOption(bool outer, bool inner, T value)
    { _outer = outer; _inner = inner; _value = value; }

    public static OptionOption<T> None => default;
    public static OptionOption<T> SomeNone => new(true, false, default);
    public static OptionOption<T> SomeSome(T v) => new(true, true, v);

    public bool IsNone => !_outer;
    public bool IsSome => _outer;
    public bool IsInnerNone => _outer && !_inner;
    public bool IsInnerSome => _outer && _inner;
    public T? InnerValue => _inner ? _value : default(T?);

    /// <summary>True when the instance contains a concrete inner Some(value).</summary>
    public bool TryGetValue(out T value) { value = _value; return _inner && _outer; }

    public override string ToString()
        => !_outer ? "None" : !_inner ? "Some(None)" : $"Some(Some({_value}))";
}

// ── FocusTrapSpec ─────────────────────────────────────────────────────────

/// <summary>
/// Collapsed focus-trap specification used during trap rebuild.
/// (Rust: pub(super) struct FocusTrapSpec in modal/stack.rs)
/// </summary>
internal readonly struct FocusTrapSpec
{
    public readonly uint GroupId;
    public readonly ulong? ReturnFocus; // Option<FocusId>

    public FocusTrapSpec(uint groupId, ulong? returnFocus)
    { GroupId = groupId; ReturnFocus = returnFocus; }
}

// ── UpstreamFocusEvent ────────────────────────────────────────────────────

/// <summary>
/// Focus change event emitted by <see cref="UpstreamFocusManager"/>.
/// (Rust: pub enum FocusEvent in focus/manager.rs)
/// </summary>
public abstract class UpstreamFocusEvent
{
    private UpstreamFocusEvent() { }

    /// <summary>A widget gained focus.</summary>
    public sealed class FocusGained : UpstreamFocusEvent
    {
        public ulong Id { get; }
        public FocusGained(ulong id) => Id = id;
        public override bool Equals(object? obj) => obj is FocusGained e && e.Id == Id;
        public override int GetHashCode() => HashCode.Combine("FocusGained", Id);
        public override string ToString() => $"FocusGained {{ id: {Id} }}";
    }

    /// <summary>A widget lost focus.</summary>
    public sealed class FocusLost : UpstreamFocusEvent
    {
        public ulong Id { get; }
        public FocusLost(ulong id) => Id = id;
        public override bool Equals(object? obj) => obj is FocusLost e && e.Id == Id;
        public override int GetHashCode() => HashCode.Combine("FocusLost", Id);
        public override string ToString() => $"FocusLost {{ id: {Id} }}";
    }

    /// <summary>Focus moved from one widget to another.</summary>
    public sealed class FocusMoved : UpstreamFocusEvent
    {
        public ulong From { get; }
        public ulong To { get; }
        public FocusMoved(ulong from, ulong to) { From = from; To = to; }
        public override bool Equals(object? obj) => obj is FocusMoved e && e.From == From && e.To == To;
        public override int GetHashCode() => HashCode.Combine("FocusMoved", From, To);
        public override string ToString() => $"FocusMoved {{ from: {From}, to: {To} }}";
    }
}

// ── UpstreamFocusNode ─────────────────────────────────────────────────────

/// <summary>
/// A node in the focus graph.
/// (Rust: FocusNode in focus/graph.rs)
/// </summary>
public sealed class UpstreamFocusNode
{
    public ulong Id { get; }
    public Rect Bounds { get; }
    public int TabIndex { get; private set; }
    public bool IsFocusable { get; private set; } = true;

    public UpstreamFocusNode(ulong id, Rect bounds) { Id = id; Bounds = bounds; }
    public UpstreamFocusNode WithTabIndex(int idx) { TabIndex = idx; return this; }
    public UpstreamFocusNode WithFocusable(bool f) { IsFocusable = f; return this; }
}

// ── UpstreamFocusGraph ────────────────────────────────────────────────────

/// <summary>
/// Minimal focus-node registry used by <see cref="UpstreamFocusManager"/>.
/// Upstream: crates/ftui-widgets/src/focus/graph.rs
/// </summary>
public sealed class UpstreamFocusGraph
{
    private readonly Dictionary<ulong, UpstreamFocusNode> _nodes = new();

    public void Insert(UpstreamFocusNode node) => _nodes[node.Id] = node;

    public UpstreamFocusNode? Remove(ulong id) { _nodes.Remove(id, out var n); return n; }

    public UpstreamFocusNode? Get(ulong id) => _nodes.TryGetValue(id, out var n) ? n : null;

    /// <summary>
    /// Ascending tab-order; only includes nodes with non-negative tab-index.
    /// Matches upstream Rust graph.tab_order() which filters tab_index &gt;= 0.
    /// Nodes with negative tab-index are intentionally excluded.
    /// </summary>
    public List<ulong> TabOrder()
    {
        return _nodes.Values
            .Where(n => n.IsFocusable && n.TabIndex >= 0)
            .OrderBy(n => n.TabIndex).ThenBy(n => n.Id)
            .Select(n => n.Id)
            .ToList();
    }
}

// ── UpstreamFocusGroup / UpstreamFocusTrap ───────────────────────────────

internal sealed class UpstreamFocusGroup
{
    public uint Id { get; }
    public List<ulong> Members { get; }
    public bool Wrap { get; } = true;
    public UpstreamFocusGroup(uint id, List<ulong> members) { Id = id; Members = members; }
    public bool Contains(ulong id) => Members.Contains(id);
}

internal sealed class UpstreamFocusTrap
{
    public uint GroupId { get; }
    public ulong? ReturnFocus { get; set; }
    public UpstreamFocusTrap(uint groupId, ulong? returnFocus) { GroupId = groupId; ReturnFocus = returnFocus; }
}

// ── UpstreamFocusManager ──────────────────────────────────────────────────

/// <summary>
/// Full-fidelity port of the upstream FocusManager (focus/manager.rs).
/// Used by <see cref="FocusAwareModalStack"/> and <see cref="ModalFocusCoordinator"/>.
/// (Rust: pub struct FocusManager)
/// </summary>
public sealed class UpstreamFocusManager
{
    private readonly UpstreamFocusGraph _graph = new();
    private ulong? _current;
    private bool _hostFocused = true;
    private ulong? _pendingFocusOnHostGain;
    private readonly List<ulong> _history = new();
    private readonly List<UpstreamFocusTrap> _trapStack = new();
    private readonly Dictionary<uint, UpstreamFocusGroup> _groups = new();
    private UpstreamFocusEvent? _lastEvent;
    private ulong _focusChangeCount;

    // ---- Public API --------------------------------------------------------

    /// <summary>Access the underlying focus graph.</summary>
    public UpstreamFocusGraph Graph() => _graph;

    /// <summary>Mutably access the underlying focus graph.</summary>
    public UpstreamFocusGraph GraphMut() => _graph;

    /// <summary>Get currently focused widget. (Rust: pub fn current)</summary>
    public ulong? Current() => _current;

    /// <summary>Whether the host window currently has focus. (Rust: pub(crate) fn host_focused)</summary>
    internal bool HostFocused() => _hostFocused;

    /// <summary>Set host-focused flag. (Rust: pub(crate) fn set_host_focused)</summary>
    internal void SetHostFocused(bool focused)
    {
        _hostFocused = focused;
        if (focused) _pendingFocusOnHostGain = null;
    }

    /// <summary>Set focus to widget; returns previous focus. (Rust: pub fn focus)</summary>
    public ulong? Focus(ulong id)
    {
        if (!CanFocus(id) || !AllowedByTrap(id)) return null;
        var prev = ActiveFocusTarget();
        if (prev == id) return prev;
        SetFocus(id);
        return prev;
    }

    /// <summary>Remove focus from current widget. (Rust: pub fn blur)</summary>
    public ulong? Blur()
    {
        var prev = _current;
        _current = null;
        if (prev.HasValue)
        {
            _lastEvent = new UpstreamFocusEvent.FocusLost(prev.Value);
            _focusChangeCount++;
        }
        return prev;
    }

    /// <summary>
    /// Apply host/window focus state. Returns true when focus state changed.
    /// (Rust: pub fn apply_host_focus)
    /// </summary>
    public bool ApplyHostFocus(bool focused)
    {
        if (!focused)
        {
            if (_current.HasValue) _pendingFocusOnHostGain = _current;
            _hostFocused = false;
            return Blur().HasValue;
        }

        _hostFocused = true;
        if (_current.HasValue && CanFocus(_current.Value) && AllowedByTrap(_current.Value))
        {
            _pendingFocusOnHostGain = null;
            return false;
        }

        var pending = _pendingFocusOnHostGain;
        _pendingFocusOnHostGain = null;
        if (pending.HasValue && CanFocus(pending.Value) && AllowedByTrap(pending.Value))
            return SetFocusWithoutHistory(pending.Value);

        if (ActiveTrapGroup() is { } ag1 && FocusFirstInGroupWithoutHistory(ag1)) return true;
        if (FocusFirstWithoutHistory()) return true;
        return false;
    }

    /// <summary>Move to next in tab order. (Rust: pub fn focus_next)</summary>
    public bool FocusNext() => MoveInTabOrder(forward: true);

    /// <summary>Go back to previous focus. (Rust: pub fn focus_back)</summary>
    public bool FocusBack()
    {
        var activeFocus = ActiveFocusTarget();
        while (_history.Count > 0)
        {
            var id = _history[^1];
            _history.RemoveAt(_history.Count - 1);
            if (activeFocus == id) continue;
            if (!CanFocus(id) || !AllowedByTrap(id)) continue;
            if (!_hostFocused) return SetPendingFocusTarget(id);
            var prev = _current;
            _current = id;
            _lastEvent = prev.HasValue
                ? new UpstreamFocusEvent.FocusMoved(prev.Value, id)
                : (UpstreamFocusEvent)new UpstreamFocusEvent.FocusGained(id);
            _focusChangeCount++;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Push focus trap; returns false if group is missing or empty.
    /// (Rust: pub fn push_trap)
    /// </summary>
    public bool PushTrap(uint groupId)
    {
        var returnFocus = _hostFocused ? _current : (_current ?? DeferredFocusTarget());
        if (!PushTrapWithReturnFocus(groupId, returnFocus)) return false;

        if (_hostFocused && !IsCurrentFocusableInGroup(groupId))
            FocusFirstInGroupWithoutHistory(groupId);
        else if (!_hostFocused)
            _pendingFocusOnHostGain = GroupPrimaryFocusTarget(groupId);
        return true;
    }

    /// <summary>Pop focus trap; restore previous focus. (Rust: pub fn pop_trap)</summary>
    public bool PopTrap()
    {
        if (_trapStack.Count == 0) return false;
        var trap = _trapStack[^1];
        _trapStack.RemoveAt(_trapStack.Count - 1);
        var hadCurrent = _current.HasValue;

        if (!_hostFocused)
        {
            var newPending = trap.ReturnFocus.HasValue
                && CanFocus(trap.ReturnFocus.Value)
                && AllowedByTrap(trap.ReturnFocus.Value)
                ? trap.ReturnFocus
                : (ActiveTrapGroup() is { } ag ? GroupPrimaryFocusTarget(ag) : null);
            _pendingFocusOnHostGain = newPending;
            return hadCurrent ? Blur().HasValue : false;
        }

        if (trap.ReturnFocus.HasValue && CanFocus(trap.ReturnFocus.Value) && AllowedByTrap(trap.ReturnFocus.Value))
            return SetFocusWithoutHistory(trap.ReturnFocus.Value);

        if (ActiveTrapGroup() is { } active)
            return FocusFirstInGroupWithoutHistory(active);

        if (!trap.ReturnFocus.HasValue)
            return hadCurrent ? Blur().HasValue : false;

        if (FocusFirstWithoutHistory()) return true;
        if (hadCurrent && _current.HasValue && !CanFocus(_current.Value)) return Blur().HasValue;
        return false;
    }

    /// <summary>Check if focus is currently trapped. (Rust: pub fn is_trapped)</summary>
    public bool IsTrapped() => ActiveTrapGroup().HasValue;

    /// <summary>Remove all active focus traps. (Rust: pub fn clear_traps)</summary>
    public void ClearTraps() => _trapStack.Clear();

    /// <summary>Create a focus group. (Rust: pub fn create_group)</summary>
    public void CreateGroup(uint id, List<ulong> members)
    {
        var filtered = FilterFocusable(members);
        _groups[id] = new UpstreamFocusGroup(id, filtered);
        RepairFocusAfterGroupChange();
    }

    /// <summary>
    /// Create a focus group preserving all declared members without filtering.
    /// (Rust: pub(crate) fn create_group_preserving_members)
    /// </summary>
    internal void CreateGroupPreservingMembers(uint id, List<ulong> members)
    {
        var deduped = DedupMembers(members);
        _groups[id] = new UpstreamFocusGroup(id, deduped);
        RepairFocusAfterGroupChange();
    }

    /// <summary>Remove an entire focus group. (Rust: pub fn remove_group)</summary>
    public void RemoveGroup(uint groupId)
    {
        if (!_groups.Remove(groupId)) return;
        _trapStack.RemoveAll(t => t.GroupId == groupId);
        RepairFocusAfterGroupChange();
    }

    /// <summary>Remove a focus group without triggering focus repair. (Rust: pub(crate) fn remove_group_without_repair)</summary>
    internal bool RemoveGroupWithoutRepair(uint groupId)
    {
        if (!_groups.Remove(groupId)) return false;
        _trapStack.RemoveAll(t => t.GroupId == groupId);
        return true;
    }

    /// <summary>Check if a focus group exists. (Rust: pub(crate) fn has_group)</summary>
    internal bool HasGroup(uint groupId) => _groups.ContainsKey(groupId);

    /// <summary>Get members of a focus group. (Rust: pub(crate) fn group_members)</summary>
    internal List<ulong> GroupMembers(uint groupId)
        => _groups.TryGetValue(groupId, out var g) ? new List<ulong>(g.Members) : new List<ulong>();

    /// <summary>Get the primary focus target in a group. (Rust: pub(crate) fn group_primary_focus_target)</summary>
    internal ulong? GroupPrimaryFocusTarget(uint groupId)
    {
        var order = GroupTabOrder(groupId);
        if (order.Count > 0) return order[0];
        if (!_groups.TryGetValue(groupId, out var group)) return null;
        foreach (var id in group.Members) if (CanFocus(id)) return id;
        return null;
    }

    /// <summary>Number of active groups (upstream: #[cfg(test)]). (Rust: pub(crate) fn group_count)</summary>
    internal int GroupCount() => _groups.Count;

    /// <summary>
    /// Deferred focus target: active target or primary of top active trap.
    /// (Rust: pub(crate) fn deferred_focus_target)
    /// </summary>
    internal ulong? DeferredFocusTarget()
    {
        var active = ActiveFocusTarget();
        if (active.HasValue) return active;
        if (ActiveTrapGroup() is { } group) return GroupPrimaryFocusTarget(group);
        return null;
    }

    /// <summary>Logically-selected focus target. (Rust: pub(crate) fn logical_focus_target)</summary>
    internal ulong? LogicalFocusTarget() => ActiveFocusTarget();

    /// <summary>Focus a widget without recording history. (Rust: pub(crate) fn focus_without_history)</summary>
    internal bool FocusWithoutHistory(ulong id) => SetFocusWithoutHistory(id);

    /// <summary>Focus first focusable without history (for focus restore). (Rust: pub(crate) fn focus_first_without_history_for_restore)</summary>
    internal bool FocusFirstWithoutHistoryForRestore() => FocusFirstWithoutHistory();

    /// <summary>
    /// Replace the deferred focus target.
    /// (Rust: pub(crate) fn replace_deferred_focus_target)
    /// </summary>
    internal void ReplaceDeferredFocusTarget(ulong? target)
    {
        _current = null;
        _pendingFocusOnHostGain = target.HasValue && CanFocus(target.Value) && AllowedByTrap(target.Value) ? target : null;
    }

    /// <summary>Push a trap with a specific return focus. (Rust: pub(crate) fn push_trap_with_return_focus)</summary>
    internal bool PushTrapWithReturnFocus(uint groupId, ulong? returnFocus)
    {
        if (!GroupHasFocusableMember(groupId)) return false;
        _trapStack.Add(new UpstreamFocusTrap(groupId, returnFocus));
        return true;
    }

    /// <summary>Repair focus after excluding a set of IDs. (Rust: pub(crate) fn repair_focus_after_excluding_ids)</summary>
    internal void RepairFocusAfterExcludingIds(IReadOnlyList<ulong> excluded)
    {
        _history.RemoveAll(id => excluded.Contains(id));
        if (!_hostFocused)
        {
            if (_current.HasValue && excluded.Contains(_current.Value)) Blur();
            return;
        }
        if (IsTrapped() || !(_current.HasValue && excluded.Contains(_current.Value))) return;
        foreach (var id in _graph.TabOrder())
        {
            if (excluded.Contains(id)) continue;
            if (SetFocusWithoutHistory(id)) return;
        }
        Blur();
    }

    /// <summary>Clear pending deferred focus if it is in the excluded set. (Rust: pub(crate) fn clear_deferred_focus_if_excluded)</summary>
    internal void ClearDeferredFocusIfExcluded(IReadOnlyList<ulong> excluded)
    {
        if (_pendingFocusOnHostGain.HasValue && excluded.Contains(_pendingFocusOnHostGain.Value))
            _pendingFocusOnHostGain = null;
    }

    /// <summary>Restore focus after a current widget became invalid. (Rust: pub(crate) fn restore_focus_after_invalid_current)</summary>
    internal void RestoreFocusAfterInvalidCurrent()
    {
        if (!_hostFocused) return;
        if (ActiveTrapGroup() is { } group && FocusFirstInGroupWithoutHistory(group)) return;
        FocusFirstWithoutHistory();
    }

    /// <summary>Take and clear the last focus event. (Rust: pub fn take_focus_event)</summary>
    public UpstreamFocusEvent? TakeFocusEvent() { var ev = _lastEvent; _lastEvent = null; return ev; }

    /// <summary>Total number of focus changes since creation. (Rust: pub fn focus_change_count)</summary>
    public ulong FocusChangeCount() => _focusChangeCount;

    // ---- Private helpers ---------------------------------------------------

    private bool CanFocus(ulong id) => _graph.Get(id)?.IsFocusable == true;

    private ulong? ActiveFocusTarget()
    {
        if (_current.HasValue && CanFocus(_current.Value) && AllowedByTrap(_current.Value)) return _current;
        if (_hostFocused) return null;
        if (_pendingFocusOnHostGain.HasValue && CanFocus(_pendingFocusOnHostGain.Value) && AllowedByTrap(_pendingFocusOnHostGain.Value))
            return _pendingFocusOnHostGain;
        return null;
    }

    private bool SetFocus(ulong id) => SetFocusTarget(id, recordHistory: true);
    private bool SetFocusWithoutHistory(ulong id) => SetFocusTarget(id, recordHistory: false);

    private bool SetFocusTarget(ulong id, bool recordHistory)
    {
        if (!_hostFocused) return SetPendingFocusTarget(id);
        return SetFocusInternal(id, recordHistory);
    }

    private bool SetFocusInternal(ulong id, bool recordHistory)
    {
        if (!CanFocus(id) || !AllowedByTrap(id)) return false;
        if (_current == id) return false;
        var prev = _current;
        if (prev.HasValue && recordHistory && (_history.Count == 0 || _history[^1] != prev.Value))
            _history.Add(prev.Value);
        _lastEvent = prev.HasValue
            ? new UpstreamFocusEvent.FocusMoved(prev.Value, id)
            : (UpstreamFocusEvent)new UpstreamFocusEvent.FocusGained(id);
        _current = id;
        _focusChangeCount++;
        return true;
    }

    private bool SetPendingFocusTarget(ulong id)
    {
        if (!CanFocus(id) || !AllowedByTrap(id)) return false;
        var prev = ActiveFocusTarget();
        _current = null;
        if (prev == id) return false;
        if (prev.HasValue)
        {
            _lastEvent = new UpstreamFocusEvent.FocusLost(prev.Value);
            _focusChangeCount++;
        }
        _pendingFocusOnHostGain = id;
        return true;
    }

    private uint? ActiveTrapGroup()
    {
        for (int i = _trapStack.Count - 1; i >= 0; i--)
        {
            var trap = _trapStack[i];
            if (GroupHasFocusableMember(trap.GroupId)) return trap.GroupId;
        }
        return null;
    }

    private bool AllowedByTrap(ulong id)
    {
        var group = ActiveTrapGroup();
        if (!group.HasValue) return true;
        return _groups.TryGetValue(group.Value, out var g) && g.Contains(id);
    }

    private bool GroupHasFocusableMember(uint groupId)
        => _groups.TryGetValue(groupId, out var g) && g.Members.Any(CanFocus);

    private List<ulong> GroupTabOrder(uint groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group)) return new List<ulong>();
        var order = _graph.TabOrder();
        return order.Where(id => group.Contains(id)).ToList();
    }

    private bool FocusFirstInGroupWithoutHistory(uint groupId)
    {
        var first = GroupPrimaryFocusTarget(groupId);
        return first.HasValue && SetFocusWithoutHistory(first.Value);
    }

    private bool FocusFirstWithoutHistory()
    {
        var order = ActiveTabOrder();
        return order.Count > 0 && SetFocusWithoutHistory(order[0]);
    }

    private List<ulong> ActiveTabOrder()
    {
        if (ActiveTrapGroup() is { } group) return GroupTabOrder(group);
        return _graph.TabOrder();
    }

    private bool MoveInTabOrder(bool forward)
    {
        var order = ActiveTabOrder();
        if (order.Count == 0) return false;
        var fallback = forward ? order[0] : order[^1];
        var wrap = ActiveTrapGroup() is { } ag && _groups.TryGetValue(ag, out var g) ? g.Wrap : true;
        var activeFocus = ActiveFocusTarget();
        ulong next;
        if (!activeFocus.HasValue)
        {
            next = fallback;
        }
        else
        {
            var idx = order.IndexOf(activeFocus.Value);
            if (idx < 0) { next = fallback; }
            else if (forward)
            {
                if (idx + 1 < order.Count) next = order[idx + 1];
                else if (wrap) next = order[0];
                else return false;
            }
            else
            {
                if (idx > 0) next = order[idx - 1];
                else if (wrap) next = order[^1];
                else return false;
            }
        }
        return SetFocus(next);
    }

    private bool IsCurrentFocusableInGroup(uint groupId)
        => _current.HasValue && CanFocus(_current.Value)
            && _groups.TryGetValue(groupId, out var g) && g.Contains(_current.Value);

    private void RepairFocusAfterGroupChange()
    {
        if (!_hostFocused) { if (_current.HasValue) Blur(); return; }
        if (ActiveTrapGroup() is { } group)
        {
            var allowed = _current.HasValue && CanFocus(_current.Value) && AllowedByTrap(_current.Value);
            if (!allowed) FocusFirstInGroupWithoutHistory(group);
        }
        else
        {
            if (_current.HasValue && !CanFocus(_current.Value) && !FocusFirstWithoutHistory()) Blur();
        }
    }

    private static List<ulong> DedupMembers(List<ulong> ids)
    {
        var out2 = new List<ulong>();
        foreach (var id in ids) if (!out2.Contains(id)) out2.Add(id);
        return out2;
    }

    private List<ulong> FilterFocusable(List<ulong> ids)
        => DedupMembers(ids).Where(CanFocus).ToList();
}

// ── UpstreamModalResult / UpstreamModalId ────────────────────────────────

/// <summary>
/// Result returned when a modal is closed.
/// (Rust: pub struct ModalResult in modal/stack.rs)
/// </summary>
public sealed class UpstreamModalResult
{
    public UpstreamModalId Id { get; init; }
    public uint? FocusGroupId { get; init; }
}

/// <summary>
/// Unique identifier for a modal. (Rust: pub struct ModalId(u64) in modal/stack.rs)
/// </summary>
public readonly struct UpstreamModalId : IEquatable<UpstreamModalId>
{
    private static ulong _counter;
    public ulong Value { get; }
    private UpstreamModalId(ulong v) => Value = v;
    internal static UpstreamModalId New() => new(Interlocked.Increment(ref _counter));
    public bool Equals(UpstreamModalId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is UpstreamModalId o && Equals(o);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(UpstreamModalId a, UpstreamModalId b) => a.Value == b.Value;
    public static bool operator !=(UpstreamModalId a, UpstreamModalId b) => a.Value != b.Value;
    public override string ToString() => $"ModalId({Value})";
}

// ── IUpstreamStackModal ───────────────────────────────────────────────────

/// <summary>
/// Trait for modal content that can be managed in the stack.
/// (Rust: pub trait StackModal in modal/stack.rs)
/// </summary>
public interface IUpstreamStackModal
{
    /// <summary>
    /// Handle an event; return non-null to close the modal.
    /// (Rust: fn handle_event)
    /// </summary>
    object? HandleEvent(object @event, object? hit);

    /// <summary>Render the modal content. (Rust: fn render_content)</summary>
    void RenderContent(Rect area, object frame);
}

// ── UpstreamModalStack ────────────────────────────────────────────────────

/// <summary>
/// Full-fidelity port of the upstream ModalStack (modal/stack.rs).
/// Manages nested modals in LIFO order with focus-group associations.
/// (Rust: pub struct ModalStack)
/// </summary>
public sealed class UpstreamModalStack
{
    private sealed class ActiveModal
    {
        public UpstreamModalId Id;
        public IUpstreamStackModal Modal = null!;
        public uint? FocusGroupId;
        public ulong? FocusReturnFocus; // Option<FocusId>
    }

    private readonly List<ActiveModal> _modals = new();

    /// <summary>Push a modal without focus association. (Rust: pub fn push)</summary>
    public UpstreamModalId Push(IUpstreamStackModal modal) => PushWithFocus(modal, null);

    /// <summary>Push a modal with an associated focus group ID. (Rust: pub fn push_with_focus)</summary>
    public UpstreamModalId PushWithFocus(IUpstreamStackModal modal, uint? focusGroupId)
    {
        var id = UpstreamModalId.New();
        _modals.Add(new ActiveModal { Id = id, Modal = modal, FocusGroupId = focusGroupId });
        return id;
    }

    /// <summary>Get the focus group ID for a modal. (Rust: pub fn focus_group_id)</summary>
    public uint? FocusGroupId(UpstreamModalId modalId)
        => _modals.FirstOrDefault(m => m.Id == modalId)?.FocusGroupId;

    /// <summary>Get the ID of the top modal. (Rust: pub fn top_id)</summary>
    public UpstreamModalId? TopId() => _modals.Count > 0 ? _modals[^1].Id : null;

    /// <summary>Pop the top modal. (Rust: pub fn pop)</summary>
    public UpstreamModalResult? Pop()
    {
        if (_modals.Count == 0) return null;
        var m = _modals[^1];
        _modals.RemoveAt(_modals.Count - 1);
        return new UpstreamModalResult { Id = m.Id, FocusGroupId = m.FocusGroupId };
    }

    /// <summary>Pop a specific modal by ID (may be non-LIFO). (Rust: pub fn pop_id)</summary>
    public UpstreamModalResult? PopId(UpstreamModalId id)
        => PopIdWithRestoreRetarget(id, retargetUpperReturnFocus: true);

    /// <summary>
    /// Pop a specific modal by ID with optional return-focus retargeting.
    /// (Rust: pub(super) fn pop_id_with_restore_retarget)
    /// </summary>
    internal UpstreamModalResult? PopIdWithRestoreRetarget(UpstreamModalId id, bool retargetUpperReturnFocus)
    {
        var idx = _modals.FindIndex(m => m.Id == id);
        if (idx < 0) return null;
        var modal = _modals[idx];
        _modals.RemoveAt(idx);

        if (retargetUpperReturnFocus && modal.FocusGroupId.HasValue)
        {
            var upper = _modals.Skip(idx).FirstOrDefault(m => m.FocusGroupId.HasValue);
            if (upper != null) upper.FocusReturnFocus = modal.FocusReturnFocus;
        }

        return new UpstreamModalResult { Id = modal.Id, FocusGroupId = modal.FocusGroupId };
    }

    /// <summary>Pop all modals, returning results in LIFO order (top first). (Rust: pub fn pop_all)</summary>
    public List<UpstreamModalResult> PopAll()
    {
        var results = new List<UpstreamModalResult>();
        while (Pop() is { } r) results.Add(r);
        return results;
    }

    /// <summary>Handle an event, routing to the top modal only. (Rust: pub fn handle_event)</summary>
    public UpstreamModalResult? HandleEvent(object @event, object? hit)
    {
        if (_modals.Count == 0) return null;
        var top = _modals[^1];
        var data = top.Modal.HandleEvent(@event, hit);
        if (data == null) return null;
        _modals.RemoveAt(_modals.Count - 1);
        return new UpstreamModalResult { Id = top.Id, FocusGroupId = top.FocusGroupId };
    }

    /// <summary>Get focus-modal specs in stack order (bottom to top). (Rust: pub(super) fn focus_modal_specs_in_order)</summary>
    internal List<(UpstreamModalId, FocusTrapSpec)> FocusModalSpecsInOrder()
    {
        var result = new List<(UpstreamModalId, FocusTrapSpec)>();
        foreach (var m in _modals)
            if (m.FocusGroupId.HasValue)
                result.Add((m.Id, new FocusTrapSpec(m.FocusGroupId.Value, m.FocusReturnFocus)));
        return result;
    }

    /// <summary>Set the return-focus for a specific modal. (Rust: pub(super) fn set_focus_return_focus)</summary>
    internal bool SetFocusReturnFocus(UpstreamModalId modalId, ulong? returnFocus)
    {
        var m = _modals.FirstOrDefault(m2 => m2.Id == modalId);
        if (m == null || !m.FocusGroupId.HasValue) return false;
        m.FocusReturnFocus = returnFocus;
        return true;
    }

    /// <summary>Find the next focus-modal after the given ID (bottom-to-top scan). (Rust: pub(super) fn next_focus_modal_after)</summary>
    internal (UpstreamModalId, uint)? NextFocusModalAfter(UpstreamModalId modalId)
    {
        var idx = _modals.FindIndex(m => m.Id == modalId);
        if (idx < 0) return null;
        for (int i = idx + 1; i < _modals.Count; i++)
            if (_modals[i].FocusGroupId.HasValue)
                return (_modals[i].Id, _modals[i].FocusGroupId!.Value);
        return null;
    }

    /// <summary>Check if the stack is empty. (Rust: pub fn is_empty)</summary>
    public bool IsEmpty() => _modals.Count == 0;

    /// <summary>Number of open modals. (Rust: pub fn depth)</summary>
    public int Depth() => _modals.Count;

    /// <summary>Render all modals (frame is passed as object; full rendering requires Frame type). (Rust: pub fn render)</summary>
    public void Render(object frame, Rect screen)
    {
        foreach (var m in _modals) m.Modal.RenderContent(screen, frame);
    }
}

// ── FocusGroupCounter ─────────────────────────────────────────────────────

/// <summary>
/// Global counter for unique focus group IDs.
/// (Rust: static FOCUS_GROUP_COUNTER: AtomicU32 = AtomicU32::new(1_000_000))
/// </summary>
public static class FocusGroupCounter
{
    private static uint _counter = 999_999; // starts at 1_000_000 after first fetch_add

    /// <summary>Atomically increment and return the old value (fetch_add semantics).</summary>
    public static uint FetchAdd()
    {
        uint prev;
        uint next;
        do
        {
            prev = Volatile.Read(ref _counter);
            next = prev + 1;
        } while (Interlocked.CompareExchange(ref _counter, next, prev) != prev);
        return prev;
    }

    /// <summary>Load the current value without incrementing.</summary>
    public static uint Load() => Volatile.Read(ref _counter);
}

// ── NextFocusGroupId ──────────────────────────────────────────────────────

/// <summary>
/// Generate a unique focus group ID that does not collide with any existing group.
/// (Rust: pub(super) fn next_focus_group_id)
/// </summary>
internal static class FocusGroupIdAllocator
{
    public static uint NextFocusGroupId(UpstreamFocusManager focusManager)
    {
        while (true)
        {
            var groupId = FocusGroupCounter.FetchAdd();
            if (!focusManager.HasGroup(groupId)) return groupId;
        }
    }
}

// ── ModalFocusCoordinator ─────────────────────────────────────────────────

/// <summary>
/// Internal coordinator combining <see cref="UpstreamModalStack"/> and
/// <see cref="UpstreamFocusManager"/> for focus-trap management.
/// (Rust: pub(super) struct ModalFocusCoordinator)
/// </summary>
internal sealed class ModalFocusCoordinator
{
    private readonly UpstreamModalStack _stack;
    private readonly UpstreamFocusManager _focusManager;

    // Rust: base_focus: &'a mut Option<Option<FocusId>>
    // We use (bool, ulong?) to represent Option<Option<ulong>>:
    //   (!set)         → None
    //   (set, null)    → Some(None)
    //   (set, ulong)   → Some(Some(ulong))
    private bool _baseFocusSet;
    private ulong? _baseFocus;

    public ModalFocusCoordinator(
        UpstreamModalStack stack,
        UpstreamFocusManager focusManager,
        bool baseFocusSet,
        ulong? baseFocus)
    {
        _stack = stack;
        _focusManager = focusManager;
        _baseFocusSet = baseFocusSet;
        _baseFocus = baseFocus;
    }

    public bool ResultBaseFocusSet => _baseFocusSet;
    public ulong? ResultBaseFocus => _baseFocus;

    // ---- Core operations ---------------------------------------------------

    /// <summary>Push a modal with optional focus trap. (Rust: pub(super) fn push_modal_with_trap)</summary>
    public UpstreamModalId PushModalWithTrap(
        IUpstreamStackModal modal,
        List<ulong>? focusableIds,
        bool trapEnabled,
        Func<UpstreamFocusManager, uint> allocateGroupId)
    {
        var baseFocusVal = _focusManager.HostFocused()
            ? _focusManager.Current()
            : _focusManager.DeferredFocusTarget();
        var wasTrapped = _focusManager.IsTrapped();

        uint? focusGroupId = null;

        if (trapEnabled && focusableIds != null)
        {
            var groupId = allocateGroupId(_focusManager);
            var hasDeclaredMembers = focusableIds.Count > 0;
            _focusManager.CreateGroupPreservingMembers(groupId, focusableIds);
            var trapped = _focusManager.PushTrap(groupId);
            if (!trapped && !hasDeclaredMembers)
            {
                _focusManager.RemoveGroup(groupId);
                // focusGroupId stays null
            }
            else
            {
                if (!wasTrapped && trapped)
                {
                    _baseFocusSet = true;
                    _baseFocus = baseFocusVal;
                }
                focusGroupId = groupId;
            }
        }

        var modalId = _stack.PushWithFocus(modal, focusGroupId);
        if (focusGroupId.HasValue)
            _stack.SetFocusReturnFocus(modalId, baseFocusVal);
        return modalId;
    }

    /// <summary>Pop the top modal. (Rust: pub(super) fn pop_modal)</summary>
    public UpstreamModalResult? PopModal()
    {
        var result = _stack.Pop();
        if (result != null) HandleClosedResult(result);
        return result;
    }

    /// <summary>Pop a specific modal by ID. (Rust: pub(super) fn pop_modal_by_id)</summary>
    public UpstreamModalResult? PopModalById(UpstreamModalId id)
    {
        if (_stack.TopId() == id) return PopModal();

        if (_stack.FocusGroupId(id) is { } groupId)
        {
            var removedMembers = _focusManager.GroupMembers(groupId);
            var removedGroupActive = GroupHasFocusableMember(groupId);
            var effectiveList = EffectiveFocusReturnFocusesInOrderSkipping(null);
            var removedEffective = effectiveList.FirstOrDefault(p => p.GroupId == groupId);
            bool hasRemovedEffective = removedEffective.GroupId != 0 || effectiveList.Any(p => p.GroupId == groupId);
            // Re-check: find by scanning
            bool foundRemovedEffective = false;
            EffectiveReturnFocusPair removedEffectivePair = default;
            foreach (var pair in effectiveList)
            {
                if (pair.GroupId == groupId) { removedEffectivePair = pair; foundRemovedEffective = true; break; }
            }

            if (_stack.NextFocusModalAfter(id) is { } upperPair)
            {
                var upperModalId = upperPair.Item1;
                bool shouldRetarget;
                if (removedGroupActive)
                {
                    shouldRetarget = true;
                }
                else
                {
                    ulong? upperReturnFocus = null;
                    foreach (var (mid, spec) in _stack.FocusModalSpecsInOrder())
                        if (mid == upperModalId) { upperReturnFocus = spec.ReturnFocus; break; }
                    shouldRetarget = !ReturnFocusRemainsValidAfterRemovingGroup(
                        upperModalId, upperReturnFocus, groupId, removedMembers);
                }

                if (shouldRetarget && foundRemovedEffective)
                    _stack.SetFocusReturnFocus(upperModalId, removedEffectivePair.ReturnFocus);
            }
        }

        var result = _stack.PopIdWithRestoreRetarget(id, retargetUpperReturnFocus: false);
        if (result == null) return null;

        if (result.FocusGroupId.HasValue)
        {
            var closingMembers = _focusManager.GroupMembers(result.FocusGroupId.Value);
            _focusManager.RemoveGroupWithoutRepair(result.FocusGroupId.Value);
            _focusManager.ClearDeferredFocusIfExcluded(closingMembers);
            RebuildFocusTraps();
            _focusManager.RepairFocusAfterExcludingIds(closingMembers);
            RefreshInactiveModalReturnFocusTargets();
        }
        return result;
    }

    /// <summary>Pop all modals. (Rust: pub(super) fn pop_all_modals)</summary>
    public List<UpstreamModalResult> PopAllModals()
    {
        var results = _stack.PopAll();
        bool removedGroup = false;
        var removedMembers = new List<ulong>();
        foreach (var result in results)
        {
            if (result.FocusGroupId.HasValue)
            {
                removedMembers.AddRange(_focusManager.GroupMembers(result.FocusGroupId.Value));
                _focusManager.RemoveGroupWithoutRepair(result.FocusGroupId.Value);
                removedGroup = true;
            }
        }
        if (removedGroup)
        {
            _focusManager.ClearDeferredFocusIfExcluded(removedMembers);
            RebuildFocusTraps();
            _focusManager.RepairFocusAfterExcludingIds(removedMembers);
            RefreshInactiveModalReturnFocusTargets();
        }
        return results;
    }

    /// <summary>Handle a modal event. (Rust: pub(super) fn handle_modal_event)</summary>
    public UpstreamModalResult? HandleModalEvent(object @event, object? hit)
    {
        if (@event is FocusEvent focusEvt)
        {
            if (focusEvt.Focused && _stack.IsEmpty() && _baseFocusSet)
            {
                var deferredFocus = _focusManager.DeferredFocusTarget();
                _focusManager.SetHostFocused(true);
                if (deferredFocus.HasValue)
                {
                    _baseFocusSet = true;
                    _baseFocus = deferredFocus;
                }
                RebuildFocusTraps();
            }
            else
            {
                _focusManager.ApplyHostFocus(focusEvt.Focused);
            }
            if (focusEvt.Focused)
                RefreshInactiveModalReturnFocusTargets();
        }

        var result = _stack.HandleEvent(@event, hit);
        if (result != null) HandleClosedResult(result);
        return result;
    }

    /// <summary>Rebuild all focus traps from the collapsed trap specs. (Rust: pub(super) fn rebuild_focus_traps)</summary>
    public void RebuildFocusTraps()
    {
        var (trapSpecs, trailingFailedRestoreSet, trailingFailedRestore) = CollapsedFocusTrapSpecs();
        var hadActiveTrapBefore = _focusManager.IsTrapped();
        var preservedLogicalTarget = _focusManager.LogicalFocusTarget();
        var activationBaseFocus = _focusManager.HostFocused()
            ? _focusManager.Current()
            : _focusManager.DeferredFocusTarget();
        _focusManager.ClearTraps();

        if (!_focusManager.HostFocused())
        {
            bool hasActiveTrap = false;
            if (_focusManager.Current().HasValue) _focusManager.Blur();

            foreach (var trap in trapSpecs)
                hasActiveTrap |= _focusManager.PushTrapWithReturnFocus(trap.GroupId, trap.ReturnFocus);

            if (hasActiveTrap && !hadActiveTrapBefore && !_baseFocusSet)
            {
                _baseFocusSet = true;
                _baseFocus = activationBaseFocus;
            }

            if (!hasActiveTrap)
            {
                var (restoreSet, restoreTarget) = GetRestoreTarget(!hadActiveTrapBefore, preservedLogicalTarget, trailingFailedRestoreSet, trailingFailedRestore);
                if (restoreSet)
                    _focusManager.ReplaceDeferredFocusTarget(restoreTarget);
            }
            else if (_focusManager.LogicalFocusTarget() == null && trailingFailedRestoreSet)
            {
                _focusManager.ReplaceDeferredFocusTarget(trailingFailedRestore);
            }
            return;
        }

        // Host is focused
        bool hasActiveTrapFocused = false;
        foreach (var trap in trapSpecs)
            hasActiveTrapFocused |= _focusManager.PushTrapWithReturnFocus(trap.GroupId, trap.ReturnFocus);

        if (hasActiveTrapFocused && !hadActiveTrapBefore && !_baseFocusSet)
        {
            _baseFocusSet = true;
            _baseFocus = activationBaseFocus;
        }

        if (!hasActiveTrapFocused)
        {
            var (restoreSet, restoreTarget) = GetRestoreTarget(!hadActiveTrapBefore, preservedLogicalTarget, trailingFailedRestoreSet, trailingFailedRestore);

            if (restoreSet && restoreTarget.HasValue)
            {
                var _ = _focusManager.FocusWithoutHistory(restoreTarget.Value);
            }
            else if (restoreSet && !restoreTarget.HasValue && _focusManager.Current().HasValue)
            {
                _focusManager.Blur();
            }

            // If focus_without_history failed, try first focusable
            if (restoreSet && restoreTarget.HasValue && _focusManager.Current() != restoreTarget.Value)
                _focusManager.FocusFirstWithoutHistoryForRestore();

            // Clear if current became non-focusable
            if (_focusManager.Current() is { } cur)
            {
                var node = _focusManager.Graph().Get(cur);
                if (node == null || !node.IsFocusable) _focusManager.Blur();
            }

            _baseFocusSet = false;
            _baseFocus = null;
            return;
        }

        if (_focusManager.LogicalFocusTarget() == null && trailingFailedRestoreSet && trailingFailedRestore.HasValue)
            _focusManager.FocusWithoutHistory(trailingFailedRestore.Value);

        _focusManager.ApplyHostFocus(true);
    }

    /// <summary>Refresh return-focus targets for inactive modals. (Rust: pub(super) fn refresh_inactive_modal_return_focus_targets)</summary>
    public void RefreshInactiveModalReturnFocusTargets()
    {
        var logicalTarget = _focusManager.LogicalFocusTarget();
        var focusModals = _stack.FocusModalSpecsInOrder();

        int? topmostActiveIndex = null;
        for (int i = focusModals.Count - 1; i >= 0; i--)
        {
            if (GroupHasFocusableMember(focusModals[i].Item2.GroupId))
            { topmostActiveIndex = i; break; }
        }

        var startIndex = topmostActiveIndex.HasValue ? topmostActiveIndex.Value + 1 : 0;
        for (int i = startIndex; i < focusModals.Count; i++)
        {
            var (modalId, trap) = focusModals[i];
            if (GroupHasFocusableMember(trap.GroupId)) continue;
            _stack.SetFocusReturnFocus(modalId, logicalTarget);
        }

        RefreshActiveModalReturnFocusTargetsForInvalidLowerSelections(focusModals, topmostActiveIndex);
    }

    // ---- Private helpers ---------------------------------------------------

    private void HandleClosedResult(UpstreamModalResult result)
    {
        if (result.FocusGroupId.HasValue) CloseFocusGroup(result.FocusGroupId.Value);
    }

    private void CloseFocusGroup(uint groupId)
    {
        var closingMembers = _focusManager.GroupMembers(groupId);
        if (GroupHasFocusableMember(groupId))
        {
            _focusManager.PopTrap();
            _focusManager.RemoveGroup(groupId);
        }
        else
        {
            _focusManager.RemoveGroupWithoutRepair(groupId);
        }
        _focusManager.RepairFocusAfterExcludingIds(closingMembers);
        if (!_focusManager.IsTrapped() && _focusManager.HostFocused())
        {
            _baseFocusSet = false;
            _baseFocus = null;
        }
        RefreshInactiveModalReturnFocusTargets();
    }

    private bool ReturnFocusRemainsValidAfterRemovingGroup(
        UpstreamModalId upperModalId,
        ulong? returnFocus,
        uint removedGroupId,
        IReadOnlyList<ulong> removedMembers)
    {
        uint? survivingLowerActiveGroup = null;
        foreach (var (modalId, trap) in _stack.FocusModalSpecsInOrder())
        {
            if (modalId == upperModalId) break;
            if (trap.GroupId == removedGroupId) continue;
            if (GroupHasFocusableMember(trap.GroupId)) survivingLowerActiveGroup = trap.GroupId;
        }

        if (survivingLowerActiveGroup.HasValue)
            return FocusTargetInGroup(returnFocus, survivingLowerActiveGroup.Value);

        return !returnFocus.HasValue
            ? true
            : FocusTargetIsFocusable(returnFocus) && !removedMembers.Contains(returnFocus.Value);
    }

    private readonly struct EffectiveReturnFocusPair
    {
        public readonly uint GroupId;
        public readonly ulong? ReturnFocus;
        public EffectiveReturnFocusPair(uint groupId, ulong? returnFocus) { GroupId = groupId; ReturnFocus = returnFocus; }
    }

    private List<EffectiveReturnFocusPair> EffectiveFocusReturnFocusesInOrderSkipping(UpstreamModalId? skippedModalId)
    {
        var effective = new List<EffectiveReturnFocusPair>();
        uint? lowerActiveGroup = null;
        bool lowerFallbackSet = false;
        ulong? lowerFallbackReturnFocus = null;

        foreach (var (modalId, trap) in _stack.FocusModalSpecsInOrder())
        {
            if (skippedModalId.HasValue && modalId == skippedModalId.Value) continue;

            ulong? effectiveReturnFocus;
            if (lowerActiveGroup.HasValue)
            {
                effectiveReturnFocus = FocusTargetInGroup(trap.ReturnFocus, lowerActiveGroup.Value)
                    ? trap.ReturnFocus
                    : (lowerFallbackSet ? lowerFallbackReturnFocus : trap.ReturnFocus);
            }
            else if (FocusTargetIsFocusable(trap.ReturnFocus))
            {
                effectiveReturnFocus = trap.ReturnFocus;
            }
            else
            {
                effectiveReturnFocus = lowerFallbackSet ? lowerFallbackReturnFocus : trap.ReturnFocus;
            }

            effective.Add(new EffectiveReturnFocusPair(trap.GroupId, effectiveReturnFocus));
            lowerFallbackSet = true;
            lowerFallbackReturnFocus = effectiveReturnFocus;
            if (GroupHasFocusableMember(trap.GroupId)) lowerActiveGroup = trap.GroupId;
        }

        return effective;
    }

    // Returns (specs, trailingFailedRestoreSet, trailingFailedRestoreValue)
    private (List<FocusTrapSpec>, bool, ulong?) CollapsedFocusTrapSpecs()
    {
        var collapsed = new List<FocusTrapSpec>();
        bool trailingSet = false;
        ulong? trailingFailedRestore = null;

        foreach (var pair in EffectiveFocusReturnFocusesInOrderSkipping(null))
        {
            if (GroupHasFocusableMember(pair.GroupId))
            {
                collapsed.Add(new FocusTrapSpec(pair.GroupId, pair.ReturnFocus));
                trailingSet = false;
                trailingFailedRestore = null;
            }
            else
            {
                trailingSet = true;
                trailingFailedRestore = pair.ReturnFocus;
            }
        }

        return (collapsed, trailingSet, trailingFailedRestore);
    }

    private bool GroupHasFocusableMember(uint groupId)
        => _focusManager.GroupMembers(groupId).Any(id => FocusTargetIsFocusable(id));

    private bool FocusTargetIsFocusable(ulong? target)
        => target.HasValue && _focusManager.Graph().Get(target.Value)?.IsFocusable == true;

    private bool FocusTargetInGroup(ulong? target, uint groupId)
        => target.HasValue && FocusTargetIsFocusable(target)
            && _focusManager.GroupMembers(groupId).Contains(target.Value);

    private ulong? FirstFocusableInGroup(uint groupId)
        => _focusManager.GroupPrimaryFocusTarget(groupId);

    private void RefreshActiveModalReturnFocusTargetsForInvalidLowerSelections(
        List<(UpstreamModalId, FocusTrapSpec)> focusModals,
        int? topmostActiveIndex)
    {
        if (!topmostActiveIndex.HasValue) return;
        for (int upperIdx = 1; upperIdx <= topmostActiveIndex.Value; upperIdx++)
        {
            var (_, lowerTrap) = focusModals[upperIdx - 1];
            var (upperModalId, upperTrap) = focusModals[upperIdx];
            if (!GroupHasFocusableMember(lowerTrap.GroupId)
                || FocusTargetInGroup(upperTrap.ReturnFocus, lowerTrap.GroupId))
                continue;
            var replacement = FirstFocusableInGroup(lowerTrap.GroupId);
            _stack.SetFocusReturnFocus(upperModalId, replacement);
        }
    }

    // Rust:
    //   let restore_target = (!had_active_trap_before)
    //       .then_some(preserved_logical_target).flatten().map(Some)
    //       .or(trailing_failed_restore)
    //       .or(*self.base_focus);
    //
    // Returns (isSet, value) representing Option<Option<FocusId>>
    private (bool isSet, ulong? value) GetRestoreTarget(
        bool notTrappedBefore,
        ulong? preservedLogicalTarget,
        bool trailingFailedRestoreSet,
        ulong? trailingFailedRestore)
    {
        if (notTrappedBefore && preservedLogicalTarget.HasValue)
            return (true, preservedLogicalTarget); // Some(Some(id))

        // (!not_trapped_before).then_some(...) → None if was trapped before
        // then .or(trailing_failed_restore)
        if (trailingFailedRestoreSet)
            return (true, trailingFailedRestore); // Some(Some(id)) or Some(None)

        if (_baseFocusSet)
            return (true, _baseFocus);

        return (false, null); // None
    }
}

// ── FocusEvent (thin wrapper) ─────────────────────────────────────────────

/// <summary>
/// Represents the Rust <c>Event::Focus(bool)</c> variant used by
/// <see cref="ModalFocusCoordinator.HandleModalEvent"/>.
/// </summary>
public sealed class FocusEvent
{
    public bool Focused { get; }
    public FocusEvent(bool focused) => Focused = focused;
}

// ── FocusAwareModalStack ──────────────────────────────────────────────────

/// <summary>
/// Modal stack with integrated focus management.
///
/// This wrapper provides automatic focus trapping when modals open and focus
/// restoration when they close. It manages both the modal stack and focus
/// manager in a coordinated way.
///
/// # Invariants
///
/// - Focus trap stack depth equals the number of modals with focus groups.
/// - Each modal's focus group ID is unique and not reused.
/// - Pop operations always call PopTrap for modals with focus groups.
///
/// (Rust: pub struct FocusAwareModalStack)
/// </summary>
public sealed class FocusAwareModalStack
{
    private readonly UpstreamModalStack _stack;
    private readonly UpstreamFocusManager _focusManager;

    // Rust: base_focus: Option<Option<FocusId>>
    // None       → (!_baseFocusSet)
    // Some(None) → (_baseFocusSet, _baseFocus == null)
    // Some(x)    → (_baseFocusSet, _baseFocus == x)
    private bool _baseFocusSet;
    private ulong? _baseFocus;

    // ---- Constructors -------------------------------------------------------

    /// <summary>Create a new focus-aware modal stack. (Rust: pub fn new)</summary>
    public FocusAwareModalStack()
    {
        _stack = new UpstreamModalStack();
        _focusManager = new UpstreamFocusManager();
    }

    /// <summary>
    /// Create from an existing focus manager. (Rust: pub fn with_focus_manager)
    ///
    /// The provided manager must not already have active modal traps.
    /// </summary>
    public static FocusAwareModalStack WithFocusManager(UpstreamFocusManager focusManager)
    {
        if (focusManager.IsTrapped())
            throw new InvalidOperationException(
                "FocusAwareModalStack requires a FocusManager without active traps");
        return new FocusAwareModalStack(new UpstreamModalStack(), focusManager);
    }

    private FocusAwareModalStack(UpstreamModalStack stack, UpstreamFocusManager focusManager)
    {
        _stack = stack;
        _focusManager = focusManager;
    }

    // ---- Default instance ---------------------------------------------------

    /// <summary>Default creates an empty stack. (Rust: impl Default)</summary>
    public static FocusAwareModalStack Default() => new();

    // ---- Modal stack delegation -------------------------------------------

    /// <summary>Push a modal without focus trapping. (Rust: pub fn push)</summary>
    public UpstreamModalId Push(IUpstreamStackModal modal) => _stack.Push(modal);

    /// <summary>
    /// Push a modal with automatic focus trapping. (Rust: pub fn push_with_trap)
    ///
    /// Creates a focus group, pushes a focus trap, moves focus to the first element.
    /// </summary>
    public UpstreamModalId PushWithTrap(IUpstreamStackModal modal, List<ulong> focusableIds)
    {
        var coord = MakeCoordinator();
        var id = coord.PushModalWithTrap(modal, focusableIds, trapEnabled: true, FocusGroupIdAllocator.NextFocusGroupId);
        SyncFrom(coord);
        return id;
    }

    /// <summary>
    /// Pop the top modal. (Rust: pub fn pop)
    ///
    /// If the modal had a focus group, the focus trap is popped and
    /// focus is restored to where it was before the modal opened.
    /// </summary>
    public UpstreamModalResult? Pop()
    {
        var coord = MakeCoordinator();
        var result = coord.PopModal();
        SyncFrom(coord);
        return result;
    }

    /// <summary>Pop a specific modal by ID. (Rust: pub fn pop_id)</summary>
    public UpstreamModalResult? PopId(UpstreamModalId id)
    {
        var coord = MakeCoordinator();
        var result = coord.PopModalById(id);
        SyncFrom(coord);
        return result;
    }

    /// <summary>Pop all modals, restoring focus to the original state. (Rust: pub fn pop_all)</summary>
    public List<UpstreamModalResult> PopAll()
    {
        var coord = MakeCoordinator();
        var results = coord.PopAllModals();
        SyncFrom(coord);
        return results;
    }

    /// <summary>
    /// Handle an event, routing to the top modal. (Rust: pub fn handle_event)
    ///
    /// Pass a <see cref="FocusEvent"/> for host focus-gain/loss notifications.
    /// </summary>
    public UpstreamModalResult? HandleEvent(object @event, object? hit = null)
    {
        var coord = MakeCoordinator();
        var result = coord.HandleModalEvent(@event, hit);
        SyncFrom(coord);
        return result;
    }

    /// <summary>Render all modals. (Rust: pub fn render)</summary>
    public void Render(object frame, Rect screen) => _stack.Render(frame, screen);

    /// <summary>
    /// Perform a direct focus-graph mutation and automatically resynchronise modal focus state.
    /// (Rust: pub fn with_focus_graph_mut)
    /// </summary>
    public R WithFocusGraphMut<R>(Func<UpstreamFocusGraph, R> f)
    {
        Exception? caught = null;
        R result = default!;
        try
        {
            result = f(_focusManager.GraphMut());
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        // Compute hadInvalidCurrent AFTER the mutation (matching upstream Rust order).
        var hadInvalidCurrent = _focusManager.Current() is { } cur
            && (_focusManager.Graph().Get(cur)?.IsFocusable != true);

        ResyncFocusState();

        var needsPostResync = hadInvalidCurrent
            && _focusManager.HostFocused()
            && (_focusManager.Current() == null
                || _focusManager.Graph().Get(_focusManager.Current()!.Value)?.IsFocusable != true);

        if (needsPostResync)
        {
            _focusManager.RestoreFocusAfterInvalidCurrent();
            ResyncInactiveModalReturnFocusTargets();
        }

        if (caught != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(caught).Throw();
        return result;
    }

    /// <summary>Convenience overload for side-effecting mutations.</summary>
    public void WithFocusGraphMut(Action<UpstreamFocusGraph> f)
        => WithFocusGraphMut<int>(g => { f(g); return 0; });

    /// <summary>Focus a specific target through the wrapped focus manager. (Rust: pub fn focus)</summary>
    public ulong? Focus(ulong id)
    {
        var previous = _focusManager.Focus(id);
        if (previous.HasValue
            || _focusManager.Current() == id
            || _focusManager.LogicalFocusTarget() == id)
        {
            ResyncInactiveModalReturnFocusTargets();
        }
        return previous;
    }

    // ---- State queries -------------------------------------------------------

    /// <summary>Check if the modal stack is empty. (Rust: pub fn is_empty)</summary>
    public bool IsEmpty() => _stack.IsEmpty();

    /// <summary>Get the number of open modals. (Rust: pub fn depth)</summary>
    public int Depth() => _stack.Depth();

    /// <summary>Check if focus is currently trapped in a modal. (Rust: pub fn is_focus_trapped)</summary>
    public bool IsFocusTrapped() => _focusManager.IsTrapped();

    /// <summary>Get a reference to the underlying modal stack. (Rust: pub fn stack)</summary>
    public UpstreamModalStack Stack() => _stack;

    /// <summary>Get a reference to the focus manager. (Rust: pub fn focus_manager)</summary>
    public UpstreamFocusManager FocusManager() => _focusManager;

    // ---- Test-only accessors (Rust: #[cfg(test)]) ----------------------------

    internal UpstreamModalStack StackMut() => _stack;
    internal UpstreamFocusManager FocusManagerMut() => _focusManager;

    // ---- Private helpers ----------------------------------------------------

    private ModalFocusCoordinator MakeCoordinator()
        => new(_stack, _focusManager, _baseFocusSet, _baseFocus);

    private void SyncFrom(ModalFocusCoordinator coord)
    {
        _baseFocusSet = coord.ResultBaseFocusSet;
        _baseFocus = coord.ResultBaseFocus;
    }

    private void ResyncFocusState()
    {
        var coord = MakeCoordinator();
        coord.RebuildFocusTraps();
        coord.RefreshInactiveModalReturnFocusTargets();
        SyncFrom(coord);
    }

    private void ResyncInactiveModalReturnFocusTargets()
    {
        var coord = MakeCoordinator();
        coord.RefreshInactiveModalReturnFocusTargets();
        SyncFrom(coord);
    }
}
