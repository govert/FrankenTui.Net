// Upstream source: .external/frankentui/crates/ftui-widgets/src/modal/focus_integration.rs (tests module)
// Full 1-1 port of all upstream focus_integration tests.

// DIVERGENCE: Rust tests use make_focus_node(id) which creates FocusNode::new(id, Rect::new(0,0,10,3)).with_tab_index(id as i32).
// DIVERGENCE: Rust tests access modals.stack (field) via modals.stack_mut(); here accessed via StackMut().
// DIVERGENCE: Rust tests access modals.focus_manager_mut() via FocusManagerMut().
// DIVERGENCE: Rust Event::Key(KeyEvent { code: KeyCode::Escape, ... }) → EscapeKeyEvent stub below.
// DIVERGENCE: Rust Event::Focus(bool) → FrankenTui.Widgets.Modal.FocusEvent(bool) wrapper.
// DIVERGENCE: Rust FocusEvent::FocusGained/FocusLost/FocusMoved → UpstreamFocusEvent.FocusGained/FocusLost/FocusMoved.
// DIVERGENCE: Rust StubWidget + WidgetModalEntry<StubWidget> → StubModal (IUpstreamStackModal) that closes on Escape.
// DIVERGENCE: Rust FOCUS_GROUP_COUNTER.load(Ordering::Relaxed) → FocusGroupCounter.Load().
// DIVERGENCE: std::panic::catch_unwind(...) → Assert.Throws<Exception> (xUnit).
// DIVERGENCE: modals.stack.focus_modal_specs_in_order() accessed via modals.Stack().FocusModalSpecsInOrder() (internal accessor via InternalsVisibleTo).

using FrankenTui.Core;
using FrankenTui.Widgets.Modal;

namespace FrankenTui.Tests.Headless;

public sealed class ModalFocusIntegrationTests
{
    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Rust: fn make_focus_node(id: FocusId) -> FocusNode
    /// FocusNode::new(id, Rect::new(0, 0, 10, 3)).with_tab_index(id as i32)
    /// </summary>
    private static UpstreamFocusNode MakeFocusNode(ulong id)
        => new UpstreamFocusNode(id, new Rect(0, 0, 10, 3)).WithTabIndex((int)id);

    /// <summary>
    /// Rust: struct StubWidget + WidgetModalEntry::new(StubWidget)
    /// A modal that closes when it receives an EscapeKeyEvent.
    /// </summary>
    private sealed class StubModal : IUpstreamStackModal
    {
        public object? HandleEvent(object @event, object? hit)
            => @event is EscapeKeyEvent ? new object() : null;

        public void RenderContent(Rect area, object frame) { }
    }

    /// <summary>Rust: Event::Key(KeyEvent { code: KeyCode::Escape, ... })</summary>
    private sealed class EscapeKeyEvent { }

    private static IUpstreamStackModal NewModal() => new StubModal();

    // ── Tests ─────────────────────────────────────────────────────────────

    [Fact]
    public void PushWithTrapCreatesFocusTrap()
    {
        var modals = new FocusAwareModalStack();

        // Add focusable nodes
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(2));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(3));

        // Focus node 3 before opening modal
        modals.FocusManagerMut().Focus(3);
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        // Push modal with trap containing nodes 1 and 2
        modals.PushWithTrap(NewModal(), new List<ulong> { 1, 2 });

        // Focus should now be on node 1 (first in group)
        Assert.True(modals.IsFocusTrapped());
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
    }

    [Fact]
    public void PopRestoresFocus()
    {
        var modals = new FocusAwareModalStack();

        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(2));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(3));

        modals.FocusManagerMut().Focus(3);

        modals.PushWithTrap(NewModal(), new List<ulong> { 1, 2 });
        Assert.Equal((ulong?)1, modals.FocusManager().Current());

        // Pop modal — focus should return to node 3
        modals.Pop();
        Assert.False(modals.IsFocusTrapped());
        Assert.Equal((ulong?)3, modals.FocusManager().Current());
    }

    [Fact]
    public void PopDiscardsClosedModalFocusHistory()
    {
        var modals = new FocusAwareModalStack();
        for (ulong id = 1; id <= 3; id++)
            modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(id));

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        Assert.Equal((ulong?)2, modals.Focus(3)); // returns previous (2)

        var result = modals.Pop();
        Assert.NotNull(result);
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.False(modals.FocusManagerMut().FocusBack());
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
    }

    [Fact]
    public void PopSkipsClosedModalFocusIdsWhenBackgroundFocusDisappears()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(50));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(100));

        modals.FocusManagerMut().Focus(100);
        modals.PushWithTrap(NewModal(), new List<ulong> { 1 });
        modals.FocusManagerMut().GraphMut().Remove(100);

        modals.Pop();
        Assert.Equal((ulong?)50, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void NestedModalsRestoreCorrectly()
    {
        var modals = new FocusAwareModalStack();
        for (ulong id = 1; id <= 6; id++)
            modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(id));

        modals.FocusManagerMut().Focus(1);

        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.PushWithTrap(NewModal(), new List<ulong> { 4, 5, 6 });
        Assert.Equal((ulong?)4, modals.FocusManager().Current());

        // Pop second modal — back to first modal's focus (node 2)
        modals.Pop();
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        // Pop first modal — back to original focus (node 1)
        modals.Pop();
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void PopRestoresNoneWhenModalOpenedWithoutFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));

        modals.PushWithTrap(NewModal(), new List<ulong> { 1 });
        Assert.Equal((ulong?)1, modals.FocusManager().Current());

        modals.Pop();
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void ResyncFocusStateRecoversAfterManualStackMutation()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(2));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(100));

        modals.FocusManagerMut().Focus(100);
        modals.PushWithTrap(NewModal(), new List<ulong> { 1, 2 });
        Assert.True(modals.IsFocusTrapped());
        Assert.Equal((ulong?)1, modals.FocusManager().Current());

        // Directly pop from the stack without going through FocusAwareModalStack
        var result = modals.StackMut().Pop();
        Assert.NotNull(result);
        Assert.True(modals.IsFocusTrapped()); // still thinks it's trapped

        // Rust: modals.resync_focus_state() — exposed as internal in C#
        // DIVERGENCE: Rust test calls resync_focus_state() which is private.
        // We call WithFocusGraphMut with identity to trigger resync.
        // Actually ResyncFocusState is private in C# — we trigger it via WithFocusGraphMut (no-op graph change).
        modals.WithFocusGraphMut(_ => { });
        Assert.False(modals.IsFocusTrapped());
        Assert.Equal((ulong?)100, modals.FocusManager().Current());
    }

    [Fact]
    public void HandleEventEscapeRestoresFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(2));

        modals.FocusManagerMut().Focus(2);

        modals.PushWithTrap(NewModal(), new List<ulong> { 1 });
        Assert.Equal((ulong?)1, modals.FocusManager().Current());

        var result = modals.HandleEvent(new EscapeKeyEvent());
        Assert.NotNull(result);
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
    }

    [Fact]
    public void HandleEventFocusLossBlursCurrentFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().Focus(1);
        var _ = modals.FocusManagerMut().TakeFocusEvent();

        var result = modals.HandleEvent(new FocusEvent(false));
        Assert.Null(result);
        Assert.Null(modals.FocusManager().Current());
        Assert.Equal(
            new UpstreamFocusEvent.FocusLost(1),
            modals.FocusManagerMut().TakeFocusEvent());
    }

    [Fact]
    public void HandleEventFocusGainRestoresTrappedFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(2));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(3));
        modals.FocusManagerMut().Focus(3);

        modals.PushWithTrap(NewModal(), new List<ulong> { 1, 2 });
        Assert.Equal((ulong?)1, modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        var result = modals.HandleEvent(new FocusEvent(true));
        Assert.Null(result);
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
    }

    [Fact]
    public void PushWithTrapAutofocusesNegativeTabindexMemberWhenModalHasNoTabbableNodes()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(
            new UpstreamFocusNode(2, new Rect(0, 0, 10, 3)).WithTabIndex(-1));
        modals.FocusManagerMut().Focus(1);

        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });

        Assert.True(modals.IsFocusTrapped());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
    }

    [Fact]
    public void PushWithTrapBlurredRestoresNegativeTabindexMemberOnFocusGain()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(
            new UpstreamFocusNode(2, new Rect(0, 0, 10, 3)).WithTabIndex(-1));
        modals.FocusManagerMut().Focus(1);
        modals.HandleEvent(new FocusEvent(false));

        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });

        Assert.True(modals.IsFocusTrapped());
        Assert.Null(modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
    }

    [Fact]
    public void PushWithoutTrapNoFocusChange()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(2));

        modals.FocusManagerMut().Focus(2);

        // Push modal without trap
        modals.Push(NewModal());

        Assert.False(modals.IsFocusTrapped());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
    }

    [Fact]
    public void PopAllRestoresAllFocus()
    {
        var modals = new FocusAwareModalStack();
        for (ulong id = 1; id <= 4; id++)
            modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(id));

        modals.FocusManagerMut().Focus(1);

        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 3 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        Assert.Equal(3, modals.Depth());
        Assert.Equal((ulong?)4, modals.FocusManager().Current());

        var results = modals.PopAll();
        Assert.Equal(3, results.Count);
        Assert.True(modals.IsEmpty());
        Assert.False(modals.IsFocusTrapped());
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
    }

    [Fact]
    public void PopAllRestoresBaseFocusWithoutIntermediateHop()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 5; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4, 5 });
        modals.Focus(5);
        modals.FocusManagerMut().TakeFocusEvent();
        var before = modals.FocusManager().FocusChangeCount();

        var results = modals.PopAll();

        Assert.Equal(2, results.Count);
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.Equal(
            new UpstreamFocusEvent.FocusMoved(5, 1),
            modals.FocusManagerMut().TakeFocusEvent());
        Assert.Equal(before + 1, modals.FocusManager().FocusChangeCount());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void PopIdRestoresNoneWhenLastModalOpenedWithoutFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));

        var modalId = modals.PushWithTrap(NewModal(), new List<ulong> { 1 });
        Assert.Equal((ulong?)1, modals.FocusManager().Current());

        modals.PopId(modalId);
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void PopIdRebuildPreservesUnfocusedBaseStateForRemainingModal()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(2));

        var lowerId = modals.PushWithTrap(NewModal(), new List<ulong> { 1 });
        var upperId = modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        var removed = modals.PopId(lowerId);
        Assert.Equal(lowerId, removed?.Id);
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        var closed = modals.Pop();
        Assert.Equal(upperId, closed?.Id);
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void TabNavigationTrappedInModal()
    {
        var modals = new FocusAwareModalStack();
        for (ulong id = 1; id <= 5; id++)
            modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(id));

        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.FocusManagerMut().FocusNext();
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.FocusManagerMut().FocusNext();
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        // Attempt to focus outside trap should fail
        Assert.Null(modals.FocusManagerMut().Focus(5));
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
    }

    [Fact]
    public void EmptyFocusGroupNoPanic()
    {
        var modals = new FocusAwareModalStack();

        // Push modal with empty focus group (edge case).
        // The trap is NOT pushed because the group has no focusable members.
        modals.PushWithTrap(NewModal(), new List<ulong>());

        Assert.False(modals.IsFocusTrapped());

        modals.Pop();
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void RejectedEmptyTrapDoesNotLeaveFocusGroupBehind()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().Focus(1);
        var groupCountBefore = modals.FocusManager().GroupCount();

        modals.PushWithTrap(NewModal(), new List<ulong>());

        Assert.False(modals.IsFocusTrapped());
        Assert.Equal(groupCountBefore, modals.FocusManager().GroupCount());
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
    }

    [Fact]
    public void LateRegisteredFocusIdsActivateModalTrapAndRestoreLatestBackgroundSelection()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(50));
            graph.Insert(MakeFocusNode(100));
        });

        modals.Focus(100);
        var modalId = modals.PushWithTrap(NewModal(), new List<ulong> { 1 });
        Assert.False(modals.IsFocusTrapped());
        Assert.Equal((ulong?)100, modals.FocusManager().Current());

        modals.Focus(50);
        Assert.Equal((ulong?)50, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(1)); });
        Assert.True(modals.IsFocusTrapped());
        Assert.Equal((ulong?)1, modals.FocusManager().Current());

        Assert.NotNull(modals.PopId(modalId));
        Assert.Equal((ulong?)50, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void BlurredPopAllAfterLateTrapActivationRestoresBackgroundFocusOnGain()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(50));
            graph.Insert(MakeFocusNode(100));
        });

        modals.Focus(100);
        modals.PushWithTrap(NewModal(), new List<ulong> { 1 });
        modals.Focus(50);

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(1)); });
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        var results = modals.PopAll();
        Assert.Equal(1, results.Count);
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)50, modals.FocusManager().Current());
    }

    [Fact]
    public void PushWithTrapDoesNotCollideWithExistingGroupIds()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(99));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(100));

        var reservedGroupId = FocusGroupCounter.Load();
        modals.FocusManager().CreateGroup(reservedGroupId, new List<ulong> { 99 });
        modals.FocusManagerMut().Focus(100);

        modals.PushWithTrap(NewModal(), new List<ulong> { 1 });
        modals.Pop();

        Assert.True(modals.FocusManagerMut().PushTrap(reservedGroupId));
        Assert.Equal((ulong?)99, modals.FocusManager().Current());
    }

    [Fact]
    public void PopIdNonTopModalRebuildsTraps()
    {
        var modals = new FocusAwareModalStack();
        for (ulong id = 1; id <= 6; id++)
            modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(id));

        modals.FocusManagerMut().Focus(1);

        var id1 = modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 3 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        Assert.Equal((ulong?)4, modals.FocusManager().Current());

        modals.PopId(id1);

        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.Equal(2, modals.Depth());
        Assert.True(modals.IsFocusTrapped());

        modals.Pop();
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.Pop();
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.True(modals.IsEmpty());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void PopIdMiddleModalRetargetsUpperReturnFocus()
    {
        var modals = new FocusAwareModalStack();
        for (ulong id = 1; id <= 6; id++)
            modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(id));

        modals.FocusManagerMut().Focus(1);

        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        var id2 = modals.PushWithTrap(NewModal(), new List<ulong> { 3 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        Assert.Equal((ulong?)4, modals.FocusManager().Current());

        modals.PopId(id2);
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.Equal(2, modals.Depth());

        modals.Pop();
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.Pop();
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void PopIdRebuildDoesNotPolluteFocusHistory()
    {
        var modals = new FocusAwareModalStack();
        for (ulong id = 1; id <= 6; id++)
            modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(id));

        modals.FocusManagerMut().Focus(1);
        modals.FocusManagerMut().Focus(6);

        var id1 = modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 3 });

        modals.PopId(id1);
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.Pop();
        Assert.Equal((ulong?)6, modals.FocusManager().Current());
        Assert.True(modals.FocusManagerMut().FocusBack());
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.False(modals.FocusManagerMut().FocusBack());
    }

    [Fact]
    public void PopIdTopModalRestoresFocusCorrectly()
    {
        var modals = new FocusAwareModalStack();
        for (ulong id = 1; id <= 4; id++)
            modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(id));

        modals.FocusManagerMut().Focus(1);

        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        var id2 = modals.PushWithTrap(NewModal(), new List<ulong> { 3 });

        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.PopId(id2);

        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.Pop();
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void PopIdTopModalPreservesUnderlyingSelectedControl()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 5; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        var upperId = modals.PushWithTrap(NewModal(), new List<ulong> { 4, 5 });
        modals.Focus(5);
        modals.FocusManagerMut().TakeFocusEvent();
        var before = modals.FocusManager().FocusChangeCount();

        Assert.NotNull(modals.PopId(upperId));
        Assert.Equal((ulong?)3, modals.FocusManager().Current());
        Assert.Equal(
            new UpstreamFocusEvent.FocusMoved(5, 3),
            modals.FocusManagerMut().TakeFocusEvent());
        Assert.Equal(before + 1, modals.FocusManager().FocusChangeCount());
        Assert.True(modals.IsFocusTrapped());

        modals.Pop();
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
    }

    [Fact]
    public void PopRemovesClosedModalFocusGroup()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(2));

        modals.FocusManagerMut().Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });

        var result = modals.Pop();
        Assert.NotNull(result);
        var groupId = result!.FocusGroupId;
        Assert.NotNull(groupId);

        Assert.False(modals.FocusManagerMut().PushTrap(groupId!.Value));
        Assert.False(modals.IsFocusTrapped());
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
    }

    [Fact]
    public void PopLastModalClearsInvalidStaleFocusWhenNoFallbackExists()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(2));

        modals.FocusManagerMut().Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.FocusManagerMut().GraphMut().Remove(1);
        modals.FocusManagerMut().GraphMut().Remove(2);

        modals.Pop();
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void DefaultCreatesEmptyStack()
    {
        var modals = FocusAwareModalStack.Default();
        Assert.True(modals.IsEmpty());
        Assert.Equal(0, modals.Depth());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void WithFocusManagerUsesProvided()
    {
        var fm = new UpstreamFocusManager();
        fm.GraphMut().Insert(MakeFocusNode(42));
        fm.Focus(42);

        var modals = FocusAwareModalStack.WithFocusManager(fm);
        Assert.True(modals.IsEmpty());
        Assert.Equal((ulong?)42, modals.FocusManager().Current());
    }

    [Fact]
    public void WithFocusManagerRejectsPretrappedManager()
    {
        var fm = new UpstreamFocusManager();
        fm.GraphMut().Insert(MakeFocusNode(1));
        fm.Focus(1);
        fm.CreateGroup(7, new List<ulong> { 1 });
        Assert.True(fm.PushTrap(7));

        Assert.Throws<InvalidOperationException>(() => FocusAwareModalStack.WithFocusManager(fm));
    }

    [Fact]
    public void StackAccessors()
    {
        var modals = new FocusAwareModalStack();
        Assert.True(modals.Stack().IsEmpty());
        modals.Push(NewModal());
        Assert.False(modals.Stack().IsEmpty());
        Assert.Equal(1, modals.StackMut().Depth());
    }

    [Fact]
    public void WithFocusGraphMutResyncsAfterPanic()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
        });
        modals.PushWithTrap(NewModal(), new List<ulong> { 1, 2 });
        Assert.Equal((ulong?)1, modals.FocusManager().Current());

        Assert.Throws<Exception>(() => {
            modals.WithFocusGraphMut(graph => {
                graph.Remove(1);
                throw new Exception("boom");
            });
        });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void WithFocusGraphMutRepairsInvalidFocusWithoutModals()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
        });
        modals.Focus(2);
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(2); });

        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void WithFocusGraphMutDoesNotRestoreFocusWhileHostBlurred()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
        });
        modals.Focus(2);
        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(2); });

        Assert.Null(modals.FocusManager().Current());
    }

    [Fact]
    public void FocusCallWhileHostBlurredDefersUntilFocusGain()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
            graph.Insert(MakeFocusNode(3));
        });
        modals.Focus(1);
        modals.FocusManagerMut().TakeFocusEvent();

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        Assert.Equal((ulong?)1, modals.Focus(3));
        Assert.Null(modals.FocusManager().Current());
        Assert.Equal(
            new UpstreamFocusEvent.FocusLost(1),
            modals.FocusManagerMut().TakeFocusEvent());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)3, modals.FocusManager().Current());
        Assert.Equal(
            new UpstreamFocusEvent.FocusGained(3),
            modals.FocusManagerMut().TakeFocusEvent());
    }

    [Fact]
    public void PopWhileHostBlurredDefersBaseFocusRestoreUntilFocusGain()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
        });
        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        var result = modals.Pop();
        Assert.NotNull(result);
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
    }

    [Fact]
    public void PopIdLastModalWhileHostBlurredRestoresBaseFocusOnFocusGain()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
        });
        modals.Focus(1);
        var modalId = modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        Assert.NotNull(modals.PopId(modalId));
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
    }

    [Fact]
    public void PopIdTopModalWhileHostBlurredRestoresUnderlyingModalSelectionOnFocusGain()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 5; id++) graph.Insert(MakeFocusNode(id));
        });
        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        var upperId = modals.PushWithTrap(NewModal(), new List<ulong> { 4, 5 });
        modals.Focus(5);
        modals.FocusManagerMut().TakeFocusEvent();

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        Assert.NotNull(modals.PopId(upperId));
        Assert.Null(modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)3, modals.FocusManager().Current());
    }

    [Fact]
    public void PopAllWhileHostBlurredRestoresBaseFocusOnFocusGain()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
            graph.Insert(MakeFocusNode(3));
        });
        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 3 });
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        var results = modals.PopAll();
        Assert.Equal(2, results.Count);
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
    }

    [Fact]
    public void FocusGainAfterBlurredPopRestoresBaseFocusWithoutIntermediateHop()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(5));
            graph.Insert(MakeFocusNode(10));
        });
        modals.Focus(5);
        modals.FocusManagerMut().TakeFocusEvent();
        modals.PushWithTrap(NewModal(), new List<ulong> { 10 });
        modals.FocusManagerMut().TakeFocusEvent();

        modals.HandleEvent(new FocusEvent(false));
        modals.FocusManagerMut().TakeFocusEvent();

        var result = modals.Pop();
        Assert.NotNull(result);
        Assert.Null(modals.FocusManager().Current());

        var before = modals.FocusManager().FocusChangeCount();
        modals.HandleEvent(new FocusEvent(true));

        Assert.Equal((ulong?)5, modals.FocusManager().Current());
        Assert.Equal(
            new UpstreamFocusEvent.FocusGained(5),
            modals.FocusManagerMut().TakeFocusEvent());
        Assert.Equal(before + 1, modals.FocusManager().FocusChangeCount());
    }

    [Fact]
    public void BlurredBackgroundFocusChangeAfterLastModalPopOverridesStaleBaseFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
            graph.Insert(MakeFocusNode(3));
        });
        modals.Focus(1);
        modals.FocusManagerMut().TakeFocusEvent();

        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        modals.FocusManagerMut().TakeFocusEvent();
        modals.HandleEvent(new FocusEvent(false));
        modals.FocusManagerMut().TakeFocusEvent();

        Assert.NotNull(modals.Pop());
        Assert.Null(modals.FocusManager().Current());

        Assert.Equal((ulong?)1, modals.Focus(3));
        Assert.Null(modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)3, modals.FocusManager().Current());
        Assert.Equal(
            new UpstreamFocusEvent.FocusGained(3),
            modals.FocusManagerMut().TakeFocusEvent());
    }

    [Fact]
    public void PopIdMiddleModalPreservesTopSelectionAndRetargetsRestoreChain()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 7; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        var middleId = modals.PushWithTrap(NewModal(), new List<ulong> { 4, 5 });
        modals.Focus(5);
        modals.PushWithTrap(NewModal(), new List<ulong> { 6, 7 });
        modals.Focus(7);
        modals.FocusManagerMut().TakeFocusEvent();

        var removed = modals.PopId(middleId);
        Assert.NotNull(removed);
        Assert.Equal((ulong?)7, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.Pop();
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.Pop();
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void PopIdBottomModalPreservesTopSelectionAndRetargetsToBaseFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 5; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        var lowerId = modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4, 5 });
        modals.Focus(5);
        modals.FocusManagerMut().TakeFocusEvent();

        var removed = modals.PopId(lowerId);
        Assert.NotNull(removed);
        Assert.Equal((ulong?)5, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.Pop();
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void PushWithTrapWhileHostBlurredDefersModalFocusUntilFocusGain()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
        });
        modals.Focus(1);
        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        Assert.Null(modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
    }

    [Fact]
    public void NestedPushWhileHostBlurredRestoresUnderlyingModalSelectionOnClose()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 4; id++) graph.Insert(MakeFocusNode(id));
        });
        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });
        Assert.Null(modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)4, modals.FocusManager().Current());

        var result = modals.Pop();
        Assert.NotNull(result);
        Assert.Equal((ulong?)3, modals.FocusManager().Current());
    }

    [Fact]
    public void FirstModalOpenedWhileBlurredFromUnfocusedBaseRestoresNone()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
        });
        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        Assert.Null(modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        var result = modals.Pop();
        Assert.NotNull(result);
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void PopIdNonTopWhileHostBlurredKeepsFocusClearedUntilFocusGain()
    {
        var modals = new FocusAwareModalStack();
        for (ulong id = 1; id <= 4; id++)
            modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(id));

        modals.FocusManagerMut().Focus(1);
        var id1 = modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 3 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });
        Assert.Equal((ulong?)4, modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        var result = modals.PopId(id1);
        Assert.NotNull(result);
        Assert.Null(modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
    }

    [Fact]
    public void PopIdTrappedModalWhileBlurredWithOnlyNonTrappedModalsRemainingRestoresBaseFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
        });

        modals.Focus(1);
        var trappedId = modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        modals.Push(NewModal());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        Assert.NotNull(modals.PopId(trappedId));
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
    }

    [Fact]
    public void PopIdInactiveTrappedModalWithOnlyNonTrappedModalsRemainingPreservesLatestBackgroundFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
            graph.Insert(MakeFocusNode(9));
        });

        modals.Focus(1);
        var trappedId = modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        modals.Push(NewModal());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(2); });
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        Assert.Equal((ulong?)1, modals.Focus(9));
        Assert.Equal((ulong?)9, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        Assert.NotNull(modals.PopId(trappedId));
        Assert.Equal((ulong?)9, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void BlurredPopIdInactiveTrappedModalWithOnlyNonTrappedModalsRemainingPreservesLatestBackgroundFocusOnFocusGain()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
            graph.Insert(MakeFocusNode(9));
        });

        modals.Focus(1);
        var trappedId = modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        modals.Push(NewModal());

        modals.WithFocusGraphMut(graph => { graph.Remove(2); });
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        Assert.Equal((ulong?)1, modals.Focus(9));
        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        Assert.NotNull(modals.PopId(trappedId));
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)9, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void BlurredPopIdInactiveTrappedModalPreservesLatestBackgroundFocusWhenTrapWentInactiveWhileBlurred()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
            graph.Insert(MakeFocusNode(9));
        });

        modals.Focus(1);
        var trappedId = modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        modals.Push(NewModal());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Remove(2); });
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        Assert.Equal((ulong?)1, modals.Focus(9));
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        Assert.NotNull(modals.PopId(trappedId));
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)9, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void FocusGainRefreshesInactiveModalRestoreTargetAfterBackgroundFallback()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(2));
            graph.Insert(MakeFocusNode(9));
        });

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        Assert.Null(modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Remove(2); });
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)9, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(2)); });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)9, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void WithFocusGraphMutBlurredEmptyTrapRestoresBaseFocusOnFocusGain()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
            graph.Insert(MakeFocusNode(3));
        });

        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(2); });

        Assert.Null(modals.FocusManager().Current());
        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)3, modals.FocusManager().Current());
    }

    [Fact]
    public void WithFocusGraphMutFocusedEmptyTrapRestoresBaseFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
            graph.Insert(MakeFocusNode(3));
        });

        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(2); });

        Assert.Equal((ulong?)3, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void WithFocusGraphMutFocusedEmptyTopTrapRestoresUnderlyingSelectedControl()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 4; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });
        Assert.Equal((ulong?)4, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });

        Assert.Equal((ulong?)3, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void WithFocusGraphMutBlurredEmptyTopTrapRestoresUnderlyingSelectedControlOnFocusGain()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 4; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });
        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)3, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void WithFocusGraphMutEmptyLowerTrapRetargetsSurvivingTopRestoreToBaseFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            foreach (var id in new ulong[] { 1, 5, 8, 10 }) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(10);
        modals.PushWithTrap(NewModal(), new List<ulong> { 5 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 8 });
        Assert.Equal((ulong?)8, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(5); });
        Assert.Equal((ulong?)8, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)10, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void PopAfterTopTrapBecomesEmptyPreservesUnderlyingTrap()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 4; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        Assert.Equal((ulong?)3, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)3, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
        Assert.Null(modals.Focus(1));
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void BlurredPopAfterTopTrapBecomesEmptyPreservesUnderlyingDeferredFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 4; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Null(modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)3, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void PopIdSkipsStaleRetargetFromInactiveMiddleModal()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 6; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        var staleMiddleId = modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        Assert.Equal((ulong?)3, modals.FocusManager().Current());
        modals.Focus(2);
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.PushWithTrap(NewModal(), new List<ulong> { 5, 6 });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        Assert.NotNull(modals.PopId(staleMiddleId));
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void BlurredPopIdSkipsStaleRetargetFromInactiveMiddleModal()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 6; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        var staleMiddleId = modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        Assert.Equal((ulong?)3, modals.Focus(2));
        modals.PushWithTrap(NewModal(), new List<ulong> { 5, 6 });

        Assert.NotNull(modals.PopId(staleMiddleId));
        Assert.Null(modals.FocusManager().Current());

        Assert.NotNull(modals.Pop());
        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void PopIdInactiveLowerModalPreservesSurvivingUpperRestoreToBaseFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            foreach (var id in new ulong[] { 5, 10, 20, 30 }) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(10);
        var lowerId = modals.PushWithTrap(NewModal(), new List<ulong> { 20 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 30 });
        Assert.Equal((ulong?)30, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(20); });
        Assert.Equal((ulong?)30, modals.FocusManager().Current());

        Assert.NotNull(modals.PopId(lowerId));
        Assert.Equal((ulong?)30, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)10, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void PopIdActiveLowerModalPropagatesNoneRestoreTargetToSurvivingUpperModal()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            graph.Insert(MakeFocusNode(1));
            graph.Insert(MakeFocusNode(2));
        });

        var lowerId = modals.PushWithTrap(NewModal(), new List<ulong> { 1 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 2 });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.PopId(lowerId));
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Null(modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void BlurredPopIdInactiveLowerModalPreservesSurvivingUpperRestoreToBaseFocus()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            foreach (var id in new ulong[] { 5, 10, 20, 30 }) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(10);
        var lowerId = modals.PushWithTrap(NewModal(), new List<ulong> { 20 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 30 });

        modals.WithFocusGraphMut(graph => { graph.Remove(20); });
        Assert.NotNull(modals.PopId(lowerId));

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        Assert.NotNull(modals.Pop());
        Assert.Null(modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)10, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());
    }

    [Fact]
    public void ReactivatedTopModalRestoresLatestUnderlyingSelection()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 4; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.Focus(2);
        Assert.Equal((ulong?)2, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(4)); });
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void ReactivatedTopModalTracksGraphRestoredSelectionWithinSameLowerGroup()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 4; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(3); });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(4)); });
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void BlurredReactivatedTopModalTracksGraphRestoredSelectionWithinSameLowerGroup()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 4; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(3); });
        Assert.Null(modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(4)); });
        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void BlurredReactivatedTopModalRestoresLatestUnderlyingSelection()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 4; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        Assert.Equal((ulong?)3, modals.Focus(2));

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(4)); });

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void ReactivatedInactiveTopModalTracksGraphRestoredUnderlyingSelection()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 7; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4, 5 });
        modals.Focus(5);
        modals.PushWithTrap(NewModal(), new List<ulong> { 6 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 7 });

        modals.WithFocusGraphMut(graph => { graph.Remove(7); });
        Assert.Equal((ulong?)6, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(6); });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(7)); });
        Assert.Equal((ulong?)7, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)5, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void ReactivatedInactiveModalChainTracksGraphRestoredUnderlyingSelection()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 5; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 5 });

        modals.WithFocusGraphMut(graph => { graph.Remove(5); });
        Assert.Equal((ulong?)4, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(3); });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(4)); });
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(5)); });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void ReactivatedLowerModalRefreshesStillInactiveUpperRestoreTarget()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            foreach (var id in new ulong[] { 1, 2, 4, 5 }) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 4 });
        modals.Focus(4);
        modals.PushWithTrap(NewModal(), new List<ulong> { 5 });

        modals.WithFocusGraphMut(graph => { graph.Remove(5); });
        Assert.Equal((ulong?)4, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(2); graph.Remove(4); });
        Assert.Equal((ulong?)1, modals.FocusManager().Current());
        Assert.False(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(2)); graph.Insert(MakeFocusNode(4)); });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(5)); });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void ReactivatedInactiveUpperModalDoesNotRestoreStaleLowerSelectionAfterTopPop()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 5; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.PushWithTrap(NewModal(), new List<ulong> { 5 });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(3); });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(4)); });
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(3)); });
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void BlurredReactivatedInactiveUpperModalDoesNotRestoreStaleLowerSelectionAfterTopPop()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 5; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.PushWithTrap(NewModal(), new List<ulong> { 5 });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(3); });
        Assert.Null(modals.FocusManager().Current());

        Assert.NotNull(modals.Pop());
        Assert.Null(modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(4)); });
        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(3)); });
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void ReactivatedMiddleModalBeforeTopCloseDoesNotRestoreStaleLowerSelection()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 5; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });
        Assert.Equal((ulong?)4, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.PushWithTrap(NewModal(), new List<ulong> { 5 });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(3); });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(4)); });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void RevalidatedStaleLowerTargetBeforeTopCloseDoesNotWinOnMiddlePop()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 5; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.PushWithTrap(NewModal(), new List<ulong> { 5 });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(3); });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(4)); });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(3)); });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void BlurredRevalidatedStaleLowerTargetBeforeTopCloseDoesNotWinOnMiddlePop()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 5; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4 });

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        Assert.Equal((ulong?)3, modals.FocusManager().Current());

        modals.PushWithTrap(NewModal(), new List<ulong> { 5 });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(3); });
        Assert.Null(modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(4)); });
        Assert.Null(modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(3)); });
        Assert.Null(modals.FocusManager().Current());

        Assert.NotNull(modals.Pop());
        Assert.Null(modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)4, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void InvalidatedLowerSelectionRetargetsUpperRestoreUsingGroupTabOrder()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 5; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        // Group is [4, 3, 2] by declaration order but tab order sorts by tab index
        modals.PushWithTrap(NewModal(), new List<ulong> { 4, 3, 2 });
        Assert.Equal((ulong?)2, modals.FocusManager().Current()); // lowest tab index
        modals.Focus(4);
        Assert.Equal((ulong?)4, modals.FocusManager().Current());

        modals.PushWithTrap(NewModal(), new List<ulong> { 5 });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void BlurredInvalidatedLowerSelectionRetargetsUpperRestoreUsingGroupTabOrder()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 5; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4, 3, 2 });
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        modals.Focus(4);
        Assert.Equal((ulong?)4, modals.FocusManager().Current());

        modals.PushWithTrap(NewModal(), new List<ulong> { 5 });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });
        Assert.Null(modals.FocusManager().Current());

        Assert.NotNull(modals.Pop());
        Assert.Null(modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)2, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void InvalidatedNegativeTabindexLowerSelectionRetargetsUpperRestoreMetadata()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(new UpstreamFocusNode(3, new Rect(0, 0, 10, 3)).WithTabIndex(-1));
        modals.FocusManagerMut().GraphMut().Insert(new UpstreamFocusNode(4, new Rect(0, 0, 10, 3)).WithTabIndex(-1));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(5));

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4, 3 });
        Assert.Equal((ulong?)4, modals.FocusManager().Current()); // negative tab: first in members order

        var upperId = modals.PushWithTrap(NewModal(), new List<ulong> { 5 });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });

        var upperReturnFocus = modals.Stack().FocusModalSpecsInOrder()
            .FirstOrDefault(pair => pair.Item1 == upperId).Item2.ReturnFocus;
        Assert.Equal((ulong?)3, upperReturnFocus);
    }

    [Fact]
    public void BlurredInvalidatedNegativeTabindexLowerSelectionRetargetsUpperRestoreMetadata()
    {
        var modals = new FocusAwareModalStack();
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(1));
        modals.FocusManagerMut().GraphMut().Insert(new UpstreamFocusNode(3, new Rect(0, 0, 10, 3)).WithTabIndex(-1));
        modals.FocusManagerMut().GraphMut().Insert(new UpstreamFocusNode(4, new Rect(0, 0, 10, 3)).WithTabIndex(-1));
        modals.FocusManagerMut().GraphMut().Insert(MakeFocusNode(5));

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4, 3 });
        Assert.Equal((ulong?)4, modals.FocusManager().Current());

        var upperId = modals.PushWithTrap(NewModal(), new List<ulong> { 5 });
        Assert.Equal((ulong?)5, modals.FocusManager().Current());

        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(4); });

        var upperReturnFocus = modals.Stack().FocusModalSpecsInOrder()
            .FirstOrDefault(pair => pair.Item1 == upperId).Item2.ReturnFocus;
        Assert.Equal((ulong?)3, upperReturnFocus);
    }

    [Fact]
    public void BlurredReactivatedInactiveTopModalTracksGraphRestoredUnderlyingSelection()
    {
        var modals = new FocusAwareModalStack();
        modals.WithFocusGraphMut(graph => {
            for (ulong id = 1; id <= 7; id++) graph.Insert(MakeFocusNode(id));
        });

        modals.Focus(1);
        modals.PushWithTrap(NewModal(), new List<ulong> { 2, 3 });
        modals.Focus(3);
        modals.PushWithTrap(NewModal(), new List<ulong> { 4, 5 });
        modals.Focus(5);
        modals.PushWithTrap(NewModal(), new List<ulong> { 6 });
        modals.PushWithTrap(NewModal(), new List<ulong> { 7 });

        modals.WithFocusGraphMut(graph => { graph.Remove(7); });
        modals.HandleEvent(new FocusEvent(false));
        Assert.Null(modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Remove(6); });
        Assert.Null(modals.FocusManager().Current());

        modals.WithFocusGraphMut(graph => { graph.Insert(MakeFocusNode(7)); });
        modals.HandleEvent(new FocusEvent(true));
        Assert.Equal((ulong?)7, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());

        Assert.NotNull(modals.Pop());
        Assert.Equal((ulong?)5, modals.FocusManager().Current());
        Assert.True(modals.IsFocusTrapped());
    }

    [Fact]
    public void DepthTracksPushPop()
    {
        var modals = new FocusAwareModalStack();
        Assert.Equal(0, modals.Depth());
        modals.Push(NewModal());
        Assert.Equal(1, modals.Depth());
        modals.Push(NewModal());
        Assert.Equal(2, modals.Depth());
        modals.Pop();
        Assert.Equal(1, modals.Depth());
    }

    [Fact]
    public void PopEmptyStackReturnsNone()
    {
        var modals = new FocusAwareModalStack();
        Assert.Null(modals.Pop());
    }
}
