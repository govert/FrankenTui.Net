// Upstream source: crates/ftui-widgets/src/inspector.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Direct 1-1 port of DiagnosticEventKind, DiagnosticEntry, TelemetryHooks,
// InspectorMode, WidgetInfo, InspectorState, InspectorOverlay, HitInfo,
// and public init/diagnostics/state management functions.

using System.Threading;
using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

// =============================================================================
// Global diagnostics state
// =============================================================================

/// <summary>Global diagnostic enable flag (checked once at startup).</summary>
public static class InspectorDiagnostics
{
    private static int _enabled; // 0 = false, 1 = true
    private static long _eventCounter;

    /// <summary>Initialize diagnostic settings from environment.</summary>
    public static void Init()
    {
        SetEnabled(EnvFlagHelper.EnvFlagEnabled("FTUI_INSPECTOR_DIAGNOSTICS"));
    }

    /// <summary>Check if diagnostics are enabled.</summary>
    public static bool IsEnabled() => Interlocked.CompareExchange(ref _enabled, 0, 0) == 1;

    /// <summary>Set diagnostics enabled state (for testing).</summary>
    public static void SetEnabled(bool enabled) =>
        Interlocked.Exchange(ref _enabled, enabled ? 1 : 0);

    /// <summary>Get next monotonic event sequence number.</summary>
    public static ulong NextEventSeq() =>
        (ulong)Interlocked.Increment(ref _eventCounter);

    /// <summary>Reset event counter (for testing determinism).</summary>
    public static void ResetEventCounter() =>
        Interlocked.Exchange(ref _eventCounter, 0);

    /// <summary>Check if deterministic mode is enabled.</summary>
    public static bool IsDeterministicMode() =>
        EnvFlagHelper.EnvFlagEnabled("FTUI_INSPECTOR_DETERMINISTIC");
}

// =============================================================================
// DiagnosticEventKind
// =============================================================================

/// <summary>Diagnostic event types for JSONL logging.</summary>
public enum DiagnosticEventKind
{
    /// <summary>Inspector toggled on/off.</summary>
    InspectorToggled,
    /// <summary>Inspector mode changed.</summary>
    ModeChanged,
    /// <summary>Hover position changed.</summary>
    HoverChanged,
    /// <summary>Selection changed.</summary>
    SelectionChanged,
    /// <summary>Detail panel toggled.</summary>
    DetailPanelToggled,
    /// <summary>Hit region visibility toggled.</summary>
    HitsToggled,
    /// <summary>Widget bounds visibility toggled.</summary>
    BoundsToggled,
    /// <summary>Widget name labels toggled.</summary>
    NamesToggled,
    /// <summary>Render time labels toggled.</summary>
    TimesToggled,
    /// <summary>Widgets cleared for a new frame.</summary>
    WidgetsCleared,
    /// <summary>Widget registered for inspection.</summary>
    WidgetRegistered,
}

/// <summary>Extensions for <see cref="DiagnosticEventKind"/>.</summary>
public static class DiagnosticEventKindExtensions
{
    /// <summary>Get the JSONL event type string.</summary>
    public static string AsStr(this DiagnosticEventKind kind) => kind switch
    {
        DiagnosticEventKind.InspectorToggled => "inspector_toggled",
        DiagnosticEventKind.ModeChanged => "mode_changed",
        DiagnosticEventKind.HoverChanged => "hover_changed",
        DiagnosticEventKind.SelectionChanged => "selection_changed",
        DiagnosticEventKind.DetailPanelToggled => "detail_panel_toggled",
        DiagnosticEventKind.HitsToggled => "hits_toggled",
        DiagnosticEventKind.BoundsToggled => "bounds_toggled",
        DiagnosticEventKind.NamesToggled => "names_toggled",
        DiagnosticEventKind.TimesToggled => "times_toggled",
        DiagnosticEventKind.WidgetsCleared => "widgets_cleared",
        DiagnosticEventKind.WidgetRegistered => "widget_registered",
        _ => "unknown",
    };
}

// =============================================================================
// DiagnosticEntry
// =============================================================================

/// <summary>JSONL diagnostic log entry.</summary>
public sealed class DiagnosticEntry : IDiagnosticRecord
{
    /// <summary>Monotonic sequence number.</summary>
    public ulong Seq { get; set; }
    /// <summary>Timestamp in microseconds.</summary>
    public ulong TimestampUs { get; set; }
    /// <summary>Event kind.</summary>
    public DiagnosticEventKind Kind { get; set; }
    /// <summary>Current inspector mode.</summary>
    public InspectorMode? Mode { get; set; }
    /// <summary>Previous inspector mode.</summary>
    public InspectorMode? PreviousMode { get; set; }
    /// <summary>Hover position.</summary>
    public (ushort X, ushort Y)? HoverPos { get; set; }
    /// <summary>Selected widget id.</summary>
    public ulong? Selected { get; set; }
    /// <summary>Widget name (if applicable).</summary>
    public string? WidgetName { get; set; }
    /// <summary>Widget area (if applicable).</summary>
    public Rect? WidgetArea { get; set; }
    /// <summary>Widget depth (if applicable).</summary>
    public byte? WidgetDepth { get; set; }
    /// <summary>Widget hit id (if applicable).</summary>
    public ulong? WidgetHitId { get; set; }
    /// <summary>Total widget count (if applicable).</summary>
    public int? WidgetCount { get; set; }
    /// <summary>Flag name (for toggles).</summary>
    public string? Flag { get; set; }
    /// <summary>Flag enabled state (for toggles).</summary>
    public bool? Enabled { get; set; }
    /// <summary>Additional context string.</summary>
    public string? Context { get; set; }
    /// <summary>Checksum for determinism verification.</summary>
    public ulong Checksum { get; set; }

    /// <summary>Create a new diagnostic entry with current timestamp.</summary>
    public DiagnosticEntry(DiagnosticEventKind kind)
    {
        var seq = InspectorDiagnostics.NextEventSeq();
        Kind = kind;
        Seq = seq;
        TimestampUs = InspectorDiagnostics.IsDeterministicMode()
            ? seq * 1_000
            : (ulong)(DateTime.UtcNow.Ticks / 10); // DIVERGENCE: microseconds from DateTime
    }

    /// <summary>Set inspector mode.</summary>
    public DiagnosticEntry WithMode(InspectorMode mode) { Mode = mode; return this; }

    /// <summary>Set previous inspector mode.</summary>
    public DiagnosticEntry WithPreviousMode(InspectorMode mode) { PreviousMode = mode; return this; }

    /// <summary>Set hover position.</summary>
    public DiagnosticEntry WithHoverPos((ushort X, ushort Y)? pos) { HoverPos = pos; return this; }

    /// <summary>Set selected widget id.</summary>
    public DiagnosticEntry WithSelected(ulong? selected) { Selected = selected; return this; }

    /// <summary>Set widget info.</summary>
    public DiagnosticEntry WithWidget(WidgetInfo widget)
    {
        WidgetName = widget.Name;
        WidgetArea = widget.Area;
        WidgetDepth = widget.Depth;
        WidgetHitId = widget.HitId;
        return this;
    }

    /// <summary>Set widget count.</summary>
    public DiagnosticEntry WithWidgetCount(int count) { WidgetCount = count; return this; }

    /// <summary>Set flag toggle details.</summary>
    public DiagnosticEntry WithFlag(string flag, bool enabled) { Flag = flag; Enabled = enabled; return this; }

    /// <summary>Set context string.</summary>
    public DiagnosticEntry WithContext(string context) { Context = context; return this; }

    /// <summary>Compute and set checksum.</summary>
    public DiagnosticEntry WithChecksum()
    {
        Checksum = ComputeChecksum();
        return this;
    }

    /// <summary>Compute FNV-1a hash of entry fields.</summary>
    private ulong ComputeChecksum()
    {
        var payload = $"{Kind}{Mode}{PreviousMode}{HoverPos}{Selected}{WidgetName ?? ""}{WidgetArea}{WidgetDepth ?? 0}{WidgetHitId ?? 0}{WidgetCount ?? 0}{Flag ?? ""}{Enabled}{Context ?? ""}";
        return Fnv1aHash.Hash(System.Text.Encoding.UTF8.GetBytes(payload));
    }

    /// <summary>Format as JSONL string.</summary>
    public string ToJsonl()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"\"seq\":{Seq}",
            $"\"ts_us\":{TimestampUs}",
            $"\"kind\":\"{Kind.AsStr()}\"",
        };

        if (Mode.HasValue) parts.Add($"\"mode\":\"{Mode.Value.AsStr()}\"");
        if (PreviousMode.HasValue) parts.Add($"\"prev_mode\":\"{PreviousMode.Value.AsStr()}\"");
        if (HoverPos.HasValue) { parts.Add($"\"hover_x\":{HoverPos.Value.X}"); parts.Add($"\"hover_y\":{HoverPos.Value.Y}"); }
        if (Selected.HasValue) parts.Add($"\"selected\":{Selected.Value}");
        if (WidgetName is not null) parts.Add($"\"widget_name\":{JsonHelper.JsonStringLiteral(WidgetName)}");
        if (WidgetArea.HasValue) parts.Add($"\"widget_area\":\"{WidgetArea.Value.X},{WidgetArea.Value.Y},{WidgetArea.Value.Width},{WidgetArea.Value.Height}\"");
        if (WidgetDepth.HasValue) parts.Add($"\"widget_depth\":{WidgetDepth.Value}");
        if (WidgetHitId.HasValue) parts.Add($"\"widget_hit_id\":{WidgetHitId.Value}");
        if (WidgetCount.HasValue) parts.Add($"\"widget_count\":{WidgetCount.Value}");
        if (Flag is not null) parts.Add($"\"flag\":{JsonHelper.JsonStringLiteral(Flag)}");
        if (Enabled.HasValue) parts.Add($"\"enabled\":{(Enabled.Value ? "true" : "false")}");
        if (Context is not null) parts.Add($"\"context\":{JsonHelper.JsonStringLiteral(Context)}");
        parts.Add($"\"checksum\":{Checksum}");

        return "{" + string.Join(",", parts) + "}";
    }
}

// =============================================================================
// TelemetryHooks
// =============================================================================

/// <summary>Telemetry hooks for observing inspector diagnostic entries.</summary>
public sealed class TelemetryHooks : IDiagnosticHookDispatch<DiagnosticEntry>
{
    private Action<DiagnosticEntry>? _onToggle;
    private Action<DiagnosticEntry>? _onModeChange;
    private Action<DiagnosticEntry>? _onHoverChange;
    private Action<DiagnosticEntry>? _onSelectionChange;
    private Action<DiagnosticEntry>? _onAny;

    public TelemetryHooks OnToggle(Action<DiagnosticEntry> f) { _onToggle = f; return this; }
    public TelemetryHooks OnModeChange(Action<DiagnosticEntry> f) { _onModeChange = f; return this; }
    public TelemetryHooks OnHoverChange(Action<DiagnosticEntry> f) { _onHoverChange = f; return this; }
    public TelemetryHooks OnSelectionChange(Action<DiagnosticEntry> f) { _onSelectionChange = f; return this; }
    public TelemetryHooks OnAny(Action<DiagnosticEntry> f) { _onAny = f; return this; }

    /// <summary>Dispatch a diagnostic entry to registered hooks.</summary>
    public void Dispatch(DiagnosticEntry entry)
    {
        _onAny?.Invoke(entry);
        switch (entry.Kind)
        {
            case DiagnosticEventKind.InspectorToggled: _onToggle?.Invoke(entry); break;
            case DiagnosticEventKind.ModeChanged: _onModeChange?.Invoke(entry); break;
            case DiagnosticEventKind.HoverChanged: _onHoverChange?.Invoke(entry); break;
            case DiagnosticEventKind.SelectionChanged: _onSelectionChange?.Invoke(entry); break;
        }
    }
}

// =============================================================================
// InspectorMode
// =============================================================================

/// <summary>Inspector display modes.</summary>
public enum InspectorMode
{
    /// <summary>Show hit regions.</summary>
    Hits,
    /// <summary>Show widget boundaries.</summary>
    Bounds,
    /// <summary>Show widget names.</summary>
    Names,
    /// <summary>Show render times.</summary>
    Times,
}

/// <summary>Extensions for <see cref="InspectorMode"/>.</summary>
public static class InspectorModeExtensions
{
    /// <summary>Cycle to the next mode.</summary>
    public static InspectorMode Cycle(this InspectorMode mode) => mode switch
    {
        InspectorMode.Hits => InspectorMode.Bounds,
        InspectorMode.Bounds => InspectorMode.Names,
        InspectorMode.Names => InspectorMode.Times,
        InspectorMode.Times => InspectorMode.Hits,
        _ => InspectorMode.Hits,
    };

    /// <summary>Returns true if this mode is active for display.</summary>
    public static bool IsActive(this InspectorMode mode) => mode switch
    {
        InspectorMode.Hits => true,
        InspectorMode.Bounds => true,
        InspectorMode.Names => true,
        InspectorMode.Times => true,
        _ => false,
    };

    /// <summary>Get the JSONL mode string.</summary>
    public static string AsStr(this InspectorMode mode) => mode switch
    {
        InspectorMode.Hits => "hits",
        InspectorMode.Bounds => "bounds",
        InspectorMode.Names => "names",
        InspectorMode.Times => "times",
        _ => "hits",
    };

    /// <summary>Whether to show hit regions in this mode.</summary>
    public static bool ShowHitRegions(this InspectorMode mode) => mode == InspectorMode.Hits;

    /// <summary>Whether to show widget bounds in this mode.</summary>
    public static bool ShowWidgetBounds(this InspectorMode mode) =>
        mode == InspectorMode.Bounds || mode == InspectorMode.Names || mode == InspectorMode.Times;
}

// =============================================================================
// WidgetInfo
// =============================================================================

/// <summary>Information about a widget registered for inspection.</summary>
public sealed class WidgetInfo
{
    /// <summary>Widget name.</summary>
    public string Name { get; }
    /// <summary>Widget area (bounds).</summary>
    public Rect Area { get; set; }
    /// <summary>Optional hit ID for hit-testing.</summary>
    public ulong? HitId { get; set; }
    /// <summary>Render time in microseconds.</summary>
    public ulong RenderTimeUs { get; set; }
    /// <summary>Depth in the widget tree.</summary>
    public byte Depth { get; set; }
    /// <summary>Child widget info entries.</summary>
    public List<WidgetInfo> Children { get; } = [];

    /// <summary>Hit regions registered by this widget.</summary>
    public List<(Rect Rect, HitRegionKind Region, HitData Data)> HitRegions { get; } = [];

    public WidgetInfo(string name, Rect area)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Area = area;
    }

    public WidgetInfo WithHitId(ulong id) { HitId = id; return this; }
    public WidgetInfo WithRenderTimeUs(ulong us) { RenderTimeUs = us; return this; }
    public WidgetInfo WithDepth(byte depth) { Depth = depth; return this; }

    /// <summary>Add a child widget info.</summary>
    public void AddChild(WidgetInfo child) => Children.Add(child);

    /// <summary>Register a hit region.</summary>
    public void AddHitRegion(Rect rect, HitRegionKind region, HitData data) =>
        HitRegions.Add((rect, region, data));

    /// <summary>Get a color for bounds based on depth.</summary>
    public static PackedRgba BoundColor(byte depth)
    {
        var colors = new[]
        {
            PackedRgba.Rgb(255, 100, 100),
            PackedRgba.Rgb(100, 255, 100),
            PackedRgba.Rgb(100, 100, 255),
            PackedRgba.Rgb(255, 255, 100),
            PackedRgba.Rgb(255, 100, 255),
            PackedRgba.Rgb(100, 255, 255),
        };
        return colors[depth % colors.Length];
    }

    /// <summary>Get a color for a hit region based on its kind.</summary>
    public static PackedRgba RegionColor(HitRegionKind region) => region switch
    {
        HitRegionKind.Content => PackedRgba.Rgb(80, 220, 80),
        HitRegionKind.Border => PackedRgba.Rgb(200, 200, 60),
        HitRegionKind.Scrollbar => PackedRgba.Rgb(60, 200, 200),
        HitRegionKind.Handle => PackedRgba.Rgb(200, 60, 200),
        HitRegionKind.Button => PackedRgba.Rgb(60, 200, 60),
        HitRegionKind.Link => PackedRgba.Rgb(60, 60, 220),
        _ => PackedRgba.Rgb(160, 160, 160),
    };
}

// =============================================================================
// InspectorState
// =============================================================================

/// <summary>State for the UI inspector overlay.</summary>
public sealed class InspectorState
{
    private bool _active;
    private InspectorMode _mode = InspectorMode.Hits;
    private (ushort X, ushort Y)? _hoverPos;
    private ulong? _selected;
    private bool _showDetailPanel;
    private bool _showHits = true;
    private bool _showBounds = true;
    private bool _showNames = true;
    private bool _showTimes = true;
    private readonly List<WidgetInfo> _widgets = [];
    private DiagnosticSupport<DiagnosticEntry, TelemetryHooks>? _diagnostics;

    /// <summary>Whether the inspector is active.</summary>
    public bool IsActive => _active;
    /// <summary>Current inspector mode.</summary>
    public InspectorMode Mode => _mode;
    /// <summary>Current hover position.</summary>
    public (ushort X, ushort Y)? HoverPos => _hoverPos;
    /// <summary>Currently selected widget ID.</summary>
    public ulong? SelectedId => _selected;
    /// <summary>Whether detail panel is shown.</summary>
    public bool ShowDetailPanel => _showDetailPanel;
    /// <summary>Whether to show hit regions.</summary>
    public bool ShowHits => _showHits && _mode.ShowHitRegions();
    /// <summary>Whether to show widget bounds.</summary>
    public bool ShowBounds => _showBounds && _mode.ShowWidgetBounds();
    /// <summary>Whether to show widget name labels.</summary>
    public bool ShowNames => _showNames;
    /// <summary>Whether to show render time labels.</summary>
    public bool ShowTimes => _showTimes;
    /// <summary>Registered widget info entries.</summary>
    public IReadOnlyList<WidgetInfo> Widgets => _widgets;

    /// <summary>Enable diagnostics with optional hooks.</summary>
    public InspectorState WithDiagnostics()
    {
        _diagnostics = new DiagnosticSupport<DiagnosticEntry, TelemetryHooks>()
            .WithLog(new DiagnosticLog<DiagnosticEntry>());
        return this;
    }

    /// <summary>Enable telemetry hooks.</summary>
    public InspectorState WithTelemetryHooks(TelemetryHooks hooks)
    {
        _diagnostics ??= new DiagnosticSupport<DiagnosticEntry, TelemetryHooks>();
        _diagnostics = _diagnostics.WithHooks(hooks);
        return this;
    }

    /// <summary>Get diagnostic log, if enabled.</summary>
    public DiagnosticLog<DiagnosticEntry>? DiagnosticLog => _diagnostics?.Log;

    /// <summary>Toggle the inspector on/off, recording a diagnostic event.</summary>
    public void Toggle()
    {
        _active = !_active;
        if (_diagnostics?.IsActive == true)
        {
            var entry = new DiagnosticEntry(DiagnosticEventKind.InspectorToggled)
                .WithMode(_mode)
                .WithChecksum();
            _diagnostics.Record(entry);
        }
    }

    /// <summary>Cycle to the next inspector mode.</summary>
    public void CycleMode()
    {
        var prev = _mode;
        _mode = _mode.Cycle();
        if (_diagnostics?.IsActive == true)
        {
            var entry = new DiagnosticEntry(DiagnosticEventKind.ModeChanged)
                .WithMode(_mode)
                .WithPreviousMode(prev)
                .WithChecksum();
            _diagnostics.Record(entry);
        }
    }

    /// <summary>Set the inspector mode by number (0-3).</summary>
    public void SetMode(byte modeNum)
    {
        if (modeNum > 3) return;
        var prev = _mode;
        _mode = (InspectorMode)modeNum;
        if (_diagnostics?.IsActive == true)
        {
            var entry = new DiagnosticEntry(DiagnosticEventKind.ModeChanged)
                .WithMode(_mode)
                .WithPreviousMode(prev)
                .WithChecksum();
            _diagnostics.Record(entry);
        }
    }

    /// <summary>Set hover position.</summary>
    public void SetHover((ushort X, ushort Y)? pos)
    {
        _hoverPos = pos;
        if (_diagnostics?.IsActive == true)
        {
            var entry = new DiagnosticEntry(DiagnosticEventKind.HoverChanged)
                .WithMode(_mode)
                .WithHoverPos(pos)
                .WithChecksum();
            _diagnostics.Record(entry);
        }
    }

    /// <summary>Select a widget by ID.</summary>
    public void Select(ulong? id)
    {
        _selected = id;
        if (_diagnostics?.IsActive == true)
        {
            var entry = new DiagnosticEntry(DiagnosticEventKind.SelectionChanged)
                .WithMode(_mode)
                .WithSelected(id)
                .WithChecksum();
            _diagnostics.Record(entry);
        }
    }

    /// <summary>Clear current selection.</summary>
    public void ClearSelection() => Select(null);

    /// <summary>Toggle detail panel visibility.</summary>
    public void ToggleDetailPanel()
    {
        _showDetailPanel = !_showDetailPanel;
        RecordToggle("detail_panel", _showDetailPanel);
    }

    /// <summary>Toggle hit region visibility.</summary>
    public void ToggleHits()
    {
        _showHits = !_showHits;
        RecordToggle("hits", _showHits, DiagnosticEventKind.HitsToggled);
    }

    /// <summary>Toggle widget bounds visibility.</summary>
    public void ToggleBounds()
    {
        _showBounds = !_showBounds;
        RecordToggle("bounds", _showBounds, DiagnosticEventKind.BoundsToggled);
    }

    /// <summary>Toggle widget name labels.</summary>
    public void ToggleNames()
    {
        _showNames = !_showNames;
        RecordToggle("names", _showNames, DiagnosticEventKind.NamesToggled);
    }

    /// <summary>Toggle render time labels.</summary>
    public void ToggleTimes()
    {
        _showTimes = !_showTimes;
        RecordToggle("times", _showTimes, DiagnosticEventKind.TimesToggled);
    }

    /// <summary>Clear all registered widgets.</summary>
    public void ClearWidgets()
    {
        _widgets.Clear();
        if (_diagnostics?.IsActive == true)
        {
            var entry = new DiagnosticEntry(DiagnosticEventKind.WidgetsCleared)
                .WithMode(_mode)
                .WithChecksum();
            _diagnostics.Record(entry);
        }
    }

    /// <summary>Register a widget for inspection.</summary>
    public void RegisterWidget(WidgetInfo info)
    {
        _widgets.Add(info);
        if (_diagnostics?.IsActive == true)
        {
            var entry = new DiagnosticEntry(DiagnosticEventKind.WidgetRegistered)
                .WithMode(_mode)
                .WithWidget(info)
                .WithWidgetCount(_widgets.Count)
                .WithChecksum();
            _diagnostics.Record(entry);
        }
    }

    /// <summary>Whether to show hit regions in current mode.</summary>
    public bool ShouldShowHits() => _active && _showHits && _mode.ShowHitRegions();

    /// <summary>Whether to show widget bounds in current mode.</summary>
    public bool ShouldShowBounds() => _active && _showBounds && _mode.ShowWidgetBounds();

    private void RecordToggle(string flag, bool enabled,
        DiagnosticEventKind? kind = null)
    {
        if (_diagnostics?.IsActive != true) return;
        var entry = new DiagnosticEntry(kind ?? DiagnosticEventKind.DetailPanelToggled)
            .WithMode(_mode)
            .WithFlag(flag, enabled)
            .WithChecksum();
        _diagnostics.Record(entry);
    }
}

// =============================================================================
// HitInfo
// =============================================================================

/// <summary>Information about a hit cell from the inspect overlay.</summary>
public readonly record struct OverlayHitInfo
{
    /// <summary>Widget ID.</summary>
    public HitId WidgetId { get; init; }
    /// <summary>Region type.</summary>
    public HitRegionKind Region { get; init; }
    /// <summary>Associated data.</summary>
    public HitData Data { get; init; }
    /// <summary>Screen position.</summary>
    public (ushort X, ushort Y) Position { get; init; }

    /// <summary>Create from a HitCell and position.</summary>
    public static OverlayHitInfo? FromCell(HitCell cell, ushort x, ushort y)
    {
        if (cell.WidgetId is null) return null;
        return new OverlayHitInfo
        {
            WidgetId = cell.WidgetId.Value,
            Region = cell.Region,
            Data = cell.Data,
            Position = (x, y),
        };
    }
}

// =============================================================================
// InspectorOverlay
// =============================================================================

/// <summary>Renders the inspector overlay visualization.
/// Upstream: crates/ftui-widgets/src/inspector.rs — InspectorOverlay rendering methods.
/// Renders hit region overlays, widget bounds, and detail panel by modifying existing buffer cells.</summary>
public sealed class InspectorOverlay
{
    private readonly InspectorState _state;

    public InspectorOverlay(InspectorState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public void Render(FrankenTui.Render.Buffer buffer, Rect area, HitGrid? hitGrid)
    {
        if (!_state.IsActive) return;

        if (_state.ShouldShowHits() && hitGrid is not null)
            RenderHitRegions(buffer, area, hitGrid);

        if (_state.ShouldShowBounds())
            RenderWidgetBounds(buffer, area);

        if (_state.ShowDetailPanel)
            RenderDetailPanel(buffer, area);
    }

    private void RenderHitRegions(FrankenTui.Render.Buffer buffer, Rect area, HitGrid hitGrid)
    {
        var hoverPos = _state.HoverPos;
        var selected = _state.SelectedId;
        var regionColors = new System.Collections.Generic.Dictionary<HitRegionKind, PackedRgba>
        {
            [HitRegionKind.Content] = PackedRgba.Rgba(80, 220, 80, 80),
            [HitRegionKind.Border] = PackedRgba.Rgba(200, 200, 60, 80),
            [HitRegionKind.Scrollbar] = PackedRgba.Rgba(60, 200, 200, 80),
            [HitRegionKind.Handle] = PackedRgba.Rgba(200, 60, 200, 80),
            [HitRegionKind.Button] = PackedRgba.Rgba(60, 200, 60, 80),
            [HitRegionKind.Link] = PackedRgba.Rgba(60, 60, 220, 80),
            [HitRegionKind.Custom] = PackedRgba.Rgba(160, 160, 160, 80),
            [HitRegionKind.None] = PackedRgba.Rgba(0, 0, 0, 0),
        };

        for (ushort y = area.Y; y < area.Bottom && y < buffer.Height; y++)
        {
            for (ushort x = area.X; x < area.Right && x < buffer.Width; x++)
            {
                var hit = hitGrid.Get(x, y);
                if (hit is null || hit.Value.IsEmpty) continue;

                var region = hit.Value.Region;
                if (!regionColors.TryGetValue(region, out var overlayColor)) continue;

                // Apply overlay to existing cell (modify in place)
                if (buffer.Get(x, y) is Cell existing)
                {
                    // Check hover/selection highlights
                    var isHovered = hoverPos?.X == x && hoverPos?.Y == y;
                    var isSelected = selected.HasValue && hit.Value.WidgetId?.Value == selected.Value;
                    var finalColor = isSelected ? PackedRgba.Rgb(255, 255, 255)
                        : isHovered ? PackedRgba.Rgb(200, 200, 200)
                        : overlayColor;

                    buffer.Set(x, y, existing.WithForeground(finalColor));
                }
            }
        }
    }

    private void RenderWidgetBounds(FrankenTui.Render.Buffer buffer, Rect area)
    {
        // Upstream renders widgets recursively; we flatten for simplicity
        foreach (var info in _state.Widgets)
        {
            RenderWidgetBound(info, buffer, area);
        }
    }

    private void RenderWidgetBound(WidgetInfo info, FrankenTui.Render.Buffer buffer, Rect clip)
    {
        var r = info.Area;
        if (r.IsEmpty) return;

        var color = WidgetInfo.BoundColor(info.Depth);
        var xEnd = (ushort)System.Math.Min(r.Right, buffer.Width);
        var yEnd = (ushort)System.Math.Min(r.Bottom, buffer.Height);

        // Draw perimeter outline: modify foreground color of edge cells
        // Top edge
        for (ushort x = r.X; x < xEnd; x++)
            if (buffer.Get(x, r.Y) is Cell c) buffer.Set(x, r.Y, c.WithForeground(color));
        // Bottom edge
        var bottomY = (ushort)(r.Bottom - 1);
        if (bottomY > r.Y)
            for (ushort x = r.X; x < xEnd; x++)
                if (buffer.Get(x, bottomY) is Cell c) buffer.Set(x, bottomY, c.WithForeground(color));
        // Left edge
        for (ushort y = (ushort)(r.Y + 1); y < yEnd && y < bottomY; y++)
            if (buffer.Get(r.X, y) is Cell c) buffer.Set(r.X, y, c.WithForeground(color));
        // Right edge
        var rightX = (ushort)(r.Right - 1);
        if (rightX > r.X)
            for (ushort y = (ushort)(r.Y + 1); y < yEnd && y < bottomY; y++)
                if (buffer.Get(rightX, y) is Cell c) buffer.Set(rightX, y, c.WithForeground(color));

        // Draw widget name label at top-left
        if (_state.ShowNames && !string.IsNullOrEmpty(info.Name))
        {
            var maxLen = (ushort)System.Math.Min(info.Name.Length, (ushort)(r.Width - 2));
            for (var i = 0; i < maxLen; i++)
                if (buffer.Get((ushort)(r.X + 1 + i), r.Y) is Cell c)
                    buffer.Set((ushort)(r.X + 1 + i), r.Y, c.WithForeground(color));
        }

        // Render children
        foreach (var child in info.Children)
            RenderWidgetBound(child, buffer, clip);
    }

    private void RenderDetailPanel(FrankenTui.Render.Buffer buffer, Rect area)
    {
        if (_state.SelectedId is null) return;

        var panelRect = new Rect(area.Right > 30 ? (ushort)(area.Right - 30) : area.X,
            area.Y, (ushort)Math.Min((int)30, (int)area.Width), (ushort)Math.Min((int)12, (int)area.Height));

        // Find selected widget
        WidgetInfo? selected = null;
        foreach (var w in _state.Widgets)
        {
            if (w.HitId == _state.SelectedId) { selected = w; break; }
            foreach (var child in w.Children)
            {
                if (child.HitId == _state.SelectedId) { selected = child; break; }
            }
            if (selected is not null) break;
        }

        if (selected is null) return;

        // Draw detail panel background
        var bg = PackedRgba.Rgb(15, 15, 30);
        var fg = PackedRgba.Rgb(200, 200, 220);
        for (ushort y = panelRect.Y; y < panelRect.Bottom && y < buffer.Height; y++)
            for (ushort x = panelRect.X; x < panelRect.Right && x < buffer.Width; x++)
                buffer.Set(x, y, new Cell(CellContent.Empty, fg, bg, CellAttributes.None));

        // Draw panel border
        var borderTemplate = new Cell(CellContent.Empty, PackedRgba.Rgb(100, 100, 180), bg, CellAttributes.None);
        buffer.DrawBorder(panelRect, BorderSet.Rounded, borderTemplate);

        // Draw widget info
        var lines = new System.Collections.Generic.List<string>
        {
            $"Name: {selected.Name}",
            $"Area: {selected.Area.X},{selected.Area.Y} {selected.Area.Width}x{selected.Area.Height}",
            $"Depth: {selected.Depth}",
            $"Render: {selected.RenderTimeUs}us",
            $"HitId: {selected.HitId}",
        };

        if (selected.HitRegions.Count > 0)
        {
            lines.Add($"Hit regions: {selected.HitRegions.Count}");
        }
        if (selected.Children.Count > 0)
        {
            lines.Add($"Children: {selected.Children.Count}");
        }

        var maxLines = Math.Min(lines.Count, panelRect.Height - 2);
        for (var i = 0; i < maxLines; i++)
        {
            var y = (ushort)(panelRect.Y + 1 + i);
            var maxChars = Math.Min(lines[i].Length, panelRect.Width - 2);
            for (var c = 0; c < maxChars; c++)
                buffer.Set((ushort)(panelRect.X + 1 + c), y,
                    new Cell(CellContent.FromChar(lines[i][c]), fg, bg, CellAttributes.None));
        }
    }
}
