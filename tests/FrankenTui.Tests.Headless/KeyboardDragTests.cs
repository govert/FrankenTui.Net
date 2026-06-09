// Upstream source: crates/ftui-widgets/src/keyboard_drag.rs (tests module)
// Full 1-1 port of all upstream keyboard_drag tests.

using FrankenTui.Core;
using FrankenTui.Widgets;
using Xunit;

namespace FrankenTui.Tests.Headless;

public class KeyboardDragTests
{
    // === KeyboardDragMode tests ===

    [Fact]
    public void ModeIsActive()
    {
        Assert.False(KeyboardDragMode.Inactive.IsActive());
        Assert.True(KeyboardDragMode.Holding.IsActive());
        Assert.True(KeyboardDragMode.Navigating.IsActive());
    }

    [Fact]
    public void ModeAsStr()
    {
        Assert.Equal("inactive", KeyboardDragMode.Inactive.AsStr());
        Assert.Equal("holding", KeyboardDragMode.Holding.AsStr());
        Assert.Equal("navigating", KeyboardDragMode.Navigating.AsStr());
    }

    // === Direction tests ===

    [Fact]
    public void DirectionOpposite()
    {
        Assert.Equal(Direction.Down, Direction.Up.Opposite());
        Assert.Equal(Direction.Up, Direction.Down.Opposite());
        Assert.Equal(Direction.Right, Direction.Left.Opposite());
        Assert.Equal(Direction.Left, Direction.Right.Opposite());
    }

    [Fact]
    public void DirectionIsVertical()
    {
        Assert.True(Direction.Up.IsVertical());
        Assert.True(Direction.Down.IsVertical());
        Assert.False(Direction.Left.IsVertical());
        Assert.False(Direction.Right.IsVertical());
    }

    // === DropTargetInfo tests ===

    [Fact]
    public void DropTargetInfoNew()
    {
        var target = new DropTargetInfo(new WidgetId(1), "Test Target", new Rect(0, 0, 10, 5));
        Assert.Equal(new WidgetId(1), target.Id);
        Assert.Equal("Test Target", target.Name);
        Assert.True(target.Enabled);
        Assert.Empty(target.AcceptedTypes);
    }

    [Fact]
    public void DropTargetInfoCanAcceptAny()
    {
        var target = new DropTargetInfo(new WidgetId(1), "Any", new Rect(0, 0, 1, 1));
        // No filter means accept any
        Assert.True(target.CanAccept("text/plain"));
        Assert.True(target.CanAccept("application/json"));
    }

    [Fact]
    public void DropTargetInfoCanAcceptFiltered()
    {
        var target = new DropTargetInfo(new WidgetId(1), "Text Only", new Rect(0, 0, 1, 1))
            .WithAcceptedTypes(new List<string> { "text/plain" });
        Assert.True(target.CanAccept("text/plain"));
        Assert.False(target.CanAccept("application/json"));
    }

    [Fact]
    public void DropTargetInfoCanAcceptWildcard()
    {
        var target = new DropTargetInfo(new WidgetId(1), "All Text", new Rect(0, 0, 1, 1))
            .WithAcceptedTypes(new List<string> { "text/*" });
        Assert.True(target.CanAccept("text/plain"));
        Assert.True(target.CanAccept("text/html"));
        Assert.False(target.CanAccept("application/json"));
    }

    [Fact]
    public void DropTargetInfoDisabled()
    {
        var target = new DropTargetInfo(new WidgetId(1), "Disabled", new Rect(0, 0, 1, 1))
            .WithEnabled(false);
        Assert.False(target.CanAccept("text/plain"));
    }

    [Fact]
    public void DropTargetInfoCenter()
    {
        var target = new DropTargetInfo(new WidgetId(1), "Test", new Rect(10, 20, 10, 6));
        Assert.Equal(((ushort)15, (ushort)23), target.Center());
    }

    // === Announcement tests ===

    [Fact]
    public void AnnouncementNormal()
    {
        var a = Announcement.Normal("Test message");
        Assert.Equal("Test message", a.Text);
        Assert.Equal(AnnouncementPriority.Normal, a.Priority);
    }

    [Fact]
    public void AnnouncementHigh()
    {
        var a = Announcement.High("Important!");
        Assert.Equal(AnnouncementPriority.High, a.Priority);
    }

    // === KeyboardDragConfig tests ===

    [Fact]
    public void ConfigDefaults()
    {
        var config = new KeyboardDragConfig();
        Assert.True(config.CancelOnEscape);
        Assert.True(config.WrapNavigation);
        Assert.Equal(2, config.ActivateKeys.Count);
    }

    // === KeyboardDragState tests ===

    [Fact]
    public void DragStateAnimation()
    {
        var payload = DragPayload.Text("test");
        var state = new KeyboardDragState(new WidgetId(1), payload);

        var initialTick = state.AnimationTick;
        state.TickAnimation();
        Assert.Equal(unchecked((byte)(initialTick + 1)), state.AnimationTick);
    }

    [Fact]
    public void DragStatePulseIntensity()
    {
        var payload = DragPayload.Text("test");
        var state = new KeyboardDragState(new WidgetId(1), payload);

        var intensity = state.PulseIntensity();
        Assert.InRange(intensity, 0.0f, 1.0f);
    }

    // === KeyboardDragManager tests ===

    [Fact]
    public void ManagerStartDrag()
    {
        var manager = KeyboardDragManager.WithDefaults();
        Assert.False(manager.IsActive());

        var payload = DragPayload.Text("item");
        Assert.True(manager.StartDrag(new WidgetId(1), payload));
        Assert.True(manager.IsActive());
        Assert.Equal(KeyboardDragMode.Holding, manager.Mode());
    }

    [Fact]
    public void ManagerDoubleStartFails()
    {
        var manager = KeyboardDragManager.WithDefaults();

        Assert.True(manager.StartDrag(new WidgetId(1), DragPayload.Text("first")));
        Assert.False(manager.StartDrag(new WidgetId(2), DragPayload.Text("second")));
    }

    [Fact]
    public void ManagerCancelDrag()
    {
        var manager = KeyboardDragManager.WithDefaults();
        manager.StartDrag(new WidgetId(1), DragPayload.Text("item"));

        var payload = manager.CancelDrag();
        Assert.NotNull(payload);
        Assert.False(manager.IsActive());
    }

    [Fact]
    public void ManagerCancelInactive()
    {
        var manager = KeyboardDragManager.WithDefaults();
        Assert.Null(manager.CancelDrag());
    }

    [Fact]
    public void ManagerNavigateTargets()
    {
        var manager = KeyboardDragManager.WithDefaults();
        manager.StartDrag(new WidgetId(1), DragPayload.Text("item"));

        var targets = new List<DropTargetInfo>
        {
            new DropTargetInfo(new WidgetId(10), "Target A", new Rect(0, 0, 10, 5)),
            new DropTargetInfo(new WidgetId(11), "Target B", new Rect(20, 0, 10, 5)),
        };

        var selected = manager.NavigateTargets(Direction.Down, targets);
        Assert.NotNull(selected);
        Assert.Equal("Target A", selected!.Name);
        Assert.Equal(KeyboardDragMode.Navigating, manager.Mode());
    }

    [Fact]
    public void ManagerNavigateEmptyTargets()
    {
        var manager = KeyboardDragManager.WithDefaults();
        manager.StartDrag(new WidgetId(1), DragPayload.Text("item"));
        manager.StateMut()!.SelectedTargetIndex = 3;
        manager.StateMut()!.Mode = KeyboardDragMode.Navigating;

        var targets = new List<DropTargetInfo>();
        var selected = manager.NavigateTargets(Direction.Down, targets);
        Assert.Null(selected);
        Assert.Equal(KeyboardDragMode.Holding, manager.Mode());
        Assert.Null(manager.State()!.SelectedTargetIndex);
    }

    [Fact]
    public void ManagerNavigateWrap()
    {
        var manager = KeyboardDragManager.WithDefaults();
        manager.StartDrag(new WidgetId(1), DragPayload.Text("item"));

        var targets = new List<DropTargetInfo>
        {
            new DropTargetInfo(new WidgetId(10), "Target A", new Rect(0, 0, 10, 5)),
            new DropTargetInfo(new WidgetId(11), "Target B", new Rect(20, 0, 10, 5)),
        };

        // Navigate to first
        _ = manager.NavigateTargets(Direction.Down, targets);
        // Navigate to second
        _ = manager.NavigateTargets(Direction.Down, targets);
        // Navigate past end, should wrap to first
        var selected = manager.NavigateTargets(Direction.Down, targets);

        Assert.Equal("Target A", selected!.Name);
    }

    [Fact]
    public void ManagerCompleteDrag()
    {
        var manager = KeyboardDragManager.WithDefaults();
        manager.StartDrag(new WidgetId(1), DragPayload.Text("item"));

        var targets = new List<DropTargetInfo>
        {
            new DropTargetInfo(new WidgetId(10), "Target A", new Rect(0, 0, 10, 5)),
        };

        _ = manager.NavigateTargets(Direction.Down, targets);

        var result = manager.CompleteDrag();
        Assert.NotNull(result);
        var (payload, idx) = result!.Value;
        Assert.Equal("item", payload.AsText());
        Assert.Equal(0, idx);
        Assert.False(manager.IsActive());
    }

    [Fact]
    public void ManagerCompleteWithoutTarget()
    {
        var manager = KeyboardDragManager.WithDefaults();
        manager.StartDrag(new WidgetId(1), DragPayload.Text("item"));

        // No target selected
        var result = manager.CompleteDrag();
        Assert.Null(result);
    }

    [Fact]
    public void ManagerNavigateNoValidTargetsClearsStaleSelection()
    {
        var manager = KeyboardDragManager.WithDefaults();
        manager.StartDrag(new WidgetId(1), new DragPayload("text/plain", []));

        var validTargets = new List<DropTargetInfo>
        {
            new DropTargetInfo(new WidgetId(10), "Text Target", new Rect(0, 0, 10, 5))
                .WithAcceptedTypes(new List<string> { "text/plain" }),
        };
        _ = manager.NavigateTargets(Direction.Down, validTargets);
        Assert.Equal(KeyboardDragMode.Navigating, manager.Mode());

        var invalidTargets = new List<DropTargetInfo>
        {
            new DropTargetInfo(new WidgetId(11), "Image Target", new Rect(20, 0, 10, 5))
                .WithAcceptedTypes(new List<string> { "image/*" }),
        };
        var selected = manager.NavigateTargets(Direction.Down, invalidTargets);

        Assert.Null(selected);
        Assert.Equal(KeyboardDragMode.Holding, manager.Mode());
        Assert.Null(manager.State()!.SelectedTargetIndex);
        Assert.Equal(KeyboardDragAction.NoneAction, manager.HandleKey(KeyboardDragKey.ActivateKey));
    }

    [Fact]
    public void ManagerHandleKeyPickup()
    {
        var manager = KeyboardDragManager.WithDefaults();
        var action = manager.HandleKey(KeyboardDragKey.ActivateKey);
        Assert.Equal(KeyboardDragAction.PickUpAction, action);
    }

    [Fact]
    public void ManagerHandleKeyDrop()
    {
        var manager = KeyboardDragManager.WithDefaults();
        manager.StartDrag(new WidgetId(1), DragPayload.Text("item"));

        // Select a target
        manager.StateMut()!.SelectedTargetIndex = 0;

        var action = manager.HandleKey(KeyboardDragKey.ActivateKey);
        Assert.Equal(KeyboardDragAction.DropAction, action);
    }

    [Fact]
    public void ManagerHandleKeyCancel()
    {
        var manager = KeyboardDragManager.WithDefaults();
        manager.StartDrag(new WidgetId(1), DragPayload.Text("item"));

        var action = manager.HandleKey(KeyboardDragKey.CancelKey);
        Assert.Equal(KeyboardDragAction.CancelAction, action);
    }

    [Fact]
    public void ManagerHandleKeyNavigate()
    {
        var manager = KeyboardDragManager.WithDefaults();
        manager.StartDrag(new WidgetId(1), DragPayload.Text("item"));

        var action = manager.HandleKey(KeyboardDragKey.NavigateKey(Direction.Down));
        Assert.IsType<KeyboardDragAction.Navigate>(action);
        Assert.Equal(Direction.Down, ((KeyboardDragAction.Navigate)action).Direction);
    }

    [Fact]
    public void ManagerAnnouncements()
    {
        var manager = KeyboardDragManager.WithDefaults();
        manager.StartDrag(new WidgetId(1), DragPayload.Text("item"));

        var announcements = manager.DrainAnnouncements();
        Assert.NotEmpty(announcements);
        Assert.Contains("Picked up", announcements[0].Text);
    }

    [Fact]
    public void ManagerAnnouncementQueueLimit()
    {
        var config = new KeyboardDragConfig { MaxAnnouncementQueue = 2 };
        var manager = new KeyboardDragManager(config);

        // Fill queue
        manager.StartDrag(new WidgetId(1), DragPayload.Text("item1"));
        _ = manager.CancelDrag();
        manager.StartDrag(new WidgetId(2), DragPayload.Text("item2"));

        // Should have at most 2 announcements
        Assert.True(manager.Announcements().Count <= 2);
    }

    [Fact]
    public void ManagerAnnouncementQueueZeroDiscardsAnnouncements()
    {
        var config = new KeyboardDragConfig { MaxAnnouncementQueue = 0 };
        var manager = new KeyboardDragManager(config);

        Assert.True(manager.StartDrag(new WidgetId(1), DragPayload.Text("item")));
        _ = manager.CancelDrag();

        Assert.Empty(manager.Announcements());
    }

    [Fact]
    public void ManagerLowerPriorityAnnouncementDoesNotEvictHigherPriority()
    {
        var config = new KeyboardDragConfig { MaxAnnouncementQueue = 1 };
        var manager = new KeyboardDragManager(config);

        Assert.True(manager.StartDrag(new WidgetId(1), DragPayload.Text("item")));
        _ = manager.CancelDrag();

        Assert.Equal(1, manager.Announcements().Count);
        Assert.Equal(AnnouncementPriority.High, manager.Announcements()[0].Priority);
        Assert.Contains("Picked up", manager.Announcements()[0].Text);
    }

    // === Target filtering tests ===

    [Fact]
    public void ManagerNavigateSkipsIncompatible()
    {
        var manager = KeyboardDragManager.WithDefaults();
        manager.StartDrag(new WidgetId(1), new DragPayload("text/plain", []));

        var targets = new List<DropTargetInfo>
        {
            new DropTargetInfo(new WidgetId(10), "Text Target", new Rect(0, 0, 10, 5))
                .WithAcceptedTypes(new List<string> { "text/plain" }),
            new DropTargetInfo(new WidgetId(11), "Image Target", new Rect(20, 0, 10, 5))
                .WithAcceptedTypes(new List<string> { "image/*" }),
            new DropTargetInfo(new WidgetId(12), "Text Target 2", new Rect(40, 0, 10, 5))
                .WithAcceptedTypes(new List<string> { "text/plain" }),
        };

        // First navigation should select Text Target
        var selected = manager.NavigateTargets(Direction.Down, targets);
        Assert.Equal("Text Target", selected!.Name);

        // Second navigation should skip Image Target and select Text Target 2
        selected = manager.NavigateTargets(Direction.Down, targets);
        Assert.Equal("Text Target 2", selected!.Name);
    }

    // === Integration tests ===

    [Fact]
    public void FullKeyboardDragLifecycle()
    {
        var manager = KeyboardDragManager.WithDefaults();

        // 1. Start drag
        Assert.True(manager.StartDrag(new WidgetId(1), DragPayload.Text("dragged_item")));
        Assert.Equal(KeyboardDragMode.Holding, manager.Mode());

        var targets = new List<DropTargetInfo>
        {
            new DropTargetInfo(new WidgetId(10), "Target A", new Rect(0, 0, 10, 5)),
            new DropTargetInfo(new WidgetId(11), "Target B", new Rect(0, 10, 10, 5)),
        };

        // 2. Navigate to target
        _ = manager.NavigateTargets(Direction.Down, targets);
        Assert.Equal(KeyboardDragMode.Navigating, manager.Mode());

        // 3. Navigate to next target
        _ = manager.NavigateTargets(Direction.Down, targets);

        // 4. Complete drop
        var result = manager.DropOnTarget(targets);
        Assert.NotNull(result);
        Assert.Equal("dragged_item", result!.Payload.AsText());
        Assert.Equal(new WidgetId(11), result.TargetId);
        Assert.Equal(1, result.TargetIndex);

        // 5. Manager is now inactive
        Assert.False(manager.IsActive());
    }

    [Fact]
    public void ManagerDropOnInvalidatedTargetKeepsDragActive()
    {
        var manager = KeyboardDragManager.WithDefaults();
        Assert.True(manager.StartDrag(new WidgetId(1), new DragPayload("text/plain", [])));

        var targets = new List<DropTargetInfo>
        {
            new DropTargetInfo(new WidgetId(10), "Text Target", new Rect(0, 0, 10, 5))
                .WithAcceptedTypes(new List<string> { "text/plain" }),
        };
        _ = manager.NavigateTargets(Direction.Down, targets);
        Assert.Equal(KeyboardDragMode.Navigating, manager.Mode());

        var invalidatedTargets = new List<DropTargetInfo>
        {
            new DropTargetInfo(new WidgetId(10), "Image Target", new Rect(0, 0, 10, 5))
                .WithAcceptedTypes(new List<string> { "image/*" }),
        };
        var result = manager.DropOnTarget(invalidatedTargets);

        Assert.Null(result);
        Assert.True(manager.IsActive());
        Assert.Equal(KeyboardDragMode.Holding, manager.Mode());
        Assert.Null(manager.State()!.SelectedTargetIndex);
    }
}
