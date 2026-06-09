// SPDX-License-Identifier: Apache-2.0
// Port of .external/frankentui/crates/ftui-widgets/src/notification_queue.rs
// Notification queue manager for handling multiple concurrent toast notifications.

using System.Collections.Generic;
using FrankenTui.Core;
using FrankenTui.Render;

namespace FrankenTui.Widgets;

/// <summary>Notification queue manager for handling multiple concurrent toast notifications.
///
/// The queue system provides:
/// - FIFO ordering with priority support (Urgent notifications jump ahead)
/// - Maximum visible limit with automatic stacking
/// - Content-based deduplication within a configurable time window
/// - Automatic expiry processing via tick-based updates
///
/// Example:
/// <code>
///   var queue = new NotificationQueue(QueueConfig.CreateDefault());
///
///   // Push notifications
///   queue.Push(Toast.New("File saved").Icon(ToastIcon.Success), NotificationPriority.Normal);
///   queue.Push(Toast.New("Error!").Icon(ToastIcon.Error), NotificationPriority.Urgent);
///
///   // Process in your event loop
///   var actions = queue.Tick(TimeSpan.FromMilliseconds(16));
///   foreach (var action in actions)
///   {
///       if (action is QueueAction.Show s) { /* render toast */ }
///       if (action is QueueAction.Hide h) { /* remove toast */ }
///   }
/// </code>
/// </summary>

// ── NotificationPriority ───────────────────────────────────────────────────────

/// <summary>Priority level for notifications.
///
/// Higher priority notifications are displayed sooner.
/// Urgent notifications jump to the front of the queue.
///
/// <para><b>DIVERGENCE (default value):</b> Rust marks <c>Normal</c> with
/// <c>#[default]</c>, making <c>NotificationPriority::default()</c> return
/// <c>Normal</c>.  In C# <c>default(NotificationPriority)</c> returns
/// <c>Low</c> (ordinal 0) because the language mandates the zero value as the
/// default for every enum.  The project-intended default is captured by
/// <see cref="NotificationPriority.ProjectDefault"/> and is asserted in
/// <c>NotificationPriorityDefaultIsNormal</c>.</para></summary>
public enum NotificationPriority
{
    /// <summary>Low priority, displayed last.</summary>
    Low    = 0,
    /// <summary>Normal priority.
    /// <para>This is the intended default priority for new notifications
    /// (equivalent to Rust's <c>#[default]</c> on this variant).
    /// In C#, <c>default(NotificationPriority)</c> returns <see cref="Low"/>
    /// — use <see cref="ProjectDefault"/> when a default value is required.</para></summary>
    Normal = 1,
    /// <summary>High priority, displayed before Normal/Low.</summary>
    High   = 2,
    /// <summary>Urgent priority, jumps to front immediately.</summary>
    Urgent = 3,
}

/// <summary>Extension helpers for <see cref="NotificationPriority"/>.</summary>
public static class NotificationPriorityDefaults
{
    /// <summary>The project-intended default priority, equivalent to Rust's
    /// <c>NotificationPriority::default()</c> which returns
    /// <c>NotificationPriority::Normal</c> via <c>#[default]</c>.
    ///
    /// <para>DIVERGENCE: C# <c>default(NotificationPriority)</c> is <c>Low</c>
    /// (ordinal 0).  This constant preserves the upstream semantic.</para></summary>
    public const NotificationPriority ProjectDefault = NotificationPriority.Normal;
}

// ── QueueConfig ────────────────────────────────────────────────────────────────

/// <summary>Configuration for the notification queue.</summary>
public sealed class QueueConfig
{
    /// <summary>Maximum number of toasts visible at once.</summary>
    public int MaxVisible { get; set; }
    /// <summary>Maximum number of notifications waiting in queue.</summary>
    public int MaxQueued { get; set; }
    /// <summary>Default auto-dismiss duration.</summary>
    public TimeSpan DefaultDuration { get; set; }
    /// <summary>Anchor position for the toast stack.</summary>
    public ToastPosition Position { get; set; }
    /// <summary>Vertical spacing between stacked toasts.</summary>
    public ushort StaggerOffset { get; set; }
    /// <summary>Time window for deduplication (in ms).</summary>
    public ulong DedupWindowMs { get; set; }

    private QueueConfig() { }

    /// <summary>Create a new configuration with default values.</summary>
    public static QueueConfig New() => CreateDefault();

    /// <summary>Return a QueueConfig populated with the same defaults as Rust's Default impl.</summary>
    public static QueueConfig CreateDefault() => new()
    {
        MaxVisible     = 3,
        MaxQueued      = 10,
        DefaultDuration = TimeSpan.FromSeconds(5),
        Position       = ToastPosition.TopRight,
        StaggerOffset  = 1,
        DedupWindowMs  = 1000,
    };

    /// <summary>Set maximum visible toasts.</summary>
    public QueueConfig WithMaxVisible(int max) { MaxVisible = max; return this; }

    /// <summary>Set maximum queued notifications.</summary>
    public QueueConfig WithMaxQueued(int max) { MaxQueued = max; return this; }

    /// <summary>Set default duration for auto-dismiss.</summary>
    public QueueConfig WithDefaultDuration(TimeSpan duration) { DefaultDuration = duration; return this; }

    /// <summary>Set anchor position for the toast stack.</summary>
    public QueueConfig WithPosition(ToastPosition position) { Position = position; return this; }

    /// <summary>Set vertical spacing between stacked toasts.</summary>
    public QueueConfig WithStaggerOffset(ushort offset) { StaggerOffset = offset; return this; }

    /// <summary>Set deduplication time window in milliseconds.</summary>
    public QueueConfig WithDedupWindowMs(ulong ms) { DedupWindowMs = ms; return this; }
}

// ── QueuedNotification ─────────────────────────────────────────────────────────

/// <summary>Internal representation of a queued notification.</summary>
internal sealed class QueuedNotification
{
    public Toast Toast { get; }
    public NotificationPriority Priority { get; }
    /// <summary>When the notification was queued (for potential time-based priority decay).</summary>
    // DIVERGENCE: Rust uses web_time::Instant (Wasm-safe). C# uses DateTime.UtcNow.
    public DateTime CreatedAt { get; }
    public ulong ContentHash { get; }

    public QueuedNotification(Toast toast, NotificationPriority priority)
    {
        Toast       = toast;
        Priority    = priority;
        CreatedAt   = DateTime.UtcNow;
        ContentHash = ComputeHash(toast);
    }

    public static ulong ComputeHash(Toast toast)
    {
        // DIVERGENCE: Rust uses DefaultHasher (std::collections::hash_map::DefaultHasher).
        // C# uses HashCode which uses a randomised seed per-process. For deduplication
        // purposes this is equivalent — we only compare hashes within the same process run.
        var hc = new HashCode();
        hc.Add(toast.Content.Message);
        if (toast.Content.Title is { } title)
            hc.Add(title);
        // HashCode.ToHashCode() returns int; cast to ulong to match the u64 field type.
        return (ulong)(uint)hc.ToHashCode();
    }
}

// ── QueueAction ────────────────────────────────────────────────────────────────

/// <summary>Actions returned by Tick() to be processed by the application.
///
/// Enum variants with data become a closed class hierarchy per porting conventions.</summary>
public abstract class QueueAction : IEquatable<QueueAction>
{
    // Closed hierarchy — no external subclasses.
    private QueueAction() { }

    /// <summary>Show a new toast at the given position.</summary>
    public sealed class Show : QueueAction
    {
        public ToastId Id { get; }
        internal Show(ToastId id) => Id = id;
        public override bool Equals(QueueAction? other) => other is Show s && s.Id == Id;
        public override int GetHashCode() => HashCode.Combine(0, Id);
        public override string ToString() => $"Show({Id})";
    }

    /// <summary>Hide an existing toast.</summary>
    public sealed class Hide : QueueAction
    {
        public ToastId Id { get; }
        internal Hide(ToastId id) => Id = id;
        public override bool Equals(QueueAction? other) => other is Hide h && h.Id == Id;
        public override int GetHashCode() => HashCode.Combine(1, Id);
        public override string ToString() => $"Hide({Id})";
    }

    /// <summary>Reposition a toast (for stacking adjustments).</summary>
    public sealed class Reposition : QueueAction
    {
        public ToastId Id { get; }
        internal Reposition(ToastId id) => Id = id;
        public override bool Equals(QueueAction? other) => other is Reposition r && r.Id == Id;
        public override int GetHashCode() => HashCode.Combine(2, Id);
        public override string ToString() => $"Reposition({Id})";
    }

    public abstract bool Equals(QueueAction? other);
    public override bool Equals(object? obj) => obj is QueueAction q && Equals(q);
    public override abstract int GetHashCode();
    public static bool operator ==(QueueAction? a, QueueAction? b)
        => a is null ? b is null : a.Equals(b);
    public static bool operator !=(QueueAction? a, QueueAction? b) => !(a == b);
}

// ── QueueStats ─────────────────────────────────────────────────────────────────

/// <summary>Queue statistics for monitoring and debugging.</summary>
public sealed class QueueStats
{
    /// <summary>Total notifications pushed.</summary>
    public ulong TotalPushed { get; set; }
    /// <summary>Notifications rejected due to queue overflow.</summary>
    public ulong OverflowCount { get; set; }
    /// <summary>Notifications rejected due to deduplication.</summary>
    public ulong DedupCount { get; set; }
    /// <summary>Notifications dismissed by user.</summary>
    public ulong UserDismissed { get; set; }
    /// <summary>Notifications expired automatically.</summary>
    public ulong AutoExpired { get; set; }
}

// ── NotificationQueue ──────────────────────────────────────────────────────────

/// <summary>Notification queue manager.
///
/// Manages multiple toast notifications with priority ordering, deduplication,
/// and automatic expiry. Use Push to add notifications and Tick to process
/// expiry in your event loop.</summary>
public sealed class NotificationQueue
{
    // DIVERGENCE: Rust uses VecDeque which supports O(1) front/back insertion and O(1) arbitrary
    // removal. C# uses LinkedList<> which supports O(1) arbitrary-position insert/remove while
    // still supporting AddFirst/RemoveFirst (pop_front) semantics.
    private readonly LinkedList<QueuedNotification> _pendingQueue;
    /// <summary>Currently visible toasts.</summary>
    private readonly List<Toast> _visible;
    /// <summary>Configuration.</summary>
    private readonly QueueConfig _config;
    /// <summary>Deduplication window.</summary>
    private readonly TimeSpan _dedupWindow;
    /// <summary>Recent content hashes for deduplication: hash → time inserted.</summary>
    // DIVERGENCE: Rust uses AHashMap (faster non-cryptographic hasher). C# uses Dictionary
    // which uses a randomised hash internally. Semantics are identical.
    private readonly Dictionary<ulong, DateTime> _recentHashes;
    /// <summary>Statistics.</summary>
    private readonly QueueStats _stats;

    /// <summary>Create a new notification queue with the given configuration.</summary>
    public NotificationQueue(QueueConfig config)
    {
        _dedupWindow  = TimeSpan.FromMilliseconds(config.DedupWindowMs);
        _config       = config;
        _pendingQueue = new LinkedList<QueuedNotification>();
        _visible      = new List<Toast>();
        _recentHashes = new Dictionary<ulong, DateTime>();
        _stats        = new QueueStats();
    }

    /// <summary>Create a new queue with default configuration.</summary>
    public static NotificationQueue WithDefaults() => new(QueueConfig.CreateDefault());

    // ── Push / helpers ────────────────────────────────────────────────────

    /// <summary>Push a notification to the queue.
    ///
    /// Returns true if the notification was accepted, false if it was
    /// rejected due to deduplication or queue overflow.</summary>
    public bool Push(Toast toast, NotificationPriority priority)
    {
        _stats.TotalPushed++;
        var queued = new QueuedNotification(ApplyDefaultDuration(toast), priority);

        // Check deduplication
        if (!DedupCheck(queued.ContentHash))
        {
            _stats.DedupCount++;
            return false;
        }

        // Check queue overflow
        if (_pendingQueue.Count >= _config.MaxQueued)
        {
            _stats.OverflowCount++;
            // Drop oldest low-priority item if possible
            var lowestNode = FindLowestPriorityNode();
            if (lowestNode is not null && lowestNode.Value.Priority < priority)
            {
                _pendingQueue.Remove(lowestNode);
            }
            else
            {
                return false; // New item is lower or equal priority
            }
        }

        // Insert based on priority
        if (priority == NotificationPriority.Urgent)
        {
            // Urgent jumps to front
            _pendingQueue.AddFirst(queued);
        }
        else
        {
            // Insert in priority order: find first node with lower priority
            var insertBefore = FindFirstNodeWithLowerPriority(priority);
            if (insertBefore is null)
                _pendingQueue.AddLast(queued);
            else
                _pendingQueue.AddBefore(insertBefore, queued);
        }

        return true;
    }

    /// <summary>Push a notification with normal priority.</summary>
    public bool Notify(Toast toast) => Push(toast, NotificationPriority.Normal);

    /// <summary>Push an urgent notification.</summary>
    public bool Urgent(Toast toast) => Push(toast, NotificationPriority.Urgent);

    /// <summary>Dismiss a specific notification by ID.</summary>
    public void Dismiss(ToastId id)
    {
        // Check visible toasts
        var visIdx = _visible.FindIndex(t => t.Id == id);
        if (visIdx >= 0 && !_visible[visIdx].State.Dismissed)
        {
            _visible[visIdx].Dismiss();
            _stats.UserDismissed++;
        }

        // Check pending queue
        var node = _pendingQueue.First;
        while (node is not null)
        {
            var next = node.Next;
            if (node.Value.Toast.Id == id)
            {
                _pendingQueue.Remove(node);
                _stats.UserDismissed++;
                break;
            }
            node = next;
        }
    }

    /// <summary>Dismiss all notifications.</summary>
    public void DismissAll()
    {
        ulong dismissedVisible = 0;
        foreach (var toast in _visible)
        {
            if (!toast.State.Dismissed)
            {
                toast.Dismiss();
                dismissedVisible++;
            }
        }
        _stats.UserDismissed += dismissedVisible + (ulong)_pendingQueue.Count;
        _pendingQueue.Clear();
    }

    /// <summary>Process a time tick, handling expiry and promotion.
    ///
    /// Call this regularly in your event loop (e.g., every frame or every 16ms).
    /// Returns a list of actions to perform.</summary>
    public List<QueueAction> Tick(TimeSpan _delta)
    {
        var actions = new List<QueueAction>();

        // Clean expired dedup hashes
        var now = DateTime.UtcNow;
        var expiredKeys = new List<ulong>();
        foreach (var kv in _recentHashes)
        {
            if (now - kv.Value >= _dedupWindow)
                expiredKeys.Add(kv.Key);
        }
        foreach (var key in expiredKeys)
            _recentHashes.Remove(key);

        // Process visible toasts for expiry and animations
        int i = 0;
        while (i < _visible.Count)
        {
            var toast = _visible[i];

            // Trigger auto-dismiss on expiry
            if (!toast.State.Dismissed && toast.IsExpired())
            {
                toast.Dismiss();
                _stats.AutoExpired++;
            }

            // Advance animation state
            toast.TickAnimation();

            if (!_visible[i].IsVisible())
            {
                var hiddenId = _visible[i].Id;
                _visible.RemoveAt(i);
                actions.Add(new QueueAction.Hide(hiddenId));
            }
            else
            {
                i++;
            }
        }

        // Promote from queue to visible
        while (_visible.Count < _config.MaxVisible)
        {
            if (_pendingQueue.First is { } firstNode)
            {
                var promotedId = firstNode.Value.Toast.Id;
                _visible.Add(firstNode.Value.Toast);
                _pendingQueue.RemoveFirst();
                actions.Add(new QueueAction.Show(promotedId));
            }
            else
            {
                break;
            }
        }

        return actions;
    }

    // ── Accessors ─────────────────────────────────────────────────────────

    /// <summary>Get currently visible toasts.</summary>
    public IReadOnlyList<Toast> Visible() => _visible;

    /// <summary>Get mutable access to visible toasts.</summary>
    public List<Toast> VisibleMut() => _visible;

    /// <summary>Get the number of notifications waiting in the queue.</summary>
    public int PendingCount() => _pendingQueue.Count;

    /// <summary>Get the number of visible toasts.</summary>
    public int VisibleCount() => _visible.Count;

    /// <summary>Get the total count (visible + pending).</summary>
    public int TotalCount() => _visible.Count + _pendingQueue.Count;

    /// <summary>Check if the queue is empty (no visible or pending notifications).</summary>
    public bool IsEmpty() => _visible.Count == 0 && _pendingQueue.Count == 0;

    /// <summary>Get queue statistics.</summary>
    public QueueStats Stats() => _stats;

    /// <summary>Get the configuration.</summary>
    public QueueConfig Config() => _config;

    /// <summary>Return the messages of all pending (not-yet-visible) notifications in queue
    /// order, for test assertions.
    ///
    /// <para>DIVERGENCE: Rust tests access <c>queue.queue</c> directly because the test
    /// module lives inside the same crate (Rust's <c>mod tests</c> with <c>use super::*</c>
    /// gives private-field access).  In C# the test assembly is separate; this
    /// <c>internal</c> accessor grants equivalent visibility via
    /// <c>InternalsVisibleTo("FrankenTui.Tests.Headless")</c>.</para></summary>
    internal IEnumerable<string> PendingMessages()
        => _pendingQueue.Select(q => q.Toast.Content.Message);

    /// <summary>Calculate stacking positions for all visible toasts.
    ///
    /// Returns a list of (ToastId, x, y) positions.</summary>
    public List<(ToastId id, ushort x, ushort y)> CalculatePositions(
        ushort terminalWidth, ushort terminalHeight, ushort margin)
    {
        var positions = new List<(ToastId, ushort, ushort)>(_visible.Count);
        bool isTop = _config.Position is
            ToastPosition.TopLeft or ToastPosition.TopCenter or ToastPosition.TopRight;

        ushort yOffset = 0;

        foreach (var toast in _visible)
        {
            var (toastWidth, toastHeight) = toast.CalculateDimensions();
            var (baseX, baseY) = _config.Position.CalculatePosition(
                terminalWidth, terminalHeight, toastWidth, toastHeight, margin);

            ushort y = isTop
                ? SatAdd16(baseY, yOffset)
                : SatSub16(baseY, yOffset);

            positions.Add((toast.Id, baseX, y));
            yOffset = SatAdd16(SatAdd16(yOffset, toastHeight), _config.StaggerOffset);
        }

        return positions;
    }

    // ── Internal methods ──────────────────────────────────────────────────

    /// <summary>Check if a content hash is a duplicate within the dedup window.</summary>
    private bool DedupCheck(ulong hash)
    {
        var now = DateTime.UtcNow;

        // Clean old hashes
        var expiredKeys = new List<ulong>();
        foreach (var kv in _recentHashes)
        {
            if (now - kv.Value >= _dedupWindow)
                expiredKeys.Add(kv.Key);
        }
        foreach (var key in expiredKeys)
            _recentHashes.Remove(key);

        // Check if duplicate
        if (_recentHashes.ContainsKey(hash))
            return false;

        _recentHashes[hash] = now;
        return true;
    }

    /// <summary>Find the linked-list node with the lowest priority item in the queue.</summary>
    private LinkedListNode<QueuedNotification>? FindLowestPriorityNode()
    {
        LinkedListNode<QueuedNotification>? lowest = null;
        var node = _pendingQueue.First;
        while (node is not null)
        {
            if (lowest is null || node.Value.Priority < lowest.Value.Priority)
                lowest = node;
            node = node.Next;
        }
        return lowest;
    }

    /// <summary>Find the first node whose priority is strictly less than the given priority.</summary>
    private LinkedListNode<QueuedNotification>? FindFirstNodeWithLowerPriority(NotificationPriority priority)
    {
        var node = _pendingQueue.First;
        while (node is not null)
        {
            if (node.Value.Priority < priority)
                return node;
            node = node.Next;
        }
        return null;
    }

    private Toast ApplyDefaultDuration(Toast toast)
    {
        if (!toast.Config.DurationExplicit)
        {
            toast.Config.Duration         = _config.DefaultDuration;
            toast.Config.DurationExplicit = true;
        }
        return toast;
    }

    private static ushort SatAdd16(ushort a, ushort b)
    {
        int sum = (int)a + b;
        return sum > ushort.MaxValue ? ushort.MaxValue : (ushort)sum;
    }

    private static ushort SatSub16(ushort a, ushort b) => a >= b ? (ushort)(a - b) : (ushort)0;
}

// ── NotificationStack ──────────────────────────────────────────────────────────

/// <summary>Widget that renders the visible toasts in a queue.
///
/// This is a thin renderer over NotificationQueue, keeping stacking logic
/// centralized in the queue while ensuring the draw path stays deterministic.</summary>
public sealed class NotificationStack : IWidget
{
    private readonly NotificationQueue _queue;
    private ushort _margin;

    /// <summary>Create a new notification stack renderer.</summary>
    public NotificationStack(NotificationQueue queue)
    {
        _queue  = queue;
        _margin = 1;
    }

    /// <summary>Set the margin from the screen edge.</summary>
    public NotificationStack WithMargin(ushort margin)
    {
        _margin = margin;
        return this;
    }

    // Expose for test assertions (matches Rust pub(crate) field pattern)
    internal ushort MarginField => _margin;

    /// <inheritdoc />
    public void Render(Rect area, Frame frame)
    {
        if (area.IsEmpty || _queue.Visible().Count == 0)
            return;

        var positions = _queue.CalculatePositions(area.Width, area.Height, _margin);
        var visible   = _queue.Visible();

        for (int idx = 0; idx < visible.Count && idx < positions.Count; idx++)
        {
            var toast = visible[idx];
            var (_, relX, relY) = positions[idx];
            var (toastWidth, toastHeight) = toast.CalculateDimensions();
            ushort x = SatAdd16(area.X, relX);
            ushort y = SatAdd16(area.Y, relY);
            var toastArea  = new Rect(x, y, toastWidth, toastHeight);
            var renderArea = toastArea.Intersection(area);
            if (!renderArea.IsEmpty)
                toast.Render(renderArea, frame);
        }
    }

    private static ushort SatAdd16(ushort a, ushort b)
    {
        int sum = (int)a + b;
        return sum > ushort.MaxValue ? ushort.MaxValue : (ushort)sum;
    }
}
