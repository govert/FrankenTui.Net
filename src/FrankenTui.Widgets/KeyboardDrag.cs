// Port of .external/frankentui/crates/ftui-widgets/src/keyboard_drag.rs
// Keyboard-driven drag-and-drop support (bd-1csc.4).

using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

// ---------------------------------------------------------------------------
// KeyboardDragMode
// ---------------------------------------------------------------------------

/// <summary>Current mode of a keyboard drag operation.</summary>
public enum KeyboardDragMode
{
    /// <summary>No keyboard drag in progress.</summary>
    Inactive,
    /// <summary>Item picked up, awaiting target selection.</summary>
    Holding,
    /// <summary>Actively navigating between drop targets.</summary>
    Navigating,
}

/// <summary>Extension methods for <see cref="KeyboardDragMode"/>.</summary>
public static class KeyboardDragModeExtensions
{
    /// <summary>Returns true if a drag is in progress.</summary>
    public static bool IsActive(this KeyboardDragMode mode) =>
        mode != KeyboardDragMode.Inactive;

    /// <summary>Returns the stable string representation.</summary>
    public static string AsStr(this KeyboardDragMode mode) => mode switch
    {
        KeyboardDragMode.Inactive => "inactive",
        KeyboardDragMode.Holding => "holding",
        KeyboardDragMode.Navigating => "navigating",
        _ => "inactive",
    };
}

// ---------------------------------------------------------------------------
// Direction
// ---------------------------------------------------------------------------

/// <summary>Navigation direction for keyboard drag target selection.</summary>
public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

/// <summary>Extension methods for <see cref="Direction"/>.</summary>
public static class DirectionExtensions
{
    /// <summary>Returns the opposite direction.</summary>
    public static Direction Opposite(this Direction direction) => direction switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => direction,
    };

    /// <summary>Returns true for vertical directions.</summary>
    public static bool IsVertical(this Direction direction) =>
        direction == Direction.Up || direction == Direction.Down;
}

// ---------------------------------------------------------------------------
// DropTargetInfo
// ---------------------------------------------------------------------------

/// <summary>Information about a potential drop target for keyboard navigation.</summary>
public sealed class DropTargetInfo
{
    /// <summary>Unique identifier for the target widget.</summary>
    public WidgetId Id { get; }
    /// <summary>Human-readable name for accessibility.</summary>
    public string Name { get; }
    /// <summary>Bounding rectangle in screen coordinates.</summary>
    public Rect Bounds { get; }
    /// <summary>Accepted drag types (MIME-like patterns).</summary>
    public List<string> AcceptedTypes { get; private set; }
    /// <summary>Whether this target is currently enabled.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Create a new drop target info.</summary>
    public DropTargetInfo(WidgetId id, string name, Rect bounds)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Bounds = bounds;
        AcceptedTypes = new List<string>();
        Enabled = true;
    }

    /// <summary>Add accepted drag types.</summary>
    public DropTargetInfo WithAcceptedTypes(List<string> types)
    {
        AcceptedTypes = types;
        return this;
    }

    /// <summary>Set enabled state.</summary>
    public DropTargetInfo WithEnabled(bool enabled)
    {
        Enabled = enabled;
        return this;
    }

    /// <summary>Check if this target can accept the given payload type.</summary>
    public bool CanAccept(string dragType)
    {
        if (!Enabled)
            return false;
        if (AcceptedTypes.Count == 0)
            return true; // Accept any if no filter specified
        foreach (var pattern in AcceptedTypes)
        {
            if (pattern == "*" || pattern == "*/*")
                return true;
            if (pattern.EndsWith("/*"))
            {
                var prefix = pattern[..^2];
                if (dragType.StartsWith(prefix) && dragType.Length > prefix.Length && dragType[prefix.Length] == '/')
                    return true;
            }
            else if (pattern == dragType)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Returns the center point of this target's bounds.</summary>
    public (ushort X, ushort Y) Center() => (
        (ushort)(Bounds.X + Bounds.Width / 2),
        (ushort)(Bounds.Y + Bounds.Height / 2)
    );
}

// ---------------------------------------------------------------------------
// Announcement
// ---------------------------------------------------------------------------

/// <summary>Screen reader announcement for accessibility.</summary>
public sealed class Announcement
{
    /// <summary>The text to announce.</summary>
    public string Text { get; }
    /// <summary>Priority level (higher = more important).</summary>
    public AnnouncementPriority Priority { get; }

    public Announcement(string text, AnnouncementPriority priority)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Priority = priority;
    }

    /// <summary>Create a normal priority announcement.</summary>
    public static Announcement Normal(string text) =>
        new(text, AnnouncementPriority.Normal);

    /// <summary>Create a high priority announcement.</summary>
    public static Announcement High(string text) =>
        new(text, AnnouncementPriority.High);
}

/// <summary>Priority level for announcements.</summary>
public enum AnnouncementPriority
{
    /// <summary>Low priority, may be skipped if queue is full.</summary>
    Low,
    /// <summary>Normal priority.</summary>
    Normal,
    /// <summary>High priority, interrupts current announcement.</summary>
    High,
}

// ---------------------------------------------------------------------------
// KeyboardDragConfig
// ---------------------------------------------------------------------------

/// <summary>Configuration for keyboard drag behavior.</summary>
public sealed class KeyboardDragConfig
{
    /// <summary>Keys that activate drag (pick up or drop). Default: Space, Enter.</summary>
    public List<ActivateKey> ActivateKeys { get; set; } = new() { ActivateKey.Space, ActivateKey.Enter };

    /// <summary>Whether Escape cancels the drag.</summary>
    public bool CancelOnEscape { get; set; } = true;

    /// <summary>Style for highlighting the selected drop target.</summary>
    public TargetHighlightStyle TargetHighlightStyle { get; set; } = TargetHighlightStyle.Default();

    /// <summary>Style for highlighting invalid drop targets.</summary>
    public TargetHighlightStyle InvalidTargetStyle { get; set; } = TargetHighlightStyle.InvalidDefault();

    /// <summary>Whether to wrap around when navigating past the last/first target.</summary>
    public bool WrapNavigation { get; set; } = true;

    /// <summary>Maximum announcements to queue.</summary>
    public int MaxAnnouncementQueue { get; set; } = 5;
}

/// <summary>Keys that can activate drag operations.</summary>
public enum ActivateKey
{
    Space,
    Enter,
}

// ---------------------------------------------------------------------------
// TargetHighlightStyle
// ---------------------------------------------------------------------------

/// <summary>Visual style for highlighting drop targets during keyboard drag.</summary>
public sealed class TargetHighlightStyle
{
    /// <summary>Border style (character to use for highlighting).</summary>
    public char BorderChar { get; set; }
    /// <summary>Foreground color for the highlight border.</summary>
    public PackedRgba BorderFg { get; set; }
    /// <summary>Background color for the target area.</summary>
    public PackedRgba? Background { get; set; }
    /// <summary>Whether to render a pulsing animation.</summary>
    public bool AnimatePulse { get; set; }

    public TargetHighlightStyle(char borderChar, PackedRgba borderFg, PackedRgba? background, bool animatePulse)
    {
        BorderChar = borderChar;
        BorderFg = borderFg;
        Background = background;
        AnimatePulse = animatePulse;
    }

    /// <summary>Default highlight style (blue border, pulse).</summary>
    public static TargetHighlightStyle Default() => new(
        '█',
        PackedRgba.Rgb(100, 180, 255), // Blue highlight
        PackedRgba.Rgba(100, 180, 255, 40), // Subtle blue tint
        true
    );

    /// <summary>Style for invalid drop targets.</summary>
    public static TargetHighlightStyle InvalidDefault() => new(
        '▪',
        PackedRgba.Rgb(180, 100, 100), // Red highlight
        PackedRgba.Rgba(180, 100, 100, 20), // Subtle red tint
        false
    );

    /// <summary>Create a custom style.</summary>
    public static TargetHighlightStyle New(char borderChar, PackedRgba fg) =>
        new(borderChar, fg, null, false);

    /// <summary>Set background color.</summary>
    public TargetHighlightStyle WithBackground(PackedRgba bg)
    {
        Background = bg;
        return this;
    }

    /// <summary>Enable pulse animation.</summary>
    public TargetHighlightStyle WithPulse()
    {
        AnimatePulse = true;
        return this;
    }
}

// ---------------------------------------------------------------------------
// KeyboardDragState
// ---------------------------------------------------------------------------

/// <summary>State of an active keyboard drag operation.</summary>
public sealed class KeyboardDragState
{
    /// <summary>Widget that initiated the drag.</summary>
    public WidgetId SourceId { get; }
    /// <summary>Data being dragged.</summary>
    public DragPayload Payload { get; }
    /// <summary>Currently selected drop target index (into available targets list).</summary>
    public int? SelectedTargetIndex { get; set; }
    /// <summary>Current mode.</summary>
    public KeyboardDragMode Mode { get; set; }
    /// <summary>Animation tick for pulse effect.</summary>
    public byte AnimationTick { get; private set; }

    internal KeyboardDragState(WidgetId sourceId, DragPayload payload)
    {
        SourceId = sourceId;
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        SelectedTargetIndex = null;
        Mode = KeyboardDragMode.Holding;
        AnimationTick = 0;
    }

    /// <summary>Advance the animation tick.</summary>
    public void TickAnimation()
    {
        AnimationTick = unchecked((byte)(AnimationTick + 1));
    }

    /// <summary>Get the pulse intensity (0.0 to 1.0) for animation.</summary>
    public float PulseIntensity()
    {
        // Simple sine-based pulse: 0.5 + 0.5 * sin(tick * 0.15)
        var angle = AnimationTick * 0.15f;
        return 0.5f + 0.5f * MathF.Sin(angle);
    }
}

// ---------------------------------------------------------------------------
// KeyboardDragKey
// ---------------------------------------------------------------------------

/// <summary>Key events relevant to keyboard drag operations.</summary>
public abstract class KeyboardDragKey
{
    private KeyboardDragKey() { }

    /// <summary>Activation key (Space or Enter by default).</summary>
    public sealed class Activate : KeyboardDragKey { }

    /// <summary>Cancellation key (Escape by default).</summary>
    public sealed class Cancel : KeyboardDragKey { }

    /// <summary>Navigation key.</summary>
    public sealed class Navigate : KeyboardDragKey
    {
        public Direction Direction { get; }
        public Navigate(Direction direction) { Direction = direction; }
    }

    // Convenience singletons for the non-data variants
    public static readonly Activate ActivateKey = new();
    public static readonly Cancel CancelKey = new();
    public static KeyboardDragKey NavigateKey(Direction dir) => new Navigate(dir);
}

// ---------------------------------------------------------------------------
// KeyboardDragAction
// ---------------------------------------------------------------------------

/// <summary>Action resulting from key handling.</summary>
public abstract class KeyboardDragAction
{
    private KeyboardDragAction() { }

    /// <summary>No action needed.</summary>
    public sealed class None : KeyboardDragAction { }

    /// <summary>Pick up the focused item to start a drag.</summary>
    public sealed class PickUp : KeyboardDragAction { }

    /// <summary>Navigate to next target in direction.</summary>
    public sealed class Navigate : KeyboardDragAction
    {
        public Direction Direction { get; }
        public Navigate(Direction direction) { Direction = direction; }
    }

    /// <summary>Drop on the selected target.</summary>
    public sealed class Drop : KeyboardDragAction { }

    /// <summary>Cancel the drag operation.</summary>
    public sealed class Cancel : KeyboardDragAction { }

    // Convenience singletons for the non-data variants
    public static readonly None NoneAction = new();
    public static readonly PickUp PickUpAction = new();
    public static readonly Drop DropAction = new();
    public static readonly Cancel CancelAction = new();
    public static KeyboardDragAction NavigateAction(Direction dir) => new Navigate(dir);
}

// ---------------------------------------------------------------------------
// KeyboardDropResult
// ---------------------------------------------------------------------------

/// <summary>Result of a completed keyboard drag-and-drop operation.</summary>
public sealed class KeyboardDropResult
{
    /// <summary>The dropped payload.</summary>
    public DragPayload Payload { get; }
    /// <summary>Source widget ID.</summary>
    public WidgetId SourceId { get; }
    /// <summary>Target widget ID.</summary>
    public WidgetId TargetId { get; }
    /// <summary>Target index in the targets list.</summary>
    public int TargetIndex { get; }

    public KeyboardDropResult(DragPayload payload, WidgetId sourceId, WidgetId targetId, int targetIndex)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        SourceId = sourceId;
        TargetId = targetId;
        TargetIndex = targetIndex;
    }
}

// ---------------------------------------------------------------------------
// KeyboardDragManager
// ---------------------------------------------------------------------------

/// <summary>
/// Manager for keyboard-driven drag operations.
///
/// <para>Keyboard-driven drag-and-drop support (bd-1csc.4).</para>
/// <para>This module enables drag operations via keyboard for accessibility, complementing
/// the mouse-based drag protocol in <see cref="Drag"/>.</para>
///
/// <para>Invariants:</para>
/// <list type="number">
///   <item>A keyboard drag is either <c>Inactive</c>, <c>Holding</c>, or <c>Navigating</c>.</item>
///   <item><c>StartDrag</c> can only be called in <c>Inactive</c> mode.</item>
///   <item><c>NavigateTargets</c> can only be called in <c>Holding</c> or <c>Navigating</c> mode.</item>
///   <item><c>CompleteDrag</c> transitions to <c>Inactive</c> regardless of success/failure.</item>
///   <item><c>CancelDrag</c> always transitions to <c>Inactive</c>.</item>
/// </list>
/// </summary>
public sealed class KeyboardDragManager
{
    /// <summary>Configuration.</summary>
    private readonly KeyboardDragConfig _config;
    /// <summary>Current drag state (if any).</summary>
    private KeyboardDragState? _state;
    /// <summary>Announcement queue.</summary>
    private readonly List<Announcement> _announcements;

    /// <summary>Create a new keyboard drag manager.</summary>
    public KeyboardDragManager(KeyboardDragConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _state = null;
        _announcements = new List<Announcement>();
    }

    /// <summary>Create with default configuration.</summary>
    public static KeyboardDragManager WithDefaults() =>
        new(new KeyboardDragConfig());

    /// <summary>Get the current drag mode.</summary>
    public KeyboardDragMode Mode() =>
        _state?.Mode ?? KeyboardDragMode.Inactive;

    /// <summary>Check if a drag is active.</summary>
    public bool IsActive() => _state is not null;

    /// <summary>Get the current drag state.</summary>
    public KeyboardDragState? State() => _state;

    /// <summary>Get mutable access to the drag state.</summary>
    public KeyboardDragState? StateMut() => _state;

    /// <summary>
    /// Start a keyboard drag operation.
    /// Returns <c>true</c> if the drag was started successfully.
    /// Returns <c>false</c> if a drag is already in progress.
    /// </summary>
    public bool StartDrag(WidgetId sourceId, DragPayload payload)
    {
        if (_state is not null)
            return false;

        var description = payload.DisplayText
            ?? payload.AsText()
            ?? "item";

        QueueAnnouncement(Announcement.High($"Picked up: {description}"));

        _state = new KeyboardDragState(sourceId, payload);
        return true;
    }

    /// <summary>
    /// Navigate to the next drop target in the given direction.
    /// Returns the newly selected target info if navigation succeeded.
    /// </summary>
    public DropTargetInfo? NavigateTargets(Direction direction, IReadOnlyList<DropTargetInfo> targets)
    {
        var state = _state;
        if (state is null)
            return null;

        if (targets.Count == 0)
        {
            state.SelectedTargetIndex = null;
            state.Mode = KeyboardDragMode.Holding;
            return null;
        }

        // Filter to valid targets that can accept the payload
        var validIndices = new List<int>();
        for (var i = 0; i < targets.Count; i++)
        {
            if (targets[i].CanAccept(state.Payload.DragType))
                validIndices.Add(i);
        }

        if (validIndices.Count == 0)
        {
            state.SelectedTargetIndex = null;
            state.Mode = KeyboardDragMode.Holding;
            QueueAnnouncement(Announcement.Normal("No valid drop targets available"));
            return null;
        }

        // Update mode to navigating
        state.Mode = KeyboardDragMode.Navigating;

        // Find current position among valid targets
        int? currentValidIdx = null;
        if (state.SelectedTargetIndex.HasValue)
        {
            var sel = state.SelectedTargetIndex.Value;
            for (var i = 0; i < validIndices.Count; i++)
            {
                if (validIndices[i] == sel)
                {
                    currentValidIdx = i;
                    break;
                }
            }
        }

        // Calculate next index based on direction and current selection
        int nextValidIdx;
        if (!currentValidIdx.HasValue)
        {
            nextValidIdx = 0; // No selection, start at first
        }
        else if (direction == Direction.Down || direction == Direction.Right)
        {
            var idx = currentValidIdx.Value;
            if (idx + 1 < validIndices.Count)
                nextValidIdx = idx + 1;
            else if (_config.WrapNavigation)
                nextValidIdx = 0;
            else
                nextValidIdx = idx;
        }
        else // Up or Left
        {
            var idx = currentValidIdx.Value;
            if (idx > 0)
                nextValidIdx = idx - 1;
            else if (_config.WrapNavigation)
                nextValidIdx = validIndices.Count - 1;
            else
                nextValidIdx = idx;
        }

        var targetIdx = validIndices[nextValidIdx];
        state.SelectedTargetIndex = targetIdx;

        var target = targets[targetIdx];
        var position = $"{nextValidIdx + 1} of {validIndices.Count}";
        QueueAnnouncement(Announcement.Normal($"Drop target: {target.Name} ({position})"));

        return target;
    }

    /// <summary>Navigate to a specific target by index.</summary>
    public bool SelectTarget(int targetIndex, IReadOnlyList<DropTargetInfo> targets)
    {
        var state = _state;
        if (state is null)
            return false;

        if (targetIndex >= targets.Count)
        {
            state.SelectedTargetIndex = null;
            state.Mode = KeyboardDragMode.Holding;
            return false;
        }

        var target = targets[targetIndex];
        if (!target.CanAccept(state.Payload.DragType))
        {
            state.SelectedTargetIndex = null;
            state.Mode = KeyboardDragMode.Holding;
            return false;
        }

        state.Mode = KeyboardDragMode.Navigating;
        state.SelectedTargetIndex = targetIndex;

        QueueAnnouncement(Announcement.Normal($"Drop target: {target.Name}"));
        return true;
    }

    /// <summary>
    /// Complete the drag operation (drop on selected target).
    /// Returns <c>null</c> if no target is selected or no drag is active.
    /// Returns <c>(payload, targetIndex)</c> with the payload and target index.
    /// </summary>
    public (DragPayload Payload, int TargetIndex)? CompleteDrag()
    {
        var state = _state;
        if (state is null)
            return null;
        if (!state.SelectedTargetIndex.HasValue)
            return null;

        _state = null;
        return (state.Payload, state.SelectedTargetIndex.Value);
    }

    /// <summary>Complete the drag with a specific target and get the drop result info.</summary>
    public KeyboardDropResult? DropOnTarget(IReadOnlyList<DropTargetInfo> targets)
    {
        if (_state is null)
            return null;
        if (!_state.SelectedTargetIndex.HasValue)
            return null;

        var targetIdx = _state.SelectedTargetIndex.Value;
        var dragType = _state.Payload.DragType;

        if (targetIdx >= targets.Count)
        {
            ClearSelectedTarget();
            QueueAnnouncement(Announcement.Normal("Selected drop target unavailable"));
            return null;
        }

        var target = targets[targetIdx];
        if (!target.CanAccept(dragType))
        {
            ClearSelectedTarget();
            QueueAnnouncement(Announcement.Normal(
                "Selected drop target no longer accepts this item"));
            return null;
        }

        var targetId = target.Id;
        var targetName = target.Name;
        var state = _state!;
        _state = null;

        QueueAnnouncement(Announcement.High($"Dropped on: {targetName}"));

        return new KeyboardDropResult(state.Payload, state.SourceId, targetId, targetIdx);
    }

    /// <summary>
    /// Cancel the current drag operation.
    /// Returns the payload if a drag was active.
    /// </summary>
    public DragPayload? CancelDrag()
    {
        var state = _state;
        if (state is null)
            return null;

        _state = null;
        QueueAnnouncement(Announcement.Normal("Drop cancelled"));
        return state.Payload;
    }

    /// <summary>
    /// Handle key press during keyboard drag.
    /// Returns <see cref="KeyboardDragAction"/> indicating what action was triggered.
    /// </summary>
    public KeyboardDragAction HandleKey(KeyboardDragKey key)
    {
        switch (key)
        {
            case KeyboardDragKey.Activate:
                if (IsActive())
                {
                    // If navigating with a selected target, complete the drop
                    if (_state?.SelectedTargetIndex.HasValue == true)
                        return KeyboardDragAction.DropAction;
                    else
                        // No target selected yet, stay in current state
                        return KeyboardDragAction.NoneAction;
                }
                else
                {
                    // No drag active, signal to pick up
                    return KeyboardDragAction.PickUpAction;
                }

            case KeyboardDragKey.Cancel:
                if (IsActive() && _config.CancelOnEscape)
                    return KeyboardDragAction.CancelAction;
                else
                    return KeyboardDragAction.NoneAction;

            case KeyboardDragKey.Navigate nav:
                if (IsActive())
                    return KeyboardDragAction.NavigateAction(nav.Direction);
                else
                    return KeyboardDragAction.NoneAction;

            default:
                return KeyboardDragAction.NoneAction;
        }
    }

    /// <summary>Advance animation state.</summary>
    public void Tick()
    {
        _state?.TickAnimation();
    }

    /// <summary>Get and clear pending announcements.</summary>
    public List<Announcement> DrainAnnouncements()
    {
        var result = new List<Announcement>(_announcements);
        _announcements.Clear();
        return result;
    }

    /// <summary>Peek at pending announcements without clearing.</summary>
    public IReadOnlyList<Announcement> Announcements() => _announcements;

    /// <summary>Queue an announcement for screen readers.</summary>
    private void QueueAnnouncement(Announcement announcement)
    {
        if (_config.MaxAnnouncementQueue == 0)
            return;

        if (_announcements.Count >= _config.MaxAnnouncementQueue)
        {
            // Remove lowest priority announcement
            var lowestPos = -1;
            var lowestPriority = AnnouncementPriority.High;
            for (var i = 0; i < _announcements.Count; i++)
            {
                if (_announcements[i].Priority <= lowestPriority)
                {
                    lowestPriority = _announcements[i].Priority;
                    lowestPos = i;
                }
            }
            if (lowestPos >= 0)
            {
                if (announcement.Priority < lowestPriority)
                    return;
                _announcements.RemoveAt(lowestPos);
            }
        }
        _announcements.Add(announcement);
    }

    private void ClearSelectedTarget()
    {
        if (_state is not null)
        {
            _state.SelectedTargetIndex = null;
            _state.Mode = KeyboardDragMode.Holding;
        }
    }

    /// <summary>Render the target highlight overlay.</summary>
    public void RenderHighlight(IReadOnlyList<DropTargetInfo> targets, Frame frame)
    {
        var state = _state;
        if (state is null)
            return;
        if (!state.SelectedTargetIndex.HasValue)
            return;
        var targetIdx = state.SelectedTargetIndex.Value;
        if (targetIdx >= targets.Count)
            return;
        var target = targets[targetIdx];

        var style = target.CanAccept(state.Payload.DragType)
            ? _config.TargetHighlightStyle
            : _config.InvalidTargetStyle;

        var bounds = target.Bounds;
        if (bounds.IsEmpty)
            return;

        // Apply background tint if configured
        if (style.Background.HasValue)
        {
            var bg = style.Background.Value;
            // Calculate effective alpha based on pulse
            byte alpha;
            if (style.AnimatePulse)
            {
                var baseAlpha = bg.A / 255.0f;
                var pulsed = baseAlpha * (0.5f + 0.5f * state.PulseIntensity());
                alpha = (byte)(pulsed * 255.0f);
            }
            else
            {
                alpha = bg.A;
            }

            var effectiveBg = PackedRgba.Rgba(bg.R, bg.G, bg.B, alpha);

            // Fill background
            for (ushort y = bounds.Y; y < bounds.Bottom; y++)
            {
                for (ushort x = bounds.X; x < bounds.Right; x++)
                {
                    var existing = frame.Buffer.Get(x, y);
                    if (existing.HasValue)
                    {
                        var cell = existing.Value;
                        frame.Buffer.Set(x, y, new Cell(cell.Content, cell.Foreground, effectiveBg, cell.Attributes));
                    }
                }
            }
        }

        // Draw highlight border
        var borderFg = style.BorderFg;
        var borderChar = style.BorderChar;

        // Top and bottom borders
        for (ushort x = bounds.X; x < bounds.Right; x++)
        {
            // Top
            var topCell = Cell.FromChar(borderChar);
            frame.Buffer.SetFast(x, bounds.Y, new Cell(topCell.Content, borderFg, topCell.Background, topCell.Attributes));

            // Bottom
            var bottomY = (ushort)(bounds.Y + (bounds.Height > 0 ? bounds.Height - 1 : 0));
            if (bounds.Height > 1)
            {
                var botCell = Cell.FromChar(borderChar);
                frame.Buffer.SetFast(x, bottomY, new Cell(botCell.Content, borderFg, botCell.Background, botCell.Attributes));
            }
        }

        // Left and right borders (excluding corners)
        if (bounds.Height > 2)
        {
            var yStart = (ushort)(bounds.Y + 1);
            var yEnd = (ushort)(bounds.Y + bounds.Height - 1);
            for (ushort y = yStart; y < yEnd; y++)
            {
                // Left
                var leftCell = Cell.FromChar(borderChar);
                frame.Buffer.SetFast(bounds.X, y, new Cell(leftCell.Content, borderFg, leftCell.Background, leftCell.Attributes));

                // Right
                var rightX = (ushort)(bounds.X + (bounds.Width > 0 ? bounds.Width - 1 : 0));
                if (bounds.Width > 1)
                {
                    var rightCell = Cell.FromChar(borderChar);
                    frame.Buffer.SetFast(rightX, y, new Cell(rightCell.Content, borderFg, rightCell.Background, rightCell.Attributes));
                }
            }
        }
    }
}
