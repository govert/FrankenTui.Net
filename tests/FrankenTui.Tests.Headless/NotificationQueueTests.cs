// Upstream source: .external/frankentui/crates/ftui-widgets/src/notification_queue.rs (tests module)
// Full 1-1 port of all upstream notification_queue tests.

using FrankenTui.Core;
using FrankenTui.Render;
using FrankenTui.Widgets;

namespace FrankenTui.Tests.Headless;

public class NotificationQueueTests
{
    // ── Test helpers (mirrors Rust test helpers) ──────────────────────────

    /// <summary>Create a persistent, no-animation toast for deterministic testing.</summary>
    private static Toast MakeToast(string msg)
        => Toast.WithId(ToastId.New(0), msg)
               .Persistent()
               .NoAnimation();

    /// <summary>Create an ephemeral (auto-dismiss) no-animation toast.</summary>
    private static Toast MakeEphemeralToast(string msg)
        => Toast.New(msg).NoAnimation();

    // ── Tests ─────────────────────────────────────────────────────────────

    [Fact]
    public void TestQueueNew()
    {
        var queue = NotificationQueue.WithDefaults();
        Assert.True(queue.IsEmpty());
        Assert.Equal(0, queue.VisibleCount());
        Assert.Equal(0, queue.PendingCount());
    }

    [Fact]
    public void TestQueuePushAndTick()
    {
        var queue = NotificationQueue.WithDefaults();

        queue.Push(MakeToast("Hello"), NotificationPriority.Normal);
        Assert.Equal(1, queue.PendingCount());
        Assert.Equal(0, queue.VisibleCount());

        // Tick promotes from queue to visible
        var actions = queue.Tick(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, queue.PendingCount());
        Assert.Equal(1, queue.VisibleCount());
        Assert.Equal(1, actions.Count);
        Assert.IsType<QueueAction.Show>(actions[0]);
    }

    [Fact]
    public void TestQueueFifo()
    {
        var config = QueueConfig.CreateDefault().WithMaxVisible(1);
        var queue  = new NotificationQueue(config);

        queue.Push(MakeToast("First"),  NotificationPriority.Normal);
        queue.Push(MakeToast("Second"), NotificationPriority.Normal);
        queue.Push(MakeToast("Third"),  NotificationPriority.Normal);

        queue.Tick(TimeSpan.FromMilliseconds(16));
        Assert.Equal("First", queue.Visible()[0].Content.Message);

        // Dismiss first, tick to get second
        queue.VisibleMut()[0].Dismiss();
        queue.Tick(TimeSpan.FromMilliseconds(16));
        Assert.Equal("Second", queue.Visible()[0].Content.Message);
    }

    [Fact]
    public void TestQueueMaxVisible()
    {
        var config = QueueConfig.CreateDefault().WithMaxVisible(2);
        var queue  = new NotificationQueue(config);

        queue.Push(MakeToast("A"), NotificationPriority.Normal);
        queue.Push(MakeToast("B"), NotificationPriority.Normal);
        queue.Push(MakeToast("C"), NotificationPriority.Normal);

        queue.Tick(TimeSpan.FromMilliseconds(16));

        Assert.Equal(2, queue.VisibleCount());
        Assert.Equal(1, queue.PendingCount());
    }

    [Fact]
    public void TestQueuePriorityUrgent()
    {
        var config = QueueConfig.CreateDefault().WithMaxVisible(1);
        var queue  = new NotificationQueue(config);

        queue.Push(MakeToast("Normal1"), NotificationPriority.Normal);
        queue.Push(MakeToast("Normal2"), NotificationPriority.Normal);
        queue.Push(MakeToast("Urgent"),  NotificationPriority.Urgent);

        queue.Tick(TimeSpan.FromMilliseconds(16));
        // Urgent should jump to front
        Assert.Equal("Urgent", queue.Visible()[0].Content.Message);
    }

    [Fact]
    public void TestQueuePriorityOrdering()
    {
        // Upstream: queue.queue.iter().map(|q| q.toast.content.message.as_str()).collect()
        // asserts ["High", "Normal", "Low"] on the queue under test.
        // max_visible(0) prevents any automatic promotion so the pending order is stable.
        var config = QueueConfig.CreateDefault().WithMaxVisible(0); // No auto-promote
        var queue  = new NotificationQueue(config);

        queue.Push(MakeToast("Low"),    NotificationPriority.Low);
        queue.Push(MakeToast("Normal"), NotificationPriority.Normal);
        queue.Push(MakeToast("High"),   NotificationPriority.High);

        // Queue should be ordered High, Normal, Low — assert directly on queue under test
        // via the internal PendingMessages() accessor (InternalsVisibleTo grants access).
        Assert.Equal(new[] { "High", "Normal", "Low" }, queue.PendingMessages());
    }

    [Fact]
    public void TestQueueDedup()
    {
        var config = QueueConfig.CreateDefault().WithDedupWindowMs(1000);
        var queue  = new NotificationQueue(config);

        Assert.True(queue.Push(MakeToast("Same message"), NotificationPriority.Normal));
        Assert.False(queue.Push(MakeToast("Same message"), NotificationPriority.Normal));

        Assert.Equal(1UL, queue.Stats().DedupCount);
    }

    [Fact]
    public void TestQueueOverflow()
    {
        var config = QueueConfig.CreateDefault().WithMaxQueued(2);
        var queue  = new NotificationQueue(config);

        Assert.True(queue.Push(MakeToast("A"), NotificationPriority.Normal));
        Assert.True(queue.Push(MakeToast("B"), NotificationPriority.Normal));
        // Third should fail (queue full)
        Assert.False(queue.Push(MakeToast("C"), NotificationPriority.Normal));

        Assert.Equal(1UL, queue.Stats().OverflowCount);
    }

    [Fact]
    public void TestQueueOverflowDropsLowerPriority()
    {
        // Upstream: assert!(messages.contains(&"High")) where messages is built from
        // queue.queue.iter() — i.e. the queue under test, not a separate queue.
        var config = QueueConfig.CreateDefault().WithMaxQueued(2);
        var queue  = new NotificationQueue(config);

        Assert.True(queue.Push(MakeToast("Low1"), NotificationPriority.Low));
        Assert.True(queue.Push(MakeToast("Low2"), NotificationPriority.Low));
        // High priority should drop a low priority item
        Assert.True(queue.Push(MakeToast("High"), NotificationPriority.High));

        Assert.Equal(2, queue.PendingCount());
        // Assert "High" is present in the actual queue under test (mirrors upstream
        // messages.contains(&"High") on queue.queue.iter()).
        Assert.Contains("High", queue.PendingMessages());
    }

    [Fact]
    public void TestQueueDismiss()
    {
        var queue = NotificationQueue.WithDefaults();

        queue.Push(MakeToast("Test"), NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        var id = queue.Visible()[0].Id;
        queue.Dismiss(id);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        Assert.Equal(0, queue.VisibleCount());
        Assert.Equal(1UL, queue.Stats().UserDismissed);
    }

    [Fact]
    public void TestQueueDismissAll()
    {
        var queue = NotificationQueue.WithDefaults();

        queue.Push(MakeToast("A"), NotificationPriority.Normal);
        queue.Push(MakeToast("B"), NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        queue.DismissAll();
        queue.Tick(TimeSpan.FromMilliseconds(16));

        Assert.True(queue.IsEmpty());
        Assert.Equal(2UL, queue.Stats().UserDismissed);
    }

    [Fact]
    public void TestQueueCalculatePositionsTop()
    {
        var config = QueueConfig.CreateDefault().WithPosition(ToastPosition.TopRight);
        var queue  = new NotificationQueue(config);

        queue.Push(MakeToast("A"), NotificationPriority.Normal);
        queue.Push(MakeToast("B"), NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        var positions = queue.CalculatePositions(80, 24, 1);
        Assert.Equal(2, positions.Count);

        // First toast should be at top, second below
        Assert.True(positions[0].y < positions[1].y);
    }

    [Fact]
    public void TestQueueCalculatePositionsBottom()
    {
        var config = QueueConfig.CreateDefault().WithPosition(ToastPosition.BottomRight);
        var queue  = new NotificationQueue(config);

        queue.Push(MakeToast("A"), NotificationPriority.Normal);
        queue.Push(MakeToast("B"), NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        var positions = queue.CalculatePositions(80, 24, 1);
        Assert.Equal(2, positions.Count);

        // First toast should be at bottom, second above
        Assert.True(positions[0].y > positions[1].y);
    }

    [Fact]
    public void TestQueueNotifyHelper()
    {
        var queue = NotificationQueue.WithDefaults();
        Assert.True(queue.Notify(MakeToast("Normal")));
        queue.Tick(TimeSpan.FromMilliseconds(16));
        Assert.Equal(1, queue.VisibleCount());
    }

    [Fact]
    public void TestQueueUrgentHelper()
    {
        var config = QueueConfig.CreateDefault().WithMaxVisible(1);
        var queue  = new NotificationQueue(config);

        queue.Notify(MakeToast("Normal"));
        queue.Urgent(MakeToast("Urgent"));
        queue.Tick(TimeSpan.FromMilliseconds(16));

        Assert.Equal("Urgent", queue.Visible()[0].Content.Message);
    }

    [Fact]
    public void TestQueueStats()
    {
        var queue = NotificationQueue.WithDefaults();

        queue.Push(MakeToast("A"), NotificationPriority.Normal);
        queue.Push(MakeToast("A"), NotificationPriority.Normal); // Dedup
        queue.Tick(TimeSpan.FromMilliseconds(16));

        Assert.Equal(2UL, queue.Stats().TotalPushed);
        Assert.Equal(1UL, queue.Stats().DedupCount);
    }

    [Fact]
    public void TestQueueConfigBuilder()
    {
        var config = QueueConfig.New()
            .WithMaxVisible(5)
            .WithMaxQueued(20)
            .WithDefaultDuration(TimeSpan.FromSeconds(10))
            .WithPosition(ToastPosition.BottomLeft)
            .WithStaggerOffset(2)
            .WithDedupWindowMs(500);

        Assert.Equal(5,                          config.MaxVisible);
        Assert.Equal(20,                         config.MaxQueued);
        Assert.Equal(TimeSpan.FromSeconds(10),   config.DefaultDuration);
        Assert.Equal(ToastPosition.BottomLeft,   config.Position);
        Assert.Equal(2,                          (int)config.StaggerOffset);
        Assert.Equal(500UL,                      config.DedupWindowMs);
    }

    [Fact]
    public void TestQueueTotalCount()
    {
        var config = QueueConfig.CreateDefault().WithMaxVisible(1);
        var queue  = new NotificationQueue(config);

        queue.Push(MakeToast("A"), NotificationPriority.Normal);
        queue.Push(MakeToast("B"), NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        Assert.Equal(2, queue.TotalCount());
        Assert.Equal(1, queue.VisibleCount());
        Assert.Equal(1, queue.PendingCount());
    }

    [Fact]
    public void QueueConfigDefaultValues()
    {
        var config = QueueConfig.CreateDefault();
        Assert.Equal(3,                          config.MaxVisible);
        Assert.Equal(10,                         config.MaxQueued);
        Assert.Equal(TimeSpan.FromSeconds(5),    config.DefaultDuration);
        Assert.Equal(ToastPosition.TopRight,     config.Position);
        Assert.Equal(1,                          (int)config.StaggerOffset);
        Assert.Equal(1000UL,                     config.DedupWindowMs);
    }

    [Fact]
    public void NotificationPriorityDefaultIsNormal()
    {
        // Upstream: assert_eq!(NotificationPriority::default(), NotificationPriority::Normal)
        // Rust's #[default] makes ::default() return Normal (ordinal 1).
        //
        // DIVERGENCE: In C#, default(NotificationPriority) returns Low (ordinal 0) because
        // the language always uses the zero value as the default for enums.  The assertions
        // below capture both facts:
        //   1. The C# language default for the enum is Low — NOT Normal — which is a genuine
        //      semantic gap vs. the Rust source.
        //   2. The project-intended default is Normal, preserved via
        //      NotificationPriorityDefaults.ProjectDefault (equivalent to Rust's #[default]).
        Assert.Equal(NotificationPriority.Low, default(NotificationPriority));
        Assert.Equal(NotificationPriority.Normal, NotificationPriorityDefaults.ProjectDefault);
    }

    [Fact]
    public void NotificationPriorityOrdering()
    {
        Assert.True(NotificationPriority.Low    < NotificationPriority.Normal);
        Assert.True(NotificationPriority.Normal < NotificationPriority.High);
        Assert.True(NotificationPriority.High   < NotificationPriority.Urgent);
    }

    [Fact]
    public void QueueDefaultTraitDelegatesToWithDefaults()
    {
        var queue = NotificationQueue.WithDefaults();
        Assert.True(queue.IsEmpty());
        Assert.Equal(3, queue.Config().MaxVisible);
    }

    [Fact]
    public void IsEmptyFalseWhenPending()
    {
        var queue = NotificationQueue.WithDefaults();
        queue.Push(MakeToast("X"), NotificationPriority.Normal);
        Assert.False(queue.IsEmpty());
    }

    [Fact]
    public void IsEmptyFalseWhenVisible()
    {
        var queue = NotificationQueue.WithDefaults();
        queue.Push(MakeToast("X"), NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));
        Assert.False(queue.IsEmpty());
    }

    [Fact]
    public void VisibleMutAllowsModification()
    {
        var queue = NotificationQueue.WithDefaults();
        queue.Push(MakeToast("Original"), NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        // Dismiss via VisibleMut
        queue.VisibleMut()[0].Dismiss();
        queue.Tick(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, queue.VisibleCount());
    }

    [Fact]
    public void ConfigAccessorReturnsConfig()
    {
        var config = QueueConfig.CreateDefault().WithMaxVisible(7).WithStaggerOffset(3);
        var queue  = new NotificationQueue(config);
        Assert.Equal(7, queue.Config().MaxVisible);
        Assert.Equal(3, (int)queue.Config().StaggerOffset);
    }

    [Fact]
    public void DismissAllClearsQueueAndVisible()
    {
        var config = QueueConfig.CreateDefault().WithMaxVisible(1);
        var queue  = new NotificationQueue(config);

        queue.Push(MakeToast("A"), NotificationPriority.Normal);
        queue.Push(MakeToast("B"), NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        // After tick: A is visible, B is pending.
        Assert.Equal(1, queue.VisibleCount());
        Assert.Equal(1, queue.PendingCount());

        queue.DismissAll();
        // dismiss_all counts both the visible and pending toast.
        Assert.Equal(2UL, queue.Stats().UserDismissed);
        Assert.Equal(0, queue.PendingCount());

        // Next tick removes the dismissed visible toast
        queue.Tick(TimeSpan.FromMilliseconds(16));
        Assert.True(queue.IsEmpty());
    }

    [Fact]
    public void DismissDoesNotDoubleCountAlreadyDismissedVisibleToast()
    {
        var queue = NotificationQueue.WithDefaults();
        queue.Push(MakeToast("A"), NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        var id = queue.Visible()[0].Id;
        queue.Dismiss(id);
        queue.Dismiss(id);

        Assert.Equal(1UL, queue.Stats().UserDismissed);
    }

    [Fact]
    public void QueueAppliesConfigDefaultDurationToDefaultToasts()
    {
        var config = QueueConfig.CreateDefault().WithDefaultDuration(TimeSpan.FromSeconds(12));
        var queue  = new NotificationQueue(config);

        queue.Push(MakeEphemeralToast("A"), NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        Assert.Equal(TimeSpan.FromSeconds(12), queue.Visible()[0].Config.Duration);
    }

    [Fact]
    public void QueuePreservesPersistentToastsWhenApplyingDefaultDuration()
    {
        var config = QueueConfig.CreateDefault().WithDefaultDuration(TimeSpan.FromSeconds(12));
        var queue  = new NotificationQueue(config);

        queue.Push(MakeToast("A"), NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        Assert.Null(queue.Visible()[0].Config.Duration);
    }

    [Fact]
    public void QueuePreservesExplicitCustomDuration()
    {
        var config = QueueConfig.CreateDefault().WithDefaultDuration(TimeSpan.FromSeconds(12));
        var queue  = new NotificationQueue(config);

        queue.Push(
            Toast.New("A").Duration(TimeSpan.FromSeconds(2)).NoAnimation(),
            NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        Assert.Equal(TimeSpan.FromSeconds(2), queue.Visible()[0].Config.Duration);
    }

    [Fact]
    public void QueuePreservesExplicitDurationEvenWhenEqualToToastDefault()
    {
        var config = QueueConfig.CreateDefault().WithDefaultDuration(TimeSpan.FromSeconds(12));
        var queue  = new NotificationQueue(config);

        queue.Push(
            Toast.New("A").Duration(TimeSpan.FromSeconds(5)).NoAnimation(),
            NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        Assert.Equal(TimeSpan.FromSeconds(5), queue.Visible()[0].Config.Duration);
    }

    [Fact]
    public void QueueActionEquality()
    {
        var id = ToastId.New(42);
        Assert.Equal(new QueueAction.Show(id),        new QueueAction.Show(id));
        Assert.Equal(new QueueAction.Hide(id),        new QueueAction.Hide(id));
        Assert.Equal(new QueueAction.Reposition(id),  new QueueAction.Reposition(id));
        Assert.NotEqual((QueueAction)new QueueAction.Show(id), new QueueAction.Hide(id));
    }

    [Fact]
    public void QueueStatsDefaultAllZero()
    {
        var stats = new QueueStats();
        Assert.Equal(0UL, stats.TotalPushed);
        Assert.Equal(0UL, stats.OverflowCount);
        Assert.Equal(0UL, stats.DedupCount);
        Assert.Equal(0UL, stats.UserDismissed);
        Assert.Equal(0UL, stats.AutoExpired);
    }

    [Fact]
    public void CalculatePositionsEmptyReturnsEmpty()
    {
        var queue     = NotificationQueue.WithDefaults();
        var positions = queue.CalculatePositions(80, 24, 1);
        Assert.Empty(positions);
    }

    [Fact]
    public void NotificationStackEmptyAreaRendersNothing()
    {
        var queue = NotificationQueue.WithDefaults();
        queue.Push(MakeToast("Hello"), NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        var pool      = new GraphemePool();
        var frame     = new Frame(40, 10, pool);
        var emptyArea = new Rect(0, 0, 0, 0);

        // Should not panic
        new NotificationStack(queue).Render(emptyArea, frame);
    }

    [Fact]
    public void NotificationStackMarginBuilder()
    {
        var queue = NotificationQueue.WithDefaults();
        var stack = new NotificationStack(queue).WithMargin(5);
        Assert.Equal(5, (int)stack.MarginField);
    }

    [Fact]
    public void NotificationStackRendersVisibleToast()
    {
        var queue = NotificationQueue.WithDefaults();
        queue.Push(MakeToast("Hello"), NotificationPriority.Normal);
        queue.Tick(TimeSpan.FromMilliseconds(16));

        var pool  = new GraphemePool();
        var frame = new Frame(40, 10, pool);
        var area  = new Rect(0, 0, 40, 10);

        new NotificationStack(queue).WithMargin(0).Render(area, frame);

        var positions = queue.CalculatePositions(40, 10, 0);
        var (_, x, y) = positions[0];
        var cell = frame.Buffer.Get(x, y);
        Assert.True(cell.HasValue, "stack should render toast content");
        Assert.False(cell!.Value.IsEmpty, "stack should render toast content");
    }
}
