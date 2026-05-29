// Upstream source: crates/ftui-widgets/src/drag.rs
// Upstream basis: f958e59e1406a90fdb92512103e3591911a9d68c
// Direct 1-1 port of DragPayload, DragConfig, DragState, Draggable,
// DropPosition, DropResult, DropTarget, DragPreviewConfig, DragPreview.

using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

// =============================================================================
// DragPayload
// =============================================================================

/// <summary>Data carried during a drag operation.</summary>
public sealed class DragPayload
{
    /// <summary>MIME-like type identifier (e.g., "text/plain", "widget/list-item").</summary>
    public string DragType { get; }
    /// <summary>Raw serialized data.</summary>
    public byte[] Data { get; }
    /// <summary>Human-readable preview text shown during drag (optional).</summary>
    public string? DisplayText { get; set; }

    /// <summary>Create a payload with raw bytes.</summary>
    public DragPayload(string dragType, byte[] data)
    {
        DragType = dragType ?? throw new ArgumentNullException(nameof(dragType));
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>Create a plain-text payload with type "text/plain".</summary>
    public static DragPayload Text(string text)
    {
        var data = System.Text.Encoding.UTF8.GetBytes(text);
        return new DragPayload("text/plain", data) { DisplayText = text };
    }

    /// <summary>Create a payload with custom display text.</summary>
    public DragPayload WithDisplayText(string text)
    {
        DisplayText = text;
        return this;
    }

    /// <summary>Attempt to decode the data as a UTF-8 string.</summary>
    public string? AsText()
    {
        try { return System.Text.Encoding.UTF8.GetString(Data); }
        catch { return null; }
    }

    /// <summary>Returns the byte length of the payload data.</summary>
    public int DataLen => Data.Length;

    /// <summary>
    /// Returns true if the payload type matches the given pattern.
    /// Supports exact match and wildcard prefix (e.g., "text/*").
    /// </summary>
    public bool MatchesType(string pattern)
    {
        if (pattern == "*" || pattern == "*/*") return true;
        if (pattern.EndsWith("/*"))
        {
            var prefix = pattern[..^2];
            return DragType.StartsWith(prefix) && DragType.Length > prefix.Length && DragType[prefix.Length] == '/';
        }
        return DragType == pattern;
    }
}

// =============================================================================
// DragConfig
// =============================================================================

/// <summary>Configuration for drag gesture detection.</summary>
public sealed class DragConfig
{
    /// <summary>Minimum movement in cells before a drag starts (default: 3).</summary>
    public ushort ThresholdCells { get; set; } = 3;
    /// <summary>Delay in milliseconds before drag starts (default: 0).</summary>
    public ulong StartDelayMs { get; set; } = 0;
    /// <summary>Whether pressing Escape cancels an active drag (default: true).</summary>
    public bool CancelOnEscape { get; set; } = true;

    public DragConfig WithThreshold(ushort cells)
    {
        ThresholdCells = cells;
        return this;
    }

    public DragConfig WithDelay(ulong ms)
    {
        StartDelayMs = ms;
        return this;
    }

    public DragConfig NoEscapeCancel()
    {
        CancelOnEscape = false;
        return this;
    }
}

// =============================================================================
// DragState
// =============================================================================

/// <summary>Active drag operation state.</summary>
public sealed class DragState
{
    /// <summary>Widget that initiated the drag.</summary>
    public WidgetId SourceId { get; }
    /// <summary>Data being dragged.</summary>
    public DragPayload Payload { get; }
    /// <summary>Position where the drag started.</summary>
    public (ushort X, ushort Y) StartPos { get; private set; }
    /// <summary>Current drag position.</summary>
    public (ushort X, ushort Y) CurrentPos { get; private set; }
    /// <summary>Optional custom preview widget.</summary>
    public IWidget? Preview { get; set; }

    public DragState(WidgetId sourceId, DragPayload payload, ushort startX, ushort startY)
    {
        SourceId = sourceId;
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        StartPos = (startX, startY);
        CurrentPos = (startX, startY);
    }

    /// <summary>Set a custom preview widget.</summary>
    public DragState WithPreview(IWidget preview)
    {
        Preview = preview;
        return this;
    }

    /// <summary>Update the current position during a drag move.</summary>
    public void UpdatePosition(ushort x, ushort y)
    {
        CurrentPos = (x, y);
    }

    /// <summary>Manhattan distance from start to current position.</summary>
    public uint Distance() =>
        (uint)(Math.Abs(CurrentPos.X - StartPos.X) + Math.Abs(CurrentPos.Y - StartPos.Y));

    /// <summary>Delta from start to current position as (dx, dy).</summary>
    public (int Dx, int Dy) Delta() => (
        CurrentPos.X - StartPos.X,
        CurrentPos.Y - StartPos.Y
    );
}

// =============================================================================
// Draggable trait
// =============================================================================

/// <summary>Interface for widgets that can be drag sources.
/// Upstream: crates/ftui-widgets/src/drag.rs — Draggable trait.
/// Methods: drag_type(), drag_data(), on_drag_start(), on_drag_end().</summary>
public interface IDraggable
{
    /// <summary>Returns the type string for the drag payload (e.g., "text/plain").</summary>
    string DragType { get; }

    /// <summary>Returns the payload data to be dragged.</summary>
    DragPayload DragData { get; }

    /// <summary>Called when a drag starts. Default is no-op.</summary>
    void OnDragStart() { }

    /// <summary>Called when a drag ends (success or cancellation). Default is no-op.</summary>
    void OnDragEnd(bool success) { }
}

// =============================================================================
// DropPosition
// =============================================================================

/// <summary>Position within a drop target.</summary>
public enum DropPosition
{
    /// <summary>Drop before the target.</summary>
    Before,
    /// <summary>Drop after the target.</summary>
    After,
    /// <summary>Drop on (inside/replace) the target.</summary>
    On,
}

// =============================================================================
// DropResult
// =============================================================================

/// <summary>Result of a drop operation.</summary>
public sealed class DropResult
{
    private readonly ResultKind _kind;
    private readonly string? _reason;

    private DropResult(ResultKind kind, string? reason = null)
    {
        _kind = kind;
        _reason = reason;
    }

    /// <summary>Drop was accepted.</summary>
    public static DropResult Accepted() => new(ResultKind.Accepted);
    /// <summary>Drop was rejected with an optional reason.</summary>
    public static DropResult Rejected(string? reason = null) => new(ResultKind.Rejected, reason);
    /// <summary>Drop was cancelled.</summary>
    public static DropResult Cancelled() => new(ResultKind.Cancelled);

    public bool IsAccepted => _kind == ResultKind.Accepted;

    private enum ResultKind { Accepted, Rejected, Cancelled }
}

// =============================================================================
// DropTarget trait
// =============================================================================

/// <summary>Interface for widgets that can accept drops.</summary>
public interface IDropTarget
{
    /// <summary>Returns the type string this target accepts.</summary>
    string AcceptsType { get; }

    /// <summary>Check if a drop at the given position would be accepted.</summary>
    bool CanDrop(DragPayload payload, ushort x, ushort y);

    /// <summary>Called when a payload is dropped — returns the DropResult.</summary>
    DropResult OnDrop(DragPayload payload, ushort x, ushort y, DropPosition position);

    /// <summary>Get the drop position for a given cursor position.</summary>
    DropPosition GetDropPosition(ushort x, ushort y);
}

// =============================================================================
// DropPosition helper
// =============================================================================

public static class DropPositionHelper
{
    /// <summary>Determine drop position from y coordinate within a list.</summary>
    public static DropPosition FromList(ushort y, ushort itemHeight, int itemCount)
    {
        var itemIndex = y / itemHeight;
        var offsetInItem = y % itemHeight;
        if (itemIndex >= itemCount) return DropPosition.After;
        if (offsetInItem < itemHeight / 2) return DropPosition.Before;
        return DropPosition.After;
    }

    /// <summary>Returns the logical index for this drop position.</summary>
    public static int? Index(this DropPosition position, int? currentIndex) => position switch
    {
        DropPosition.Before => currentIndex,
        DropPosition.After => currentIndex.HasValue ? currentIndex.Value + 1 : null,
        DropPosition.On => currentIndex,
        _ => null,
    };

    /// <summary>Returns true if this position represents an insertion (Before or After).</summary>
    public static bool IsInsertion(this DropPosition position) =>
        position == DropPosition.Before || position == DropPosition.After;

}

// =============================================================================
// DragPreviewConfig
// =============================================================================

/// <summary>Configuration for the drag preview rendering.</summary>
public sealed class DragPreviewConfig
{
    /// <summary>Opacity of the preview (0.0 = transparent, 1.0 = opaque).</summary>
    public float Opacity { get; set; } = 0.8f;
    /// <summary>Offset from cursor position.</summary>
    public (short X, short Y) Offset { get; set; } = (2, 0);
    /// <summary>Size of the preview (null = auto-size from widget).</summary>
    public (ushort Width, ushort Height)? Size { get; set; }
    /// <summary>Background color of the preview.</summary>
    public PackedRgba? Background { get; set; }
    /// <summary>Whether to draw a border around the preview.</summary>
    public bool ShowBorder { get; set; } = true;

    public DragPreviewConfig WithOpacity(float opacity)
    {
        Opacity = opacity;
        return this;
    }

    public DragPreviewConfig WithOffset(short x, short y)
    {
        Offset = (x, y);
        return this;
    }

    public DragPreviewConfig WithSize(ushort width, ushort height)
    {
        Size = (width, height);
        return this;
    }

    public DragPreviewConfig WithBackground(PackedRgba color)
    {
        Background = color;
        return this;
    }

    public DragPreviewConfig WithBorder()
    {
        ShowBorder = true;
        return this;
    }

    /// <summary>Calculate the preview rectangle from cursor position and viewport.</summary>
    public Rect? PreviewRect(ushort cursorX, ushort cursorY, Rect viewport)
    {
        var (offX, offY) = Offset;
        var (w, h) = Size ?? ((ushort)10, (ushort)3); // default preview size

        var x = (ushort)Math.Clamp(cursorX + offX, viewport.X, (ushort)(viewport.Right - w));
        var y = (ushort)Math.Clamp(cursorY + offY, viewport.Y, (ushort)(viewport.Bottom - h));

        return new Rect(x, y, w, h);
    }
}

// =============================================================================
// DragPreview
// =============================================================================

/// <summary>Renders a drag preview overlay.</summary>
public sealed class DragPreview
{
    private readonly DragState _dragState;
    private readonly DragPreviewConfig _config;

    public DragPreview(DragState dragState)
        : this(dragState, new DragPreviewConfig()) { }

    public DragPreview(DragState dragState, DragPreviewConfig config)
    {
        _dragState = dragState ?? throw new ArgumentNullException(nameof(dragState));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>Render the drag preview into the given buffer.</summary>
    public void Render(FrankenTui.Render.Buffer buffer, Rect viewport)
    {
        var previewRect = _config.PreviewRect(
            _dragState.CurrentPos.X, _dragState.CurrentPos.Y, viewport);
        if (previewRect is null) return;

        var r = previewRect.Value;
        if (r.IsEmpty) return;

        // Default preview: draw a bordered box with the payload display text
        var bg = _config.Background ?? PackedRgba.Rgb(60, 60, 80);
        var borderColor = PackedRgba.Rgb(180, 180, 220);

        var defaultCell = new Cell(CellContent.Empty, borderColor, bg, CellAttributes.None);
        var borderSet = BorderSet.Rounded;

        // Fill preview area
        for (ushort cy = r.Y; cy < r.Bottom && cy < buffer.Height; cy++)
            for (ushort cx = r.X; cx < r.Right && cx < buffer.Width; cx++)
                buffer.Set(cx, cy, new Cell(CellContent.Empty, PackedRgba.Rgb(200, 200, 200), bg, CellAttributes.None));

        // Draw border
        buffer.DrawBorder(r, borderSet, defaultCell);

        // Draw display text if available
        var text = _dragState.Payload.DisplayText ?? _dragState.Payload.AsText() ?? "(drag)";
        var textMaxLen = (ushort)Math.Min(text.Length, (ushort)(r.Width - 2));
        if (textMaxLen > 0)
        {
            var textCell = new Cell(CellContent.Empty, PackedRgba.Rgb(220, 220, 255), bg, CellAttributes.None);
            for (var i = 0; i < textMaxLen; i++)
                buffer.Set((ushort)(r.X + 1 + i), (ushort)(r.Y + 1), textCell.WithChar(text[i]));
        }
    }
}
