// Port of .external/frankentui/crates/ftui-widgets/src/modal/stack.rs
// Modal stack for managing nested modals with proper z-ordering.

// DIVERGENCE: Rust #[cfg(feature = "tracing")] blocks (tracing::debug_span!, tracing::debug!,
// web_time::Instant) have no .NET equivalent and are omitted.  The two tracing-gated tests
// (tracing_modal_render_span_has_required_fields,
//  tracing_focus_change_and_trap_events_emitted_for_modal_lifecycle) are skipped with
// [Fact(Skip = "tracing feature not ported to .NET")].
//
// DIVERGENCE: Rust HitRegion::Custom(u8) with arbitrary byte tags is mapped to
// HitRegionKind (enum).  Named constants use dedicated enum members (ModalBackdrop,
// ModalContent); generic Custom(n) tags used in test stubs (Custom(99), Custom(100))
// are represented by HitRegionKind.Custom with the tag stored in HitData.
// Comparisons in test stubs are adapted accordingly.
//
// DIVERGENCE: ModalFocusIntegration is cfg(test)-only in the upstream.  In C# it is a
// regular public class (no cfg(test) equivalent exists).
//
// DIVERGENCE: Rust ModalFocusIntegration delegates to ModalFocusCoordinator (focus_integration.rs).
// The C# ModalFocusIntegration in this file reimplements the coordinator logic directly
// against ModalStack + UpstreamFocusManager, mirroring the upstream Rust behaviour
// without requiring a second UpstreamModalStack bridge.
//
// DIVERGENCE: Rust tests access private field `stack.modals` directly (e.g. for z_order_increasing
// and hit_id tests).  In C# the _modals list is internal-visible via the internal accessor.

using System.Threading;
using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets.Modal;

// ── Global modal ID counter ───────────────────────────────────────────────

/// <summary>
/// Global counter for unique modal IDs.
/// Rust: <c>static MODAL_ID_COUNTER: AtomicU64 = AtomicU64::new(1)</c>.
/// </summary>
internal static class ModalIdCounter
{
    private static ulong _counter = 0;
    /// <summary>Returns the next unique ID (atomically incremented).</summary>
    public static ulong Next() => Interlocked.Increment(ref _counter);
}

// ── ModalId ───────────────────────────────────────────────────────────────

/// <summary>
/// Unique identifier for a modal in the stack.
/// Rust: <c>pub struct ModalId(u64)</c>.
/// </summary>
public readonly struct ModalId : IEquatable<ModalId>
{
    // DIVERGENCE: Internal constructor exposed for test (ModalId(999999)).
    internal readonly ulong _raw;

    internal ModalId(ulong value) => _raw = value;

    /// <summary>Create a new unique modal ID. Rust: <c>fn new() -&gt; Self</c>.</summary>
    internal static ModalId NewId() => new(ModalIdCounter.Next());

    /// <summary>Get the raw ID value. Rust: <c>pub const fn id(self) -&gt; u64</c>.</summary>
    public ulong Id() => _raw;

    public bool Equals(ModalId other) => _raw == other._raw;
    public override bool Equals(object? obj) => obj is ModalId m && Equals(m);
    public override int GetHashCode() => _raw.GetHashCode();
    public static bool operator ==(ModalId a, ModalId b) => a._raw == b._raw;
    public static bool operator !=(ModalId a, ModalId b) => a._raw != b._raw;
    public override string ToString() => $"ModalId({_raw})";
}

// ── ModalResultData ───────────────────────────────────────────────────────

/// <summary>
/// Modal result data variants.
/// Rust: <c>pub enum ModalResultData</c> — closed class hierarchy per AGENTS.md.
/// </summary>
public abstract class ModalResultData
{
    private ModalResultData() { }

    /// <summary>Dialog was dismissed (escaped or cancelled). Rust: <c>Dismissed</c>.</summary>
    public sealed class Dismissed : ModalResultData
    {
        public static readonly Dismissed Instance = new();
        private Dismissed() { }
        public override string ToString() => "Dismissed";
    }

    /// <summary>Dialog was confirmed. Rust: <c>Confirmed</c>.</summary>
    public sealed class Confirmed : ModalResultData
    {
        public static readonly Confirmed Instance = new();
        private Confirmed() { }
        public override string ToString() => "Confirmed";
    }

    /// <summary>Dialog returned a custom string value. Rust: <c>Custom(String)</c>.</summary>
    public sealed class Custom : ModalResultData
    {
        public string Value { get; }
        public Custom(string value) => Value = value;
        public override string ToString() => $"Custom({Value})";
    }
}

// ── ModalResult ───────────────────────────────────────────────────────────

/// <summary>
/// Result returned when a modal is closed.
/// Rust: <c>pub struct ModalResult</c>.
/// </summary>
public sealed class ModalResult
{
    /// <summary>The modal ID that was closed. Rust: <c>pub id: ModalId</c>.</summary>
    public required ModalId Id { get; init; }
    /// <summary>Optional result data from the modal. Rust: <c>pub data: Option&lt;ModalResultData&gt;</c>.</summary>
    public ModalResultData? Data { get; init; }
    /// <summary>
    /// Focus group ID if one was associated (for calling FocusManager.pop_trap).
    /// Rust: <c>pub focus_group_id: Option&lt;u32&gt;</c>.
    /// </summary>
    public uint? FocusGroupId { get; init; }
}

// ── FocusTrapSpec (re-declared for this file's context) ───────────────────
// Note: FocusTrapSpec is defined in ModalFocusIntegration.cs and is shared.
// We do NOT redefine it here — we use the one from that file.

// ── IStackModal ───────────────────────────────────────────────────────────

/// <summary>
/// Trait for modal content that can be managed in the stack.
/// Rust: <c>pub trait StackModal: Send</c>.
///
/// This trait abstracts over different modal implementations (Dialog, custom modals)
/// so they can all be managed by the same stack.
///
/// # Focus Management (bd-39vx.5)
///
/// Modals can optionally participate in focus management by providing:
/// - <see cref="FocusableIds"/>: List of focusable widget IDs within the modal
/// - <see cref="AriaModal"/>: Whether this modal should be treated as an ARIA modal
///
/// When focus management is enabled, the caller should:
/// 1. Create a focus group from <see cref="FocusableIds"/> when the modal opens
/// 2. Push a focus trap to constrain Tab navigation within the modal
/// 3. Auto-focus the first focusable widget
/// 4. Restore previous focus when the modal closes
/// </summary>
public interface IStackModal
{
    /// <summary>
    /// A stable-ish label for this modal type, used for tracing/logging.
    /// Default: the C# type name of the concrete modal implementation.
    /// Rust: <c>fn modal_type(&amp;self) -&gt; &amp;'static str</c>.
    /// </summary>
    string ModalType() => GetType().Name;

    /// <summary>
    /// Render the modal content at the given area.
    /// Rust: <c>fn render_content(&amp;self, area: Rect, frame: &amp;mut Frame)</c>.
    /// </summary>
    void RenderContent(Rect area, Frame frame);

    /// <summary>
    /// Handle an event, returning non-null to signal the modal should close.
    ///
    /// <paramref name="hit"/> is the last rendered hit-test result for the pointer location,
    /// if one exists. <paramref name="hitId"/> is the stack-assigned ID for this modal.
    ///
    /// Rust: <c>fn handle_event(&amp;mut self, event: &amp;Event, hit: Option&lt;(HitId, HitRegion, HitData)&gt;, hit_id: HitId) -&gt; Option&lt;ModalResultData&gt;</c>.
    /// </summary>
    ModalResultData? HandleEvent(TerminalEvent @event, (HitId, HitRegionKind, HitData)? hit, HitId hitId);

    /// <summary>
    /// Get the modal's size constraints.
    /// Rust: <c>fn size_constraints(&amp;self) -&gt; ModalSizeConstraints</c>.
    /// </summary>
    ModalSizeConstraints SizeConstraints();

    /// <summary>
    /// Get the backdrop configuration.
    /// Rust: <c>fn backdrop_config(&amp;self) -&gt; BackdropConfig</c>.
    /// </summary>
    BackdropConfig BackdropConfig();

    /// <summary>
    /// Whether this modal can be closed by pressing Escape.
    /// Rust: <c>fn close_on_escape(&amp;self) -&gt; bool</c> (default: true).
    /// </summary>
    bool CloseOnEscape() => true;

    /// <summary>
    /// Whether this modal can be closed by clicking the backdrop.
    /// Rust: <c>fn close_on_backdrop(&amp;self) -&gt; bool</c> (default: true).
    /// </summary>
    bool CloseOnBackdrop() => true;

    /// <summary>
    /// Whether this modal is an ARIA modal (accessibility semantic).
    ///
    /// ARIA modals:
    /// - Trap focus within the modal (Tab cannot escape)
    /// - Announce modal semantics to screen readers
    /// - Block interaction with content behind the modal
    ///
    /// Default: <c>true</c> for accessibility compliance.
    ///
    /// # Invariants
    /// - When AriaModal() returns true, focus MUST be trapped within the modal.
    /// - Screen readers should announce modal state changes.
    ///
    /// # Failure Modes
    /// - If focus trap is not configured, Tab may escape (accessibility violation).
    ///
    /// Rust: <c>fn aria_modal(&amp;self) -&gt; bool</c> (default: true).
    /// </summary>
    bool AriaModal() => true;

    /// <summary>
    /// Get the IDs of focusable widgets within this modal.
    ///
    /// These IDs are used to create a focus group when the modal opens.
    /// The first ID in the list receives auto-focus.
    ///
    /// Returns <c>null</c> if focus management is not needed (e.g. non-interactive modals).
    ///
    /// Rust: <c>fn focusable_ids(&amp;self) -&gt; Option&lt;Vec&lt;ModalFocusId&gt;&gt;</c> (default: None).
    /// </summary>
    List<ulong>? FocusableIds() => null;
}

// ── ActiveModal ───────────────────────────────────────────────────────────

/// <summary>
/// An active modal in the stack.
/// Rust: <c>struct ActiveModal</c> (private/internal).
/// </summary>
internal sealed class ActiveModal
{
    /// <summary>Unique identifier for this modal. Rust: <c>id: ModalId</c>.</summary>
    public required ModalId Id;
    /// <summary>
    /// Z-index for layering (reserved for future compositor integration).
    /// Rust: <c>#[allow(dead_code)] z_index: u32</c>.
    /// </summary>
    public required uint ZIndex;
    /// <summary>The modal content. Rust: <c>modal: Box&lt;dyn StackModal&gt;</c>.</summary>
    public required IStackModal Modal;
    /// <summary>Hit ID for this modal's hit regions. Rust: <c>hit_id: HitId</c>.</summary>
    public required HitId HitId;
    /// <summary>Focus group ID for focus trap integration. Rust: <c>focus_group_id: Option&lt;u32&gt;</c>.</summary>
    public uint? FocusGroupId;
    /// <summary>
    /// Focus target to restore when this modal closes.
    /// Rust: <c>focus_return_focus: Option&lt;FocusId&gt;</c>.
    /// </summary>
    public ulong? FocusReturnFocus; // Option<FocusId = u64>
}

// ── ModalSizeConstraints extensions ──────────────────────────────────────

/// <summary>
/// Extension methods for <see cref="ModalSizeConstraints"/> required by the modal stack render path.
/// Rust: <c>impl ModalSizeConstraints { pub fn clamp(self, available: Size) -&gt; Size }</c>
/// defined in container.rs and used by stack.rs.
/// </summary>
public static class ModalSizeConstraintsStackExtensions
{
    /// <summary>
    /// Clamp the given available size to these constraints (but never exceed available).
    /// Rust: <c>pub fn clamp(self, available: Size) -&gt; Size</c>.
    /// </summary>
    public static Size Clamp(this ModalSizeConstraints c, Size available)
    {
        ushort w = available.Width;
        ushort h = available.Height;

        if (c.MaxWidth.HasValue)  w = Math.Min(w, c.MaxWidth.Value);
        if (c.MaxHeight.HasValue) h = Math.Min(h, c.MaxHeight.Value);
        if (c.MinWidth  > 0) w = (ushort)Math.Min(Math.Max(w, c.MinWidth),  available.Width);
        if (c.MinHeight > 0) h = (ushort)Math.Min(Math.Max(h, c.MinHeight), available.Height);

        return new Size(w, h);
    }
}

// ── ModalStack ────────────────────────────────────────────────────────────

/// <summary>
/// Stack of active modals with z-ordering and input routing.
///
/// # Invariants
///
/// - <c>_modals</c> is ordered by z_index (lowest to highest).
/// - <c>_nextZ</c> always produces a z_index greater than any existing modal.
/// - Input is only routed to the top modal (last in the list).
///
/// Rust: <c>pub struct ModalStack</c>.
/// </summary>
public sealed class ModalStack
{
    // DIVERGENCE: _modals is internal (not private) so tests can access z_index and hit_id.
    internal readonly List<ActiveModal> _modals;
    private uint _nextZ;
    private uint _nextHitId;

    // Base z-index constants.
    // Rust: const BASE_MODAL_Z: u32 = 1000; const Z_INCREMENT: u32 = 10;
    private const uint BaseModalZ  = 1000;
    private const uint ZIncrement  = 10;

    // ── Default / new ─────────────────────────────────────────────────────

    /// <summary>
    /// Create an empty modal stack.
    /// Rust: impl Default + <c>pub fn new() -&gt; Self</c>.
    /// </summary>
    public ModalStack()
    {
        _modals    = new List<ActiveModal>();
        _nextZ     = 0;
        _nextHitId = 1000; // Start hit IDs high to avoid conflicts
    }

    // ── Stack Operations ──────────────────────────────────────────────────

    /// <summary>
    /// Push a modal onto the stack.
    /// Returns the unique <see cref="ModalId"/> for the pushed modal.
    /// Rust: <c>pub fn push(&amp;mut self, modal: Box&lt;dyn StackModal&gt;) -&gt; ModalId</c>.
    /// </summary>
    public ModalId Push(IStackModal modal) => PushWithFocus(modal, null);

    /// <summary>
    /// Push a modal with an associated focus group ID.
    ///
    /// The focus group ID is used to integrate with FocusManager:
    /// 1. Before calling this, create a focus group with focus_manager.create_group(id, members)
    /// 2. Then call focus_manager.push_trap(id) to trap focus within the modal
    /// 3. When the modal closes, call focus_manager.pop_trap() to restore focus
    ///
    /// Returns the unique <see cref="ModalId"/> for the pushed modal.
    /// Rust: <c>pub fn push_with_focus(&amp;mut self, modal: Box&lt;dyn StackModal&gt;, focus_group_id: Option&lt;u32&gt;) -&gt; ModalId</c>.
    /// </summary>
    public ModalId PushWithFocus(IStackModal modal, uint? focusGroupId)
    {
        var id     = ModalId.NewId();
        var zIndex = BaseModalZ + _nextZ;
        _nextZ    += ZIncrement;

        var hitId  = HitId.New(_nextHitId);
        _nextHitId++;

        _modals.Add(new ActiveModal
        {
            Id           = id,
            ZIndex       = zIndex,
            Modal        = modal,
            HitId        = hitId,
            FocusGroupId = focusGroupId,
        });

        return id;
    }

    /// <summary>
    /// Get the focus group ID for a modal.
    /// Returns <c>null</c> if the modal doesn't exist or has no focus group.
    /// Rust: <c>pub fn focus_group_id(&amp;self, modal_id: ModalId) -&gt; Option&lt;u32&gt;</c>.
    /// </summary>
    public uint? FocusGroupId(ModalId modalId)
    {
        foreach (var m in _modals)
            if (m.Id == modalId) return m.FocusGroupId;
        return null;
    }

    /// <summary>
    /// Get the focus group ID for the top modal.
    /// Useful for checking if focus trap should be active.
    /// Rust: <c>pub fn top_focus_group_id(&amp;self) -&gt; Option&lt;u32&gt;</c>.
    /// </summary>
    public uint? TopFocusGroupId()
        => _modals.Count > 0 ? _modals[^1].FocusGroupId : null;

    /// <summary>
    /// Find the next focus-modal (scanning bottom-to-top) after the given ID.
    /// Rust: <c>pub(super) fn next_focus_modal_after(&amp;self, modal_id: ModalId) -&gt; Option&lt;(ModalId, u32)&gt;</c>.
    /// </summary>
    internal (ModalId, uint)? NextFocusModalAfter(ModalId modalId)
    {
        int idx = IndexOf(modalId);
        if (idx < 0) return null;
        for (int i = idx + 1; i < _modals.Count; i++)
            if (_modals[i].FocusGroupId.HasValue)
                return (_modals[i].Id, _modals[i].FocusGroupId!.Value);
        return null;
    }

    /// <summary>
    /// Pop the top modal from the stack.
    /// Returns the result if a modal was popped, or <c>null</c> if the stack is empty.
    /// If the modal had a focus group, the caller should call FocusManager.pop_trap().
    /// Rust: <c>pub fn pop(&amp;mut self) -&gt; Option&lt;ModalResult&gt;</c>.
    /// </summary>
    public ModalResult? Pop()
    {
        if (_modals.Count == 0) return null;
        var modal = _modals[^1];
        _modals.RemoveAt(_modals.Count - 1);
        return new ModalResult { Id = modal.Id, Data = null, FocusGroupId = modal.FocusGroupId };
    }

    /// <summary>
    /// Pop a specific modal by ID.
    /// Returns the result if the modal was found and removed, or <c>null</c> if not found.
    /// Note: This breaks strict LIFO ordering but is sometimes needed.
    /// If the modal had a focus group, the caller should handle focus restoration.
    /// Rust: <c>pub fn pop_id(&amp;mut self, id: ModalId) -&gt; Option&lt;ModalResult&gt;</c>.
    /// </summary>
    public ModalResult? PopId(ModalId id)
        => PopIdWithRestoreRetarget(id, retargetUpperReturnFocus: true);

    /// <summary>
    /// Pop a specific modal by ID with optional return-focus retargeting.
    /// Rust: <c>pub(super) fn pop_id_with_restore_retarget(...) -&gt; Option&lt;ModalResult&gt;</c>.
    /// </summary>
    internal ModalResult? PopIdWithRestoreRetarget(ModalId id, bool retargetUpperReturnFocus)
    {
        int idx = IndexOf(id);
        if (idx < 0) return null;
        var modal = _modals[idx];
        _modals.RemoveAt(idx);

        if (retargetUpperReturnFocus && modal.FocusGroupId.HasValue)
        {
            for (int i = idx; i < _modals.Count; i++)
            {
                if (_modals[i].FocusGroupId.HasValue)
                {
                    _modals[i].FocusReturnFocus = modal.FocusReturnFocus;
                    break;
                }
            }
        }

        return new ModalResult { Id = modal.Id, Data = null, FocusGroupId = modal.FocusGroupId };
    }

    /// <summary>
    /// Pop all modals from the stack.
    /// Returns results in LIFO order (top first).
    /// Rust: <c>pub fn pop_all(&amp;mut self) -&gt; Vec&lt;ModalResult&gt;</c>.
    /// </summary>
    public List<ModalResult> PopAll()
    {
        var results = new List<ModalResult>(_modals.Count);
        while (Pop() is { } r) results.Add(r);
        return results;
    }

    /// <summary>
    /// Get a reference to the top modal.
    /// Rust: <c>pub fn top(&amp;self) -&gt; Option&lt;&amp;dyn StackModal&gt;</c>.
    /// </summary>
    public IStackModal? Top()
        => _modals.Count > 0 ? _modals[^1].Modal : null;

    /// <summary>
    /// Get a mutable reference to the top modal.
    /// Rust: <c>pub fn top_mut(&amp;mut self) -&gt; Option&lt;&amp;mut dyn StackModal&gt;</c>.
    /// DIVERGENCE: C# interfaces are reference types; mutable access is implicit.
    /// </summary>
    public IStackModal? TopMut()
        => _modals.Count > 0 ? _modals[^1].Modal : null;

    // ── State Queries ─────────────────────────────────────────────────────

    /// <summary>Check if the stack is empty. Rust: <c>pub fn is_empty(&amp;self) -&gt; bool</c>.</summary>
    public bool IsEmpty() => _modals.Count == 0;

    /// <summary>Get the number of modals in the stack. Rust: <c>pub fn depth(&amp;self) -&gt; usize</c>.</summary>
    public int Depth() => _modals.Count;

    /// <summary>
    /// Check if a modal with the given ID exists in the stack.
    /// Rust: <c>pub fn contains(&amp;self, id: ModalId) -&gt; bool</c>.
    /// </summary>
    public bool Contains(ModalId id)
    {
        foreach (var m in _modals)
            if (m.Id == id) return true;
        return false;
    }

    /// <summary>
    /// Get the ID of the top modal, if any.
    /// Rust: <c>pub fn top_id(&amp;self) -&gt; Option&lt;ModalId&gt;</c>.
    /// </summary>
    public ModalId? TopId()
        => _modals.Count > 0 ? _modals[^1].Id : null;

    /// <summary>
    /// Get focus group IDs for active modals in stack order (bottom to top).
    /// Rust: <c>pub fn focus_group_ids_in_order(&amp;self) -&gt; Vec&lt;u32&gt;</c>.
    /// </summary>
    public List<uint> FocusGroupIdsInOrder()
    {
        var result = new List<uint>();
        foreach (var m in _modals)
            if (m.FocusGroupId.HasValue) result.Add(m.FocusGroupId.Value);
        return result;
    }

    /// <summary>
    /// Get focus-modal specs in stack order (bottom to top).
    /// Rust: <c>pub(super) fn focus_modal_specs_in_order(&amp;self) -&gt; Vec&lt;(ModalId, FocusTrapSpec)&gt;</c>.
    /// </summary>
    internal List<(ModalId, FocusTrapSpec)> FocusModalSpecsInOrder()
    {
        var result = new List<(ModalId, FocusTrapSpec)>();
        foreach (var m in _modals)
            if (m.FocusGroupId.HasValue)
                result.Add((m.Id, new FocusTrapSpec(m.FocusGroupId.Value, m.FocusReturnFocus)));
        return result;
    }

    /// <summary>
    /// Set the return-focus for a specific modal.
    /// Rust: <c>pub(super) fn set_focus_return_focus(&amp;mut self, modal_id: ModalId, return_focus: Option&lt;FocusId&gt;) -&gt; bool</c>.
    /// </summary>
    internal bool SetFocusReturnFocus(ModalId modalId, ulong? returnFocus)
    {
        foreach (var m in _modals)
        {
            if (m.Id == modalId)
            {
                if (!m.FocusGroupId.HasValue) return false;
                m.FocusReturnFocus = returnFocus;
                return true;
            }
        }
        return false;
    }

    // ── Event Handling ────────────────────────────────────────────────────

    /// <summary>
    /// Handle an event, routing it to the top modal only.
    ///
    /// Returns <c>Some(ModalResult)</c> if the top modal closed, otherwise <c>null</c>.
    /// If the result contains a <c>focus_group_id</c>, the caller should call
    /// FocusManager.pop_trap() to restore focus.
    ///
    /// For mouse interactions, pass the provenance-aware result from
    /// Frame.HitTestDetailed. Plain (HitId, HitRegionKind, HitData) tuples
    /// do not carry enough ownership information for layered modal routing.
    ///
    /// Rust: <c>pub fn handle_event(&amp;mut self, event: &amp;Event, hit: Option&lt;HitTestResult&gt;) -&gt; Option&lt;ModalResult&gt;</c>.
    /// </summary>
    public ModalResult? HandleEvent(TerminalEvent @event, HitTestResult? hit)
    {
        if (_modals.Count == 0) return null;

        int topIndex  = _modals.Count - 1;
        ulong topOwner = _modals[topIndex].Id.Id();
        var hitId     = _modals[topIndex].HitId;
        var id        = _modals[topIndex].Id;
        var focusGroupId = _modals[topIndex].FocusGroupId;

        // Filter hit: only pass through if it belongs to the top modal's owner.
        (HitId, HitRegionKind, HitData)? filteredHit = null;
        if (hit.HasValue && hit.Value.Owner == topOwner)
            filteredHit = hit.Value.IntoTuple();

        var data = _modals[topIndex].Modal.HandleEvent(@event, filteredHit, hitId);
        if (data != null)
        {
            // Modal wants to close.
            _modals.RemoveAt(topIndex);
            return new ModalResult { Id = id, Data = data, FocusGroupId = focusGroupId };
        }

        return null;
    }

    // ── Rendering ─────────────────────────────────────────────────────────

    /// <summary>
    /// Render all modals in z-order.
    ///
    /// Modals are rendered from bottom to top. Lower modals have reduced
    /// backdrop opacity to create a visual depth effect.
    ///
    /// Rust: <c>pub fn render(&amp;self, frame: &amp;mut Frame, screen: Rect)</c>.
    /// </summary>
    public void Render(Frame frame, Rect screen)
    {
        if (_modals.Count == 0) return;

        int modalCount = _modals.Count;
        for (int i = 0; i < modalCount; i++)
        {
            var modal  = _modals[i];
            bool isTop = i == modalCount - 1;

            // Calculate backdrop opacity with depth dimming.
            float baseOpacity = modal.Modal.BackdropConfig().Opacity;
            float opacity     = isTop ? baseOpacity : baseOpacity * 0.5f;

            // Render backdrop.
            if (opacity > 0.0f)
            {
                var bgColor = modal.Modal.BackdropConfig().Color.WithOpacity(opacity);
                WidgetDrawing.SetStyleArea(frame.Buffer, screen, new WidgetStyle(null, bgColor, null));
            }

            frame.WithHitOwner(modal.Id.Id(), innerFrame =>
            {
                // Register backdrop hits even when the modal content clamps to zero.
                // A zero-sized modal can still present a visible overlay and should
                // still receive backdrop clicks.
                if (!screen.IsEmpty)
                    innerFrame.RegisterHit(screen, modal.HitId, HitRegionKind.ModalBackdrop, 0);

                // Calculate modal content area.
                var constraints = modal.Modal.SizeConstraints();
                var available   = new Size(screen.Width, screen.Height);
                var size        = constraints.Clamp(available);

                if (size.Width == 0 || size.Height == 0)
                    return;

                // Center the modal.
                ushort x = (ushort)(screen.X + (screen.Width  - size.Width)  / 2);
                ushort y = (ushort)(screen.Y + (screen.Height - size.Height) / 2);
                var contentArea = new Rect(x, y, size.Width, size.Height);

                // Register hit regions so close_on_backdrop / custom mouse dispatch
                // can distinguish backdrop from content clicks.
                if (!contentArea.IsEmpty)
                    innerFrame.RegisterHit(contentArea, modal.HitId, HitRegionKind.ModalContent, 0);

                // Render modal content.
                modal.Modal.RenderContent(contentArea, innerFrame);
            });
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private int IndexOf(ModalId id)
    {
        for (int i = 0; i < _modals.Count; i++)
            if (_modals[i].Id == id) return i;
        return -1;
    }
}

// ── WidgetModalEntry<W> ───────────────────────────────────────────────────

/// <summary>
/// A simple modal entry that wraps any <see cref="IWidget"/>.
/// Rust: <c>pub struct WidgetModalEntry&lt;W&gt;</c>.
/// </summary>
public sealed class WidgetModalEntry<W> : IStackModal where W : IWidget
{
    private readonly W _widget;

    // DIVERGENCE: Fields are internal so tests can inspect them directly
    // (mirrors Rust test access to struct fields).
    internal ModalSizeConstraints _size;
    internal BackdropConfig       _backdrop;
    internal bool                 _closeOnEscape;
    internal bool                 _closeOnBackdrop;
    internal bool                 _ariaModal;
    internal List<ulong>?         _focusableIds;

    /// <summary>
    /// Create a new modal entry with a widget.
    /// Rust: <c>pub fn new(widget: W) -&gt; Self</c>.
    /// </summary>
    public WidgetModalEntry(W widget)
    {
        _widget          = widget;
        _size            = new ModalSizeConstraints
        {
            MinWidth  = 30,
            MaxWidth  = 60,
            MinHeight = 10,
            MaxHeight = 20,
        };
        _backdrop        = BackdropConfig.Default;
        _closeOnEscape   = true;
        _closeOnBackdrop = true;
        _ariaModal       = true;
        _focusableIds    = null;
    }

    /// <summary>
    /// Set size constraints.
    /// Rust: <c>#[must_use] pub fn size(mut self, size: ModalSizeConstraints) -&gt; Self</c>.
    /// </summary>
    public WidgetModalEntry<W> Size(ModalSizeConstraints size) { _size = size; return this; }

    /// <summary>
    /// Set backdrop configuration.
    /// Rust: <c>#[must_use] pub fn backdrop(mut self, backdrop: BackdropConfig) -&gt; Self</c>.
    /// </summary>
    public WidgetModalEntry<W> Backdrop(BackdropConfig backdrop) { _backdrop = backdrop; return this; }

    /// <summary>
    /// Set whether Escape closes the modal.
    /// Rust: <c>#[must_use] pub fn close_on_escape(mut self, close: bool) -&gt; Self</c>.
    /// </summary>
    public WidgetModalEntry<W> CloseOnEscape(bool close) { _closeOnEscape = close; return this; }

    /// <summary>
    /// Set whether backdrop click closes the modal.
    /// Rust: <c>#[must_use] pub fn close_on_backdrop(mut self, close: bool) -&gt; Self</c>.
    /// </summary>
    public WidgetModalEntry<W> CloseOnBackdrop(bool close) { _closeOnBackdrop = close; return this; }

    /// <summary>
    /// Set whether this modal is an ARIA modal.
    ///
    /// ARIA modals trap focus and announce semantics to screen readers.
    /// Default is <c>true</c> for accessibility compliance.
    ///
    /// Rust: <c>#[must_use] pub fn with_aria_modal(mut self, aria_modal: bool) -&gt; Self</c>.
    /// </summary>
    public WidgetModalEntry<W> WithAriaModal(bool ariaModal) { _ariaModal = ariaModal; return this; }

    /// <summary>
    /// Set the focusable widget IDs for focus trap integration.
    /// Rust: <c>#[must_use] pub fn with_focusable_ids(mut self, ids: Vec&lt;ModalFocusId&gt;) -&gt; Self</c>.
    /// </summary>
    public WidgetModalEntry<W> WithFocusableIds(List<ulong> ids)
    {
        _focusableIds = new List<ulong>(ids);
        return this;
    }

    // ── IStackModal implementation ────────────────────────────────────────

    void IStackModal.RenderContent(Rect area, Frame frame) => _widget.Render(area, frame);

    ModalResultData? IStackModal.HandleEvent(TerminalEvent @event, (HitId, HitRegionKind, HitData)? hit, HitId hitId)
    {
        // Handle backdrop click to close.
        if (_closeOnBackdrop
            && @event is MouseTerminalEvent mouseEvt
            && mouseEvt.Gesture.Kind   == TerminalMouseKind.Down
            && mouseEvt.Gesture.Button == TerminalMouseButton.Left
            && hit.HasValue
            && hit.Value.Item1 == hitId
            && hit.Value.Item2 == HitRegionKind.ModalBackdrop)
        {
            return ModalResultData.Dismissed.Instance;
        }

        // Handle Escape to close.
        if (_closeOnEscape
            && @event is KeyTerminalEvent keyEvt
            && keyEvt.Gesture.Key == TerminalKey.Escape)
        {
            return ModalResultData.Dismissed.Instance;
        }

        return null;
    }

    ModalSizeConstraints IStackModal.SizeConstraints() => _size;
    BackdropConfig       IStackModal.BackdropConfig()  => _backdrop;
    bool IStackModal.CloseOnEscape()                   => _closeOnEscape;
    bool IStackModal.CloseOnBackdrop()                 => _closeOnBackdrop;
    bool IStackModal.AriaModal()                       => _ariaModal;
    List<ulong>? IStackModal.FocusableIds()            => _focusableIds != null ? new List<ulong>(_focusableIds) : null;
}

// ── ModalFocusIntegration ─────────────────────────────────────────────────

/// <summary>
/// Helper for integrating <see cref="ModalStack"/> with <see cref="UpstreamFocusManager"/>.
///
/// This class provides a convenient API for:
/// - Pushing modals with automatic focus trap setup
/// - Popping modals with focus restoration
/// - Managing focus groups for nested modals
///
/// Rust: <c>#[cfg(test)] pub struct ModalFocusIntegration&lt;'a&gt;</c>.
/// DIVERGENCE: Rust marks this cfg(test); C# has no cfg(test) equivalent.
/// The implementation is a regular public class that owns references to the stack
/// and focus manager.  On construction the current "base focus" state is captured
/// from the focus manager, matching the Rust constructor behaviour.
/// </summary>
public sealed class ModalFocusIntegration
{
    private readonly ModalStack              _stack;
    private readonly UpstreamFocusManager   _focus;

    // Rust: base_focus: Option<Option<FocusId>>
    //   None         → _baseFocusSet = false
    //   Some(None)   → _baseFocusSet = true, _baseFocus = null
    //   Some(Some(x))→ _baseFocusSet = true, _baseFocus = x
    private bool   _baseFocusSet;
    private ulong? _baseFocus;

    /// <summary>
    /// Create a new integration helper.
    /// Rust: <c>pub fn new(stack: &amp;'a mut ModalStack, focus: &amp;'a mut FocusManager) -&gt; Self</c>.
    /// </summary>
    public ModalFocusIntegration(ModalStack stack, UpstreamFocusManager focus)
    {
        _stack = stack;
        _focus = focus;
        // Capture the base_focus from FocusManager::base_trap_return_focus().
        // In Rust this is a private method; we mirror the behaviour by reading the
        // current logical focus target when the integration is created.
        var baseFocus = focus.HostFocused() ? focus.Current() : focus.DeferredFocusTarget();
        if (focus.IsTrapped())
        {
            // If already trapped, there's a live base_focus.
            _baseFocusSet = true;
            _baseFocus    = baseFocus;
        }
        else
        {
            _baseFocusSet = false;
            _baseFocus    = null;
        }
    }

    // ── Internal coordinator bridge ───────────────────────────────────────
    // We mirror the ModalFocusCoordinator logic directly here so we can operate
    // on ModalStack (which uses ModalId) rather than UpstreamModalStack.

    private ModalFocusCoordinator MakeCoord()
    {
        // Bridge: build a temporary UpstreamModalStack that mirrors our ModalStack's
        // focus state, then create the coordinator.
        var bridge = BuildBridge();
        return new ModalFocusCoordinator(bridge, _focus, _baseFocusSet, _baseFocus);
    }

    private UpstreamModalStack BuildBridge()
    {
        // Build a minimal UpstreamModalStack that reflects the same focus group
        // associations as our ModalStack.
        var bridge = new UpstreamModalStack();
        foreach (var m in _stack._modals)
        {
            var upId = bridge.PushWithFocus(new NoOpUpstreamModal(), m.FocusGroupId);
            if (m.FocusGroupId.HasValue)
                bridge.SetFocusReturnFocus(upId, m.FocusReturnFocus);
            // Store the mapping.
            _modalIdToUpstream[m.Id._raw] = upId;
        }
        return bridge;
    }

    private readonly Dictionary<ulong, UpstreamModalId> _modalIdToUpstream = new();

    private void SyncFrom(ModalFocusCoordinator coord)
    {
        _baseFocusSet = coord.ResultBaseFocusSet;
        _baseFocus    = coord.ResultBaseFocus;
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Push a modal with automatic focus management.
    ///
    /// 1. Creates a focus group from modal.FocusableIds() (if provided)
    /// 2. Pushes a focus trap to constrain Tab navigation
    /// 3. Auto-focuses the first focusable widget
    /// 4. Stores the previous focus for restoration on close
    ///
    /// Returns the modal ID.
    /// Rust: <c>pub fn push_with_focus(&amp;mut self, modal: Box&lt;dyn StackModal&gt;) -&gt; ModalId</c>.
    /// </summary>
    public ModalId PushWithFocus(IStackModal modal)
    {
        var focusableIds = modal.FocusableIds();
        var isAriaModal  = modal.AriaModal();

        var baseFocusVal = _focus.HostFocused() ? _focus.Current() : _focus.DeferredFocusTarget();
        var wasTrapped   = _focus.IsTrapped();

        uint? focusGroupId = null;

        if (isAriaModal && focusableIds != null)
        {
            var groupId          = FocusGroupIdAllocator.NextFocusGroupId(_focus);
            var hasDeclaredMembers = focusableIds.Count > 0;
            _focus.CreateGroupPreservingMembers(groupId, focusableIds);
            bool trapped = _focus.PushTrap(groupId);
            if (!trapped && !hasDeclaredMembers)
            {
                _focus.RemoveGroup(groupId);
                // focusGroupId stays null
            }
            else
            {
                if (!wasTrapped && trapped)
                {
                    _baseFocusSet = true;
                    _baseFocus    = baseFocusVal;
                }
                focusGroupId = groupId;
            }
        }

        var modalId = _stack.PushWithFocus(modal, focusGroupId);
        if (focusGroupId.HasValue)
            _stack.SetFocusReturnFocus(modalId, baseFocusVal);
        return modalId;
    }

    /// <summary>
    /// Pop the top modal with focus restoration.
    /// Rust: <c>pub fn pop_with_focus(&amp;mut self) -&gt; Option&lt;ModalResult&gt;</c>.
    /// </summary>
    public ModalResult? PopWithFocus()
    {
        var result = _stack.Pop();
        if (result != null) HandleClosedResult(result);
        return result;
    }

    /// <summary>
    /// Pop a specific modal with focus restoration/rebuild.
    /// Rust: <c>pub fn pop_id_with_focus(&amp;mut self, id: ModalId) -&gt; Option&lt;ModalResult&gt;</c>.
    /// </summary>
    public ModalResult? PopIdWithFocus(ModalId id)
    {
        if (_stack.TopId() == id) return PopWithFocus();

        // Non-top removal: handle focus retargeting for upper modals.
        if (_stack.FocusGroupId(id) is { } groupId)
        {
            var removedMembers      = _focus.GroupMembers(groupId);
            var removedGroupActive  = GroupHasFocusableMember(groupId);

            if (_stack.NextFocusModalAfter(id) is { } upperPair)
            {
                var (upperModalId, _) = upperPair;
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

                if (shouldRetarget)
                {
                    var effectiveReturnFocus = EffectiveReturnFocusForGroup(groupId);
                    _stack.SetFocusReturnFocus(upperModalId, effectiveReturnFocus);
                }
            }
        }

        var result2 = _stack.PopIdWithRestoreRetarget(id, retargetUpperReturnFocus: false);
        if (result2 == null) return null;

        if (result2.FocusGroupId.HasValue)
        {
            var closingMembers = _focus.GroupMembers(result2.FocusGroupId.Value);
            _focus.RemoveGroupWithoutRepair(result2.FocusGroupId.Value);
            _focus.ClearDeferredFocusIfExcluded(closingMembers);
            RebuildFocusTraps();
            _focus.RepairFocusAfterExcludingIds(closingMembers);
            RefreshInactiveModalReturnFocusTargets();
        }
        return result2;
    }

    /// <summary>
    /// Pop all modals with focus restoration/rebuild.
    /// Rust: <c>pub fn pop_all_with_focus(&amp;mut self) -&gt; Vec&lt;ModalResult&gt;</c>.
    /// </summary>
    public List<ModalResult> PopAllWithFocus()
    {
        var results      = _stack.PopAll();
        bool removedGroup = false;
        var removedMembers = new List<ulong>();
        foreach (var result in results)
        {
            if (result.FocusGroupId.HasValue)
            {
                removedMembers.AddRange(_focus.GroupMembers(result.FocusGroupId.Value));
                _focus.RemoveGroupWithoutRepair(result.FocusGroupId.Value);
                removedGroup = true;
            }
        }
        if (removedGroup)
        {
            _focus.ClearDeferredFocusIfExcluded(removedMembers);
            RebuildFocusTraps();
            _focus.RepairFocusAfterExcludingIds(removedMembers);
            RefreshInactiveModalReturnFocusTargets();
        }
        return results;
    }

    /// <summary>
    /// Handle an event with automatic focus trap popping.
    /// If the event causes the modal to close, the focus trap is popped.
    /// Rust: <c>pub fn handle_event(...) -&gt; Option&lt;ModalResult&gt;</c>.
    /// </summary>
    public ModalResult? HandleEvent(TerminalEvent @event, HitTestResult? hit = null)
    {
        // Handle focus (window focus gain/loss) events.
        if (@event is FocusTerminalEvent focusEvt)
        {
            _focus.ApplyHostFocus(focusEvt.Focused);
            if (focusEvt.Focused)
                RefreshInactiveModalReturnFocusTargets();
            return null;
        }

        var result = _stack.HandleEvent(@event, hit);
        if (result != null) HandleClosedResult(result);
        return result;
    }

    /// <summary>
    /// Check if focus is currently trapped in a modal.
    /// Rust: <c>pub fn is_focus_trapped(&amp;self) -&gt; bool</c>.
    /// </summary>
    public bool IsFocusTrapped() => _focus.IsTrapped();

    /// <summary>
    /// Get a reference to the underlying modal stack.
    /// Rust: <c>pub fn stack(&amp;self) -&gt; &amp;ModalStack</c>.
    /// </summary>
    public ModalStack Stack() => _stack;

    /// <summary>
    /// Get a mutable reference to the underlying modal stack.
    /// Warning: Direct manipulation may desync focus state. Call ResyncFocusState() after.
    /// Rust: <c>pub fn stack_mut(&amp;mut self) -&gt; &amp;mut ModalStack</c>.
    /// </summary>
    public ModalStack StackMut() => _stack;

    /// <summary>
    /// Get a reference to the underlying focus manager.
    /// Rust: <c>pub fn focus(&amp;self) -&gt; &amp;FocusManager</c>.
    /// </summary>
    public UpstreamFocusManager Focus() => _focus;

    /// <summary>
    /// Get a mutable reference to the underlying focus manager.
    /// Warning: Direct manipulation may desync modal focus restoration.
    /// Rust: <c>pub fn focus_mut(&amp;mut self) -&gt; &amp;mut FocusManager</c>.
    /// </summary>
    public UpstreamFocusManager FocusMut() => _focus;

    /// <summary>
    /// Rebuild modal focus state after direct mutation via StackMut() or FocusMut().
    /// Rust: <c>pub fn resync_focus_state(&amp;mut self)</c>.
    /// </summary>
    public void ResyncFocusState()
    {
        RebuildFocusTraps();
        RefreshInactiveModalReturnFocusTargets();
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void HandleClosedResult(ModalResult result)
    {
        if (result.FocusGroupId.HasValue)
            CloseFocusGroup(result.FocusGroupId.Value);
    }

    private void CloseFocusGroup(uint groupId)
    {
        var closingMembers = _focus.GroupMembers(groupId);
        if (GroupHasFocusableMember(groupId))
        {
            _focus.PopTrap();
            _focus.RemoveGroup(groupId);
        }
        else
        {
            _focus.RemoveGroupWithoutRepair(groupId);
        }
        _focus.RepairFocusAfterExcludingIds(closingMembers);
        if (!_focus.IsTrapped() && _focus.HostFocused())
        {
            _baseFocusSet = false;
            _baseFocus    = null;
        }
        RefreshInactiveModalReturnFocusTargets();
    }

    private bool GroupHasFocusableMember(uint groupId)
        => _focus.GroupMembers(groupId).Any(id => _focus.Graph().Get(id)?.IsFocusable == true);

    private bool ReturnFocusRemainsValidAfterRemovingGroup(
        ModalId upperModalId,
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
            : FocusTargetIsFocusable(returnFocus) && !removedMembers.Contains(returnFocus!.Value);
    }

    private ulong? EffectiveReturnFocusForGroup(uint groupId)
    {
        // Walk specs to find the effective return focus for the given group.
        ulong? effectiveReturnFocus = null;
        foreach (var (_, spec) in _stack.FocusModalSpecsInOrder())
        {
            if (spec.GroupId == groupId)
            {
                effectiveReturnFocus = spec.ReturnFocus;
                break;
            }
        }
        return effectiveReturnFocus;
    }

    private bool FocusTargetIsFocusable(ulong? target)
        => target.HasValue && _focus.Graph().Get(target.Value)?.IsFocusable == true;

    private bool FocusTargetInGroup(ulong? target, uint groupId)
        => target.HasValue && FocusTargetIsFocusable(target)
            && _focus.GroupMembers(groupId).Contains(target.Value);

    private void RebuildFocusTraps()
    {
        var specs = FocusModalSpecsInOrder();
        var hadActiveTrapBefore       = _focus.IsTrapped();
        var preservedLogicalTarget    = _focus.LogicalFocusTarget();
        var activationBaseFocus       = _focus.HostFocused() ? _focus.Current() : _focus.DeferredFocusTarget();
        _focus.ClearTraps();

        bool hasActiveTrap = false;
        if (!_focus.HostFocused())
        {
            if (_focus.Current().HasValue) _focus.Blur();
            foreach (var (groupId, returnFocus) in specs)
                hasActiveTrap |= _focus.PushTrapWithReturnFocus(groupId, returnFocus);

            if (hasActiveTrap && !hadActiveTrapBefore && !_baseFocusSet)
            {
                _baseFocusSet = true;
                _baseFocus    = activationBaseFocus;
            }

            if (!hasActiveTrap)
            {
                var (restoreSet, restoreTarget) = GetRestoreTarget(!hadActiveTrapBefore, preservedLogicalTarget, false, null);
                if (restoreSet)
                    _focus.ReplaceDeferredFocusTarget(restoreTarget);
            }
            return;
        }

        // Host is focused.
        foreach (var (groupId, returnFocus) in specs)
            hasActiveTrap |= _focus.PushTrapWithReturnFocus(groupId, returnFocus);

        if (hasActiveTrap && !hadActiveTrapBefore && !_baseFocusSet)
        {
            _baseFocusSet = true;
            _baseFocus    = activationBaseFocus;
        }

        if (!hasActiveTrap)
        {
            var (restoreSet, restoreTarget) = GetRestoreTarget(!hadActiveTrapBefore, preservedLogicalTarget, false, null);
            if (restoreSet && restoreTarget.HasValue)
                _focus.FocusWithoutHistory(restoreTarget.Value);
            else if (restoreSet && !restoreTarget.HasValue && _focus.Current().HasValue)
                _focus.Blur();
            if (restoreSet && restoreTarget.HasValue && _focus.Current() != restoreTarget.Value)
                _focus.FocusFirstWithoutHistoryForRestore();
            // Clear if current became non-focusable.
            if (_focus.Current() is { } cur && (_focus.Graph().Get(cur)?.IsFocusable != true))
                _focus.Blur();
            _baseFocusSet = false;
            _baseFocus    = null;
            return;
        }

        _focus.ApplyHostFocus(true);
    }

    // Returns collapsed active-group (groupId, returnFocus) pairs.
    private List<(uint groupId, ulong? returnFocus)> FocusModalSpecsInOrder()
    {
        var result = new List<(uint, ulong?)>();
        foreach (var (_, spec) in _stack.FocusModalSpecsInOrder())
            if (GroupHasFocusableMember(spec.GroupId))
                result.Add((spec.GroupId, spec.ReturnFocus));
        return result;
    }

    private (bool isSet, ulong? value) GetRestoreTarget(
        bool notTrappedBefore, ulong? preservedLogicalTarget,
        bool trailingSet, ulong? trailingRestore)
    {
        if (notTrappedBefore && preservedLogicalTarget.HasValue)
            return (true, preservedLogicalTarget);
        if (trailingSet)
            return (true, trailingRestore);
        if (_baseFocusSet)
            return (true, _baseFocus);
        return (false, null);
    }

    private void RefreshInactiveModalReturnFocusTargets()
    {
        var logicalTarget   = _focus.LogicalFocusTarget();
        var focusModals     = _stack.FocusModalSpecsInOrder();
        int? topmostActive  = null;
        for (int i = focusModals.Count - 1; i >= 0; i--)
        {
            if (GroupHasFocusableMember(focusModals[i].Item2.GroupId))
            { topmostActive = i; break; }
        }

        int startIndex = topmostActive.HasValue ? topmostActive.Value + 1 : 0;
        for (int i = startIndex; i < focusModals.Count; i++)
        {
            var (modalId, trap) = focusModals[i];
            if (GroupHasFocusableMember(trap.GroupId)) continue;
            _stack.SetFocusReturnFocus(modalId, logicalTarget);
        }

        // Refresh active modal return targets for invalid lower selections.
        if (!topmostActive.HasValue) return;
        for (int upperIdx = 1; upperIdx <= topmostActive.Value; upperIdx++)
        {
            var (_, lowerTrap) = focusModals[upperIdx - 1];
            var (upperModalId, upperTrap) = focusModals[upperIdx];
            if (!GroupHasFocusableMember(lowerTrap.GroupId)
                || FocusTargetInGroup(upperTrap.ReturnFocus, lowerTrap.GroupId))
                continue;
            var replacement = _focus.GroupPrimaryFocusTarget(lowerTrap.GroupId);
            _stack.SetFocusReturnFocus(upperModalId, replacement);
        }
    }
}

// ── NoOpUpstreamModal ─────────────────────────────────────────────────────

/// <summary>
/// Minimal IUpstreamStackModal used internally as a bridge placeholder.
/// DIVERGENCE: This is an implementation detail of the C# bridge infrastructure.
/// </summary>
internal sealed class NoOpUpstreamModal : IUpstreamStackModal
{
    public object? HandleEvent(object @event, object? hit) => null;
    public void RenderContent(Rect area, object frame) { }
}
