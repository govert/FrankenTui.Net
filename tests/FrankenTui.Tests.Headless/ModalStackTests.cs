// Upstream source: .external/frankentui/crates/ftui-widgets/src/modal/stack.rs (tests module)
// Full 1-1 port of all upstream ModalStack tests.
//
// DIVERGENCE: Rust HitRegion::Custom(u8) algebraic variant with a u8 payload is mapped to
// HitRegionKind enum members (Custom, ModalBackdrop, ModalContent) or HitRegionKind.Custom
// with the tag stored in HitData. Custom(99) and Custom(100) in CloseOnInnerHitModal and
// CloseOnCollidingInnerHitModal are represented as HitRegionKind.Custom with the tag value
// stored exclusively in HitData (tag 99 vs 100 is not round-tripped through HitTestResult.Region).
// DIVERGENCE: Upstream ModalId(999999) literal construction uses an internal-accessible ctor.
// DIVERGENCE: Tracing-gated tests (tracing_modal_render_span_has_required_fields and
// tracing_focus_change_and_trap_events_emitted_for_modal_lifecycle) are skipped.
// DIVERGENCE: Upstream tests access struct field stack.modals directly; C# tests access
// internal _modals list via InternalsVisibleTo.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;
using FrankenTui.Widgets.Modal;
using Xunit;

namespace FrankenTui.Tests.Headless;

// ── Test stub types ───────────────────────────────────────────────────────────

/// <summary>
/// Minimal widget stub. Rust: struct StubWidget; impl Widget for StubWidget.
/// </summary>
file sealed class StubWidget : IWidget
{
    public void Render(Rect area, Frame frame) { }
}

/// <summary>
/// Closes on any hit. Rust: struct CloseOnAnyHitModal; impl StackModal.
/// </summary>
file sealed class CloseOnAnyHitModal : IStackModal
{
    void IStackModal.RenderContent(Rect area, Frame frame) { }

    ModalResultData? IStackModal.HandleEvent(
        TerminalEvent @event,
        (HitId, HitRegionKind, HitData)? hit,
        HitId hitId)
        => hit.HasValue ? ModalResultData.Dismissed.Instance : null;

    ModalSizeConstraints IStackModal.SizeConstraints() => new ModalSizeConstraints
    {
        MinWidth = 10, MaxWidth = 10, MinHeight = 3, MaxHeight = 3
    };

    BackdropConfig IStackModal.BackdropConfig() => BackdropConfig.Default;

    bool IStackModal.CloseOnBackdrop() => false;
}

/// <summary>
/// Closes when a backdrop hit is received on the modal's own hit_id.
/// Rust: struct CloseOnBackdropHitModal; impl StackModal.
/// </summary>
file sealed class CloseOnBackdropHitModal : IStackModal
{
    void IStackModal.RenderContent(Rect area, Frame frame) { }

    ModalResultData? IStackModal.HandleEvent(
        TerminalEvent @event,
        (HitId, HitRegionKind, HitData)? hit,
        HitId hitId)
    {
        if (@event is MouseTerminalEvent mouseEvt
            && mouseEvt.Gesture.Kind == TerminalMouseKind.Down
            && mouseEvt.Gesture.Button == TerminalMouseButton.Left
            && hit.HasValue
            && hit.Value.Item1 == hitId
            && hit.Value.Item2 == HitRegionKind.ModalBackdrop)
        {
            return ModalResultData.Dismissed.Instance;
        }

        return null;
    }

    ModalSizeConstraints IStackModal.SizeConstraints() => new ModalSizeConstraints
    {
        MinWidth = 10, MaxWidth = 10, MinHeight = 3, MaxHeight = 3
    };

    BackdropConfig IStackModal.BackdropConfig() => BackdropConfig.Default;

    bool IStackModal.CloseOnBackdrop() => false;
}

/// <summary>
/// Registers a hit with HitId(4242) and closes when that hit is received.
/// Rust: struct CloseOnInnerHitModal; impl StackModal.
/// DIVERGENCE: HitRegion::Custom(99) becomes HitRegionKind.Custom; the tag 99 is
/// stored in HitData but is not preserved through Frame.RegisterHit / HitTestDetailed
/// (HitData is 0 in RegisterHit call). The test therefore matches on HitId(4242) alone.
/// </summary>
file sealed class CloseOnInnerHitModal : IStackModal
{
    void IStackModal.RenderContent(Rect area, Frame frame)
    {
        if (!area.IsEmpty)
        {
            // Rust: frame.register_hit(area, HitId::new(4242), HitRegion::Custom(99), 0)
            // DIVERGENCE: HitRegion::Custom(99) mapped to HitRegionKind.Custom (tag not stored separately).
            frame.RegisterHit(area, HitId.New(4242), HitRegionKind.Custom, 0);
        }
    }

    ModalResultData? IStackModal.HandleEvent(
        TerminalEvent @event,
        (HitId, HitRegionKind, HitData)? hit,
        HitId hitId)
    {
        // Rust: id == HitId::new(4242) && region == HitRegion::Custom(99)
        // DIVERGENCE: Tag 99 is not preserved in HitRegionKind; match on HitId(4242) + Custom.
        if (hit.HasValue
            && hit.Value.Item1 == HitId.New(4242)
            && hit.Value.Item2 == HitRegionKind.Custom)
        {
            return ModalResultData.Dismissed.Instance;
        }

        return null;
    }

    ModalSizeConstraints IStackModal.SizeConstraints() => new ModalSizeConstraints
    {
        MinWidth = 10, MaxWidth = 10, MinHeight = 3, MaxHeight = 3
    };

    BackdropConfig IStackModal.BackdropConfig() => BackdropConfig.Default;

    bool IStackModal.CloseOnBackdrop() => false;
}

/// <summary>
/// Registers a hit with HitId(1000) (collides with lower modal's hit_id) and closes
/// when that hit is received.
/// Rust: struct CloseOnCollidingInnerHitModal; impl StackModal.
/// DIVERGENCE: HitRegion::Custom(100) mapped to HitRegionKind.Custom.
/// </summary>
file sealed class CloseOnCollidingInnerHitModal : IStackModal
{
    void IStackModal.RenderContent(Rect area, Frame frame)
    {
        if (!area.IsEmpty)
        {
            // Rust: frame.register_hit(area, HitId::new(1000), HitRegion::Custom(100), 0)
            // DIVERGENCE: HitRegion::Custom(100) mapped to HitRegionKind.Custom (tag not stored separately).
            frame.RegisterHit(area, HitId.New(1000), HitRegionKind.Custom, 0);
        }
    }

    ModalResultData? IStackModal.HandleEvent(
        TerminalEvent @event,
        (HitId, HitRegionKind, HitData)? hit,
        HitId hitId)
    {
        // Rust: id == HitId::new(1000) && region == HitRegion::Custom(100)
        // DIVERGENCE: Tag 100 not preserved; match on HitId(1000) + Custom region.
        if (hit.HasValue
            && hit.Value.Item1 == HitId.New(1000)
            && hit.Value.Item2 == HitRegionKind.Custom)
        {
            return ModalResultData.Dismissed.Instance;
        }

        return null;
    }

    ModalSizeConstraints IStackModal.SizeConstraints() => new ModalSizeConstraints
    {
        MinWidth = 10, MaxWidth = 10, MinHeight = 3, MaxHeight = 3
    };

    BackdropConfig IStackModal.BackdropConfig() => BackdropConfig.Default;

    bool IStackModal.CloseOnBackdrop() => false;
}

// ── Test helpers ──────────────────────────────────────────────────────────────

file static class EventFactory
{
    public static TerminalEvent EscapeKey() => new KeyTerminalEvent(
        new KeyGesture(TerminalKey.Escape, TerminalModifiers.None),
        DateTimeOffset.UtcNow);

    public static TerminalEvent MouseLeftDown(ushort col, ushort row) => new MouseTerminalEvent(
        new MouseGesture(col, row, TerminalMouseButton.Left, TerminalMouseKind.Down),
        DateTimeOffset.UtcNow);

    public static TerminalEvent FocusGain() => new FocusTerminalEvent(true, DateTimeOffset.UtcNow);
    public static TerminalEvent FocusLoss() => new FocusTerminalEvent(false, DateTimeOffset.UtcNow);
}

// ── ModalStackTests ───────────────────────────────────────────────────────────

public sealed class ModalStackTests
{
    // ── Basic stack operations ────────────────────────────────────────────────

    [Fact]
    public void EmptyStack()
    {
        var stack = new ModalStack();
        Assert.True(stack.IsEmpty());
        Assert.Equal(0, stack.Depth());
        Assert.Null(stack.Top());
        Assert.Null(stack.TopId());
    }

    [Fact]
    public void PushIncreasesDepth()
    {
        var stack = new ModalStack();
        var id1 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        Assert.Equal(1, stack.Depth());
        Assert.False(stack.IsEmpty());
        Assert.True(stack.Contains(id1));

        var id2 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        Assert.Equal(2, stack.Depth());
        Assert.True(stack.Contains(id2));
        Assert.Equal(id2, stack.TopId());
    }

    [Fact]
    public void PopLifoOrder()
    {
        var stack = new ModalStack();
        var id1 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        var id2 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        var id3 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));

        var result = stack.Pop();
        Assert.Equal(id3, result?.Id);
        Assert.Equal(2, stack.Depth());

        result = stack.Pop();
        Assert.Equal(id2, result?.Id);
        Assert.Equal(1, stack.Depth());

        result = stack.Pop();
        Assert.Equal(id1, result?.Id);
        Assert.True(stack.IsEmpty());
    }

    [Fact]
    public void PopEmptyReturnsNone()
    {
        var stack = new ModalStack();
        Assert.Null(stack.Pop());
    }

    [Fact]
    public void PopById()
    {
        var stack = new ModalStack();
        var id1 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        var id2 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        var id3 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));

        // Pop middle modal
        var result = stack.PopId(id2);
        Assert.Equal(id2, result?.Id);
        Assert.Equal(2, stack.Depth());
        Assert.False(stack.Contains(id2));
        Assert.True(stack.Contains(id1));
        Assert.True(stack.Contains(id3));
    }

    [Fact]
    public void PopByNonexistentId()
    {
        var stack = new ModalStack();
        _ = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));

        // DIVERGENCE: Upstream uses ModalId(999999) tuple-struct literal; C# uses internal ctor.
        var fakeId = new ModalId(999999);
        Assert.Null(stack.PopId(fakeId));
        Assert.Equal(1, stack.Depth());
    }

    [Fact]
    public void PopAll()
    {
        var stack = new ModalStack();
        var id1 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        var id2 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        var id3 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));

        var results = stack.PopAll();
        Assert.Equal(3, results.Count);
        // LIFO order: id3, id2, id1
        Assert.Equal(id3, results[0].Id);
        Assert.Equal(id2, results[1].Id);
        Assert.Equal(id1, results[2].Id);
        Assert.True(stack.IsEmpty());
    }

    [Fact]
    public void ZOrderIncreasing()
    {
        var stack = new ModalStack();

        // Push multiple modals
        stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));

        // Verify z-order is increasing
        // DIVERGENCE: Rust accesses stack.modals directly; C# uses internal _modals accessor.
        var zIndices = stack._modals.Select(m => m.ZIndex).ToList();
        for (int i = 1; i < zIndices.Count; i++)
        {
            Assert.True(zIndices[i] > zIndices[i - 1], "z_index should be strictly increasing");
        }
    }

    [Fact]
    public void EscapeClosesTopModal()
    {
        var stack = new ModalStack();
        var id1 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        var id2 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));

        // Escape should close top modal (id2)
        var result = stack.HandleEvent(EventFactory.EscapeKey(), null);
        Assert.NotNull(result);
        Assert.Equal(id2, result!.Id);
        Assert.Equal(1, stack.Depth());
        Assert.Equal(id1, stack.TopId());
    }

    [Fact]
    public void RenderDoesNotPanic()
    {
        var stack = new ModalStack();
        stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));

        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);
        var screen = new Rect(0, 0, 80, 24);

        // Should not throw
        stack.Render(frame, screen);
    }

    [Fact]
    public void RenderEmptyStackNoOp()
    {
        var stack = new ModalStack();
        var pool = new GraphemePool();
        var frame = new Frame(80, 24, pool);
        var screen = new Rect(0, 0, 80, 24);

        // Should be a no-op
        stack.Render(frame, screen);
    }

    [Fact]
    public void ContainsAfterPop()
    {
        var stack = new ModalStack();
        var id1 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));

        Assert.True(stack.Contains(id1));
        stack.Pop();
        Assert.False(stack.Contains(id1));
    }

    [Fact]
    public void UniqueModalIds()
    {
        var stack = new ModalStack();
        var id1 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        var id2 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        var id3 = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));

        Assert.NotEqual(id1, id2);
        Assert.NotEqual(id2, id3);
        Assert.NotEqual(id1, id3);
    }

    [Fact]
    public void WidgetModalEntryBuilder()
    {
        // Rust: ModalSizeConstraints::new().min_width(40).max_width(80)
        // DIVERGENCE: Upstream ModalSizeConstraints uses Option<u16> for min_width; C# uses ushort directly.
        var entry = new WidgetModalEntry<StubWidget>(new StubWidget())
            .Size(new ModalSizeConstraints { MinWidth = 40, MaxWidth = 80 })
            .Backdrop(new BackdropConfig(PackedRgba.Rgb(0, 0, 0), 0.8f))
            .CloseOnEscape(false)
            .CloseOnBackdrop(false);

        Assert.False(entry._closeOnEscape);
        Assert.False(entry._closeOnBackdrop);
        // DIVERGENCE: MinWidth is ushort (not Option<u16>); upstream has Some(40).
        // DIVERGENCE: MaxWidth is ushort? (nullable); upstream has Some(80).
        Assert.Equal((ushort)40, entry._size.MinWidth);
        Assert.Equal((ushort?)80, entry._size.MaxWidth);
    }

    [Fact]
    public void EscapeDisabledDoesNotClose()
    {
        var stack = new ModalStack();
        stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()).CloseOnEscape(false));

        // Escape should NOT close the modal
        var result = stack.HandleEvent(EventFactory.EscapeKey(), null);
        Assert.Null(result);
        Assert.Equal(1, stack.Depth());
    }

    [Fact]
    public void BackdropClickClosesTopModal()
    {
        var stack = new ModalStack();
        var topId = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));

        var click = EventFactory.MouseLeftDown(0, 0);
        var hit = HitTestResult.New(HitId.New(1000), HitRegionKind.ModalBackdrop, 0, topId.Id());

        var result = stack.HandleEvent(click, hit);
        Assert.NotNull(result);
        Assert.Equal(topId, result!.Id);
        Assert.IsType<ModalResultData.Dismissed>(result.Data);
        Assert.True(stack.IsEmpty());
    }

    [Fact]
    public void ContentClickDoesNotCloseTopModal()
    {
        var stack = new ModalStack();
        stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));

        var click = EventFactory.MouseLeftDown(5, 5);
        var hit = HitTestResult.New(HitId.New(1000), HitRegionKind.ModalContent, 0, stack.TopId()!.Value.Id());

        var result = stack.HandleEvent(click, hit);
        Assert.Null(result);
        Assert.Equal(1, stack.Depth());
    }

    [Fact]
    public void CustomModalReceivesBackdropHitWithoutBuiltinAutoClose()
    {
        var stack = new ModalStack();
        var topId = stack.Push(new CloseOnBackdropHitModal());

        var click = EventFactory.MouseLeftDown(0, 0);
        var hit = HitTestResult.New(HitId.New(1000), HitRegionKind.ModalBackdrop, 0, topId.Id());

        var result = stack.HandleEvent(click, hit);
        Assert.NotNull(result);
        Assert.Equal(topId, result!.Id);
        Assert.IsType<ModalResultData.Dismissed>(result.Data);
        Assert.True(stack.IsEmpty());
    }

    [Fact]
    public void CustomModalReceivesInnerWidgetHit()
    {
        var stack = new ModalStack();
        var topId = stack.Push(new CloseOnInnerHitModal());
        // DIVERGENCE: Upstream accesses stack.modals.last().hit_id; C# uses internal _modals.
        var topHitId = stack._modals.Last().HitId;

        var pool = new GraphemePool();
        var frame = Frame.WithHitGrid(20, 10, pool);
        var screen = new Rect(0, 0, 20, 10);
        stack.Render(frame, screen);

        var hit = frame.HitTestDetailed(10, 4);
        // Rust: HitTestResult::new(HitId::new(4242), HitRegion::Custom(99), 0, Some(top_id.id()))
        // DIVERGENCE: HitRegion::Custom(99) mapped to HitRegionKind.Custom (tag not preserved).
        Assert.NotNull(hit);
        Assert.Equal(HitId.New(4242), hit!.Value.Id);
        Assert.Equal(HitRegionKind.Custom, hit.Value.Region);
        Assert.Equal(topId.Id(), hit.Value.Owner);
        Assert.NotEqual(topHitId, hit.Value.Id);

        var click = EventFactory.MouseLeftDown(10, 4);

        var result = stack.HandleEvent(click, hit);
        Assert.NotNull(result);
        Assert.Equal(topId, result!.Id);
        Assert.IsType<ModalResultData.Dismissed>(result.Data);
        Assert.True(stack.IsEmpty());
    }

    [Fact]
    public void CustomModalReceivesInnerWidgetHitEvenWhenHitIdCollidesWithLowerModal()
    {
        var stack = new ModalStack();
        _ = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        var topId = stack.Push(new CloseOnCollidingInnerHitModal());
        // DIVERGENCE: Upstream accesses stack.modals.last().hit_id; C# uses internal _modals.
        var topHitId = stack._modals.Last().HitId;

        var pool = new GraphemePool();
        var frame = Frame.WithHitGrid(20, 10, pool);
        var screen = new Rect(0, 0, 20, 10);
        stack.Render(frame, screen);

        var hit = frame.HitTestDetailed(10, 4);
        // Rust: HitTestResult::new(HitId::new(1000), HitRegion::Custom(100), 0, Some(top_id.id()))
        // DIVERGENCE: HitRegion::Custom(100) mapped to HitRegionKind.Custom (tag not preserved).
        Assert.NotNull(hit);
        Assert.Equal(HitId.New(1000), hit!.Value.Id);
        Assert.Equal(HitRegionKind.Custom, hit.Value.Region);
        Assert.Equal(topId.Id(), hit.Value.Owner);
        Assert.NotEqual(topHitId, hit.Value.Id);

        var click = EventFactory.MouseLeftDown(10, 4);

        var result = stack.HandleEvent(click, hit);
        Assert.NotNull(result);
        Assert.Equal(topId, result!.Id);
        Assert.IsType<ModalResultData.Dismissed>(result.Data);
        Assert.Equal(1, stack.Depth());
    }

    [Fact]
    public void ZeroSizedModalStillRegistersBackdropHit()
    {
        var stack = new ModalStack();
        stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget())
            .Size(new ModalSizeConstraints { MaxWidth = 0, MaxHeight = 0 }));

        // DIVERGENCE: Upstream accesses stack.modals.last().hit_id; C# uses internal _modals.
        var hitId = stack._modals.Last().HitId;
        var pool = new GraphemePool();
        var frame = Frame.WithHitGrid(20, 10, pool);
        var screen = new Rect(0, 0, 20, 10);

        stack.Render(frame, screen);

        var hit = frame.HitTestDetailed(0, 0);
        Assert.NotNull(hit);
        Assert.Equal(hitId, hit!.Value.Id);
        Assert.Equal(HitRegionKind.ModalBackdrop, hit.Value.Region);
        Assert.Equal(stack.TopId()!.Value.Id(), hit.Value.Owner);
    }

    [Fact]
    public void ForeignLowerModalHitIsNotRoutedToTopModal()
    {
        var stack = new ModalStack();
        var lowerId = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));
        var topId = stack.Push(new CloseOnAnyHitModal());

        var click = EventFactory.MouseLeftDown(0, 0);
        // Hit belongs to lower modal's owner
        var lowerBackdropHit = HitTestResult.New(HitId.New(1000), HitRegionKind.ModalBackdrop, 0, lowerId.Id());

        var result = stack.HandleEvent(click, lowerBackdropHit);
        Assert.Null(result);
        Assert.Equal(2, stack.Depth());
        Assert.Equal(topId, stack.TopId());
    }

    // ── Focus group integration tests ─────────────────────────────────────────

    [Fact]
    public void PushWithFocusTracksGroupId()
    {
        var stack = new ModalStack();
        var modalId = stack.PushWithFocus(new WidgetModalEntry<StubWidget>(new StubWidget()), 42);

        Assert.Equal((uint)42, stack.FocusGroupId(modalId));
        Assert.Equal((uint)42, stack.TopFocusGroupId());
    }

    [Fact]
    public void PopReturnsFocusGroupId()
    {
        var stack = new ModalStack();
        stack.PushWithFocus(new WidgetModalEntry<StubWidget>(new StubWidget()), 99);

        var result = stack.Pop();
        Assert.NotNull(result);
        Assert.Equal((uint)99, result!.FocusGroupId);
    }

    [Fact]
    public void PopIdReturnsFocusGroupId()
    {
        var stack = new ModalStack();
        var id1 = stack.PushWithFocus(new WidgetModalEntry<StubWidget>(new StubWidget()), 10);
        _ = stack.PushWithFocus(new WidgetModalEntry<StubWidget>(new StubWidget()), 20);

        var result = stack.PopId(id1);
        Assert.NotNull(result);
        Assert.Equal((uint)10, result!.FocusGroupId);
    }

    [Fact]
    public void HandleEventReturnsFocusGroupId()
    {
        var stack = new ModalStack();
        stack.PushWithFocus(new WidgetModalEntry<StubWidget>(new StubWidget()), 77);

        var result = stack.HandleEvent(EventFactory.EscapeKey(), null);
        Assert.NotNull(result);
        Assert.Equal((uint)77, result!.FocusGroupId);
    }

    [Fact]
    public void PushWithoutFocusHasNoneGroupId()
    {
        var stack = new ModalStack();
        var modalId = stack.Push(new WidgetModalEntry<StubWidget>(new StubWidget()));

        Assert.Null(stack.FocusGroupId(modalId));
        Assert.Null(stack.TopFocusGroupId());
    }

    [Fact]
    public void NestedFocusGroupsTrackCorrectly()
    {
        var stack = new ModalStack();
        _ = stack.PushWithFocus(new WidgetModalEntry<StubWidget>(new StubWidget()), 1);
        var id2 = stack.PushWithFocus(new WidgetModalEntry<StubWidget>(new StubWidget()), 2);
        _ = stack.PushWithFocus(new WidgetModalEntry<StubWidget>(new StubWidget()), 3);

        // Top should be group 3
        Assert.Equal((uint)3, stack.TopFocusGroupId());

        // Pop top, now group 2 is on top
        stack.Pop();
        Assert.Equal((uint)2, stack.TopFocusGroupId());

        // Query specific modal
        Assert.Equal((uint)2, stack.FocusGroupId(id2));
    }

    // ── ARIA modal tests ──────────────────────────────────────────────────────

    [Fact]
    public void DefaultAriaModalIsTrue()
    {
        var entry = new WidgetModalEntry<StubWidget>(new StubWidget());
        Assert.True(entry._ariaModal);
    }

    [Fact]
    public void AriaModalBuilder()
    {
        var entry = new WidgetModalEntry<StubWidget>(new StubWidget()).WithAriaModal(false);
        Assert.False(entry._ariaModal);
    }

    [Fact]
    public void FocusableIdsBuilder()
    {
        var entry = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 1, 2, 3 });
        Assert.Equal(new List<ulong> { 1, 2, 3 }, entry._focusableIds);
    }

    [Fact]
    public void StackModalAriaModalTrait()
    {
        IStackModal entry = new WidgetModalEntry<StubWidget>(new StubWidget());
        Assert.True(entry.AriaModal()); // Default true

        IStackModal entryNonAria = new WidgetModalEntry<StubWidget>(new StubWidget()).WithAriaModal(false);
        Assert.False(entryNonAria.AriaModal());
    }

    [Fact]
    public void StackModalFocusableIdsTrait()
    {
        IStackModal entry = new WidgetModalEntry<StubWidget>(new StubWidget());
        Assert.Null(entry.FocusableIds()); // Default none

        IStackModal entryWithIds = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 10, 20 });
        Assert.Equal(new List<ulong> { 10, 20 }, entryWithIds.FocusableIds());
    }

    // ── ModalFocusIntegration tests ───────────────────────────────────────────

    [Fact]
    public void FocusIntegrationPushCreatesTrap()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        // Register focusable nodes
        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(2, new Rect(0, 1, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(100, new Rect(0, 10, 10, 1))); // Outside modal

        // Focus outside modal initially
        focus.Focus(100);
        Assert.Equal((ulong)100, focus.Current());

        var integrator = new ModalFocusIntegration(stack, focus);

        // Push modal with focusable IDs
        var modal = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 1, 2 });
        _ = integrator.PushWithFocus(modal);

        // Focus should now be trapped
        Assert.True(integrator.IsFocusTrapped());

        // Focus should move to first focusable in modal
        Assert.Equal((ulong)1, integrator.Focus().Current());
    }

    [Fact]
    public void FocusIntegrationPopRestoresFocus()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        // Register focusable nodes
        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(2, new Rect(0, 1, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(100, new Rect(0, 10, 10, 1))); // Trigger element

        // Focus the trigger element before opening modal
        focus.Focus(100);
        Assert.Equal((ulong)100, focus.Current());

        var integrator = new ModalFocusIntegration(stack, focus);

        // Push modal
        var modal = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 1, 2 });
        integrator.PushWithFocus(modal);

        // Focus is in modal
        Assert.True(integrator.IsFocusTrapped());

        // Pop modal
        var result = integrator.PopWithFocus();
        Assert.NotNull(result);

        // Focus should be restored to trigger element
        Assert.False(integrator.IsFocusTrapped());
        Assert.Equal((ulong)100, integrator.Focus().Current());
    }

    [Fact]
    public void FocusIntegrationPopIdWithFocusPreservesTopTrapAndRestoresBaseAfterLastPop()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(2, new Rect(0, 1, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(100, new Rect(0, 10, 10, 1)));
        focus.Focus(100);

        var integrator = new ModalFocusIntegration(stack, focus);
        var lower = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 1 });
        var lowerId = integrator.PushWithFocus(lower);
        var top = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 2 });
        integrator.PushWithFocus(top);

        var removed = integrator.PopIdWithFocus(lowerId);
        Assert.NotNull(removed);
        Assert.True(integrator.IsFocusTrapped());
        Assert.Equal((ulong)2, integrator.Focus().Current());

        var finalResult = integrator.PopWithFocus();
        Assert.NotNull(finalResult);
        Assert.False(integrator.IsFocusTrapped());
        Assert.Equal((ulong)100, integrator.Focus().Current());
    }

    [Fact]
    public void FocusIntegrationPopIdWithFocusPreservesUnfocusedBaseAcrossHelperInstances()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(2, new Rect(0, 1, 10, 1)));

        ModalId lowerId, upperId;
        {
            var integrator = new ModalFocusIntegration(stack, focus);
            var lower = new WidgetModalEntry<StubWidget>(new StubWidget())
                .WithFocusableIds(new List<ulong> { 1 });
            var upper = new WidgetModalEntry<StubWidget>(new StubWidget())
                .WithFocusableIds(new List<ulong> { 2 });
            lowerId = integrator.PushWithFocus(lower);
            upperId = integrator.PushWithFocus(upper);
            Assert.Equal((ulong)2, integrator.Focus().Current());
        }

        {
            var integrator = new ModalFocusIntegration(stack, focus);
            var removed = integrator.PopIdWithFocus(lowerId);
            Assert.NotNull(removed);
            Assert.Equal(lowerId, removed!.Id);
            Assert.Equal((ulong)2, integrator.Focus().Current());
            Assert.True(integrator.IsFocusTrapped());

            var closed = integrator.PopWithFocus();
            Assert.NotNull(closed);
            Assert.Equal(upperId, closed!.Id);
        }

        Assert.Null(focus.Current());
        Assert.False(focus.IsTrapped());
    }

    [Fact]
    public void FocusIntegrationResyncFocusStateRecoversAfterManualStackMutation()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(2, new Rect(0, 1, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(100, new Rect(0, 10, 10, 1)));
        focus.Focus(100);

        var integrator = new ModalFocusIntegration(stack, focus);
        var modal = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 1, 2 });
        integrator.PushWithFocus(modal);
        Assert.True(integrator.IsFocusTrapped());
        Assert.Equal((ulong)1, integrator.Focus().Current());

        var result = integrator.StackMut().Pop();
        Assert.NotNull(result);
        Assert.True(integrator.IsFocusTrapped());

        integrator.ResyncFocusState();
        Assert.False(integrator.IsFocusTrapped());
        Assert.Equal((ulong)100, integrator.Focus().Current());
    }

    [Fact]
    public void FocusIntegrationResyncUpdatesInactiveModalReturnTargetsAfterManualFocusChange()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        for (ulong id = 1; id <= 4; id++)
        {
            focus.GraphMut().Insert(new UpstreamFocusNode(id, new Rect(0, 0, 10, 1)).WithTabIndex((int)id));
        }

        focus.Focus(1);

        ModalId upperId;
        var integrator = new ModalFocusIntegration(stack, focus);
        var lower = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 2, 3 });
        integrator.PushWithFocus(lower);
        integrator.FocusMut().Focus(3);

        var upper = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 4 });
        upperId = integrator.PushWithFocus(upper);

        _ = integrator.FocusMut().GraphMut().Remove(4);
        integrator.ResyncFocusState();
        Assert.Equal((ulong)3, integrator.Focus().Current());

        integrator.FocusMut().Focus(2);
        integrator.ResyncFocusState();
        Assert.Equal((ulong)2, integrator.Focus().Current());

        integrator.FocusMut().GraphMut().Insert(
            new UpstreamFocusNode(4, new Rect(0, 0, 10, 1)).WithTabIndex(4));
        integrator.ResyncFocusState();
        Assert.Equal((ulong)4, integrator.Focus().Current());

        var result = integrator.PopIdWithFocus(upperId);
        Assert.NotNull(result);
        Assert.Equal(upperId, result!.Id);
        Assert.Equal((ulong)2, integrator.Focus().Current());
        Assert.True(integrator.IsFocusTrapped());
    }

    [Fact]
    public void FocusIntegrationPopSkipsClosedModalFocusIdsWhenBackgroundFocusDisappears()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(50, new Rect(0, 1, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(100, new Rect(0, 10, 10, 1)));
        focus.Focus(100);

        var integrator = new ModalFocusIntegration(stack, focus);
        var modal = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 1 });
        integrator.PushWithFocus(modal);
        _ = integrator.FocusMut().GraphMut().Remove(100);

        var result = integrator.PopWithFocus();
        Assert.NotNull(result);
        Assert.Equal((ulong)50, integrator.Focus().Current());
        Assert.False(integrator.IsFocusTrapped());
    }

    [Fact]
    public void FocusIntegrationPopRemovesClosedModalFocusGroup()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(2, new Rect(0, 1, 10, 1)));

        focus.Focus(1);

        var integrator = new ModalFocusIntegration(stack, focus);
        var modal = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 2 });
        integrator.PushWithFocus(modal);

        var result = integrator.PopWithFocus();
        Assert.NotNull(result);
        var groupId = result!.FocusGroupId;
        Assert.NotNull(groupId);

        Assert.False(integrator.FocusMut().PushTrap(groupId!.Value));
        Assert.False(integrator.IsFocusTrapped());
        Assert.Equal((ulong)1, integrator.Focus().Current());
    }

    [Fact]
    public void FocusIntegrationEscapeRestoresFocus()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(100, new Rect(0, 10, 10, 1)));

        focus.Focus(100);

        var integrator = new ModalFocusIntegration(stack, focus);

        var modal = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 1 });
        integrator.PushWithFocus(modal);

        Assert.True(integrator.IsFocusTrapped());

        // Simulate Escape key
        var result = integrator.HandleEvent(EventFactory.EscapeKey(), null);

        Assert.NotNull(result);
        Assert.False(integrator.IsFocusTrapped());
        Assert.Equal((ulong)100, integrator.Focus().Current());
    }

    [Fact]
    public void FocusIntegrationAppliesHostFocusEvents()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(2, new Rect(0, 1, 10, 1)));
        focus.Focus(2);

        var integrator = new ModalFocusIntegration(stack, focus);
        var modal = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 1 });
        integrator.PushWithFocus(modal);
        Assert.Equal((ulong)1, integrator.Focus().Current());

        var blur = EventFactory.FocusLoss();
        Assert.Null(integrator.HandleEvent(blur, null));
        Assert.Null(integrator.Focus().Current());

        var gain = EventFactory.FocusGain();
        Assert.Null(integrator.HandleEvent(gain, null));
        Assert.Equal((ulong)1, integrator.Focus().Current());
    }

    [Fact]
    public void FocusIntegrationNonAriaModalNoTrap()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(100, new Rect(0, 10, 10, 1)));

        focus.Focus(100);

        var integrator = new ModalFocusIntegration(stack, focus);

        // Push non-ARIA modal (aria_modal = false)
        var modal = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithAriaModal(false)
            .WithFocusableIds(new List<ulong> { 1 });
        integrator.PushWithFocus(modal);

        // Focus should NOT be trapped for non-ARIA modals
        Assert.False(integrator.IsFocusTrapped());
    }

    [Fact]
    public void FocusIntegrationRejectedEmptyTrapDoesNotLeaveFocusGroupBehind()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.Focus(1);

        var integrator = new ModalFocusIntegration(stack, focus);
        var modal = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong>());
        integrator.PushWithFocus(modal);

        Assert.False(integrator.IsFocusTrapped());
        Assert.False(integrator.FocusMut().PushTrap(1));
        Assert.Equal((ulong)1, integrator.Focus().Current());
    }

    [Fact]
    public void RecreatedFocusIntegrationDoesNotReuseLiveGroupIds()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(2, new Rect(0, 1, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(100, new Rect(0, 10, 10, 1)));

        focus.Focus(100);

        uint firstGroupId;
        {
            var integrator = new ModalFocusIntegration(stack, focus);
            var modal = new WidgetModalEntry<StubWidget>(new StubWidget())
                .WithFocusableIds(new List<ulong> { 1 });
            var modalId = integrator.PushWithFocus(modal);
            firstGroupId = integrator.Stack().FocusGroupId(modalId)!.Value;
        }

        uint secondGroupId;
        {
            var integrator = new ModalFocusIntegration(stack, focus);
            var modal = new WidgetModalEntry<StubWidget>(new StubWidget())
                .WithFocusableIds(new List<ulong> { 2 });
            var modalId = integrator.PushWithFocus(modal);
            secondGroupId = integrator.Stack().FocusGroupId(modalId)!.Value;
        }

        Assert.NotEqual(firstGroupId, secondGroupId);

        {
            var integrator = new ModalFocusIntegration(stack, focus);
            var top = integrator.PopWithFocus();
            Assert.NotNull(top);
            Assert.Equal(secondGroupId, top!.FocusGroupId);
            Assert.True(integrator.IsFocusTrapped());
            Assert.Equal((ulong)1, integrator.Focus().Current());

            var lower = integrator.PopWithFocus();
            Assert.NotNull(lower);
            Assert.Equal(firstGroupId, lower!.FocusGroupId);
            Assert.False(integrator.IsFocusTrapped());
            Assert.Equal((ulong)100, integrator.Focus().Current());
        }
    }

    [Fact]
    public void FocusIntegrationDoesNotCollideWithExistingGroupIds()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(99, new Rect(0, 1, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(100, new Rect(0, 10, 10, 1)));
        focus.CreateGroup(1000, new List<ulong> { 99 });
        focus.Focus(100);

        var integrator = new ModalFocusIntegration(stack, focus);
        var modal = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 1 });
        integrator.PushWithFocus(modal);
        _ = integrator.PopWithFocus();
        Assert.True(integrator.FocusMut().PushTrap(1000));
        Assert.Equal((ulong)99, integrator.Focus().Current());
    }

    [Fact]
    public void FocusIntegrationNestedModals()
    {
        var stack = new ModalStack();
        var focus = new UpstreamFocusManager();

        // Register nodes for both modals and background
        focus.GraphMut().Insert(new UpstreamFocusNode(1, new Rect(0, 0, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(2, new Rect(0, 1, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(10, new Rect(0, 5, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(11, new Rect(0, 6, 10, 1)));
        focus.GraphMut().Insert(new UpstreamFocusNode(100, new Rect(0, 10, 10, 1)));

        focus.Focus(100);

        var integrator = new ModalFocusIntegration(stack, focus);

        // Push first modal
        var modal1 = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 1, 2 });
        integrator.PushWithFocus(modal1);
        Assert.Equal((ulong)1, integrator.Focus().Current());

        // Push second modal (nested)
        var modal2 = new WidgetModalEntry<StubWidget>(new StubWidget())
            .WithFocusableIds(new List<ulong> { 10, 11 });
        integrator.PushWithFocus(modal2);
        Assert.Equal((ulong)10, integrator.Focus().Current());

        // Pop second modal - should restore to first modal's focus
        integrator.PopWithFocus();
        Assert.Equal((ulong)1, integrator.Focus().Current());

        // Pop first modal - should restore to original focus
        integrator.PopWithFocus();
        Assert.Equal((ulong)100, integrator.Focus().Current());
    }

    // ── Tracing tests (skipped — feature not ported) ──────────────────────────

    [Fact(Skip = "tracing feature not ported to .NET")]
    public void TracingModalRenderSpanHasRequiredFields()
    {
        // Upstream: tracing_modal_render_span_has_required_fields
        // DIVERGENCE: Rust cfg(feature = "tracing") with tracing_subscriber; no .NET equivalent.
    }

    [Fact(Skip = "tracing feature not ported to .NET")]
    public void TracingFocusChangeAndTrapEventsEmittedForModalLifecycle()
    {
        // Upstream: tracing_focus_change_and_trap_events_emitted_for_modal_lifecycle
        // DIVERGENCE: Rust cfg(feature = "tracing") with tracing_subscriber; no .NET equivalent.
    }
}
