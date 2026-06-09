// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-render/src/frame.rs
// Frame = Buffer + GraphemePool for a render pass (minimal surface for string_model usage).

using FrankenTui.Core;

namespace FrankenTui.Render;

// ── WidgetSignal ──────────────────────────────────────────────────────────────
// Port of pub struct WidgetSignal in frame.rs (lines 321-373).

/// <summary>
/// Carries rendering priority metadata for a widget within a frame.
/// Budget-aware wrappers register one signal per render call so the runtime
/// can make informed skip/degrade decisions.
/// <para>Port of <c>pub struct WidgetSignal</c> in ftui-render/src/frame.rs.</para>
/// </summary>
public sealed class WidgetSignal
{
    /// <summary>Stable widget identifier.</summary>
    public ulong WidgetId { get; set; }
    /// <summary>Whether this widget is essential.</summary>
    public bool Essential { get; set; }
    /// <summary>Base priority in [0, 1].</summary>
    public float Priority { get; set; } = 0.5f;
    /// <summary>Milliseconds since last render.</summary>
    public ulong StalenessMs { get; set; }
    /// <summary>Focus boost in [0, 1].</summary>
    public float FocusBoost { get; set; }
    /// <summary>Interaction boost in [0, 1].</summary>
    public float InteractionBoost { get; set; }
    /// <summary>Widget area in cells (width * height).</summary>
    public uint AreaCells { get; set; } = 1;
    /// <summary>Estimated render cost in microseconds.</summary>
    public float CostEstimateUs { get; set; } = 5.0f;
    /// <summary>Recent measured cost (EMA), if available.</summary>
    public float RecentCostUs { get; set; } = 5.0f;

    /// <summary>Create a signal with neutral defaults.
    /// Port of <c>WidgetSignal::new(widget_id)</c>.</summary>
    public static WidgetSignal New(ulong widgetId) => new() { WidgetId = widgetId };

    /// <summary>Create a shallow clone of this signal.
    /// Port of Rust <c>#[derive(Clone)]</c> on <c>WidgetSignal</c>.</summary>
    public WidgetSignal Clone() => new()
    {
        WidgetId       = WidgetId,
        Essential      = Essential,
        Priority       = Priority,
        StalenessMs    = StalenessMs,
        FocusBoost     = FocusBoost,
        InteractionBoost = InteractionBoost,
        AreaCells      = AreaCells,
        CostEstimateUs = CostEstimateUs,
        RecentCostUs   = RecentCostUs,
    };
}

public sealed class Frame
{
    readonly GraphemePool _pool;
    public Buffer Buffer { get; private set; }
    HitGrid? _hitGrid;

    public Frame(ushort width, ushort height, GraphemePool pool)
    {
        Buffer = new Buffer(width, height);
        _pool = pool;
    }

    /// <summary>Create a frame with an attached hit grid for mouse interaction.
    /// Matches Rust Frame::with_hit_grid.</summary>
    public static Frame WithHitGrid(ushort width, ushort height, GraphemePool pool)
    {
        var frame = new Frame(width, height, pool);
        frame._hitGrid = new HitGrid(width, height);
        return frame;
    }

    public Buffer BufferOverride { set => Buffer = value; }

    public ushort Width => Buffer.Width;
    public ushort Height => Buffer.Height;

    public DegradationLevel Degradation { get; private set; } = DegradationLevel.Full;
    public void SetDegradation(DegradationLevel l) => Degradation = l;

    public GraphemeId InternWithWidth(string text, byte width) => _pool.Intern(text, width);

    // ── Hit owner scoping ─────────────────────────────────────────────────
    // Matches Rust Frame::with_hit_owner — scopes all RegisterHit calls within
    // the action to carry the given owner ulong.
    private ulong? _currentHitOwner;

    /// <summary>Register a hit region for mouse interaction.
    /// Matches Rust Frame::register_hit.</summary>
    public void RegisterHit(Rect area, HitId id, HitRegionKind region, ulong data)
    {
        // DIVERGENCE: upstream register_hit carries an optional HitOwner; HitGrid.Register
        // does not yet accept HitOwner. Owner scoping is tracked via _currentHitOwner but
        // the value is not forwarded until HitGrid gains an overload for it.
        _hitGrid?.Register(area, id, region, new HitData(data));
    }

    /// <summary>
    /// Execute <paramref name="action"/> with all RegisterHit calls tagged with
    /// <paramref name="owner"/>. Restores the previous owner after the call.
    /// Matches Rust Frame::with_hit_owner(owner, |frame| { ... }).
    /// </summary>
    public void WithHitOwner(ulong owner, Action<Frame> action)
    {
        var previous = _currentHitOwner;
        _currentHitOwner = owner;
        try { action(this); }
        finally { _currentHitOwner = previous; }
    }

    /// <summary>Hit-test at the given position.
    /// Returns (HitId, HitRegionKind, data) if a region was registered, else null.
    /// Matches Rust Frame::hit_test.</summary>
    public (HitId, HitRegionKind, ulong)? HitTest(ushort x, ushort y)
    {
        var result = _hitGrid?.HitTest(x, y);
        if (result is null) return null;
        return (result.Value.Item1, result.Value.Item2, result.Value.Item3.Value);
    }

    /// <summary>
    /// Hit-test at the given position, returning full provenance including owner.
    /// Matches Rust Frame::hit_test_detailed.
    /// </summary>
    public HitTestResult? HitTestDetailed(ushort x, ushort y)
    {
        // DIVERGENCE: HitGrid.Get is used (HitGrid.GetCell does not exist in this port).
        var cell = _hitGrid?.Get(x, y);
        if (cell is null) return null;
        var c = cell.Value;
        return new HitTestResult
        {
            Id = c.WidgetId!.Value,
            Region = c.Region,
            Data = c.Data,
            // DIVERGENCE: HitCell does not carry Owner in this port; Owner defaults to null.
            Owner = null,
        };
    }

    // ── Widget budget/signal tracking (used by Budgeted<W>) ────────────
    readonly List<(ulong id,bool essential,uint areaCells)> _signals=new();
    ulong[] _renderSet=Array.Empty<ulong>();

    /// <summary>
    /// Register a widget signal produced by a <c>Budgeted</c> wrapper.
    /// Port of <c>pub fn register_widget_signal(&amp;mut self, signal: WidgetSignal)</c> in frame.rs.
    /// </summary>
    public void RegisterWidgetSignal(WidgetSignal signal)
        => _signals.Add((signal.WidgetId, signal.Essential, signal.AreaCells));

    // DIVERGENCE: The simplified (widgetId, essential, areaCells) overload is kept for
    // backwards compatibility with pre-existing callers that pre-date the WidgetSignal port.
    /// <summary>Simplified overload retained for existing callers.</summary>
    public void RegisterWidgetSignal(ulong widgetId, bool essential, uint areaCells)
        => _signals.Add((widgetId, essential, areaCells));

    public bool ShouldRenderWidget(ulong widgetId, bool essential)
    {
        // During degradation, skip non-essential widgets if we have a render set
        if (_renderSet.Length > 0)
        {
            foreach (var id in _renderSet)
                if (id == widgetId) return true;
            if (!essential) return false;
        }
        return true;
    }

    public void SetRenderSet(ulong[] ids) => _renderSet = ids;

    // ── Cursor position tracking ──────────────────────────────────────────
    // Upstream: frame.set_cursor(Some((x,y))) / frame.set_cursor_visible(true)

    /// <summary>Screen cursor position set by the focused widget; null = not set.</summary>
    public (ushort x, ushort y)? CursorPosition { get; private set; }

    /// <summary>Whether the hardware cursor should be visible this frame.</summary>
    public bool CursorVisible { get; private set; }

    /// <summary>Set or clear the cursor position. Matches Rust frame.set_cursor(Option).</summary>
    public void SetCursor((ushort x, ushort y)? position) => CursorPosition = position;

    /// <summary>Set cursor visibility. Matches Rust frame.set_cursor_visible(bool).</summary>
    public void SetCursorVisible(bool visible) => CursorVisible = visible;

    // ── Link registry ─────────────────────────────────────────────────────────
    // Upstream: frame.register_link(url) → u32. Returns 0 if not available/full.
    // Delegates to LinkRegistry (port of ftui-render/src/link_registry.rs).
    readonly LinkRegistry _linkRegistry = new();

    /// <summary>Register a hyperlink URL and return its ID.
    /// Returns 0 if the link registry is full or the URL is unsafe.
    /// Matches Rust Frame::register_link.</summary>
    public uint RegisterLink(string url) => _linkRegistry.Register(url);

    // DIVERGENCE: Rust frame.arena provides arena-based allocation for grapheme slices.
    // .NET has no equivalent; callers fall back to heap allocation. Exposed as null.
    public object? Arena => null;
}
